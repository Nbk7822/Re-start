using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// Painted areas on the grid (ADR-0028): tile sets, the per-tile cache, and the questions other systems
/// ask of zones ("is this tile a stockpile that keeps berries?"). Zones are intentions: nothing here
/// orders anyone. Every query is a lookup; ties always break by zone id, then (y, x).
/// </summary>
public static partial class Zones
{
    /// <summary>The behaviour of a stockpile: goods put down there are the camp's stores.</summary>
    public const string Stores = "stores";

    public const string UseLayer = "use";

    // ================= tile sets =================

    /// <summary>The canonical runs of a set of tiles: sorted by y then x, merged, no overlaps, duplicates dropped.</summary>
    public static List<TileRun> Canonical(IEnumerable<Point> tiles)
    {
        var list = new List<Point>(tiles);
        list.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
        var runs = new List<TileRun>();
        foreach (Point p in list)
        {
            if (runs.Count > 0)
            {
                TileRun last = runs[^1];
                if (last.Y == p.Y && p.X < last.X + last.Length) continue;
                if (last.Y == p.Y && p.X == last.X + last.Length)
                {
                    runs[^1] = last with { Length = last.Length + 1 };
                    continue;
                }
            }
            runs.Add(new TileRun(p.Y, p.X, 1));
        }
        return runs;
    }

    /// <summary>The same tiles in canonical form (runs may arrive unsorted, overlapping or split).</summary>
    public static List<TileRun> Canonical(IEnumerable<TileRun> runs) => Canonical(Each(runs));

    /// <summary>Every tile of a set of runs, in their order (row-major for canonical runs).</summary>
    public static IEnumerable<Point> Each(IEnumerable<TileRun> runs)
    {
        foreach (TileRun r in runs)
            for (int x = r.X; x < r.X + r.Length; x++) yield return new Point(x, r.Y);
    }

    public static int Count(IReadOnlyList<TileRun> runs)
    {
        int n = 0;
        foreach (TileRun r in runs) n += r.Length;
        return n;
    }

    public static bool Contains(IReadOnlyList<TileRun> runs, Point p)
    {
        foreach (TileRun r in runs)
            if (r.Y == p.Y && p.X >= r.X && p.X < r.X + r.Length) return true;
        return false;
    }

    public static List<TileRun> FromRect(Rect r)
    {
        var runs = new List<TileRun>(r.H);
        for (int y = r.Y; y < r.Y + r.H; y++) runs.Add(new TileRun(y, r.X, r.W));
        return runs;
    }

    /// <summary>The smallest rectangle holding every tile (W and H 0 for no tiles).</summary>
    public static Rect Bounds(IReadOnlyList<TileRun> runs)
    {
        if (runs.Count == 0) return new Rect(0, 0, 0, 0);
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
        foreach (TileRun r in runs)
        {
            x0 = Math.Min(x0, r.X);
            x1 = Math.Max(x1, r.X + r.Length - 1);
            y0 = Math.Min(y0, r.Y);
            y1 = Math.Max(y1, r.Y);
        }
        return new Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    /// <summary>A tile to head for when going to a zone as a whole: the centre of its bounds if that lies in it, else its first tile.</summary>
    public static Point Landmark(Zone z)
    {
        if (z.Tiles.Count == 0) return Point.None;
        Point centre = Bounds(z.Tiles).Center;
        return Contains(z.Tiles, centre) ? centre : new Point(z.Tiles[0].X, z.Tiles[0].Y);
    }

    // ================= lookups =================

    public static Zone? ById(World w, int id)
    {
        foreach (Zone z in w.Map.Zones)
            if (z.Id == id) return z;
        return null;
    }

    public static ZoneDef? Def(World w, Zone z) => w.Content.TryZone(z.Kind);

    public static bool IsStores(World w, Zone z) => Def(w, z)?.Behaviour == Stores;

    /// <summary>A stockpile that keeps this item.</summary>
    public static bool Keeps(World w, Zone z, string itemId) => IsStores(w, z) && z.Items.Contains(itemId) && z.Tiles.Count > 0;

    /// <summary>Does any stockpile keep this item?</summary>
    public static bool Keeps(World w, string itemId)
    {
        foreach (Zone z in w.Map.Zones)
            if (Keeps(w, z, itemId)) return true;
        return false;
    }

    /// <summary>The use-layer zone on a tile (a stockpile or a meeting place), or null.</summary>
    public static Zone? UseZoneAt(World w, Point p)
    {
        if (!w.Map.InBounds(p)) return null;
        int id = UseCache(w)[w.Map.Index(p)];
        return id == 0 ? null : ById(w, id);
    }

    /// <summary>
    /// The use-layer zone on a tile among <paramref name="zones"/> (what queued and drafted commands will make, from
    /// <see cref="CampPreview"/>), or among the world's when null. Use zones never overlap, in a preview as in the world.
    /// </summary>
    public static Zone? UseZoneAt(World w, IReadOnlyList<Zone>? zones, Point p)
    {
        if (zones == null) return UseZoneAt(w, p);
        foreach (Zone z in zones)
            if (Def(w, z) is { Layer: UseLayer } && Contains(z.Tiles, p)) return z;
        return null;
    }

    /// <summary>
    /// The one question about goods: does a stockpile here keep this item? Stock.Add asks it to decide
    /// whether a stack put down is stores (ADR-0028); Build's stores will extend this same predicate.
    /// </summary>
    public static bool Accepts(World w, Point p, string itemId) => UseZoneAt(w, p) is { } z && Keeps(w, z, itemId);

    /// <summary>The first stockpile (by id) keeping this item, or null.</summary>
    public static Zone? FirstKeeping(World w, string itemId)
    {
        foreach (Zone z in w.Map.Zones)
            if (Keeps(w, z, itemId)) return z;
        return null;
    }

    // ================= the world's zones =================

    /// <summary>
    /// The camp starts with the demo's stockpile, as a zone that keeps every item on the rectangle WorldGen
    /// laid out, and a meeting place on the open ground round the hearth (step 2). Called last in world
    /// creation, so no id handed out before it moves. Neither was anyone's choice: their MadeEvent is 0.
    /// </summary>
    public static void CreateStart(World w)
    {
        var items = new List<string>();
        foreach (ItemDef i in w.Content.Items) items.Add(i.Id);
        items.Sort(StringComparer.Ordinal);
        w.Map.Zones.Add(new Zone { Id = w.NewId(), Kind = "stockpile", Tiles = FromRect(w.Map.StartStockpile), Items = items });
        w.Map.Zones.Add(new Zone { Id = w.NewId(), Kind = "meeting_place", Tiles = HearthRing(w) });
        Changed(w.Map);
    }

    /// <summary>Marks the zones changed: the cache is rebuilt before its next read and the view redraws.</summary>
    public static void Changed(Map m)
    {
        m.ZonesDirty = true;
        m.Revision++;
    }

    private static int[] UseCache(World w)
    {
        Rebuild(w);
        return w.Map.UseZone;
    }

    /// <summary>The home area tile flags (zones step 3), rebuilt with the use-layer cache.</summary>
    private static byte[] HomeCache(World w)
    {
        Rebuild(w);
        return w.Map.HomeAt;
    }

    /// <summary>Rebuilds both per-tile caches (the use-layer zone id, the home area flag) when the zones or the map changed.</summary>
    private static void Rebuild(World w)
    {
        Map m = w.Map;
        if (!m.ZonesDirty && m.UseZone.Length == m.Width * m.Height && m.HomeAt.Length == m.Width * m.Height) return;
        var cache = new int[m.Width * m.Height];
        var home = new byte[m.Width * m.Height];
        foreach (Zone z in m.Zones)
        {
            ZoneDef? def = Def(w, z);
            if (def is { Layer: UseLayer })
            {
                foreach (Point p in Each(z.Tiles))
                    if (m.InBounds(p)) cache[m.Index(p)] = z.Id;
            }
            else if (def is { Behaviour: HomeArea })
            {
                foreach (Point p in Each(z.Tiles))
                    if (m.InBounds(p)) home[m.Index(p)] = 1;
            }
        }
        m.UseZone = cache;
        m.HomeAt = home;
        m.ZonesDirty = false;
    }
}
