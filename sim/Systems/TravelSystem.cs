using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// Moves characters along their paths, one tile at a time. A step takes the species' ticks per
/// tile, ×1.4 on a diagonal and ×2 into a bush. If the next tile has become blocked the path is
/// dropped and the work system re-plans.
/// </summary>
public static class TravelSystem
{
    public static bool Start(World w, Character c, Point target)
    {
        c.MoveTicks = 0;
        if (c.Pos == target)
        {
            c.Path.Clear();
            return true;
        }
        List<Point>? path = Pathfinder.Find(w.Map, c.Pos, target);
        if (path == null)
        {
            c.Path.Clear();
            return false;
        }
        c.Path = path;
        return true;
    }

    public static void Step(World w)
    {
        foreach (Character c in w.Characters)
        {
            if (!c.IsActive || c.Downed || c.Asleep || c.Path.Count == 0) continue;
            Point next = c.Path[0];
            if (!w.Map.Passable(next))
            {
                c.Path.Clear();
                c.MoveTicks = 0;
                continue;
            }
            if (c.MoveTicks == 0) c.Facing = FacingFor(c.Pos, next);
            c.MoveTicks++;
            if (c.MoveTicks < TicksFor(w, c, c.Pos, next)) continue;
            c.Pos = next;
            c.Path.RemoveAt(0);
            c.MoveTicks = 0;
        }
    }

    public static int TicksFor(World w, Character c, Point from, Point to)
    {
        int ticks = w.Content.SpeciesById(c.SpeciesId).TicksPerTile;
        if (from.X != to.X && from.Y != to.Y) ticks = ticks * 14 / 10;
        if (w.Map.IsBush(to)) ticks *= 2;
        return Math.Max(1, ticks);
    }

    public static Facing FacingFor(Point from, Point to)
    {
        int dx = to.X - from.X, dy = to.Y - from.Y;
        if (Math.Abs(dx) >= Math.Abs(dy)) return dx >= 0 ? Facing.E : Facing.W;
        return dy > 0 ? Facing.S : Facing.N;
    }
}
