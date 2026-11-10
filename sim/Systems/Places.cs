using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>Where things are: nearest bushes, trees, cover, beds and free tiles. Ties always break by id or (y, x).</summary>
public static class Places
{
    public const int BushMax = 6;
    public const int BushRegrowPerDay = 2;
    /// <summary>In winter a bush puts out one berry every this many days: a trickle, not a harvest.</summary>
    public const int WinterRegrowEveryDays = 2;

    public static bool Daylight(World w) => Calendar.IsDaylight(w.Tick, w.Content.World);

    public static bool BurnsInDaylight(World w, Character c) => w.Content.SpeciesById(c.SpeciesId).DaylightBurnPerHour > 0;

    /// <summary>Daylight, or the last hour before dawn, when those who burn should already be under cover.</summary>
    public static bool SunDanger(World w)
    {
        WorldDef def = w.Content.World;
        int h = Calendar.Hour(w.Tick, def);
        return h >= def.DawnHour - 1 && h < def.DuskHour;
    }

    /// <summary>Bushes regrow in every season but the last (winter), so the year has a lean stretch (ADR-0013).</summary>
    public static bool GrowingSeason(World w)
    {
        WorldDef def = w.Content.World;
        return Calendar.Season(w.Tick, def) != def.SeasonsPerYear - 1;
    }

    /// <summary>The passable neighbour of target closest to from (Chebyshev), or Point.None.</summary>
    public static Point Adjacent(World w, Point target, Point from)
    {
        Point best = Point.None;
        int bestDist = int.MaxValue;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var p = new Point(target.X + dx, target.Y + dy);
                if (!w.Map.Passable(p)) continue;
                int d = p.ChebyshevTo(from);
                if (d < bestDist) { best = p; bestDist = d; }
            }
        return best;
    }

    /// <summary>The nearest bush with berries nobody else is picking (only where <paramref name="where"/> allows, if given: inside the home area after dark).</summary>
    public static Bush? BushWithBerries(World w, Point from, Func<Point, bool>? where = null)
    {
        Bush? best = null;
        int bestDist = int.MaxValue;
        foreach (Bush b in w.Map.Bushes)
        {
            if (b.Berries <= 0 || Targeted(w, JobKind.Forage, b.Id) || (where != null && !where(b.Pos))) continue;
            int d = b.Pos.ChebyshevTo(from);
            if (d < bestDist) { best = b; bestDist = d; }
        }
        return best;
    }

    /// <summary>The nearest standing tree to fell, leaving out a sacred tree its keepers would spare (ADR-0023) and a dryad's tree this feller (if named) would spare (ADR-0022).</summary>
    public static Tree? StandingTree(World w, Point from, Character? feller = null)
    {
        Tree? best = null;
        int bestDist = int.MaxValue;
        foreach (Tree t in w.Map.Trees)
        {
            if (t.Felled || Targeted(w, JobKind.GatherTimber, t.Id)) continue;
            if (feller != null && Culture.SacredTrees.Spares(w, feller, t)) continue;
            if (feller != null && Forest.Spares(w, feller, t)) continue;
            if (Adjacent(w, t.Pos, from) == Point.None) continue;
            int d = t.Pos.ChebyshevTo(from);
            if (d < bestDist) { best = t; bestDist = d; }
        }
        return best;
    }

    /// <summary>Nearest covered, passable tile (the ruins first, then sound roof sections).</summary>
    public static Point CoveredSpot(World w, Point from)
    {
        Point best = Point.None;
        int bestDist = int.MaxValue;
        bool[] cover = CoverArray(w);
        Map m = w.Map;
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                var p = new Point(x, y);
                if (!cover[m.Index(p)] || !m.Passable(p)) continue;
                int d = p.ChebyshevTo(from);
                if (d < bestDist) { best = p; bestDist = d; }
            }
        return best;
    }

    /// <summary>
    /// Where this character should sleep (build design 4.6): their own bed, an unclaimed bed they built, an
    /// unclaimed bed under a roof, any unclaimed bed; else covered floor, the shelter floor, or right here.
    /// Those who burn in daylight put cover first and take only a covered bed. Nobody sleeps in someone else's bed.
    /// </summary>
    public static Point SleepSpot(World w, Character c)
    {
        bool burns = BurnsInDaylight(w, c);
        Bed? bed = Building.SleepBed(w, c, coveredOnly: burns);
        // By day, one who burns goes to their bed only if the way there is under cover; else they would walk into the sun.
        if (bed != null && (!burns || !SunDanger(w) || CoveredWay(w, c.Pos, bed.Pos))) return bed.Pos;
        if (burns)
        {
            Point covered = CoveredSpot(w, c.Pos, c.Id);
            return covered == Point.None ? NotOthersBed(w, c.Pos, c.Id) : covered;
        }
        Point floor = CoveredSpot(w, c.Pos, c.Id);
        if (floor != Point.None && !w.Map.CoveredRuins.Contains(floor)) return floor;
        Rect s = w.Map.Shelter;
        var inside = new Point(s.X + 1 + (c.Id % (s.W - 2)), s.Y + 1 + (c.Id % (s.H - 2)));
        return NotOthersBed(w, w.Map.Passable(inside) ? inside : c.Pos, c.Id);
    }

    /// <summary>Can one walk from one tile to another without leaving cover (8 ways, never cutting a corner)?</summary>
    private static bool CoveredWay(World w, Point from, Point to)
    {
        Map m = w.Map;
        bool[] cover = CoverArray(w);
        bool Open(Point p) => m.Passable(p) && cover[m.Index(p)];
        if (!Open(from) || !Open(to)) return false;
        var seen = new bool[m.Width * m.Height];
        var queue = new Queue<Point>();
        seen[m.Index(from)] = true;
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            if (p == to) return true;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var q = new Point(p.X + dx, p.Y + dy);
                    if ((dx == 0 && dy == 0) || !Open(q) || seen[m.Index(q)]) continue;
                    if (dx != 0 && dy != 0 && (!Open(new Point(p.X + dx, p.Y)) || !Open(new Point(p.X, p.Y + dy)))) continue;
                    seen[m.Index(q)] = true;
                    queue.Enqueue(q);
                }
        }
        return false;
    }

    /// <summary>The tile itself, unless it is someone else's bed (or a free bed someone is in or heading for); then its first passable neighbour that is not.</summary>
    private static Point NotOthersBed(World w, Point p, int id)
    {
        if (!SleepTaken(w, p, id)) return p;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                var q = new Point(p.X + dx, p.Y + dy);
                if (w.Map.Passable(q) && !SleepTaken(w, q, id)) return q;
            }
        return p;
    }

    private static bool SleepTaken(World w, Point p, int id) => Building.OthersBed(w, p, id) || (Building.BedAt(w, p) != null && TakenBed(w, p, id));

    /// <summary>Nearest covered, passable tile to sleep on that is not someone else's bed (nor a free bed someone is in or heading for).</summary>
    private static Point CoveredSpot(World w, Point from, int sleeperId)
    {
        Point best = Point.None;
        int bestDist = int.MaxValue;
        bool[] cover = CoverArray(w);
        Map m = w.Map;
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                var p = new Point(x, y);
                if (!cover[m.Index(p)] || !m.Passable(p)) continue;
                int d = p.ChebyshevTo(from);
                if (d < bestDist && !SleepTaken(w, p, sleeperId)) { best = p; bestDist = d; }
            }
        return best;
    }

    /// <summary>
    /// Which tiles are covered, built in one pass over the roof sections. Map.Covered stays a live check (tests and
    /// other systems change roofs directly); a scan over every tile uses this instead of asking each tile (ADR-0027).
    /// </summary>
    private static bool[] CoverArray(World w)
    {
        Map m = w.Map;
        var cover = new bool[m.Width * m.Height];
        void Mark(Rect r)
        {
            for (int y = Math.Max(0, r.Y); y < Math.Min(m.Height, r.Y + r.H); y++)
                for (int x = Math.Max(0, r.X); x < Math.Min(m.Width, r.X + r.W); x++) cover[y * m.Width + x] = true;
        }
        Mark(m.CoveredRuins);
        foreach (RoofSection s in m.Roof)
            if (s.Sound) Mark(s.Area);
        return cover;
    }

    public static bool TakenBed(World w, Point bed, int exceptId)
    {
        foreach (Character o in w.Characters)
        {
            if (o.Id == exceptId || !o.IsActive) continue;
            if (o.Pos == bed && o.Asleep) return true;
            if (o.Job is { Kind: JobKind.Sleep } j && j.Target == bed) return true;
        }
        return false;
    }

    /// <summary>A Dice-chosen passable tile within radius of centre.</summary>
    public static Point Near(World w, Point centre, int radius)
    {
        DiceStream dice = w.Rng.Dice[DiceSystem.Work];
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var p = new Point(centre.X + dice.Range(DiceId.WanderTarget, -radius, radius + 1), centre.Y + dice.Range(DiceId.WanderTarget, -radius, radius + 1));
            if (w.Map.Passable(p)) return p;
        }
        return Point.None;
    }

    /// <summary>Is someone else already doing this kind of job on this target?</summary>
    public static bool Targeted(World w, JobKind kind, int targetId, int exceptId = 0)
    {
        foreach (Character o in w.Characters)
            if (o.Id != exceptId && o.IsActive && o.Job is { } j && j.Kind == kind && j.TargetId == targetId) return true;
        return false;
    }

    /// <summary>
    /// Where the oldest stored stack of this item lies (in a stockpile, or waiting to be carried to one), or Point.None;
    /// only where <paramref name="where"/> allows, if given (inside the home area after dark).
    /// </summary>
    public static Point FoodTile(World w, string defId, Func<Point, bool>? where = null)
    {
        ItemStack? best = null;
        foreach (ItemStack s in w.Items)
        {
            if (s.DefId != defId || s.Count <= 0 || !s.Stored || (where != null && !where(s.Pos))) continue;
            if (best == null || (s.SpoilTick != 0 && (best.SpoilTick == 0 || s.SpoilTick < best.SpoilTick))) best = s;
        }
        return best?.Pos ?? Point.None;
    }

    /// <summary>Members who eat this item from the stores (berries for humans; vampires, dryads, djinn and the undead do not).</summary>
    public static int Eaters(World w, string itemId)
    {
        int n = 0;
        foreach (Character c in w.Members())
            if (w.Content.SpeciesById(c.SpeciesId) is { EatsItem: true } sp && sp.FoodSource == itemId) n++;
        return n;
    }

    /// <summary>False for those who never tire or sleep (the undead).</summary>
    public static bool Tires(World w, Character c) => w.Content.SpeciesById(c.SpeciesId).Tires;

    /// <summary>Timber still wanted: for broken roof sections, plans (ADR-0027) and bargains, less the stock and what people carry.</summary>
    public static int TimberNeeded(World w)
    {
        int unsound = 0;
        foreach (RoofSection r in w.Map.Roof)
            if (!r.Sound && !r.Removed) unsound++;
        int perSection = w.Content.WorkById("repair_roof").TimberCost;
        int carried = 0;
        foreach (Character c in w.Characters)
            if (c.IsActive && c.Carrying is { DefId: "timber" } carry) carried += carry.Count;
        return unsound * perSection + Building.Owed(w, "timber") + Bargains.Demand(w, "timber") - Stock.Count(w, "timber") - carried;
    }

    public static SpeciesDef Species(World w, Character c) => w.Content.SpeciesById(c.SpeciesId);
}
