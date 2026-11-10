using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// The headless runner's stand-in for a player. Each world sets one of the three ration policies on
/// its first tick, chosen by seed, so a soak exercises every policy's stories (the demo's meal theft
/// and broken promises depend on it). With --plan it also marks out building plans (ADR-0027): three beds
/// in the shelter, and with "camp" then a hut with a door, a roof and two beds, each on the first morning
/// (from day 1 and day 2) that the camp has outgrown its beds. Plans go through Building.Check against the
/// world as it is when they are placed, so they are valid on every map. `run` and `soak` share this, so a
/// seed's log is the same either way.
/// </summary>
internal static class Autoplay
{
    private static readonly RationPolicy[] Policies = { RationPolicy.Hungriest, RationPolicy.PromisesFirst, RationPolicy.SelfFirst };

    /// <summary>The plans the stand-in player can make; "mixed" picks one of the first three by seed.</summary>
    internal static readonly IReadOnlyList<string> Plans = new[] { "none", "beds", "camp", "mixed" };

    internal static RationPolicy PolicyFor(ulong seed) => Policies[(int)(seed % (ulong)Policies.Length)];

    /// <summary>The plan a world gets: "mixed" chooses by (seed / 3) % 3, so plans and ration policies vary independently.</summary>
    internal static string PlanFor(string plan, ulong seed) => plan == "mixed" ? Plans[(int)(seed / 3 % 3)] : plan;

    internal static void Begin(Simulation sim) => sim.Enqueue(new SetRationPolicy(sim.World.Tick, PolicyFor(sim.World.Seed)));

    /// <summary>
    /// Runs the world for this many ticks an hour at a time, so the stand-in player can act on the hour its plans
    /// are due, against the world as it is then. A world's log stays a function of its seed and plan.
    /// </summary>
    internal static void Run(Simulation sim, long ticks, string plan)
    {
        int hour = sim.World.Content.World.TicksPerHour;
        long end = sim.World.Tick + ticks;
        while (sim.World.Tick < end)
        {
            Hour(sim, plan);
            sim.Advance(Math.Min(hour, end - sim.World.Tick));
        }
    }

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
        Point stock = m.Stockpile.Center;
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
}
