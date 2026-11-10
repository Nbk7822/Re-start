using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Sim.Api;

/// <summary>A building on the Build list: what it is called and costs, and whether it can be planned now (and if not, why).</summary>
public sealed record BuildingState(string Id, string Name, string Description, string Category, string Kind, string Cost, bool Available, string Reason);

/// <summary>One tile of a piece checked for placement: whether it can be built on and, if not, why (the build.reason.* key and its words).</summary>
public sealed record TileVerdictState(int X, int Y, bool Ok, string ReasonKey, string Reason);

/// <summary>One piece of a plan checked for placement (a tile, or a roof section), its cost and each tile's verdict.</summary>
public sealed record PieceState(int X, int Y, int W, int H, bool Ok, int Cost, IReadOnlyList<TileVerdictState> Tiles);

/// <summary>A whole plan checked for placement: each piece, what the pieces that can go cost, and how much of that is in store.</summary>
public sealed record CanPlaceState(string BuildingId, bool Ok, IReadOnlyList<PieceState> Pieces, int Cost, string Item, int InStore);

/// <summary>
/// A plan on the grid: its building and area, how it stands (StatusKey: pending, waiting_for_timber, waiting_for_walls,
/// ready, being_built, shut_in, in_the_way; Status: in words), its material here out of what it needs, its progress, and
/// who has delivered or worked on it. Pending: a placement queued and not yet applied (Id 0).
/// </summary>
public sealed record BlueprintState(int Id, string BuildingId, string Building, int X, int Y, int W, int H, string StatusKey, string Status,
    int Delivered, int Needed, string Item, int ProgressPct, IReadOnlyList<int> Contributors, IReadOnlyList<string> ContributorNames, bool Pending);

/// <summary>
/// A built thing (a wall, door, roof section or bed): its building and area, who built it and when (none for the old camp's),
/// its owner (beds), who mended it (roofs), and whether it is marked to be taken down.
/// </summary>
public sealed record StructureState(TargetKind Kind, int Id, string BuildingId, string Building, int X, int Y, int W, int H,
    IReadOnlyList<int> BuiltBy, string BuiltByNames, long BuiltEvent, string BuiltWhen, int OwnerId, string Owner, int MendedBy, string MendedByName,
    bool Marked, bool OldCamp);

/// <summary>The Build menu's reads (build design 6.4; ADR-0027). Reads never change state.</summary>
public static partial class StateApi
{
    /// <summary>What the Build list shows: every building in content order (mods' too), with its cost in words.</summary>
    public static IReadOnlyList<BuildingState> Buildings(World w)
    {
        LocTable loc = w.Content.Loc;
        var list = new List<BuildingState>();
        foreach (BuildingDef b in w.Content.Buildings)
            list.Add(new BuildingState(b.Id, loc.Get(b.NameKey), loc.Get(b.DescKey), b.Category, b.Kind, CostText(w, b), true, ""));
        return list;
    }

    /// <summary>"1 timber each", "2 timber", "1 timber per 2 tiles".</summary>
    private static string CostText(World w, BuildingDef b)
    {
        LocTable loc = w.Content.Loc;
        MaterialDef m = b.Material;
        string item = loc.Get(w.Content.ItemById(m.Item).NameKey);
        var args = new List<KeyValuePair<string, string>> { new("n", m.Count.ToString(Inv)), new("item", item) };
        if (m.PerTiles > 0)
        {
            args.Add(new KeyValuePair<string, string>("tiles", m.PerTiles.ToString(Inv)));
            return loc.Format("build.cost.per_tiles", args);
        }
        return loc.Format(b.Kind == Building.Wall ? "build.cost.each" : "build.cost.flat", args);
    }

    /// <summary>
    /// The placement check the commands make (Building.Check), with each tile's reason in words, the cost of what can go, and the
    /// stock. <paramref name="alsoPlanned"/>: pieces asked for but not yet on the map (a view's drafts, placements queued this tick)
    /// that the plan must not clash with either. <paramref name="exceptPlans"/>: placed plans asked to be called off but not yet
    /// (drafts, call-offs queued this tick), whose tiles the plan may take. <paramref name="zones"/>: the zones as queued and drafted
    /// commands will leave them, whose stockpiles the plan must keep off (all three from <see cref="CampPreview"/>); the world's when null.
    /// </summary>
    public static CanPlaceState CanPlace(World w, string buildingId, IReadOnlyList<Rect> pieces, IReadOnlyList<PlannedPiece>? alsoPlanned = null,
        IReadOnlyCollection<int>? exceptPlans = null, IReadOnlyList<Zone>? zones = null)
    {
        LocTable loc = w.Content.Loc;
        var states = new List<PieceState>();
        int cost = 0;
        bool ok = pieces.Count > 0;
        foreach (PieceVerdict v in Building.Check(w, buildingId, pieces, alsoPlanned, exceptPlans, zones))
        {
            var tiles = new List<TileVerdictState>(v.Tiles.Count);
            foreach (TileVerdict t in v.Tiles) tiles.Add(new TileVerdictState(t.Tile.X, t.Tile.Y, t.Ok, t.Reason, t.Ok ? "" : loc.Get(t.Reason)));
            states.Add(new PieceState(v.Area.X, v.Area.Y, v.Area.W, v.Area.H, v.Ok, v.Cost, tiles));
            if (v.Ok) cost += v.Cost;
            else ok = false;
        }
        string item = w.Content.TryBuilding(buildingId)?.Material.Item ?? "";
        return new CanPlaceState(buildingId, ok, states, cost, item, item.Length == 0 ? 0 : Systems.Stock.Count(w, item));
    }

    /// <summary>Every plan on the grid, in id order, then the placements queued but not yet applied.</summary>
    public static IReadOnlyList<BlueprintState> Blueprints(World w)
    {
        LocTable loc = w.Content.Loc;
        var list = new List<BlueprintState>();
        foreach (Blueprint bp in w.Blueprints)
        {
            BuildingDef def = Building.Def(w, bp);
            string key = StatusKey(w, bp, def);
            int needed = def.Cost(bp.Area), work = def.WorkFor(bp.Area);
            var names = new List<string>(bp.Contributors.Count);
            foreach (int id in bp.Contributors) names.Add(EventText.Name(w, id));
            list.Add(new BlueprintState(bp.Id, def.Id, loc.Get(def.NameKey), bp.Area.X, bp.Area.Y, bp.Area.W, bp.Area.H, key, StatusText(w, def, key),
                bp.Delivered, needed, def.Material.Item, Math.Min(100, bp.Progress * 100 / Math.Max(1, work)), bp.Contributors.ToArray(), names, false));
        }
        foreach (Command cmd in w.PendingCommands)
        {
            if (cmd is not PlaceBlueprint p || w.Content.TryBuilding(p.BuildingId) is not { } def) continue;
            foreach (Rect r in p.Pieces)
                list.Add(new BlueprintState(0, def.Id, loc.Get(def.NameKey), r.X, r.Y, r.W, r.H, "pending", StatusText(w, def, "pending"),
                    0, def.Cost(r), def.Material.Item, 0, Array.Empty<int>(), Array.Empty<string>(), true));
        }
        return list;
    }

    /// <summary>
    /// How a plan stands now. A wall or door with its timber on the site and someone (or a body) on its tile is "in the way"
    /// whether or not a builder has tried it yet: nobody takes it up while they are there (ADR-0027 review; it read "ready
    /// to build" under a body for ever).
    /// </summary>
    private static string StatusKey(World w, Blueprint bp, BuildingDef def)
    {
        if (bp.Blocked == BlockedReason.WouldShutIn && bp.BlockedRevision == w.Map.Revision) return "shut_in";
        if (def.Kind == Building.Roof && !Building.Supported(w, bp.Area, def)) return "waiting_for_walls";
        foreach (Character c in w.Characters)
            if (c.IsActive && c.Job is { Kind: JobKind.Build, TargetKind: TargetKind.Blueprint, Stage: >= 2 } j && j.TargetId == bp.Id) return "being_built";
        if (Building.Lacking(w, bp) > 0) return "waiting_for_timber";
        if (def.Kind is Building.Wall or Building.Door && Building.InTheWay(w, new Point(bp.Area.X, bp.Area.Y))) return "in_the_way";
        return "ready";
    }

    private static string StatusText(World w, BuildingDef def, string key) =>
        w.Content.Loc.Format("build.status." + key, new[] { new KeyValuePair<string, string>("item", w.Content.Loc.Get(w.Content.ItemById(def.Material.Item).NameKey)) });

    /// <summary>A built thing by kind and id (roof sections include the old camp's); null if there is none.</summary>
    public static StructureState? Structure(World w, TargetKind kind, int id)
    {
        LocTable loc = w.Content.Loc;
        StructureState Make(BuildingDef def, Rect area, List<int> builtBy, long builtEvent, int owner, int mended, long marked) =>
            new(kind, id, def.Id, loc.Get(def.NameKey), area.X, area.Y, area.W, area.H, builtBy.ToArray(), NameList(w, builtBy), builtEvent,
                w.Log.Get(builtEvent) is { } e ? EventText.When(w, e.Tick) : "", owner, owner == 0 ? "" : EventText.Name(w, owner),
                mended, mended == 0 ? "" : EventText.Name(w, mended), marked != 0, builtBy.Count == 0);
        switch (kind)
        {
            case TargetKind.Roof:
                foreach (RoofSection r in w.Map.Roof)
                    if (r.Id == id && !r.Removed) return Make(Building.Def(w, r), r.Area, r.BuiltBy, r.BuiltEvent, 0, r.MendedBy, r.MarkedEvent);
                return null;
            case TargetKind.Bed:
                return Building.BedById(w, id) is { } b ? Make(Building.Def(w, b), new Rect(b.Pos.X, b.Pos.Y, 1, 1), b.BuiltBy, b.BuiltEvent, b.OwnerId, 0, b.MarkedEvent) : null;
            case TargetKind.Structure:
                return Building.StructureById(w, id) is { } s
                    ? Make(w.Content.BuildingById(s.DefId), new Rect(s.Pos.X, s.Pos.Y, 1, 1), s.BuiltBy, s.BuiltEvent, 0, 0, s.MarkedEvent)
                    : null;
            default:
                return null;
        }
    }

    private static string NameList(World w, List<int> ids)
    {
        var names = new List<string>(ids.Count);
        foreach (int id in ids) names.Add(EventText.Name(w, id));
        return string.Join(w.Content.Loc.Get("log.list_sep"), names);
    }

    /// <summary>A person's bed in words: "the bed Oren built", "the bed they built", "a bed", or "none".</summary>
    private static string BedText(World w, Character c)
    {
        LocTable loc = w.Content.Loc;
        Bed? bed = Building.OwnedBed(w, c.Id);
        if (bed == null) return loc.Get("build.bed.none");
        int builder = Building.Builder(bed.BuiltBy);
        if (builder == 0) return loc.Get("build.bed.plain");
        if (builder == c.Id) return loc.Get(c.Id == w.PlayerId ? "build.bed.own_you" : "build.bed.own");
        return loc.Format("build.bed.built_by", new[] { new KeyValuePair<string, string>("who", EventText.ResolveMid(w, "c:" + builder.ToString(Inv))) });
    }

    /// <summary>The tile tooltip's lines for what Build put at a tile (build design 3.4): beds and who owns and built them, built walls and doors, plans, marks.</summary>
    private static void BuildLines(World w, Point p, List<string> contents)
    {
        LocTable loc = w.Content.Loc;
        KeyValuePair<string, string> Arg(string name, string value) => new(name, value);
        string Who(int id) => EventText.ResolveMid(w, "c:" + id.ToString(Inv));
        if (Building.StructureAt(w, p) is { } s)
        {
            BuildingDef def = w.Content.BuildingById(s.DefId);
            contents.Add(loc.Format("place.built_by", new[] { Arg("building", loc.Get(def.NameKey)), Arg("who", Who(Building.Builder(s.BuiltBy))) }));
            if (s.MarkedEvent != 0) contents.Add(loc.Get("place.to_take_down"));
        }
        if (Building.BedAt(w, p) is { } bed)
        {
            int builder = Building.Builder(bed.BuiltBy);
            bool mine = bed.OwnerId != 0 && bed.OwnerId == w.PlayerId;
            string owner = bed.OwnerId == 0 ? "" : EventText.Name(w, bed.OwnerId);
            contents.Add((bed.OwnerId, builder) switch
            {
                (0, 0) => loc.Get("place.bed"),
                (0, _) => loc.Format("place.bed_built", new[] { Arg("who", Who(builder)) }),
                (_, 0) => mine ? loc.Get("place.bed_owned_you") : loc.Format("place.bed_owned", new[] { Arg("owner", owner) }),
                _ => loc.Format(mine ? "place.bed_owned_built_you" : "place.bed_owned_built", new[] { Arg("owner", owner), Arg("who", Who(builder)) }),
            });
            if (bed.MarkedEvent != 0) contents.Add(loc.Get("place.to_take_down"));
        }
        foreach (RoofSection r in w.Map.Roof)
        {
            if (r.Removed || !r.Area.Contains(p)) continue;
            int builder = Building.Builder(r.BuiltBy);
            contents.Add(!r.Sound ? loc.Get("place.roof_broken")
                : builder != 0 ? loc.Format("place.roof_raised", new[] { Arg("who", Who(builder)) })
                : r.MendedBy != 0 ? loc.Format("place.roof_mended", new[] { Arg("who", Who(r.MendedBy)) })
                : loc.Get("place.roof_sound"));
            if (r.MarkedEvent != 0) contents.Add(loc.Get("place.to_take_down"));
        }
        foreach (BlueprintState bp in Blueprints(w))
        {
            if (bp.Pending || !new Rect(bp.X, bp.Y, bp.W, bp.H).Contains(p)) continue;
            BuildingDef def = w.Content.BuildingById(bp.BuildingId);
            contents.Add(loc.Format("place.planned", new[]
            {
                Arg("one", loc.Get(def.AKey)), Arg("here", bp.Delivered.ToString(Inv)), Arg("need", bp.Needed.ToString(Inv)),
                Arg("item", loc.Get(w.Content.ItemById(bp.Item).NameKey)), Arg("status", bp.Status),
            }));
        }
    }
}
