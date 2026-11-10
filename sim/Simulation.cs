using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Modding;
using Return.Sim.Snapshots;
using Return.Sim.Systems;

namespace Return.Sim;

/// <summary>
/// The facade every consumer uses (runner, tests, the game view). One <see cref="Step"/> is one tick,
/// run in a fixed system order. Same seed + same commands = same world. When you die, the end of the step
/// sends the world back to a fate knot and <see cref="World"/> becomes the restored world (ADR-0030); read it
/// afresh after every step.
/// </summary>
public sealed partial class Simulation
{
    private Simulation(World world)
    {
        World = world;
        _delivered = world.Log.All.Count;
    }

    /// <summary>How many log events hooks have been shown; hooks see only events from after they could be added.</summary>
    private int _delivered;

    public World World { get; private set; }

    /// <summary>A new world for a seed; your people and gift are rolled unless <paramref name="choice"/> chooses them for testing.</summary>
    public static Simulation Create(ContentDb content, ulong seed, RebirthChoice? choice = null) => new(WorldGen.Create(content, seed, choice));

    public void AddHook(IModHook hook)
    {
        World.Hooks.Add(hook);
        hook.OnWorldCreated(World);
    }

    public void Enqueue(Command command)
    {
        if (command.Tick < World.Tick)
            throw new ArgumentException($"Command for tick {command.Tick} is in the past (now {World.Tick}).", nameof(command));
        Command stamped = command with { Seq = World.NextCommandSeq++ };
        World.Fate.Commands.Add(new TimedCommand { EnqueuedAt = World.Tick, Command = stamped }); // for Replay (ADR-0030)
        List<Command> pending = World.PendingCommands;
        int at = pending.Count;
        while (at > 0 && pending[at - 1].Tick > stamped.Tick) at--;
        pending.Insert(at, stamped);
    }

    public void Step()
    {
        World w = World;
        if (w.Fate.Ended) return; // old age or the soul's end: there is no return from this (ADR-0030)
        _stepLogStart = w.Log.All.Count;
        if (w.Tick == 0) Fate.Wake(w);
        WorldDef def = w.Content.World;
        ApplyDueCommands(w);
        TravelSystem.Step(w);
        CombatSystem.Step(w);
        WorkSystem.Step(w);
        if (w.Tick % def.SocialEveryTicks == 0) SocialSystem.Step(w);
        if (w.Tick % def.TicksPerHour == 0)
        {
            NeedsSystem.Hourly(w);
            Bargains.Hourly(w); // a bargain's price is paid at dawn, before food is shared out (ADR-0022)
            Gifts.Hourly(w); // you eat from your satchel before food is shared out (ADR-0030)
            Rations.Assign(w);
            MoodSystem.Hourly(w);
            SocialSystem.Hourly(w);
            Systems.Culture.CultureSystem.Hourly(w);
            IncidentSystem.Hourly(w);
            Growth.Hourly(w);
        }
        DeliverToHooks();
        w.Tick++;
        EndOfTick(); // the end of your lifetime, a return, or a fate knot (ADR-0030)
    }

    /// <summary>
    /// Hooks see each event at the end of the tick it happened in, in order, after every system has
    /// run, so a hook may change the world without disturbing a system mid-loop. Events a hook emits
    /// are delivered in the same pass.
    /// </summary>
    private void DeliverToHooks()
    {
        List<SimEvent> all = World.Log.All;
        if (World.Hooks.Count == 0) { _delivered = all.Count; return; }
        while (_delivered < all.Count)
        {
            SimEvent e = all[_delivered++];
            for (int i = 0; i < World.Hooks.Count; i++) World.Hooks[i].OnEvent(World, e);
        }
    }

    public void Advance(long ticks)
    {
        for (long i = 0; i < ticks; i++) Step();
    }

    public string StateHash() => SnapshotSerializer.Hash(World);

    private static void ApplyDueCommands(World w)
    {
        List<Command> pending = w.PendingCommands;
        while (pending.Count > 0 && pending[0].Tick <= w.Tick)
        {
            Command c = pending[0];
            pending.RemoveAt(0);
            c.Apply(w);
            w.CommandLog.Add(c);
        }
    }
}
