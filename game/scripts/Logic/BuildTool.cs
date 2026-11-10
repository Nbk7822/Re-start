using System;
using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Systems;
using SimPoint = Return.Sim.Core.Point;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game.Logic;

/// <summary>What the Build tool does with a gesture (build design 3.3 and 3.5).</summary>
public enum BuildMode
{
    None,

    /// <summary>Marking out a plan of one building.</summary>
    Place,

    /// <summary>Marking built things to be taken down, or leaving marked ones standing.</summary>
    TakeDown,

    /// <summary>Calling off plans.</summary>
    CallOff,
}

/// <summary>A plan piece a call-off gesture covers: a placed plan (<see cref="BlueprintId"/>), or one piece of a draft.</summary>
public readonly record struct PlanPiece(int BlueprintId, int DraftId, SimRect Area);

/// <summary>
/// The Build tool (build design 3.3 to 3.5 and 3.8), Godot-free so the tests compile it. It holds which
/// tool is in hand and the gesture in progress, and turns a finished gesture into what the player asked
/// for: a click places one piece, a drag a straight line along its longer axis, a Shift-drag the outline
/// of a rectangle (a room in one gesture), and a roof a rectangle split into sections of at most the
/// building's maxSide. Take down and call off cover what the gesture spans. Everything goes out through
/// the host's Order, so a gesture made while paused becomes a draft (<see cref="OrderDrafts"/>), and the
/// sim checks every plan again when it applies. Plans are intentions: nobody is told to build.
/// </summary>
public sealed class BuildTool
{
    private static readonly IReadOnlyList<SimRect> NoPieces = Array.Empty<SimRect>();

    public BuildMode Mode { get; private set; }

    /// <summary>The building being placed (a buildings.json id); "" unless placing.</summary>
    public string BuildingId { get; private set; } = "";

    /// <summary>Where the press began, while dragging.</summary>
    public SimPoint Anchor { get; private set; } = SimPoint.None;

    /// <summary>The tile under the pointer, or <see cref="SimPoint.None"/> when it is off the map or over a panel.</summary>
    public SimPoint Cursor { get; private set; } = SimPoint.None;

    public bool Dragging { get; private set; }

    /// <summary>Shift held: a drag places a rectangle's outline instead of a line.</summary>
    public bool Shift { get; set; }

    /// <summary>The cursor was put there by a check hook (RETURN_PRESS), not the mouse; real mouse movement takes over again.</summary>
    public bool Pinned { get; private set; }

    /// <summary>
    /// A gesture has just ended and the pointer has not left the tile it ended on: the hint keeps saying what that
    /// gesture did (not that the tile is now planned), and no new ghost is drawn over the plan just made.
    /// </summary>
    public bool Settled { get; private set; }

    private SimPoint _settledAt = SimPoint.None;
    private List<string> _settledHint = new();

    public bool Active => Mode != BuildMode.None;

    /// <summary>Starts placing a building.</summary>
    public void Place(string buildingId) => Begin(BuildMode.Place, buildingId);

    /// <summary>Starts marking built things to be taken down.</summary>
    public void TakeDown() => Begin(BuildMode.TakeDown, "");

    /// <summary>Starts calling off plans.</summary>
    public void CallOff() => Begin(BuildMode.CallOff, "");

    /// <summary>Puts the tool down (right-click or Esc).</summary>
    public void Stop()
    {
        Mode = BuildMode.None;
        BuildingId = "";
        Dragging = false;
        Anchor = SimPoint.None;
        Pinned = false;
        Settled = false;
    }

    private void Begin(BuildMode mode, string buildingId)
    {
        Mode = mode;
        BuildingId = buildingId;
        Dragging = false;
        Anchor = SimPoint.None;
        Settled = false;
    }

    /// <summary>The pointer is over this tile (None when off the map); <paramref name="pinned"/> when a check hook put it there.</summary>
    public void Hover(SimPoint tile, bool pinned = false)
    {
        if (tile != _settledAt) Settled = false;
        Cursor = tile;
        Pinned = pinned;
    }

    /// <summary>A press on a tile starts a gesture.</summary>
    public void Press(SimPoint tile)
    {
        if (!Active || tile == SimPoint.None) return;
        Anchor = tile;
        Cursor = tile;
        Dragging = true;
        Settled = false;
    }

    /// <summary>Drops the gesture in progress without doing anything.</summary>
    public void CancelDrag()
    {
        Dragging = false;
        Anchor = SimPoint.None;
    }

    /// <summary>The tiles the gesture spans: from the press to the pointer while dragging, else the tile under the pointer.</summary>
    public SimRect Area => Cursor == SimPoint.None ? new SimRect(0, 0, 0, 0) : Span(Dragging ? Anchor : Cursor, Cursor);

    /// <summary>A gesture that never left its first tile is a click.</summary>
    public bool IsClick => !Dragging || Anchor == Cursor;

    /// <summary>The pieces the gesture in hand would place (none unless placing a known building with the pointer on the map).</summary>
    public IReadOnlyList<SimRect> Pieces(World w)
    {
        if (Mode != BuildMode.Place || Cursor == SimPoint.None || w.Content.TryBuilding(BuildingId) is not { } def) return NoPieces;
        return PiecesFor(def, Dragging ? Anchor : Cursor, Cursor, Shift && Dragging);
    }

    /// <summary>
    /// How a gesture from <paramref name="a"/> to <paramref name="b"/> becomes pieces of a building: a roof
    /// section spans the rectangle, split into sections of at most maxSide; anything else is one tile per
    /// piece, along the drag's longer axis, or round the rectangle's edge when <paramref name="outline"/>.
    /// </summary>
    public static IReadOnlyList<SimRect> PiecesFor(BuildingDef def, SimPoint a, SimPoint b, bool outline)
    {
        if (def.Spans) return Split(Span(a, b), def.MaxSide);
        List<SimPoint> tiles = outline ? Outline(a, b) : Line(a, b);
        var pieces = new List<SimRect>(tiles.Count);
        foreach (SimPoint p in tiles) pieces.Add(new SimRect(p.X, p.Y, 1, 1));
        return pieces;
    }

    // ---- Geometry ----

    /// <summary>The rectangle with these two tiles at opposite corners.</summary>
    public static SimRect Span(SimPoint a, SimPoint b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X) + 1, Math.Abs(a.Y - b.Y) + 1);

    /// <summary>
    /// A straight line from <paramref name="a"/> along the drag's longer axis (east-west when the two are
    /// equal), as far as <paramref name="b"/> reaches along it, west to east or north to south.
    /// </summary>
    public static List<SimPoint> Line(SimPoint a, SimPoint b)
    {
        var tiles = new List<SimPoint>();
        if (Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y))
            for (int x = Math.Min(a.X, b.X); x <= Math.Max(a.X, b.X); x++) tiles.Add(new SimPoint(x, a.Y));
        else
            for (int y = Math.Min(a.Y, b.Y); y <= Math.Max(a.Y, b.Y); y++) tiles.Add(new SimPoint(a.X, y));
        return tiles;
    }

    /// <summary>The tiles round the edge of the rectangle from <paramref name="a"/> to <paramref name="b"/>, in (y, x) order.</summary>
    public static List<SimPoint> Outline(SimPoint a, SimPoint b)
    {
        SimRect r = Span(a, b);
        var tiles = new List<SimPoint>();
        for (int y = r.Y; y < r.Y + r.H; y++)
            for (int x = r.X; x < r.X + r.W; x++)
                if (y == r.Y || y == r.Y + r.H - 1 || x == r.X || x == r.X + r.W - 1) tiles.Add(new SimPoint(x, y));
        return tiles;
    }

    /// <summary>
    /// A rectangle split into sections no side of which is longer than <paramref name="maxSide"/>, as evenly
    /// as it goes (a 6-wide drag makes two sections of 3, not 4 and 2), in (y, x) order. A roof plan is one
    /// piece per section (build design 3.3).
    /// </summary>
    public static List<SimRect> Split(SimRect area, int maxSide)
    {
        var sections = new List<SimRect>();
        if (area.W <= 0 || area.H <= 0) return sections;
        foreach ((int y, int h) in Cuts(area.Y, area.H, Math.Max(1, maxSide)))
            foreach ((int x, int w) in Cuts(area.X, area.W, Math.Max(1, maxSide)))
                sections.Add(new SimRect(x, y, w, h));
        return sections;
    }

    private static List<(int Start, int Size)> Cuts(int start, int length, int max)
    {
        int n = (length + max - 1) / max, size = length / n, extra = length % n;
        var cuts = new List<(int, int)>(n);
        for (int i = 0, at = start; i < n; i++)
        {
            int s = size + (i < extra ? 1 : 0);
            cuts.Add((at, s));
            at += s;
        }
        return cuts;
    }

    /// <summary>Does the area cover every tile of the piece?</summary>
    public static bool Covers(SimRect area, SimRect piece) =>
        piece.X >= area.X && piece.Y >= area.Y && piece.X + piece.W <= area.X + area.W && piece.Y + piece.H <= area.Y + area.H;

    private static bool Overlaps(SimRect a, SimRect b) =>
        a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;

    private static SimPoint Origin(SimRect r) => new(r.X, r.Y);

    /// <summary>
    /// Is a piece in a take-down or call-off gesture? A one-tile piece (a wall, a door, a bed) when its tile
    /// is in the area. A roof section is overhead, so it counts only when the gesture covers all of it, or when
    /// a click lands on it where nothing smaller is (lead's call: a click on a bed under a new roof takes the
    /// bed, not the roof over it).
    /// </summary>
    private static bool InGesture(SimRect area, bool click, SimRect piece, bool spans, bool tileTargetHere) =>
        !spans ? area.Contains(Origin(piece))
        : Covers(area, piece) || (click && !tileTargetHere && piece.Contains(Origin(area)));

    // ---- What a gesture covers ----

    /// <summary>The plans a call-off gesture covers: placed plans (unless already being called off), then the drafts' pieces.</summary>
    public static List<PlanPiece> PlansIn(World w, OrderDrafts drafts, SimRect area, bool click)
    {
        var tilePieces = new List<PlanPiece>();
        var spanning = new List<PlanPiece>();
        foreach (Blueprint bp in w.Blueprints)
        {
            if (drafts.CallingOff(bp.Id) || !Overlaps(area, bp.Area)) continue;
            bool spans = w.Content.TryBuilding(bp.DefId) is { Spans: true };
            (spans ? spanning : tilePieces).Add(new PlanPiece(bp.Id, 0, bp.Area));
        }
        foreach ((int id, PlaceBlueprint plan) in drafts.Placements)
        {
            bool spans = w.Content.TryBuilding(plan.BuildingId) is { Spans: true };
            foreach (SimRect piece in plan.Pieces)
                if (Overlaps(area, piece)) (spans ? spanning : tilePieces).Add(new PlanPiece(0, id, piece));
        }
        var hit = new List<PlanPiece>();
        foreach (PlanPiece p in tilePieces)
            if (InGesture(area, click, p.Area, false, false)) hit.Add(p);
        bool tileHere = hit.Count > 0;
        foreach (PlanPiece p in spanning)
            if (InGesture(area, click, p.Area, true, tileHere)) hit.Add(p);
        return hit;
    }

    /// <summary>The built things a take-down gesture covers that may be taken down (not the old camp's stone or roof), in id order by kind.</summary>
    public static List<BuiltRef> BuiltIn(World w, SimRect area, bool click)
    {
        var hit = new List<BuiltRef>();
        foreach (Structure s in w.Map.Structures)
            if (!s.Removed && area.Contains(s.Pos) && Building.Markable(w, new BuiltRef(TargetKind.Structure, s.Id), out _, out _))
                hit.Add(new BuiltRef(TargetKind.Structure, s.Id));
        foreach (Bed b in w.Map.Beds)
            if (area.Contains(b.Pos) && Building.Markable(w, new BuiltRef(TargetKind.Bed, b.Id), out _, out _))
                hit.Add(new BuiltRef(TargetKind.Bed, b.Id));
        bool tileHere = hit.Count > 0;
        foreach (RoofSection r in w.Map.Roof)
            if (!r.Removed && InGesture(area, click, r.Area, true, tileHere) && Building.Markable(w, new BuiltRef(TargetKind.Roof, r.Id), out _, out _))
                hit.Add(new BuiltRef(TargetKind.Roof, r.Id));
        return hit;
    }

    /// <summary>Is this built thing marked to be taken down, counting a draft's mark or unmark over the world's?</summary>
    public static bool IsMarked(World w, OrderDrafts drafts, BuiltRef t) =>
        drafts.Marked(t) ?? (Building.Markable(w, t, out _, out long marked) && marked != 0);

    /// <summary>
    /// What a take-down gesture does with the things it covers: when every one is already marked, it leaves them
    /// standing (a click on a marked thing unmarks it); otherwise it marks the ones not yet marked.
    /// </summary>
    public static (List<BuiltRef> Targets, bool Mark) TakeDownChoice(World w, OrderDrafts drafts, List<BuiltRef> covered)
    {
        var unmarked = covered.FindAll(t => !IsMarked(w, drafts, t));
        return unmarked.Count > 0 ? (unmarked, true) : (covered, false);
    }

    // ---- Finishing a gesture ----

    /// <summary>
    /// Ends the gesture in progress and does what it asked: places a plan (if any piece can go), calls off the
    /// plans it covers (a draft's pieces are simply dropped; placed plans go out as one CancelBlueprints), or
    /// marks or unmarks what it covers to be taken down. Commands go through <paramref name="order"/> (the host's
    /// Order, which keeps them as drafts while paused). True when it asked for anything.
    /// </summary>
    public bool Release(World w, OrderDrafts drafts, Action<Command> order)
    {
        if (!Dragging || Cursor == SimPoint.None)
        {
            CancelDrag();
            return false;
        }
        IReadOnlyList<SimRect> pieces = Pieces(w);
        SimRect area = Area;
        bool click = IsClick;
        _settledHint = new List<string>(Hint(w, drafts, paused: false));
        _settledAt = Cursor;
        Settled = true;
        CancelDrag();
        switch (Mode)
        {
            case BuildMode.Place:
            {
                if (pieces.Count == 0) return false;
                CanPlaceState check = StateApi.CanPlace(w, BuildingId, pieces, OrderDrafts.Planned(w, drafts));
                if (OkCount(check) == 0) return false; // nothing could go: the hint said why; no trace
                order(new PlaceBlueprint(w.Tick, BuildingId, pieces));
                return true;
            }
            case BuildMode.CallOff:
            {
                List<PlanPiece> plans = PlansIn(w, drafts, area, click);
                var placed = new List<int>();
                foreach (PlanPiece p in plans)
                    if (p.BlueprintId != 0) placed.Add(p.BlueprintId);
                    else drafts.CallOff(p.DraftId, p.Area);
                if (placed.Count > 0) order(new CancelBlueprints(w.Tick, placed));
                return plans.Count > 0;
            }
            case BuildMode.TakeDown:
            {
                List<BuiltRef> covered = BuiltIn(w, area, click);
                if (covered.Count == 0) return false;
                (List<BuiltRef> targets, bool mark) = TakeDownChoice(w, drafts, covered);
                order(new MarkTakeDown(w.Tick, targets, mark));
                return true;
            }
            default:
                return false;
        }
    }

    // ---- The hint by the cursor (build design 3.3) ----

    /// <summary>
    /// The lines of the hint by the cursor: for a plan, what it marks and costs against the stock (and that people
    /// will gather the rest), why a tile cannot take it, and how to use the tool; for take down and call off, what
    /// the gesture would do and what is here. Paused, it says the plan waits for time to move.
    /// </summary>
    public IReadOnlyList<string> Hint(World w, OrderDrafts drafts, bool paused)
    {
        var lines = new List<string>();
        if (!Active || Cursor == SimPoint.None) return lines;
        if (Settled) lines.AddRange(_settledHint);
        else switch (Mode)
        {
            case BuildMode.Place:
                PlaceHint(w, drafts, lines);
                break;
            case BuildMode.TakeDown:
            {
                List<BuiltRef> covered = BuiltIn(w, Area, IsClick);
                if (covered.Count == 0) lines.Add(Ui.T(OldCampHere(w) ? "ui.build.take_down_old" : "ui.build.take_down_none"));
                else
                {
                    (List<BuiltRef> targets, bool mark) = TakeDownChoice(w, drafts, covered);
                    lines.Add(Ui.F(mark ? "ui.build.take_down_n" : "ui.build.leave_n", ("n", Ui.N(targets.Count))));
                }
                if (!Dragging) lines.AddRange(StateApi.Location(w, Cursor.X, Cursor.Y).Contents);
                lines.Add(Ui.T("ui.build.take_down_usage"));
                break;
            }
            case BuildMode.CallOff:
            {
                int n = PlansIn(w, drafts, Area, IsClick).Count;
                lines.Add(n == 0 ? Ui.T("ui.build.call_off_none") : Ui.F("ui.build.call_off_n", ("n", Ui.N(n))));
                lines.Add(Ui.T("ui.build.call_off_usage"));
                break;
            }
        }
        if (paused && Mode != BuildMode.None) lines.Add(Ui.T("ui.build.pending"));
        return lines;
    }

    private void PlaceHint(World w, OrderDrafts drafts, List<string> lines)
    {
        if (w.Content.TryBuilding(BuildingId) is not { } def) return;
        string name = w.Content.Loc.Get(def.NameKey);
        CanPlaceState check = StateApi.CanPlace(w, BuildingId, Pieces(w), OrderDrafts.Planned(w, drafts));
        int marked = OkCount(check);
        if (marked == 0) lines.Add(Ui.F("ui.build.hint_none", ("building", name)));
        else
        {
            string item = w.Content.Loc.Get(w.Content.ItemById(check.Item).NameKey);
            var args = new[] { ("building", name), ("n", Ui.N(marked)), ("cost", Ui.N(check.Cost)), ("item", item), ("store", Ui.N(check.InStore)), ("rest", RestText(check.Item)) };
            lines.Add(Ui.F(check.Cost > check.InStore ? "ui.build.hint_short" : "ui.build.hint", args));
        }
        string reason = Reason(check);
        if (reason.Length > 0) lines.Add(Ui.F("ui.build.cannot", ("reason", reason)));
        lines.Add(Ui.T(def.Spans ? "ui.build.usage_roof" : "ui.build.usage_tiles"));
    }

    /// <summary>How many pieces of a checked plan can go.</summary>
    public static int OkCount(CanPlaceState check)
    {
        int n = 0;
        foreach (PieceState p in check.Pieces)
            if (p.Ok) n++;
        return n;
    }

    /// <summary>"people will fell trees for the rest" for timber, from ui.build.rest.&lt;item&gt; when there is one.</summary>
    private static string RestText(string item) => Ui.Has("ui.build.rest." + item) ? Ui.T("ui.build.rest." + item) : Ui.T("ui.build.rest");

    /// <summary>Why the tile under the cursor cannot take the plan, else the first tile that cannot; "" when every tile can.</summary>
    private string Reason(CanPlaceState check)
    {
        string first = "";
        foreach (PieceState p in check.Pieces)
            foreach (TileVerdictState t in p.Tiles)
            {
                if (t.Ok) continue;
                if (t.X == Cursor.X && t.Y == Cursor.Y) return t.Reason;
                if (first.Length == 0) first = t.Reason;
            }
        return first;
    }

    /// <summary>
    /// The tooltip's lines for what is drafted at a tile while paused, which the world does not hold yet: a drafted
    /// plan's piece, and a drafted mark or unmark of something built there.
    /// </summary>
    public static List<string> DraftLines(World w, OrderDrafts drafts, int x, int y)
    {
        var lines = new List<string>();
        var p = new SimPoint(x, y);
        foreach ((int _, PlaceBlueprint plan) in drafts.Placements)
        {
            if (w.Content.TryBuilding(plan.BuildingId) is not { } def) continue;
            foreach (SimRect r in plan.Pieces)
                if (r.Contains(p))
                {
                    lines.Add(Ui.F("ui.tooltip.draft_plan", ("one", w.Content.Loc.Get(def.AKey))));
                    break;
                }
        }
        var here = new List<BuiltRef>();
        if (Building.StructureAt(w, p) is { } s) here.Add(new BuiltRef(TargetKind.Structure, s.Id));
        if (Building.BedAt(w, p) is { } bed) here.Add(new BuiltRef(TargetKind.Bed, bed.Id));
        foreach (RoofSection r in w.Map.Roof)
            if (!r.Removed && r.Area.Contains(p)) here.Add(new BuiltRef(TargetKind.Roof, r.Id));
        foreach (BuiltRef t in here)
            if (drafts.Marked(t) is bool marked) lines.Add(Ui.T(marked ? "ui.tooltip.draft_take_down" : "ui.tooltip.draft_leave"));
        return lines;
    }

    /// <summary>Is the old camp's stone or roof under the cursor (which cannot be taken down until there is stone to work)?</summary>
    private bool OldCampHere(World w)
    {
        Map m = w.Map;
        if (!m.InBounds(Cursor)) return false;
        if (m.At(Cursor) is TerrainKind.Wall or TerrainKind.Door && Building.StructureAt(w, Cursor) == null) return true;
        foreach (RoofSection r in m.Roof)
            if (!r.Removed && r.BuiltBy.Count == 0 && r.Area.Contains(Cursor)) return true;
        return false;
    }
}
