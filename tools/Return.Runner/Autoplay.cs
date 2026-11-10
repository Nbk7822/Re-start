using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// The headless runner's stand-in for a player. Each world sets one of the three ration policies on
/// its first tick, chosen by seed, so a soak exercises every policy's stories (the demo's meal theft
/// and broken promises depend on it). `run` and `soak` share this, so a seed's log is the same either way.
/// </summary>
internal static class Autoplay
{
    private static readonly RationPolicy[] Policies = { RationPolicy.Hungriest, RationPolicy.PromisesFirst, RationPolicy.SelfFirst };

    internal static RationPolicy PolicyFor(ulong seed) => Policies[(int)(seed % (ulong)Policies.Length)];

    internal static void Begin(Simulation sim) => sim.Enqueue(new SetRationPolicy(sim.World.Tick, PolicyFor(sim.World.Seed)));

    /// <summary>
    /// Before each step: once you know your gift, the stand-in player gives its standing order, picked by seed so a soak
    /// exercises every choice (the satchel: keep, share, empty; ADR-0030). The default needs no order; after a return to
    /// before the order, it is given again, as a player who remembers would.
    /// </summary>
    internal static void Tick(Simulation sim)
    {
        World w = sim.World;
        if (!w.Fate.GiftAwakened || Gifts.Held(w)?.Order is not { } order) return;
        string choice = OrderFor(w.Seed, order);
        if (choice == order.Default || Gifts.Order(w, order) == choice) return;
        foreach (Command c in w.PendingCommands)
            if (c is SetStandingOrder s && s.OrderId == order.Id) return;
        sim.Enqueue(new SetStandingOrder(w.Tick, order.Id, choice));
    }

    /// <summary>The order the stand-in player gives for a seed: the seed's third, so it does not move in step with the ration policy.</summary>
    internal static string OrderFor(ulong seed, GiftOrderDef order) => order.Choices[(int)(seed / 3 % (ulong)order.Choices.Count)];
}
