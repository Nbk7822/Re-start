using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// The headless runner's stand-in for a player. Each world sets one of the three ration policies on
/// its first tick, chosen by seed, so a soak exercises every policy's stories (the demo's meal theft
/// and broken promises depend on it), and then lays out the camp's zones by the chosen plan (stockpiles
/// and the home area, ADR-0028) and its days by the chosen schedule plan (ADR-0029). `run` and `soak` share this, so a seed's log is the
/// same either way.
/// </summary>
internal static class Autoplay
{
    private static readonly RationPolicy[] Policies = { RationPolicy.Hungriest, RationPolicy.PromisesFirst, RationPolicy.SelfFirst };

    /// <summary>The zone plans `--zones` accepts (design 9.1): no zones, dry stores, a home area round the camp clearing, one a row short of the watch post, dry stores and a home area, or a mix by seed.</summary>
    internal static readonly IReadOnlyList<string> ZonePlans = new[] { "none", "dry", "home", "short", "both", "mixed" };

    /// <summary>
    /// What `--zones mixed` picks, by (seed / 9) % 5, so it varies apart from the ration policy (seed % 3)
    /// and the build and schedule plans ((seed / 3) % 3, (seed / 36) % 3).
    /// </summary>
    private static readonly string[] Mixed = { "none", "dry", "home", "short", "both" };

    internal static RationPolicy PolicyFor(ulong seed) => Policies[(int)(seed % (ulong)Policies.Length)];

    /// <summary>The plan a world runs under: mixed picks one of none, dry, home, short and both by seed.</summary>
    internal static string ZonePlanFor(string plan, ulong seed) => plan != "mixed" ? plan : Mixed[(int)(seed / 9 % (ulong)Mixed.Length)];

    /// <summary>The schedule plans `--days` accepts (ADR-0029): content as shipped, no watch, or Mira on nights; mixed picks by seed.</summary>
    internal static readonly IReadOnlyList<string> DaysPlans = new[] { "default", "nowatch", "nightwatcher", "mixed" };

    /// <summary>
    /// The schedule plan a world runs under: mixed picks by (seed / 36) % 3, so it varies apart from the ration policy
    /// (seed % 3), build's plans ((seed / 3) % 3) and zones' ((seed / 9) % 5).
    /// </summary>
    internal static string DaysPlanFor(string plan, ulong seed) =>
        plan != "mixed" ? plan : new[] { "default", "nowatch", "nightwatcher" }[(int)(seed / 36 % 3)];

    internal static void Begin(Simulation sim, string zones = "none", string days = "default")
    {
        sim.Enqueue(new SetRationPolicy(sim.World.Tick, PolicyFor(sim.World.Seed)));
        string plan = ZonePlanFor(zones, sim.World.Seed);
        if (plan is "dry" or "both") Dry(sim);
        if (plan is "home" or "both") Home(sim, shortOfThePost: false);
        if (plan == "short") Home(sim, shortOfThePost: true);
        switch (DaysPlanFor(days, sim.World.Seed))
        {
            case "nowatch":
                sim.Enqueue(new SetWatchHours(sim.World.Tick, 0, 0));
                break;
            case "nightwatcher":
                if (sim.World.Characters.Find(c => c.CastKey == "mira") is { } mira) sim.Enqueue(new SetPersonDay(sim.World.Tick, mira.Id, NightWatcher()));
                break;
        }
    }

    /// <summary>The nightwatcher's own day (design 8.1): sleep 07:00-15:00, work 15:00-19:00 and 22:00-07:00; the camp's free time between.</summary>
    internal static DayBlock?[] NightWatcher()
    {
        var day = new DayBlock?[24];
        for (int h = 0; h < 24; h++)
            day[h] = h is >= 7 and < 15 ? DayBlock.Sleep : h is >= 15 and < 19 || h >= 22 || h < 7 ? DayBlock.Work : null;
        return day;
    }

    /// <summary>
    /// The home plans (design 9.1): the camp clearing (the 22 × 19 rectangle world generation clears round the shelter,
    /// which holds the watch post) as the home area; short of the post, the same without the rows from the watch post's
    /// row down, so the post lies just outside (story 3's setup, the only plan that exercises the watch's skip). Tiles go
    /// through the commands' own check.
    /// </summary>
    private static void Home(Simulation sim, bool shortOfThePost)
    {
        World w = sim.World;
        Rect clearing = WorldGen.ClearingOf(w.Map.Shelter);
        if (shortOfThePost) clearing = new Rect(clearing.X, clearing.Y, clearing.W, Math.Max(1, w.Map.WatchPost.Y - clearing.Y));
        var area = new List<Point>();
        for (int y = clearing.Y; y < clearing.Y + clearing.H; y++)
            for (int x = clearing.X; x < clearing.X + clearing.W; x++) area.Add(new Point(x, y));
        var ok = new List<Point>();
        foreach (ZoneTileState t in StateApi.CanZone(w, "home", area))
            if (t.Ok) ok.Add(new Point(t.X, t.Y));
        sim.Enqueue(new MarkZone(w.Tick, 0, "home", Zones.Canonical(ok)));
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
