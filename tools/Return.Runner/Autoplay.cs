using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// The headless runner's stand-in for a player. Each world sets one of the three ration policies on
/// its first tick, chosen by seed, so a soak exercises every policy's stories (the demo's meal theft
/// and broken promises depend on it), and then lays out the camp's zones by the chosen plan
/// (ADR-0028). `run` and `soak` share this, so a seed's log is the same either way.
/// </summary>
internal static class Autoplay
{
    private static readonly RationPolicy[] Policies = { RationPolicy.Hungriest, RationPolicy.PromisesFirst, RationPolicy.SelfFirst };

    /// <summary>The zone plans `--zones` accepts. home, short and both come with the home area (zones step 3).</summary>
    internal static readonly IReadOnlyList<string> ZonePlans = new[] { "none", "dry", "mixed" };

    /// <summary>
    /// What `--zones mixed` picks, by (seed / 9) % 5, so it varies apart from the ration policy (seed % 3)
    /// and the build and schedule plans ((seed / 3) % 3, (seed / 36) % 3).
    /// </summary>
    private static readonly string[] Mixed = { "none", "dry", "home", "short", "both" };

    internal static RationPolicy PolicyFor(ulong seed) => Policies[(int)(seed % (ulong)Policies.Length)];

    /// <summary>
    /// The plan a world runs under. Until the home area exists, mixed's home and short worlds run with no
    /// zones and its both worlds run dry, so each seed keeps its plan's stockpile when step 3 lands.
    /// </summary>
    internal static string ZonePlanFor(string plan, ulong seed)
    {
        if (plan != "mixed") return plan;
        return Mixed[(int)(seed / 9 % (ulong)Mixed.Length)] switch
        {
            "home" or "short" => "none",
            "both" => "dry",
            string p => p,
        };
    }

    internal static void Begin(Simulation sim, string zones = "none")
    {
        sim.Enqueue(new SetRationPolicy(sim.World.Tick, PolicyFor(sim.World.Seed)));
        if (ZonePlanFor(zones, sim.World.Seed) == "dry") Dry(sim);
    }

    /// <summary>
    /// The dry plan: a stockpile on the covered ruins' floor, then the camp's first stockpile taken away,
    /// in that order, so the camp always has somewhere to keep its goods. Tiles go through the same check
    /// the commands make (StateApi.CanZone), so the plan is valid on every map.
    /// </summary>
    private static void Dry(Simulation sim)
    {
        World w = sim.World;
        Rect r = w.Map.CoveredRuins;
        var floor = new List<Point>();
        for (int y = r.Y; y < r.Y + r.H; y++)
            for (int x = r.X; x < r.X + r.W; x++) floor.Add(new Point(x, y));
        var ok = new List<Point>();
        foreach (ZoneTileState t in StateApi.CanZone(w, "stockpile", floor))
            if (t.Ok) ok.Add(new Point(t.X, t.Y));
        Zone? first = w.Map.Zones.Find(z => z.Kind == "stockpile");
        sim.Enqueue(new MarkZone(w.Tick, 0, "stockpile", Zones.Canonical(ok)));
        if (first != null) sim.Enqueue(new RemoveZone(w.Tick, first.Id));
    }
}
