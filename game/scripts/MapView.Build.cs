using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Art;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Systems;
using SimPoint = Return.Sim.Core.Point;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game;

/// <summary>
/// The Build menu on the map (build design 3.3, 3.4 and 3.8; ADR-0027). In the lit world: beds (their
/// blanket the owner's cloth), and plans under way (logs on the site, walls rising, rafters then boards).
/// On the unshaded overlay, so they read at night: plans not yet begun as pale ochre dashed outlines with a
/// faint silhouette, drafts made while paused paler and captioned, what cannot go crossed out in red, and
/// things marked to be taken down outlined in red dashes. The tool in hand takes its gestures from here.
/// </summary>
public partial class MapView
{
    private static readonly Color PlanInk = new(0.88f, 0.74f, 0.46f);
    private static readonly Color BadInk = new(0.92f, 0.33f, 0.26f);
    private static readonly Color AreaInk = new(0.93f, 0.9f, 0.83f);

    /// <summary>A blanket nobody has claimed: undyed wool.</summary>
    private static readonly Color Undyed = Palette.Hex(0x8a7d62);

    private int _artRevision;
    private bool[] _planWalls = Array.Empty<bool>();
    private Func<int, int, bool>? _planJoins;
    private readonly List<(Vector2 At, string Text)> _captions = new();

    /// <summary>
    /// Follows what is built: the world art (wall map, grass, roofs) is made again whenever the map's revision
    /// moves. Keeps the tool's gesture honest: a drag whose release landed on a panel ends where the pointer is.
    /// </summary>
    private void UpdateBuild(World w)
    {
        if (w.Map.Revision != _artRevision)
        {
            _artRevision = w.Map.Revision;
            _art = new WorldArt(w.Map, _tex);
        }
        BuildTool tool = _host!.Build;
        if (!tool.Active) return;
        if (tool.Dragging)
        {
            tool.Shift = Input.IsKeyPressed(Key.Shift);
            if (!Input.IsMouseButtonPressed(MouseButton.Left)) _host!.FinishGesture();
        }
        else if (!tool.Pinned && GetViewport().GuiGetHoveredControl() != null) tool.Hover(SimPoint.None);
    }

    /// <summary>With a Build tool in hand: a left press starts a gesture, the release ends it, right-click puts the tool down.</summary>
    private bool BuildInput(InputEvent e)
    {
        BuildTool tool = _host!.Build;
        if (!tool.Active) return false;
        switch (e)
        {
            case InputEventMouseMotion:
                tool.Hover(MouseTile(clamp: tool.Dragging));
                return false;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
            {
                SimPoint tile = MouseTile(clamp: false);
                if (tile == SimPoint.None) return true;
                tool.Shift = press.ShiftPressed;
                tool.Hover(tile);
                tool.Press(tile);
                return true;
            }
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release:
                if (!tool.Dragging) return true;
                tool.Shift = release.ShiftPressed;
                tool.Hover(MouseTile(clamp: true));
                _host!.FinishGesture();
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                tool.Stop();
                return true;
            default:
                return false;
        }
    }

    /// <summary>The tile under the mouse; off the map it is none, or (while dragging) the nearest edge tile.</summary>
    private SimPoint MouseTile(bool clamp)
    {
        Vector2I t = TileUnderMouse();
        if (InBounds(t)) return new SimPoint(t.X, t.Y);
        if (!clamp) return SimPoint.None;
        Map m = _host!.World.Map;
        return new SimPoint(Math.Clamp(t.X, 0, m.Width - 1), Math.Clamp(t.Y, 0, m.Height - 1));
    }

    /// <summary>A tile's centre on screen (the overlay's space), for the hint by the cursor.</summary>
    public Vector2 ScreenOfTile(SimPoint p) => GetGlobalTransformWithCanvas() * TileCenter(p);

    /// <summary>On-screen size of one tile.</summary>
    public float TileOnScreen => TileSize * GetGlobalTransformWithCanvas().X.Length();

    // ================= the lit world: beds and plans under way =================

    private void CollectBuild(World w)
    {
        Map m = w.Map;
        for (int i = 0; i < m.Beds.Count; i++) _drawables.Add(new Drawable(WorldArt.BedSortY(m.Beds[i].Pos), Kind.Bed, i, 0.2f));
        if (_planWalls.Length != m.Width * m.Height) _planWalls = new bool[m.Width * m.Height];
        else Array.Clear(_planWalls);
        for (int i = 0; i < w.Blueprints.Count; i++)
        {
            Blueprint bp = w.Blueprints[i];
            if (w.Content.TryBuilding(bp.DefId) is not { } def) continue;
            var at = new SimPoint(bp.Area.X, bp.Area.Y);
            if (def.Kind is Building.Wall or Building.Door && m.InBounds(at)) _planWalls[m.Index(at)] = true;
            if (bp.Progress > 0)
            {
                float y = def.Spans ? _art.RoofSortY(bp.Area) : def.Kind == Building.Bed ? WorldArt.BedSortY(at) : WorldArt.WallSortY(at);
                _drawables.Add(new Drawable(y, Kind.Plan, i, def.Spans ? 2f : def.Kind == Building.Bed ? 0.2f : 1f));
            }
            else if (bp.Delivered > 0 && Building.SiteTile(w, bp.Area) is var site && site != SimPoint.None)
                _drawables.Add(new Drawable((site.Y + 0.7f) * TileSize, Kind.PlanLogs, i, 0f));
        }
    }

    private void PaintBuild(World w, Drawable d)
    {
        Map m = w.Map;
        switch (d.Kind)
        {
            case Kind.Bed:
            {
                Bed bed = m.Beds[d.Index];
                WorldArt.DrawBed(_brush, bed.Pos, bed.BuiltBy.Count > 0 || bed.DefId.Length > 0, BlanketOf(w, bed));
                break;
            }
            case Kind.Plan:
            {
                Blueprint bp = w.Blueprints[d.Index];
                BuildingDef def = w.Content.BuildingById(bp.DefId);
                float done = Math.Clamp(bp.Progress / (float)Math.Max(1, def.WorkFor(bp.Area)), 0f, 1f);
                var at = new SimPoint(bp.Area.X, bp.Area.Y);
                switch (def.Kind)
                {
                    case Building.Wall: _art.DrawTimberWall(_brush, at, done, _planJoins ??= PlanJoins); break;
                    case Building.Door: _art.DrawTimberDoor(_brush, at, done); break;
                    case Building.Bed: WorldArt.DrawBed(_brush, at, true, Undyed, done); break;
                    case Building.Roof: _art.DrawRoofFrame(_brush, bp.Area, done, RoofAlpha(bp.Id), bp.Id); break;
                }
                break;
            }
            case Kind.PlanLogs:
            {
                Blueprint bp = w.Blueprints[d.Index];
                SimPoint site = Building.SiteTile(w, bp.Area);
                WorldArt.DrawLogPile(_brush, (site.X + 0.5f) * TileSize, (site.Y + 0.62f) * TileSize, bp.Delivered, bp.Id);
                break;
            }
        }
    }

    /// <summary>A wall going up runs into walls and doors that stand and into the plan's other walls and doors.</summary>
    private bool PlanJoins(int x, int y)
    {
        Map m = _host!.World.Map;
        var p = new SimPoint(x, y);
        return m.InBounds(p) && (m.At(p) is TerrainKind.Wall or TerrainKind.Door || _planWalls[m.Index(p)]);
    }

    /// <summary>A bed's blanket: its owner's cloth (as the owner is drawn, moonlit at night), or undyed wool when it is nobody's.</summary>
    private Color BlanketOf(World w, Bed bed)
    {
        if (bed.OwnerId == 0 || w.Find(bed.OwnerId) is not { } owner) return Undyed;
        Color cloth = _shown.TryGetValue(owner.Id, out Shown? s) && s.HasLook ? s.BaseLook.Cloth : ClothOf(owner);
        return cloth.Lerp(Moon, 0.32f * _light.Night);
    }

    /// <summary>Someone's cloth, darkened and muted as their figure wears it (LookOf).</summary>
    private Color ClothOf(Character c)
    {
        Look look = _looks.For(c.CastKey);
        Rgb cloth = look.Cloth ?? CastRgb(c.CastKey) ?? new Rgb(0.45f, 0.43f, 0.4f);
        return Palette.Of(cloth.Muted(0.8f, 0.25f));
    }

    // ================= the overlay: plan marks =================

    /// <summary>
    /// Plans and marks above the night's tint: placed plans (dashed until work begins, then a thin solid line; red
    /// when a builder refused to shut people in), placements queued this tick, drafts (paler, captioned "takes
    /// effect when time moves", what cannot go crossed out), things marked to be taken down (red dashes), and
    /// what the tool in hand would do.
    /// </summary>
    private void PaintPlanMarks(World w)
    {
        _captions.Clear();
        OrderDrafts drafts = _host!.Drafts;
        BuildTool tool = _host.Build;
        foreach (Blueprint bp in w.Blueprints)
        {
            if (drafts.CallingOff(bp.Id) || w.Content.TryBuilding(bp.DefId) is not { } def) continue;
            bool shutIn = bp.Blocked == BlockedReason.WouldShutIn && bp.BlockedRevision == w.Map.Revision;
            Color ink = shutIn ? BadInk : PlanInk;
            Rect2 r = ScreenRect(PieceRect(bp.Area));
            if (bp.Progress > 0) _brush.RectOutline(r, ink.WithAlpha(0.55f), 1f);
            else
            {
                Ghost(def, bp.Area, ink.WithAlpha(0.15f));
                Dashes(r, ink.WithAlpha(0.9f));
            }
        }

        List<PlannedPiece> planned = OrderDrafts.Queued(w);
        foreach (Command c in w.PendingCommands)
            if (c is PlaceBlueprint queued && w.Content.TryBuilding(queued.BuildingId) != null)
                foreach (SimRect piece in queued.Pieces) Dashes(ScreenRect(PieceRect(piece)), PlanInk.WithAlpha(0.9f));

        // Drafts, paler; one caption over the topmost of them says they wait for time to move.
        Rect2? first = null;
        foreach ((int _, PlaceBlueprint plan) in drafts.Placements)
        {
            if (w.Content.TryBuilding(plan.BuildingId) is not { } def) continue;
            CanPlaceState check = StateApi.CanPlace(w, plan.BuildingId, plan.Pieces, planned);
            MarkCheck(def, check, 0.5f);
            foreach (PieceState p in check.Pieces)
            {
                if (!p.Ok) continue;
                var area = new SimRect(p.X, p.Y, p.W, p.H);
                planned.Add(new PlannedPiece(plan.BuildingId, area));
                Rect2 r = ScreenRect(PieceRect(area));
                if (first == null || r.Position.Y < first.Value.Position.Y || (r.Position.Y == first.Value.Position.Y && r.Position.X < first.Value.Position.X)) first = r;
            }
        }
        if (first is Rect2 at) _captions.Add((at.Position + new Vector2(0f, -3f), Ui.T("ui.build.pending")));

        MarkedToTakeDown(w, drafts);

        if (!tool.Active || tool.Cursor == SimPoint.None || tool.Settled) return;
        switch (tool.Mode)
        {
            case BuildMode.Place when w.Content.TryBuilding(tool.BuildingId) is { } def:
                MarkCheck(def, StateApi.CanPlace(w, tool.BuildingId, tool.Pieces(w), planned), 1f);
                break;
            case BuildMode.TakeDown:
            {
                (List<BuiltRef> targets, bool mark) = BuildTool.TakeDownChoice(w, drafts, BuildTool.BuiltIn(w, tool.Area, tool.IsClick));
                foreach (BuiltRef t in targets)
                    if (BuiltRect(w, t) is Rect2 r) Dashes(r, (mark ? BadInk : PlanInk).WithAlpha(1f), 1.6f);
                if (tool.Dragging) Dashes(ScreenRect(TilesRect(tool.Area)), AreaInk.WithAlpha(0.7f));
                break;
            }
            case BuildMode.CallOff:
            {
                foreach (PlanPiece p in BuildTool.PlansIn(w, drafts, tool.Area, tool.IsClick)) Cross(ScreenRect(PieceRect(p.Area)), BadInk);
                if (tool.Dragging) Dashes(ScreenRect(TilesRect(tool.Area)), AreaInk.WithAlpha(0.7f));
                break;
            }
        }
    }

    /// <summary>A checked plan: each piece that can go as a faint silhouette in a dashed outline, each tile that cannot crossed out (a roof section that cannot go, outlined in red).</summary>
    private void MarkCheck(BuildingDef def, CanPlaceState check, float strength)
    {
        foreach (PieceState p in check.Pieces)
        {
            var area = new SimRect(p.X, p.Y, p.W, p.H);
            if (p.Ok)
            {
                Ghost(def, area, PlanInk.WithAlpha(0.16f * strength));
                Dashes(ScreenRect(PieceRect(area)), PlanInk.WithAlpha(0.95f * strength));
                continue;
            }
            if (def.Spans) Dashes(ScreenRect(PieceRect(area)), BadInk.WithAlpha(0.8f * strength));
            foreach (TileVerdictState t in p.Tiles)
                if (!t.Ok) Cross(ScreenRect(PieceRect(new SimRect(t.X, t.Y, 1, 1))), BadInk.WithAlpha(Math.Max(0.6f, strength)));
        }
    }

    /// <summary>Everything marked to be taken down, in the world or by a draft, outlined in red dashes (a draft's unmark clears it).</summary>
    private void MarkedToTakeDown(World w, OrderDrafts drafts)
    {
        Map m = w.Map;
        foreach (Structure s in m.Structures)
            if (!s.Removed) MarkIfMarked(w, drafts, new BuiltRef(TargetKind.Structure, s.Id), s.MarkedEvent);
        foreach (Bed b in m.Beds) MarkIfMarked(w, drafts, new BuiltRef(TargetKind.Bed, b.Id), b.MarkedEvent);
        foreach (RoofSection r in m.Roof)
            if (!r.Removed) MarkIfMarked(w, drafts, new BuiltRef(TargetKind.Roof, r.Id), r.MarkedEvent);
    }

    private void MarkIfMarked(World w, OrderDrafts drafts, BuiltRef t, long markedEvent)
    {
        bool? drafted = drafts.Marked(t);
        if (!(drafted ?? markedEvent != 0) || BuiltRect(w, t) is not Rect2 r) return;
        Dashes(r, BadInk.WithAlpha(drafted == true ? 0.6f : 0.95f), 1.4f);
    }

    /// <summary>Where a built thing shows on screen: its tile, or for a roof section the roof as drawn on its walls.</summary>
    private Rect2? BuiltRect(World w, BuiltRef t) => t.Kind switch
    {
        TargetKind.Structure when Building.StructureById(w, t.Id) is { } s => ScreenRect(PieceRect(new SimRect(s.Pos.X, s.Pos.Y, 1, 1))),
        TargetKind.Bed when Building.BedById(w, t.Id) is { } b => ScreenRect(PieceRect(new SimRect(b.Pos.X, b.Pos.Y, 1, 1))),
        TargetKind.Roof when Building.RoofById(w, t.Id) is { } r => ScreenRect(_art.RoofTop(r.Area).Grow(-1f)),
        _ => null,
    };

    /// <summary>A faint silhouette of what a plan will be: a wall's band, a door's posts, a bed, a roof on its walls.</summary>
    private void Ghost(BuildingDef def, SimRect area, Color tint)
    {
        float x = area.X * TileSize, y = area.Y * TileSize, cy = y + TileSize * 0.5f, h = WorldArt.WallThick / 2f, ground = cy + h;
        switch (def.Kind)
        {
            case Building.Wall:
                _brush.Rect(ScreenRect(new Rect2(x, ground - WorldArt.WallH, TileSize, WorldArt.WallH)), tint);
                _brush.Rect(ScreenRect(new Rect2(x, cy - h - WorldArt.WallH, TileSize, 2f * h)), tint.Lightened(0.2f));
                break;
            case Building.Door:
                _brush.Rect(ScreenRect(new Rect2(x - 1.5f, ground - WorldArt.WallH - 4f, 3.6f, WorldArt.WallH + 4f)), tint);
                _brush.Rect(ScreenRect(new Rect2(x + TileSize - 2.1f, ground - WorldArt.WallH - 4f, 3.6f, WorldArt.WallH + 4f)), tint);
                break;
            case Building.Bed:
                _brush.Rect(ScreenRect(new Rect2(x + 3f, y + 1f, TileSize - 6f, TileSize - 3f)), tint);
                break;
            default:
                if (def.Spans) _brush.Rect(ScreenRect(_art.RoofTop(area)), tint);
                break;
        }
    }

    /// <summary>A piece's tiles in map pixels, a little inside the grid lines.</summary>
    private static Rect2 PieceRect(SimRect a) => new(a.X * TileSize + 2f, a.Y * TileSize + 2f, a.W * TileSize - 4f, a.H * TileSize - 4f);

    private static Rect2 TilesRect(SimRect a) => new(a.X * TileSize, a.Y * TileSize, a.W * TileSize, a.H * TileSize);

    private void Dashes(Rect2 r, Color c, float width = 1.2f)
    {
        float dash = Mathf.Clamp(r.Size.X / 5f, 2.5f, 5f);
        Vector2 a = r.Position, b = new(r.End.X, r.Position.Y), e = r.End, d = new(r.Position.X, r.End.Y);
        _brush.Dashed(a, b, c, width, dash);
        _brush.Dashed(b, e, c, width, dash);
        _brush.Dashed(e, d, c, width, dash);
        _brush.Dashed(d, a, c, width, dash);
    }

    /// <summary>A red cross: this tile cannot take the plan, or this plan would be called off.</summary>
    private void Cross(Rect2 r, Color c)
    {
        Rect2 i = r.Grow(-r.Size.X * 0.18f);
        float width = Mathf.Clamp(r.Size.X / 9f, 1.4f, 3f);
        _brush.Line(i.Position, i.End, Palette.NameShadow.WithAlpha(0.6f * c.A), width + 1.4f);
        _brush.Line(new Vector2(i.End.X, i.Position.Y), new Vector2(i.Position.X, i.End.Y), Palette.NameShadow.WithAlpha(0.6f * c.A), width + 1.4f);
        _brush.Line(i.Position, i.End, c, width);
        _brush.Line(new Vector2(i.End.X, i.Position.Y), new Vector2(i.Position.X, i.End.Y), c, width);
    }

    /// <summary>The drafts' captions, placed with the names so names keep clear of them.</summary>
    private void BuildCaptions()
    {
        foreach ((Vector2 at, string text) in _captions) AddMark(at.Round(), text, CountFontSize, PlanInk, new Vector2(0f, 1f), 3);
    }
}
