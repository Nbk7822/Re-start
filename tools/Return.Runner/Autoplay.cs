using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// The headless runner's stand-in for a player. Each world sets one of the three ration policies on
/// its first tick, chosen by seed, so a soak exercises every policy's stories (the demo's meal theft
/// and broken promises depend on it). It then lays out the camp's zones by the chosen zone plan (stockpiles
/// and the home area, ADR-0028) and its days by the chosen schedule plan (ADR-0029). With --plan it also
/// marks out building plans (ADR-0027): three beds in the shelter, and with "camp" then a hut with a door,
/// a roof and two beds, each on the first morning (from day 1 and day 2) that the camp has outgrown its
/// beds. Plans go through Building.Check against the world as it is when they are placed, so they are
/// valid on every map. `run` and `soak` share this, so a seed's log is the same either way.
/// </summary>
internal static class Autoplay
{
    private static readonly RationPolicy[] Policies = { RationPolicy.Hungriest, RationPolicy.PromisesFirst, RationPolicy.SelfFirst };

    /// <summary>The plans the stand-in player can make; "mixed" picks one of the first three by seed.</summary>
    internal static readonly IReadOnlyList<string> Plans = new[] { "none", "beds", "camp", "mixed" };

    internal static RationPolicy PolicyFor(ulong seed) => Policies[(int)(seed % (ulong)Policies.Length)];

    /// <summary>The plan a world gets: "mixed" chooses by (seed / 3) % 3, so plans and ration policies vary independently.</summary>
    internal static string PlanFor(string plan, ulong seed) => plan == "mixed" ? Plans[(int)(seed / 3 % 3)] : plan;

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

    /// <summary>
    /// Runs the world for this many more ticks under a building plan, through <see cref="StoryLog.Run"/>, the one loop
    /// that steps a world (a return can send it back, ADR-0030), so <see cref="Hour"/> acts on the hour its plans are
    /// due against the world as it is then (ADR-0027). A world's log stays a function of its seed and plan.
    /// </summary>
    internal static void Run(Simulation sim, long ticks, string plan) => StoryLog.Run(sim, sim.World.Tick + ticks, RunSetup.Plain, plan);

    /// <summary>
    /// At 08:00 each morning, marks out what is due. The design set the beds for day 1 and the hut for day 2; the
    /// stand-in instead waits, from those mornings on, until the camp has outgrown its beds (ADR-0027: a deviation
    /// for the founder). On fixed days the camp was built before anyone who would care had arrived.
    /// </summary>
    internal static void Hour(Simulation sim, string plan)
    {
        if (plan is not ("beds" or "camp")) return;
        World w = sim.World;
        WorldDef def = w.Content.World;
        long tick = w.Tick;
        if (tick % def.TicksPerDay != 8L * def.TicksPerHour || !Outgrown(w)) return;
        if (!HasPlanned(w))
        {
            PlaceShelterBeds(sim);
            return;
        }
        if (plan == "camp" && tick >= def.TicksPerDay + 8L * def.TicksPerHour && !HutPlanned(w)) PlaceHut(sim);
    }

    /// <summary>Has the stand-in marked out anything yet (the shelter beds come first)?</summary>
    private static bool HasPlanned(World w) =>
        w.Log.All.Exists(e => e.Key.StartsWith("event.world.planned", StringComparison.Ordinal) || e.Key.StartsWith("event.world.plan_failed", StringComparison.Ordinal));

    private static bool HutPlanned(World w) =>
        w.Blueprints.Exists(b => b.DefId == "wall") || w.Map.Structures.Exists(s => s.DefId == "wall")
        || w.Log.All.Exists(e => e.Key.StartsWith("event.world.plan_failed", StringComparison.Ordinal) && e.Args.Exists(a => a.Value == "k:building.wall.a"));

    /// <summary>The camp has outgrown its beds: someone in it who sleeps has no bed, and no bed is free.</summary>
    private static bool Outgrown(World w)
    {
        if (w.Map.Beds.Exists(b => b.OwnerId == 0)) return false;
        foreach (Character c in w.Members())
            if (c.Named && Places.Tires(w, c) && Building.OwnedBed(w, c.Id) == null) return true;
        return false;
    }

    /// <summary>Three beds on free shelter floor, keeping clear the tile just inside each door, first in (y, x) order.</summary>
    private static void PlaceShelterBeds(Simulation sim)
    {
        World w = sim.World;
        Rect s = w.Map.Shelter;
        var candidates = new List<Rect>();
        for (int y = s.Y + 1; y < s.Y + s.H - 1; y++)
            for (int x = s.X + 1; x < s.X + s.W - 1; x++)
                if (!InsideADoor(w, s, new Point(x, y))) candidates.Add(new Rect(x, y, 1, 1));
        var beds = new List<Rect>();
        foreach (PieceVerdict v in Building.Check(w, "bed", candidates))
            if (v.Ok && beds.Count < 3) beds.Add(v.Area);
        if (beds.Count > 0) sim.Enqueue(new PlaceBlueprint(w.Tick, "bed", beds));
    }

    private static bool InsideADoor(World w, Rect s, Point p)
    {
        foreach ((int dx, int dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
        {
            var q = new Point(p.X + dx, p.Y + dy);
            if (!s.Contains(q) || (q.X != s.X && q.X != s.X + s.W - 1 && q.Y != s.Y && q.Y != s.Y + s.H - 1)) continue;
            if (w.Map.InBounds(q) && w.Map.At(q) == TerrainKind.Door) return true;
        }
        return false;
    }

    /// <summary>
    /// A 5×4 ring of walls with its door facing the hearth, a roof over its inside and two beds in it, at the nearest
    /// valid site to the stockpile (rect centres, Chebyshev; ties by y, then x) with open ground all round it.
    /// </summary>
    private static void PlaceHut(Simulation sim)
    {
        World w = sim.World;
        Hut? hut = FindHut(w);
        if (hut == null) return;
        long t = w.Tick;
        sim.Enqueue(new PlaceBlueprint(t, "wall", hut.Walls));
        sim.Enqueue(new PlaceBlueprint(t, "door", new[] { hut.Door }));
        sim.Enqueue(new PlaceBlueprint(t, "roof", new[] { hut.Roof }));
        sim.Enqueue(new PlaceBlueprint(t, "bed", hut.Beds));
    }

    internal sealed record Hut(Rect Ring, List<Rect> Walls, Rect Door, Rect Roof, List<Rect> Beds);

    internal static Hut? FindHut(World w)
    {
        const int width = 5, height = 4;
        Map m = w.Map;
        Point stock = m.StartStockpile.Center;
        var sites = new List<(int Dist, int Y, int X)>();
        for (int y = 1; y + height < m.Height - 1; y++)
            for (int x = 1; x + width < m.Width - 1; x++)
                sites.Add((new Point(x + width / 2, y + height / 2).ChebyshevTo(stock), y, x));
        sites.Sort();
        foreach ((int _, int y, int x) in sites)
        {
            Hut? hut = HutAt(w, new Rect(x, y, width, height));
            if (hut != null) return hut;
        }
        return null;
    }

    private static Hut? HutAt(World w, Rect ring)
    {
        Map m = w.Map;
        for (int y = ring.Y - 1; y <= ring.Y + ring.H; y++)
            for (int x = ring.X - 1; x <= ring.X + ring.W; x++)
            {
                var p = new Point(x, y);
                if (!ring.Contains(p) && !m.Passable(p)) return null; // a way round it on every side
            }
        Point door = DoorFacingHearth(m, ring);
        var walls = new List<Rect>();
        for (int y = ring.Y; y < ring.Y + ring.H; y++)
            for (int x = ring.X; x < ring.X + ring.W; x++)
            {
                bool edge = x == ring.X || x == ring.X + ring.W - 1 || y == ring.Y || y == ring.Y + ring.H - 1;
                if (edge && new Point(x, y) != door) walls.Add(new Rect(x, y, 1, 1));
            }
        var inside = new Rect(ring.X + 1, ring.Y + 1, ring.W - 2, ring.H - 2);
        var planned = walls.ConvertAll(r => new PlannedPiece("wall", r));
        if (Building.Check(w, "wall", walls).Any(v => !v.Ok)) return null;
        var doorPiece = new Rect(door.X, door.Y, 1, 1);
        if (!Building.Check(w, "door", new[] { doorPiece }, planned)[0].Ok) return null;
        planned.Add(new PlannedPiece("door", doorPiece));
        if (!Building.Check(w, "roof", new[] { inside }, planned)[0].Ok) return null;
        planned.Add(new PlannedPiece("roof", inside));
        // The two beds go where they keep out of the doorway: the inside tiles farthest from the door.
        var tiles = new List<Point>();
        for (int y = inside.Y; y < inside.Y + inside.H; y++)
            for (int x = inside.X; x < inside.X + inside.W; x++) tiles.Add(new Point(x, y));
        tiles.Sort((a, b) =>
        {
            int byDistance = b.ChebyshevTo(door).CompareTo(a.ChebyshevTo(door));
            return byDistance != 0 ? byDistance : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
        });
        var beds = new List<Rect> { new(tiles[0].X, tiles[0].Y, 1, 1), new(tiles[1].X, tiles[1].Y, 1, 1) };
        if (Building.Check(w, "bed", beds, planned).Any(v => !v.Ok)) return null;
        return new Hut(ring, walls, doorPiece, inside, beds);
    }

    /// <summary>The middle of the ring's side that faces the hearth (west or east when the hearth is further across than down).</summary>
    private static Point DoorFacingHearth(Map m, Rect ring)
    {
        int dx = m.Hearth.X - (ring.X + ring.W / 2), dy = m.Hearth.Y - (ring.Y + ring.H / 2);
        if (Math.Abs(dx) > Math.Abs(dy))
            return new Point(dx < 0 ? ring.X : ring.X + ring.W - 1, ring.Y + (ring.H - 1) / 2);
        return new Point(ring.X + ring.W / 2, dy < 0 ? ring.Y : ring.Y + ring.H - 1);
    }

    /// <summary>The zone plans `--zones` accepts (design 9.1): no zones, dry stores, a home area round the camp clearing, one a row short of the watch post, dry stores and a home area, or a mix by seed.</summary>
    internal static readonly IReadOnlyList<string> ZonePlans = new[] { "none", "dry", "home", "short", "both", "mixed" };

    /// <summary>
    /// What `--zones mixed` picks, by (seed / 9) % 5, so it varies apart from the ration policy (seed % 3)
    /// and the build and schedule plans ((seed / 3) % 3, (seed / 36) % 3).
    /// </summary>
    private static readonly string[] Mixed = { "none", "dry", "home", "short", "both" };

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

    /// <summary>
    /// The schedule plan a world actually runs under: the one <see cref="DaysPlanFor"/> deals it, except that a player who knows
    /// they are an undead bound to the watch never takes the watch away. With none, their purpose is gone, they come to rest,
    /// and every return replays it, a death loop (seeds 40 and 65 of the nightly soak once Schedule met the return), so the
    /// stand-in keeps the watch then, and the world runs under the shipped hours: "default", which the soak tallies it under.
    /// </summary>
    internal static string DaysPlanRun(World w, string days)
    {
        string plan = DaysPlanFor(days, w.Seed);
        return plan == "nowatch" && BoundToTheWatch(w) ? "default" : plan;
    }

    /// <summary>The stand-in's first tick: the ration policy, the zone plan and the days plan. Returns the days plan it ran (<see cref="DaysPlanRun"/>).</summary>
    internal static string Begin(Simulation sim, string zones = "none", string days = "default")
    {
        sim.Enqueue(new SetRationPolicy(sim.World.Tick, PolicyFor(sim.World.Seed)));
        string plan = ZonePlanFor(zones, sim.World.Seed);
        if (plan is "dry" or "both") Dry(sim);
        if (plan is "home" or "both") Home(sim, shortOfThePost: false);
        if (plan == "short") Home(sim, shortOfThePost: true);
        string ran = DaysPlanRun(sim.World, days);
        switch (ran)
        {
            case "nowatch":
                sim.Enqueue(new SetWatchHours(sim.World.Tick, 0, 0));
                break;
            case "nightwatcher":
                if (sim.World.Characters.Find(c => c.CastKey == "mira") is { } mira) sim.Enqueue(new SetPersonDay(sim.World.Tick, mira.Id, NightWatcher()));
                break;
        }
        return ran;
    }

    /// <summary>You are an undead whose purpose is keeping the watch (ADR-0022).</summary>
    private static bool BoundToTheWatch(World w) =>
        w.Find(w.PlayerId) is { } you && NatureNeeds.Get(you, Purpose.NeedId) is { BondWork: "keep_watch" };

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
