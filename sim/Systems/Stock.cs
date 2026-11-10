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

    /// <summary>
    /// Takes where the taker stands (ADR-0028): from stores on their tile first, then from the nearest
    /// stored stack (ties soonest to spoil, then by id). Eating and roof timber take this way.
    /// </summary>
    public static int Take(World w, string defId, int count, Point from)
    {
        var candidates = new List<ItemStack>();
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Count > 0 && s.Stored) candidates.Add(s);
        candidates.Sort((a, b) =>
        {
            int byDist = a.Pos.ChebyshevTo(from).CompareTo(b.Pos.ChebyshevTo(from));
            return byDist != 0 ? byDist : SoonestFirst(a, b);
        });
        return TakeFrom(w, candidates, count);
    }

    /// <summary>Stores of this item within <paramref name="reach"/> tiles of a spot, soonest to spoil first: what a thief standing there can carry off.</summary>
    public static List<ItemStack> Near(World w, string defId, Point at, int reach)
    {
        var near = new List<ItemStack>();
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Count > 0 && s.Stored && s.Pos.ChebyshevTo(at) <= reach) near.Add(s);
        near.Sort(SoonestFirst);
        return near;
    }

    /// <summary>Takes up to <paramref name="count"/> from these stacks, in order.</summary>
    public static int Take(World w, List<ItemStack> stacks, int count) => TakeFrom(w, stacks, count);

    /// <summary>Stores of this item lying in the open, not under a sound roof or in the covered ruins (what the damp can reach).</summary>
    public static List<ItemStack> InTheOpen(World w, string defId)
    {
        var open = new List<ItemStack>();
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Count > 0 && s.Stored && !w.Map.Covered(s.Pos)) open.Add(s);
        open.Sort(SoonestFirst);
        return open;
    }

    public static int Total(IReadOnlyList<ItemStack> stacks)
    {
        int n = 0;
        foreach (ItemStack s in stacks) n += s.Count;
        return n;
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
    /// Where to put a stack down (ADR-0028): the tile nearest <paramref name="from"/> whose stockpile
    /// keeps the item and already holds some, else the nearest free tile of a stockpile that keeps it,
    /// else the nearest such tile at all; ties go to the lower tile index. Point.None when no stockpile keeps it.
    /// </summary>
    public static Point DropTile(World w, string defId, Point from)
    {
        Map m = w.Map;
        Point holding = Point.None, free = Point.None, any = Point.None;
        int holdingKey = int.MaxValue, freeKey = int.MaxValue, anyKey = int.MaxValue;
        foreach (Zone z in m.Zones)
        {
            if (!Zones.Keeps(w, z, defId)) continue;
            foreach (Point p in Zones.Each(z.Tiles))
            {
                if (!m.InBounds(p)) continue;
                // Nearest first, then the lower tile index: one key orders both.
                int key = p.ChebyshevTo(from) * m.Width * m.Height + m.Index(p);
                if (key < anyKey) { any = p; anyKey = key; }
                bool holds = false, occupied = false;
                foreach (ItemStack s in w.Items)
                {
                    if (s.Pos != p) continue;
                    occupied = true;
                    if (s.DefId == defId) holds = true;
                }
                if (holds && key < holdingKey) { holding = p; holdingKey = key; }
                if (!occupied && key < freeKey) { free = p; freeKey = key; }
            }
        }
        return holding != Point.None ? holding : free != Point.None ? free : any;
    }
}
