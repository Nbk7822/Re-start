using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Marking, reshaping and removing zones (ADR-0028). One set of rules works on a list of zones, so a
/// command, the last-stockpile guard and the view's preview of pending and drafted choices all agree.
/// A command applies its change to a copy, checks the guard on the copy, and only then commits it and
/// tells the Chronicle; a change that would leave an item with no stockpile is ignored whole.
/// </summary>
public static partial class Zones
{
    /// <summary>What a change did, for the events that describe it.</summary>
    internal sealed class Outcome
    {
        public Zone? Zone;
        public bool Created;
        public List<TileRun> OldTiles = new();
        public List<string> OldItems = new();
    }

    // ================= which tiles may hold a zone =================

    /// <summary>
    /// Null if a zone of this kind may lie on the tile, else the loc key of why not. A use zone (a
    /// stockpile, a meeting place) needs open, passable ground: not the hearth, a bush or a bed.
    /// Roofs overhead are fine. Build's buildings and plans will be refused here too.
    /// </summary>
    public static string? Check(World w, ZoneDef def, Point p)
    {
        Map m = w.Map;
        if (!m.InBounds(p)) return "zone_check.outside";
        if (def.Layer != UseLayer) return null;
        if (p == m.Hearth) return "zone_check.hearth";
        if (!m.Passable(p)) return "zone_check.blocked";
        if (m.IsBush(p)) return "zone_check.bush";
        foreach (Bed b in m.Beds)
            if (b.Pos == p) return "zone_check.bed";
        return null;
    }

    // ================= the rules, on a list of zones =================

    public static List<Zone> Clone(IEnumerable<Zone> zones)
    {
        var copy = new List<Zone>();
        foreach (Zone z in zones)
            copy.Add(new Zone { Id = z.Id, Kind = z.Kind, Tiles = new List<TileRun>(z.Tiles), Items = new List<string>(z.Items), MadeEvent = z.MadeEvent, MadeBy = z.MadeBy });
        return copy;
    }

    /// <summary>
    /// Marks tiles as a zone of this kind: zone id 0 makes a new one (a negative id names a zone the
    /// view drafted while paused), otherwise that zone's tiles become exactly these. Tiles that fail
    /// <see cref="Check"/> are dropped; a second zone of a single kind joins the first; use-zone tiles
    /// are taken from any other use zone, and one left with no tiles is gone. Items (stockpiles only)
    /// default to every item for a new stockpile and to what it kept for an old one. Null if nothing
    /// would change.
    /// </summary>
    internal static Outcome? Mark(World w, List<Zone> zones, int zoneId, string kind, IReadOnlyList<TileRun> tiles, IReadOnlyList<string>? items, Func<int> newId)
    {
        ZoneDef? def = w.Content.TryZone(kind);
        if (def == null) return null;
        var valid = new List<Point>();
        foreach (Point p in Each(tiles))
            if (Check(w, def, p) == null) valid.Add(p);
        List<TileRun> marked = Canonical(valid);
        if (marked.Count == 0) return null;

        Zone? target = zoneId == 0 ? null : zones.Find(z => z.Id == zoneId);
        if (zoneId > 0 && (target == null || target.Kind != kind)) return null;
        bool join = target == null && def.Single;
        if (join) target = zones.Find(z => z.Kind == kind);
        List<TileRun> next = join && target != null ? Canonical(Each(target.Tiles).Concat(Each(marked))) : marked;
        List<string>? nextItems = NormalItems(w, def, items, target);
        if (nextItems == null) return null;
        if (target != null && SameRuns(target.Tiles, next) && target.Items.SequenceEqual(nextItems)) return null;

        var outcome = new Outcome { Created = target == null, OldTiles = target?.Tiles ?? new List<TileRun>(), OldItems = target?.Items ?? new List<string>() };
        if (target == null)
        {
            target = new Zone { Id = newId(), Kind = kind };
            zones.Add(target);
        }
        target.Tiles = next;
        target.Items = nextItems;
        if (def.Layer == UseLayer) TakeTiles(w, zones, target);
        outcome.Zone = target;
        return outcome;
    }

    /// <summary>A stockpile keeps the items it is set to keep, and nothing else. Null if that leaves it keeping nothing.</summary>
    internal static Outcome? SetItems(World w, List<Zone> zones, int zoneId, IReadOnlyList<string> items)
    {
        Zone? z = zones.Find(x => x.Id == zoneId);
        if (z == null || Def(w, z) is not { Behaviour: Stores } def) return null;
        List<string>? next = NormalItems(w, def, items, z);
        if (next == null || z.Items.SequenceEqual(next)) return null;
        var outcome = new Outcome { Zone = z, OldTiles = z.Tiles, OldItems = z.Items };
        z.Items = next;
        return outcome;
    }

    /// <summary>Every item in items.json is kept by some stockpile with tiles: moving or unticking must never leave the camp nowhere to put its food.</summary>
    public static bool EveryItemKept(World w, IReadOnlyList<Zone> zones)
    {
        foreach (ItemDef item in w.Content.Items)
        {
            bool kept = false;
            foreach (Zone z in zones)
                if (IsStores(w, z) && z.Tiles.Count > 0 && z.Items.Contains(item.Id)) { kept = true; break; }
            if (!kept) return false;
        }
        return true;
    }

    /// <summary>Would removing this zone leave an item with no stockpile? (The view greys out its ✕.)</summary>
    public static bool CanRemove(World w, IReadOnlyList<Zone> zones, int zoneId)
    {
        var trial = new List<Zone>(zones);
        trial.RemoveAll(z => z.Id == zoneId);
        return EveryItemKept(w, trial);
    }

    /// <summary>Would a stockpile keeping exactly these items leave every item kept somewhere?</summary>
    public static bool CanKeep(World w, IReadOnlyList<Zone> zones, int zoneId, IReadOnlyList<string> items)
    {
        List<Zone> trial = Clone(zones);
        Zone? z = trial.Find(x => x.Id == zoneId);
        if (z == null) return false;
        z.Items = new List<string>(items);
        return items.Count > 0 && EveryItemKept(w, trial);
    }

    private static List<string>? NormalItems(World w, ZoneDef def, IReadOnlyList<string>? items, Zone? existing)
    {
        if (def.Behaviour != Stores) return new List<string>();
        if (items == null) return existing != null ? new List<string>(existing.Items) : AllItems(w);
        var list = new List<string>();
        foreach (string id in items)
            if (w.Content.TryItem(id) != null && !list.Contains(id)) list.Add(id);
        list.Sort(StringComparer.Ordinal);
        return list.Count == 0 ? null : list;
    }

    private static List<string> AllItems(World w)
    {
        var items = new List<string>();
        foreach (ItemDef i in w.Content.Items) items.Add(i.Id);
        items.Sort(StringComparer.Ordinal);
        return items;
    }

    private static bool SameRuns(IReadOnlyList<TileRun> a, IReadOnlyList<TileRun> b) => a.Count == b.Count && a.SequenceEqual(b);

    private static void TakeTiles(World w, List<Zone> zones, Zone taker)
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            Zone other = zones[i];
            if (other == taker || Def(w, other) is not { Layer: UseLayer }) continue;
            var keep = new List<Point>();
            bool overlap = false;
            foreach (Point p in Each(other.Tiles))
            {
                if (Contains(taker.Tiles, p)) overlap = true;
                else keep.Add(p);
            }
            if (!overlap) continue;
            if (keep.Count == 0) zones.RemoveAt(i);
            else other.Tiles = Canonical(keep);
        }
    }

    // ================= commands =================

    internal static void ApplyMark(World w, MarkZone cmd)
    {
        List<Zone> trial = Clone(w.Map.Zones);
        int fresh = w.NextId;
        Outcome? o = Mark(w, trial, cmd.ZoneId > 0 ? cmd.ZoneId : 0, cmd.Kind, cmd.Tiles, cmd.Items, () => fresh);
        if (o?.Zone == null || !EveryItemKept(w, trial)) return;
        if (o.Created) w.NewId();
        Commit(w, trial);
        Zone z = o.Zone;
        int by = w.PlayerId;
        bool reshaped = o.Created || !SameRuns(o.OldTiles, z.Tiles);
        if (reshaped)
        {
            string key = o.Created ? "event.world.zone_marked" : Moved(o.OldTiles, z.Tiles) ? "event.world.zone_moved" : "event.world.zone_changed";
            SimEvent e = w.Log.Emit(w, SystemTag.World, key, Actors(w), Array.Empty<long>(), null,
                EventArg.Char("a", by), EventArg.Key("zone", ArticleKey(z.Kind, indefinite: o.Created)),
                EventArg.Key("place", PlacePhraseKey(w, z.Tiles)), EventArg.Num("n", Count(z.Tiles)));
            z.MadeEvent = e.Id;
            z.MadeBy = by;
        }
        ItemEvents(w, z, o.Created ? AllItems(w) : o.OldItems, by);
        StoreLooseGoods(w);
    }

    internal static void ApplyRemove(World w, RemoveZone cmd)
    {
        Zone? z = ById(w, cmd.ZoneId);
        if (z == null) return;
        List<Zone> trial = Clone(w.Map.Zones);
        trial.RemoveAll(x => x.Id == cmd.ZoneId);
        if (!EveryItemKept(w, trial)) return;
        string place = PlacePhraseKey(w, z.Tiles);
        Commit(w, trial);
        w.Log.Emit(w, SystemTag.World, "event.world.zone_removed", Actors(w), Array.Empty<long>(), null,
            EventArg.Char("a", w.PlayerId), EventArg.Key("zone", ArticleKey(z.Kind, indefinite: false)), EventArg.Key("place", place));
    }

    internal static void ApplySetItems(World w, SetZoneItems cmd)
    {
        List<Zone> trial = Clone(w.Map.Zones);
        Outcome? o = SetItems(w, trial, cmd.ZoneId, cmd.Items);
        if (o?.Zone == null || !EveryItemKept(w, trial)) return;
        Commit(w, trial);
        ItemEvents(w, o.Zone, o.OldItems, w.PlayerId);
        StoreLooseGoods(w);
    }

    /// <summary>One line per item a stockpile now keeps or no longer keeps (a toggle changes one).</summary>
    private static void ItemEvents(World w, Zone z, IReadOnlyList<string> before, int by)
    {
        if (!IsStores(w, z)) return;
        string place = PlacePhraseKey(w, z.Tiles);
        foreach (ItemDef item in w.Content.Items)
        {
            bool was = before.Contains(item.Id), now = z.Items.Contains(item.Id);
            if (was == now) continue;
            w.Log.Emit(w, SystemTag.World, now ? "event.world.zone_items" : "event.world.zone_items_not", Actors(w), Array.Empty<long>(), null,
                EventArg.Char("a", by), EventArg.Item("item", item.Id), EventArg.Key("zone", ArticleKey(z.Kind, indefinite: false)), EventArg.Key("place", place));
        }
    }

    private static int[] Actors(World w) => w.Find(w.PlayerId) == null ? Array.Empty<int>() : new[] { w.PlayerId };

    /// <summary>Fewer than half of the old tiles are still in the zone: it was moved, not reshaped.</summary>
    private static bool Moved(IReadOnlyList<TileRun> before, IReadOnlyList<TileRun> after)
    {
        int kept = 0;
        foreach (Point p in Each(before))
            if (Contains(after, p)) kept++;
        return kept * 2 < Count(before);
    }

    private static void Commit(World w, List<Zone> zones)
    {
        w.Map.Zones = zones;
        Changed(w.Map);
    }

    /// <summary>
    /// Goods lying loose on ground a stockpile now keeps are stores from now on (a windfall the player
    /// paints a stockpile over); berries that become stores are shared out at once, as a delivery is.
    /// Stores on ground no stockpile keeps any more stay stores, waiting to be carried.
    /// </summary>
    private static void StoreLooseGoods(World w)
    {
        ItemStack? food = null;
        foreach (ItemStack s in w.Items)
        {
            if (s.Stored || !Accepts(w, s.Pos, s.DefId)) continue;
            s.Stored = true;
            food ??= s;
        }
        if (food != null) Rations.AfterDeposit(w, food);
    }

    // ================= what pending commands and drafts will make =================

    /// <summary>
    /// The zones as they will be once every pending command (and then the view's drafts, in order)
    /// has applied: a copy, with no events and no ids handed out. A drafted zone keeps its negative id.
    /// </summary>
    public static List<Zone> Preview(World w, IEnumerable<Command>? drafts = null)
    {
        List<Zone> zones = Clone(w.Map.Zones);
        int next = w.NextId;
        void Run(Command c)
        {
            List<Zone> trial = Clone(zones);
            bool ok;
            switch (c)
            {
                case MarkZone m:
                {
                    int id = m.ZoneId < 0 ? m.ZoneId : next;
                    Outcome? o = Mark(w, trial, m.ZoneId, m.Kind, m.Tiles, m.Items, () => id);
                    ok = o?.Zone != null;
                    if (ok && o!.Created && m.ZoneId >= 0) next++;
                    break;
                }
                case RemoveZone r:
                    ok = trial.RemoveAll(z => z.Id == r.ZoneId) > 0;
                    break;
                case SetZoneItems s:
                    ok = SetItems(w, trial, s.ZoneId, s.Items) != null;
                    break;
                default:
                    return;
            }
            if (ok && EveryItemKept(w, trial)) zones = trial;
        }
        foreach (Command c in w.PendingCommands) Run(c);
        if (drafts != null)
            foreach (Command c in drafts) Run(c);
        return zones;
    }

    /// <summary>Would this zone command change anything once the queued commands have applied? A draft that changes nothing is not sent.</summary>
    public static bool Changes(World w, Command c)
    {
        List<Zone> before = Preview(w), after = Preview(w, new[] { c });
        if (before.Count != after.Count) return true;
        for (int i = 0; i < before.Count; i++)
        {
            Zone a = before[i], b = after[i];
            if (a.Id != b.Id || a.Kind != b.Kind || !SameRuns(a.Tiles, b.Tiles) || !a.Items.SequenceEqual(b.Items)) return true;
        }
        return false;
    }

    // ================= words =================

    /// <summary>"zone.stockpile.a" ("a stockpile") or "zone.stockpile.the" ("the stockpile"); ContentDb requires both for every kind.</summary>
    public static string ArticleKey(string kind, bool indefinite) => "zone." + kind + (indefinite ? ".a" : ".the");

    /// <summary>
    /// Where a zone lies, as a loc key: in the covered ruins or the shelter when most of its tiles are
    /// inside them; otherwise by the hearth, the watch post or the river, whichever is nearest within 8
    /// tiles of its centre (ties in that order); otherwise out in the open.
    /// </summary>
    public static string PlacePhraseKey(World w, IReadOnlyList<TileRun> tiles)
    {
        int n = Count(tiles);
        if (n == 0) return "place_phrase.open";
        Map m = w.Map;
        int ruins = 0, shelter = 0;
        foreach (Point p in Each(tiles))
        {
            if (m.CoveredRuins.Contains(p)) ruins++;
            if (m.Shelter.Contains(p)) shelter++;
        }
        if (ruins * 2 > n) return "place_phrase.covered_ruins";
        if (shelter * 2 > n) return "place_phrase.shelter";
        Point centre = Bounds(tiles).Center;
        string best = "place_phrase.open";
        int bestDist = PlaceRange + 1;
        foreach ((string key, int d) in new[] { ("place_phrase.hearth", centre.ChebyshevTo(m.Hearth)), ("place_phrase.watch_post", centre.ChebyshevTo(m.WatchPost)), ("place_phrase.river", RiverDistance(m, centre)) })
            if (d < bestDist) { best = key; bestDist = d; }
        return best;
    }

    /// <summary>A landmark names a zone's place only within this many tiles of the zone's centre.</summary>
    public const int PlaceRange = 8;

    private static int RiverDistance(Map m, Point from)
    {
        int best = int.MaxValue;
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                var p = new Point(x, y);
                if (m.At(p) == TerrainKind.Water) best = Math.Min(best, p.ChebyshevTo(from));
            }
        return best;
    }
}
