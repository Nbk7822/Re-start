using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Sim.Api;

/// <summary>A kind of zone the player can mark (only behaviours this build implements): its name, its one-line hint and its colour.</summary>
public sealed record ZoneKindState(string Id, string Name, string Hint, string Color, string Behaviour, string Layer, bool Single);

/// <summary>
/// One of the camp's zones: its kind and name, where it lies in words ("in the covered ruins"), how many
/// tiles and their bounds, the items a stockpile keeps (ids and names), and whether a queued command will change it.
/// </summary>
public sealed record ZoneState(int Id, string Kind, string Name, string Place, int Tiles, Rect Bounds,
    IReadOnlyList<string> ItemIds, IReadOnlyList<string> Items, bool Pending);

/// <summary>Whether a tile may hold a zone of a kind, and if not, why (a loc key resolved to words).</summary>
public sealed record ZoneTileState(int X, int Y, bool Ok, string Reason);

public static partial class StateApi
{
    /// <summary>The kinds of zone the player can mark, in zones.json order.</summary>
    public static IReadOnlyList<ZoneKindState> ZoneKinds(World w)
    {
        LocTable loc = w.Content.Loc;
        var list = new List<ZoneKindState>();
        foreach (ZoneDef d in w.Content.Zones)
            list.Add(new ZoneKindState(d.Id, loc.Get(d.NameKey), loc.Get(d.HintKey), d.Color, d.Behaviour, d.Layer, d.Single));
        return list;
    }

    /// <summary>
    /// The camp's zones as they will be once queued commands apply (and then <paramref name="drafts"/>,
    /// the view's choices made while paused), in id order with drafted zones last. Pending marks a zone
    /// a queued command changes.
    /// </summary>
    public static IReadOnlyList<ZoneState> Zones(World w, IEnumerable<Command>? drafts = null)
    {
        LocTable loc = w.Content.Loc;
        List<Zone> applied = w.Map.Zones;
        List<Zone> pending = Systems.Zones.Preview(w);
        List<Zone> shown = drafts == null ? pending : Systems.Zones.Preview(w, drafts);
        var list = new List<ZoneState>(shown.Count);
        foreach (Zone z in shown)
        {
            ZoneDef? def = w.Content.TryZone(z.Kind);
            Zone? now = applied.Find(a => a.Id == z.Id);
            Zone? queued = pending.Find(a => a.Id == z.Id);
            bool changed = now == null || queued == null || !now.Tiles.SequenceEqual(queued.Tiles) || !now.Items.SequenceEqual(queued.Items);
            var names = new List<string>(z.Items.Count);
            foreach (string id in z.Items) names.Add(loc.Get(w.Content.ItemById(id).NameKey));
            list.Add(new ZoneState(z.Id, z.Kind, def == null ? z.Kind : loc.Get(def.NameKey), loc.Get(Systems.Zones.PlacePhraseKey(w, z.Tiles)),
                Systems.Zones.Count(z.Tiles), Systems.Zones.Bounds(z.Tiles), z.Items, names, changed));
        }
        return list;
    }

    /// <summary>For each tile, whether a zone of this kind may lie there and why not: the same check the commands make.</summary>
    public static IReadOnlyList<ZoneTileState> CanZone(World w, string kind, IEnumerable<Point> tiles)
    {
        LocTable loc = w.Content.Loc;
        ZoneDef def = w.Content.ZoneById(kind);
        var list = new List<ZoneTileState>();
        foreach (Point p in tiles)
        {
            string? why = Systems.Zones.Check(w, def, p);
            list.Add(new ZoneTileState(p.X, p.Y, why == null, why == null ? "" : loc.Get(why)));
        }
        return list;
    }

    /// <summary>The tooltip line for a use zone on a tile: "Stockpile (berries, timber)", or the zone's name.</summary>
    private static string? ZoneLine(World w, Point p)
    {
        if (Systems.Zones.UseZoneAt(w, p) is not { } z || w.Content.TryZone(z.Kind) is not { } def) return null;
        LocTable loc = w.Content.Loc;
        string name = loc.Get(def.NameKey);
        if (def.Behaviour != Systems.Zones.Stores) return name;
        var items = new List<string>(z.Items.Count);
        foreach (string id in z.Items) items.Add(loc.Get(w.Content.ItemById(id).NameKey));
        return loc.Format("place.zone.stores", new[]
        {
            new KeyValuePair<string, string>("zone", name),
            new KeyValuePair<string, string>("items", string.Join(loc.Get("place.zone.item_sep"), items)),
        });
    }
}
