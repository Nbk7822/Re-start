using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>One tile of a planned piece, with why it cannot be built there ("" when it can): a build.reason.* loc key.</summary>
public sealed record TileVerdict(Point Tile, string Reason)
{
    public bool Ok => Reason.Length == 0;
}

/// <summary>One piece of a plan (a tile, or a roof section), whether it can be placed, each tile's verdict, and what it costs.</summary>
public sealed record PieceVerdict(Rect Area, bool Ok, IReadOnlyList<TileVerdict> Tiles, int Cost)
{
    /// <summary>The first reason a tile gives, or "" if every tile is fine.</summary>
    public string Reason
    {
        get
        {
            foreach (TileVerdict t in Tiles)
                if (!t.Ok) return t.Reason;
            return "";
        }
    }
}

/// <summary>A piece some other plan would put somewhere, for checking a whole set of plans before any is placed (the stand-in player's hut).</summary>
public readonly record struct PlannedPiece(string BuildingId, Rect Area);

/// <summary>
/// Placement rules (build design 4.4). One function answers for each tile with a reason key; the view's
/// ghost (through StateApi.CanPlace), each command's Apply and the stand-in player all use it. Pieces in
/// one call are checked in order, each against the world, the plans already placed (but those being called
/// off: <c>exceptPlans</c>), any hypothetical <see cref="PlannedPiece"/>s, and the pieces before it that passed.
/// </summary>
public static partial class Building
{
    public const string ReasonUnknown = "build.reason.unknown";
    public const string ReasonOffMap = "build.reason.off_map";
    public const string ReasonTerrain = "build.reason.terrain";
    public const string ReasonCamp = "build.reason.camp";
    public const string ReasonTree = "build.reason.tree";
    public const string ReasonBush = "build.reason.bush";
    public const string ReasonBody = "build.reason.body";
    public const string ReasonPlanned = "build.reason.planned";
    public const string ReasonBuilt = "build.reason.built";
    public const string ReasonRoofed = "build.reason.roofed";
    public const string ReasonUnderRoof = "build.reason.under_roof";
    public const string ReasonWallBelow = "build.reason.wall_below";
    public const string ReasonTooFar = "build.reason.too_far";
    public const string ReasonTooBig = "build.reason.too_big";
    public const string ReasonDoorFrame = "build.reason.door_frame";

    /// <param name="exceptPlans">Placed plans to treat as gone: ones asked to be called off but not yet (a view's drafts, or a call-off queued this tick), whose tiles a new plan may take.</param>
    public static IReadOnlyList<PieceVerdict> Check(World w, string buildingId, IReadOnlyList<Rect> pieces, IReadOnlyList<PlannedPiece>? alsoPlanned = null,
        IReadOnlyCollection<int>? exceptPlans = null)
    {
        var verdicts = new List<PieceVerdict>(pieces.Count);
        BuildingDef? def = w.Content.TryBuilding(buildingId);
        if (def == null)
        {
            foreach (Rect r in pieces) verdicts.Add(new PieceVerdict(r, false, new[] { new TileVerdict(new Point(r.X, r.Y), ReasonUnknown) }, 0));
            return verdicts;
        }

        // Every plan a new piece must not clash with: those placed (but those being called off), the hypothetical ones, and the pieces before it here.
        var plans = new List<(string Kind, Rect Area)>();
        foreach (Blueprint bp in w.Blueprints)
            if ((exceptPlans == null || !exceptPlans.Contains(bp.Id)) && w.Content.TryBuilding(bp.DefId) is { } other) plans.Add((other.Kind, bp.Area));
        if (alsoPlanned != null)
            foreach (PlannedPiece p in alsoPlanned)
                if (w.Content.TryBuilding(p.BuildingId) is { } other) plans.Add((other.Kind, p.Area));

        foreach (Rect area in pieces)
        {
            var tiles = new List<TileVerdict>(Math.Max(1, area.W * area.H));
            bool tooBig = def.Spans ? area.W > def.MaxSide || area.H > def.MaxSide : area.W != 1 || area.H != 1;
            bool ok = area.W >= 1 && area.H >= 1;
            for (int y = area.Y; y < area.Y + Math.Max(1, area.H); y++)
                for (int x = area.X; x < area.X + Math.Max(1, area.W); x++)
                {
                    var p = new Point(x, y);
                    string reason = tooBig ? ReasonTooBig : TileReason(w, def, p, plans);
                    if (reason.Length > 0) ok = false;
                    tiles.Add(new TileVerdict(p, reason));
                }
            verdicts.Add(new PieceVerdict(area, ok, tiles, def.Cost(area)));
            if (ok) plans.Add((def.Kind, area));
        }
        return verdicts;
    }

    /// <summary>The pieces of a plan that can be placed now, in order.</summary>
    public static List<Rect> Placeable(World w, string buildingId, IReadOnlyList<Rect> pieces, IReadOnlyList<PlannedPiece>? alsoPlanned = null,
        IReadOnlyCollection<int>? exceptPlans = null)
    {
        var ok = new List<Rect>();
        foreach (PieceVerdict v in Check(w, buildingId, pieces, alsoPlanned, exceptPlans))
            if (v.Ok) ok.Add(v.Area);
        return ok;
    }

    private static string TileReason(World w, BuildingDef def, Point p, List<(string Kind, Rect Area)> plans)
    {
        Map m = w.Map;
        if (!m.InBounds(p)) return ReasonOffMap;
        TerrainKind ground = m.At(p);
        if (ground is TerrainKind.Wall or TerrainKind.Door) return def.Kind == Roof ? ReasonWallBelow : ReasonBuilt;
        if (!def.PlaceOn.Contains(TerrainName(ground))) return ReasonTerrain;
        if (p == m.Hearth || p == m.WatchPost || m.Stockpile.Contains(p) || m.CoveredRuins.Contains(p)) return ReasonCamp;
        if (m.TreeAt(p) != null) return ReasonTree;
        if (m.BushAt(p) != null) return ReasonBush;
        if (BodyAt(w, p)) return ReasonBody;

        // Built things: a roof is overhead, so it may go over a bed, and a bed under a roof.
        if (def.Kind != Roof && BedAt(w, p) != null) return ReasonBuilt;
        if (def.Kind != Bed && StandingRoofOver(w, p) != null) return def.Kind == Roof ? ReasonRoofed : ReasonUnderRoof;

        foreach ((string kind, Rect area) in plans)
        {
            if (!area.Contains(p)) continue;
            string clash = Clash(def.Kind, kind);
            if (clash.Length > 0) return clash;
        }

        if (def.Kind == Roof && !NearWall(w, p, def.SupportRange, plans, standingOnly: false)) return ReasonTooFar;
        if (def.Kind == Door && !Framed(w, p, plans)) return ReasonDoorFrame;
        return "";
    }

    /// <summary>Why a new piece of one kind cannot share a tile with a plan of another ("" if it can: a roof over a bed).</summary>
    private static string Clash(string newKind, string planKind) => (newKind, planKind) switch
    {
        (Roof, Roof) => ReasonRoofed,
        (Roof, Bed) or (Bed, Roof) => "",
        (Roof, _) => ReasonWallBelow,
        (_, Roof) => ReasonUnderRoof,
        _ => ReasonPlanned,
    };

    /// <summary>A wall or door stands (or, unless standingOnly, is planned) within range of p (Chebyshev).</summary>
    internal static bool NearWall(World w, Point p, int range, List<(string Kind, Rect Area)>? plans, bool standingOnly)
    {
        Map m = w.Map;
        for (int y = p.Y - range; y <= p.Y + range; y++)
            for (int x = p.X - range; x <= p.X + range; x++)
            {
                var q = new Point(x, y);
                if (m.InBounds(q) && m.At(q) is TerrainKind.Wall or TerrainKind.Door) return true;
            }
        if (standingOnly || plans == null) return false;
        foreach ((string kind, Rect area) in plans)
        {
            if (kind is not (Wall or Door)) continue;
            var q = new Point(area.X, area.Y);
            if (q.ChebyshevTo(p) <= range) return true;
        }
        return false;
    }

    /// <summary>A door needs a wall, standing or planned, on two opposite sides.</summary>
    private static bool Framed(World w, Point p, List<(string Kind, Rect Area)> plans)
    {
        bool WallAt(int dx, int dy)
        {
            var q = new Point(p.X + dx, p.Y + dy);
            if (w.Map.InBounds(q) && w.Map.At(q) == TerrainKind.Wall) return true;
            foreach ((string kind, Rect area) in plans)
                if (kind == Wall && area.Contains(q)) return true;
            return false;
        }
        return (WallAt(0, -1) && WallAt(0, 1)) || (WallAt(-1, 0) && WallAt(1, 0));
    }

    /// <summary>The buildings.json name of a terrain ("grass", "soil", "floor"; anything else cannot be built on).</summary>
    private static string TerrainName(TerrainKind t) => t switch
    {
        TerrainKind.Grass => "grass",
        TerrainKind.Soil => "soil",
        TerrainKind.Floor => "floor",
        _ => "",
    };

    private static bool BodyAt(World w, Point p)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Dead && c.Pos == p) return true;
        return false;
    }
}
