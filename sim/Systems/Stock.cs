using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// Camp stores: stacks put away in a stockpile (ItemStack.Stored), wherever they lie now (ADR-0028).
/// A stack put down on a tile whose stockpile keeps it becomes stores; goods left behind when a
/// stockpile moves stay stores, waiting to be carried. Taking uses the oldest food first.
/// </summary>
public static class Stock
{
    public static int Count(World w, string defId)
    {
        int n = 0;
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Stored) n += s.Count;
        return n;
    }

    /// <summary>
    /// Adds a fresh stack at a tile, merging with a stack there of the same item, spoil time and
    /// standing. It is stores if a stockpile there keeps the item, or if it was stores already
    /// (<paramref name="stored"/>: a hauler's load, dropped on the way).
    /// </summary>
    public static ItemStack Add(World w, string defId, int count, Point at, bool stored = false)
    {
        ItemDef def = w.Content.ItemById(defId);
        long spoil = def.SpoilDays > 0 ? w.Tick + (long)def.SpoilDays * w.Content.World.TicksPerDay : 0;
        bool isStored = stored || Zones.Accepts(w, at, defId);
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Pos == at && s.SpoilTick == spoil && s.Stored == isStored)
            {
                s.Count += count;
                return s;
            }
        var stack = new ItemStack { Id = w.NewId(), DefId = defId, Count = count, Pos = at, SpoilTick = spoil, Stored = isStored };
        w.Items.Add(stack);
        return stack;
    }

    /// <summary>Removes up to <paramref name="count"/> from the stores, soonest-to-spoil first. Returns how many were taken.</summary>
    public static int Take(World w, string defId, int count)
    {
        var candidates = new List<ItemStack>();
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Count > 0 && s.Stored) candidates.Add(s);
        candidates.Sort(SoonestFirst);
        return TakeFrom(w, candidates, count);
    }

    /// <summary>Soonest to spoil first (never-spoiling last), then by stack id.</summary>
    internal static int SoonestFirst(ItemStack a, ItemStack b)
    {
        long sa = a.SpoilTick == 0 ? long.MaxValue : a.SpoilTick, sb = b.SpoilTick == 0 ? long.MaxValue : b.SpoilTick;
        int bySpoil = sa.CompareTo(sb);
        return bySpoil != 0 ? bySpoil : a.Id.CompareTo(b.Id);
    }

    private static int TakeFrom(World w, List<ItemStack> ordered, int count)
    {
        int taken = 0;
        foreach (ItemStack s in ordered)
        {
            if (taken == count) break;
            int n = Math.Min(s.Count, count - taken);
            s.Count -= n;
            taken += n;
        }
        w.Items.RemoveAll(s => s.Count <= 0);
        return taken;
    }

    /// <summary>
    /// Where to put a stack down: a tile whose stockpile keeps the item and already holds some, else
    /// the first free tile of a stockpile that keeps it (zones by id, tiles row by row), else that
    /// stockpile's first tile; Point.None when no stockpile keeps it.
    /// </summary>
    public static Point DropTile(World w, string defId)
    {
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && Zones.Accepts(w, s.Pos, defId)) return s.Pos;
        Point first = Point.None;
        foreach (Zone z in w.Map.Zones)
        {
            if (!Zones.Keeps(w, z, defId)) continue;
            foreach (Point p in Zones.Each(z.Tiles))
            {
                if (first == Point.None) first = p;
                if (!Occupied(w, p)) return p;
            }
        }
        return first;
    }

    private static bool Occupied(World w, Point p)
    {
        foreach (ItemStack s in w.Items)
            if (s.Pos == p) return true;
        return false;
    }
}
