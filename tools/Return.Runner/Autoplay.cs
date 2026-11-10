using Return.Sim;

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

    /// <summary>Before each step: anything the stand-in player decides as the run unfolds.</summary>
    internal static void Tick(Simulation sim)
    {
    }
}
