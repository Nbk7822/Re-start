using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>Camp stores: item stacks lying inside the stockpile zone. Taking uses the oldest food first.</summary>
public static class Stock
{
    public static int Count(World w, string defId)
    {
        int n = 0;
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && w.Map.Stockpile.Contains(s.Pos)) n += s.Count;
        return n;
    }

    /// <summary>Adds a fresh stack at a tile (merging with a stack of the same item and spoil time there).</summary>
    public static ItemStack Add(World w, string defId, int count, Point at)
    {
        ItemDef def = w.Content.ItemById(defId);
        long spoil = def.SpoilDays > 0 ? w.Tick + (long)def.SpoilDays * w.Content.World.TicksPerDay : 0;
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Pos == at && s.SpoilTick == spoil)
            {
                s.Count += count;
                return s;
            }
        var stack = new ItemStack { Id = w.NewId(), DefId = defId, Count = count, Pos = at, SpoilTick = spoil };
        w.Items.Add(stack);
        return stack;
    }

    /// <summary>Removes up to <paramref name="count"/> from stockpile stacks, soonest-to-spoil first. Returns how many were taken.</summary>
    public static int Take(World w, string defId, int count)
    {
        var candidates = new List<ItemStack>();
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && s.Count > 0 && w.Map.Stockpile.Contains(s.Pos)) candidates.Add(s);
        candidates.Sort((a, b) =>
        {
            long sa = a.SpoilTick == 0 ? long.MaxValue : a.SpoilTick, sb = b.SpoilTick == 0 ? long.MaxValue : b.SpoilTick;
            int bySpoil = sa.CompareTo(sb);
            return bySpoil != 0 ? bySpoil : a.Id.CompareTo(b.Id);
        });
        int taken = 0;
        foreach (ItemStack s in candidates)
        {
            if (taken == count) break;
            int n = Math.Min(s.Count, count - taken);
            s.Count -= n;
            taken += n;
        }
        w.Items.RemoveAll(s => s.Count <= 0);
        return taken;
    }

    /// <summary>A free tile inside the stockpile for dropping a new stack (prefers a tile already holding that item).</summary>
    public static Point DropTile(World w, string defId)
    {
        Rect r = w.Map.Stockpile;
        foreach (ItemStack s in w.Items)
            if (s.DefId == defId && r.Contains(s.Pos)) return s.Pos;
        for (int y = r.Y; y < r.Y + r.H; y++)
            for (int x = r.X; x < r.X + r.W; x++)
            {
                var p = new Point(x, y);
                bool used = false;
                foreach (ItemStack s in w.Items)
                    if (s.Pos == p) { used = true; break; }
                if (!used) return p;
            }
        return new Point(r.X, r.Y);
    }
}
