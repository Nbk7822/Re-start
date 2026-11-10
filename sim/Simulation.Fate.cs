using System.IO.Compression;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Modding;
using Return.Sim.Snapshots;
using Return.Sim.Systems;

namespace Return.Sim;

/// <summary>What a return did: the world that was lost, the world gone back to, and the lost future's index in its run memory.</summary>
public sealed record FateReturn(World Lost, World Restored, int Future);

/// <summary>
/// Return by Death (the player design §5.3, ADR-0030): fate knots, their archive, and the return that swaps one world
/// for another. The archive lives here, outside World: inside it, every snapshot would contain the earlier ones.
/// Everything runs between ticks, after the step's systems and hooks, never inside a system.
/// </summary>
public sealed partial class Simulation
{
    private static readonly byte[] BundleMagic = "RTN3"u8.ToArray();

    /// <summary>Each knot's gzipped world snapshot, in the order they were tied.</summary>
    private readonly List<(int Id, byte[] Snapshot)> _archive = new();

    /// <summary>How many log events there were when the current step began (the step's own events follow).</summary>
    private int _stepLogStart;
    private bool _endNotified;

    /// <summary>Raised after a return has swapped the world, once the return's own events are logged and hooks have seen them.</summary>
    public event Action<FateReturn>? Returned;

    /// <summary>Whether the director ties fate knots; off only to prove tying one changes nothing but the run memory.</summary>
    internal bool TieKnots { get; set; } = true;

    /// <summary>The ids of the knots in the archive, oldest first.</summary>
    internal IReadOnlyList<int> ArchivedKnots => _archive.ConvertAll(a => a.Id);

    /// <summary>The gzipped bytes the archive holds for a knot (tests read them to price a knot).</summary>
    internal byte[]? ArchivedSnapshot(int id) => _archive.Find(a => a.Id == id).Snapshot;

    /// <summary>A knot's world as the archive holds it (tests compare a return against it).</summary>
    internal World? KnotWorld(int id) =>
        _archive.Find(a => a.Id == id).Snapshot is { } snap ? SnapshotSerializer.Load(Unzip(snap), World.Content) : null;

    /// <summary>
    /// Between ticks: the end of your lifetime, then a return if you died, then a knot if the director ties one.
    /// </summary>
    private void EndOfTick()
    {
        World w = World;
        if (w.Fate.Ended)
        {
            NotifyLifeEnded();
            return;
        }
        if (w.Fate.PendingDeath != 0)
        {
            Return();
            return;
        }
        if (!TieKnots) return;
        (bool due, KnotReason reason, long trigger, string key) = Fate.KnotDue(w, _stepLogStart);
        if (due) TieKnot(w, reason, trigger, key);
    }

    private void TieKnot(World w, KnotReason reason, long trigger, string key)
    {
        FateBook f = w.Fate;
        var knot = new KnotRecord { Id = f.NextKnotId++, Tick = w.Tick, Reason = reason, TriggerEvent = trigger, Trigger = key, Timeline = f.Timeline };
        f.Knots.Add(knot);
        _archive.Add((knot.Id, Zip(SnapshotSerializer.Save(w))));
    }

    /// <summary>
    /// You died: go back to a knot (§5.3). Pick it with the dying world's Fate stream; freeze what came after it into the
    /// Chronicle of Lost Futures and find the memories it leaves; carry the run memory forward (dropping knots tied after
    /// the chosen one); restore the knot and give it that memory, your learned customs and whom you met; reseed the Dice
    /// and re-draw the arrivals still to come; then swap worlds, log the return and what you remember, and let hooks,
    /// gifts and the view know.
    /// </summary>
    private void Return()
    {
        World dying = World;
        FateBook f = dying.Fate;
        ContentDb content = dying.Content;
        long deathEvent = f.PendingDeath;
        long deathTick = dying.Log.Get(deathEvent)?.Tick ?? dying.Tick;
        var eligible = f.Knots.FindAll(k => _archive.Exists(a => a.Id == k.Id));
        KnotRecord? knot = Fate.Pick(eligible, deathTick, content.Rebirth.Return, content.World, dying.Rng.Dice[DiceSystem.Fate]);
        // None far enough back (only a world restored without its knots, dying soon after tying a fresh one): the oldest there is.
        knot ??= eligible.Count > 0 ? eligible[0] : null;
        if (knot == null)
        {
            // No knot at all to go back to (dying before the first tick ends): this death stands.
            f.Ended = true;
            f.EndKind = "no_return";
            f.EndTick = dying.Tick;
            f.PendingDeath = 0;
            NotifyLifeEnded();
            return;
        }

        World restored = SnapshotSerializer.Load(Unzip(_archive.Find(a => a.Id == knot.Id).Snapshot), content);
        int cut = restored.Log.All.Count;
        List<Memories.Found> found = Memories.Find(dying, restored, cut);
        var quoted = new List<long>(found.Count);
        foreach (Memories.Found m in found) quoted.Add(m.Event);
        (LostFuture future, Dictionary<long, int> lineOf) = LostFutures.Build(dying, cut, knot.Tick, deathEvent, quoted);

        int futureIndex = f.LostFutures.Count;
        f.LostFutures.Add(future);
        f.Timelines.Add(new TimelineRecord
        {
            Timeline = f.Timeline, StartTick = f.TimelineStart, DeathTick = deathTick, Forced = f.ForcedDeathTick >= 0, Commands = f.Commands,
        });
        foreach (Memories.Found m in found)
        {
            f.Memories.RemoveAll(x => x.PersonId == m.PersonId);
            f.Memories.Add(new Memory { PersonId = m.PersonId, Kind = m.Kind, Opinion = m.Opinion, Grudge = m.Grudge, Future = futureIndex, Line = lineOf[m.Event] });
        }
        Gifts.KeepEchoes(dying, f);
        Character dyingYou = dying.Get(dying.PlayerId);
        f.Met = Union(f.Met, dyingYou.Met);
        f.Knots.RemoveAll(k => k.Tick > knot.Tick);
        _archive.RemoveAll(a => !f.Knots.Exists(k => k.Id == a.Id));
        knot.Uses++;
        f.Timeline++;
        f.Returns++;
        f.TimelineStart = restored.Tick;
        f.Commands = new List<TimedCommand>();
        f.ForcedDeathTick = -1;
        f.PendingDeath = 0;

        restored.Fate = f;
        Character you = restored.Get(restored.PlayerId);
        you.LearnedCustoms = UnionOrdinal(you.LearnedCustoms, dyingYou.LearnedCustoms); // culture-faith §8: what you learned, you keep
        you.Met = Union(you.Met, f.Met);
        restored.Rng.Dice.Reseed(Mix64.Combine((ulong)f.Timeline, (ulong)deathTick));
        WorldGen.RedrawArrivals(restored);
        restored.Gift.ReturnedTick = restored.Tick;
        restored.Gift.Familiar.Clear();
        restored.Gift.FamiliarEvents.Clear();
        restored.Gift.FamiliarFriends.Clear();

        restored.Hooks.AddRange(dying.Hooks);
        World = restored;
        _delivered = restored.Log.All.Count;
        SimEvent back = restored.Log.Emit(restored, SystemTag.Fate, Fate.ReturnedKey, new[] { you.Id }, new[] { knot.TriggerEvent }, null,
            EventArg.Char("a", you.Id), EventArg.Num("future", futureIndex));
        restored.Gift.ReturnedEvent = back.Id;
        Memories.Apply(restored, back.Id);
        Gifts.OnReturned(restored, back);
        DeliverToHooks();
        foreach (IModHook hook in restored.Hooks)
            if (hook is IFateHook fateHook) fateHook.OnReturned(restored, future);
        Returned?.Invoke(new FateReturn(dying, restored, futureIndex));
    }

    private void NotifyLifeEnded()
    {
        if (_endNotified) return;
        _endNotified = true;
        DeliverToHooks();
        foreach (IModHook hook in World.Hooks)
            if (hook is IFateHook fateHook) fateHook.OnLifeEnded(World);
    }

    /// <summary>Steps until the world reaches <paramref name="tick"/> (a return sends it back, so this can take more steps than ticks), the run ends, or <paramref name="maxSteps"/> steps have run. True if it reached the tick.</summary>
    public bool AdvanceTo(long tick, long maxSteps = long.MaxValue)
    {
        for (long steps = 0; World.Tick < tick && !World.Fate.Ended; steps++)
        {
            if (steps >= maxSteps) return false;
            Step();
        }
        return World.Tick >= tick;
    }

    /// <summary>Kills you now, between ticks, so the next step ends in a return (the soak's --fates, and tests). False if you cannot die now.</summary>
    internal bool ForceDeath() => Fate.ForceDeath(World);

    /// <summary>
    /// Re-runs a whole life (§10): the same seed and the same commands in each timeline give the same run. Each finished
    /// timeline's commands are enqueued when the world reaches the tick they were given at, its forced death (if any)
    /// is forced again, and it runs until its return; the last timeline runs to <paramref name="untilTick"/>. A finished
    /// timeline that does not end in a death at its recorded tick has diverged: that throws, rather than running on.
    /// </summary>
    public static Simulation Replay(ContentDb content, ulong seed, FateBook life, long untilTick, RebirthChoice? choice = null)
    {
        Simulation sim = Create(content, seed, choice);
        foreach (TimelineRecord t in life.Timelines)
        {
            sim.RunTimeline(t.Commands, t.Forced ? t.DeathTick : -1, long.MaxValue, t.DeathTick);
            FateBook now = sim.World.Fate;
            if (now.Timeline != t.Timeline + 1 || now.LostFutures[^1].DeathTick != t.DeathTick)
                throw new InvalidOperationException($"Replay diverged: timeline {t.Timeline} did not end in a death at tick {t.DeathTick}.");
        }
        sim.RunTimeline(life.Commands, life.ForcedDeathTick, untilTick, long.MaxValue);
        return sim;
    }

    private void RunTimeline(List<TimedCommand> commands, long forcedAt, long untilTick, long deathTick)
    {
        int timeline = World.Fate.Timeline, next = 0;
        while (World.Fate.Timeline == timeline && !World.Fate.Ended && World.Tick < untilTick)
        {
            if (World.Tick > deathTick)
                throw new InvalidOperationException($"Replay diverged: timeline {timeline} reached tick {World.Tick} without the death it had at tick {deathTick}.");
            while (next < commands.Count && commands[next].EnqueuedAt <= World.Tick) Enqueue(commands[next++].Command);
            if (forcedAt >= 0 && World.Tick == forcedAt) ForceDeath();
            Step();
        }
    }

    /// <summary>
    /// The whole simulation as bytes: the world snapshot and the knot archive, so a restored simulation can still go
    /// back to the knots its run memory lists. <see cref="StateHash"/> hashes the world alone.
    /// </summary>
    public byte[] Snapshot()
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(BundleMagic);
            byte[] world = SnapshotSerializer.Save(World);
            bw.Write(world.Length);
            bw.Write(world);
            bw.Write(_archive.Count);
            foreach ((int id, byte[] snap) in _archive)
            {
                bw.Write(id);
                bw.Write(snap.Length);
                bw.Write(snap);
            }
        }
        return ms.ToArray();
    }

    /// <summary>A simulation from <see cref="Snapshot"/>'s bytes, or from a bare world snapshot (which has no knots to go back to).</summary>
    public static Simulation Restore(ContentDb content, byte[] snapshot)
    {
        if (!snapshot.AsSpan().StartsWith(BundleMagic))
        {
            World bare = SnapshotSerializer.Load(snapshot, content);
            bare.Fate.Knots.Clear(); // their snapshots are not here; the director ties a fresh knot at the end of the next safe tick
            return new Simulation(bare);
        }
        using var ms = new MemoryStream(snapshot);
        using var br = new BinaryReader(ms);
        br.ReadBytes(BundleMagic.Length);
        World w = SnapshotSerializer.Load(br.ReadBytes(br.ReadInt32()), content);
        var sim = new Simulation(w);
        int knots = br.ReadInt32();
        for (int i = 0; i < knots; i++)
        {
            int id = br.ReadInt32();
            sim._archive.Add((id, br.ReadBytes(br.ReadInt32())));
        }
        return sim;
    }

    private static byte[] Zip(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest)) gz.Write(data);
        return ms.ToArray();
    }

    private static byte[] Unzip(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return output.ToArray();
    }

    private static List<int> Union(List<int> a, List<int> b)
    {
        var set = new SortedSet<int>(a);
        set.UnionWith(b);
        return new List<int>(set);
    }

    private static List<string> UnionOrdinal(List<string> a, List<string> b)
    {
        var set = new SortedSet<string>(a, StringComparer.Ordinal);
        set.UnionWith(b);
        return new List<string>(set);
    }
}
