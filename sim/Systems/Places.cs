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

    public static Bush? BushWithBerries(World w, Point from)
    {
        Bush? best = null;
        int bestDist = int.MaxValue;
        foreach (Bush b in w.Map.Bushes)
        {
            if (b.Berries <= 0 || Targeted(w, JobKind.Forage, b.Id)) continue;
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
        Map m = w.Map;
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                var p = new Point(x, y);
                if (!m.Covered(p) || !m.Passable(p)) continue;
                int d = p.ChebyshevTo(from);
                if (d < bestDist) { best = p; bestDist = d; }
            }
        return best;
    }

    /// <summary>Where this character should sleep: cover for those who burn, else a free bed under a sound roof, any free bed, the shelter floor, or right here.</summary>
    public static Point SleepSpot(World w, Character c)
    {
        if (BurnsInDaylight(w, c))
        {
            Point covered = CoveredSpot(w, c.Pos);
            return covered == Point.None ? c.Pos : covered;
        }
        Bed? soundBed = null, anyBed = null;
        foreach (Bed b in w.Map.Beds)
        {
            if (TakenBed(w, b.Pos, c.Id)) continue;
            anyBed ??= b;
            if (soundBed == null && w.Map.Covered(b.Pos)) soundBed = b;
        }
        if (soundBed != null) return soundBed.Pos;
        if (anyBed != null) return anyBed.Pos;
        Point floor = CoveredSpot(w, c.Pos);
        if (floor != Point.None && !w.Map.CoveredRuins.Contains(floor)) return floor;
        Rect s = w.Map.Shelter;
        var inside = new Point(s.X + 1 + (c.Id % (s.W - 2)), s.Y + 1 + (c.Id % (s.H - 2)));
        return w.Map.Passable(inside) ? inside : c.Pos;
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

    public static bool SomeoneOnWatch(World w, int exceptId = 0)
    {
        foreach (Character o in w.Characters)
            if (o.Id != exceptId && o.Status == CharStatus.Member && o.Job is { Kind: JobKind.KeepWatch }) return true;
        return false;
    }

    /// <summary>Where the oldest stored stack of this item lies (in a stockpile, or waiting to be carried to one), or Point.None.</summary>
    public static Point FoodTile(World w, string defId)
    {
        ItemStack? best = null;
        foreach (ItemStack s in w.Items)
        {
            if (s.DefId != defId || s.Count <= 0 || !s.Stored) continue;
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

    public static int TimberNeeded(World w)
    {
        int unsound = 0;
        foreach (RoofSection r in w.Map.Roof)
            if (!r.Sound) unsound++;
        int perSection = w.Content.WorkById("repair_roof").TimberCost;
        int carried = 0;
        foreach (Character c in w.Characters)
            if (c.IsActive && c.Carrying is { DefId: "timber" } carry) carried += carry.Count;
        return unsound * perSection + Bargains.Demand(w, "timber") - Stock.Count(w, "timber") - carried;
    }

    public static SpeciesDef Species(World w, Character c) => w.Content.SpeciesById(c.SpeciesId);
}
