using System.Collections.Generic;
using System.Linq;
using Godot;
using Return.Game.Art;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Systems;
using SimPoint = Return.Sim.Core.Point;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game;

/// <summary>
/// Zones on the map (ADR-0028), drawn every frame from the sim's zones as they will be once queued and
/// drafted choices apply. The rest of the time a stockpile or meeting place shows only its dashed edge,
/// as the stockpile always did; while the Zones list is open or a marking tool is in hand every zone
/// shows a faint fill, its edge and its name. A zone drafted while paused is hatched. The box being
/// dragged shows the tiles it can take, and a ✕ on those it cannot. Muted colours, thin lines (ADR-0019).
/// </summary>
public partial class MapView
{
    private readonly HashSet<SimPoint> _zoneTiles = new();
    private readonly Dictionary<string, Color> _zoneColors = new(System.StringComparer.Ordinal);

    /// <summary>The marking tool's mouse: press, drag and release mark; right-click stops; clicks never select people meanwhile. True if handled.</summary>
    private bool ZoneInput(InputEvent e)
    {
        ZoneTool tool = _host!.Tool;
        if (!tool.Active) return false;
        Vector2I t = TileUnderMouse();
        var tile = new SimPoint(t.X, t.Y);
        switch (e)
        {
            case InputEventMouseMotion:
                tool.Move(tile);
                return false;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                _host.StopZoneTool();
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb:
                if (mb.Pressed)
                {
                    tool.Move(tile);
                    tool.Press(tile);
                }
                else
                {
                    tool.Move(tile);
                    _host.ReleaseZoneTool();
                }
                return true;
            default:
                return false;
        }
    }

    /// <summary>Zones in the lit world pass, under everything that stands, so night tints them like the ground.</summary>
    private void DrawZones(World w)
    {
        const float T = TileSize;
        bool full = _host!.ZonesShown;
        List<Zone> applied = w.Map.Zones;
        foreach (Zone z in Zones.Preview(w, _host.Drafts.All))
        {
            if (w.Content.TryZone(z.Kind) is not { } def) continue;
            Color c = ZoneColor(def);
            Zone? now = applied.Find(a => a.Id == z.Id);
            bool drafted = now == null || !now.Tiles.SequenceEqual(z.Tiles);
            _zoneTiles.Clear();
            foreach (SimPoint p in Zones.Each(z.Tiles)) _zoneTiles.Add(p);
            foreach (SimPoint p in _zoneTiles)
            {
                float x = p.X * T, y = p.Y * T;
                if (full) _brush.Rect(x, y, T, T, c.WithAlpha(0.15f));
                if (drafted)
                {
                    Color hatch = c.WithAlpha(0.45f);
                    _brush.Line(x, y + T * 0.5f, x + T * 0.5f, y, hatch, 1f);
                    _brush.Line(x, y + T, x + T, y, hatch, 1f);
                    _brush.Line(x + T * 0.5f, y + T, x + T, y + T * 0.5f, hatch, 1f);
                }
            }
            Color edge = c.WithAlpha(full ? 0.85f : 0.45f);
            foreach (SimPoint p in _zoneTiles)
            {
                float x = p.X * T, y = p.Y * T;
                if (!_zoneTiles.Contains(new SimPoint(p.X, p.Y - 1))) _brush.Dashed(new Vector2(x, y + 1f), new Vector2(x + T, y + 1f), edge, 1.2f, 4f);
                if (!_zoneTiles.Contains(new SimPoint(p.X, p.Y + 1))) _brush.Dashed(new Vector2(x, y + T - 1f), new Vector2(x + T, y + T - 1f), edge, 1.2f, 4f);
                if (!_zoneTiles.Contains(new SimPoint(p.X - 1, p.Y))) _brush.Dashed(new Vector2(x + 1f, y), new Vector2(x + 1f, y + T), edge, 1.2f, 4f);
                if (!_zoneTiles.Contains(new SimPoint(p.X + 1, p.Y))) _brush.Dashed(new Vector2(x + T - 1f, y), new Vector2(x + T - 1f, y + T), edge, 1.2f, 4f);
            }
        }
        DrawDrag(w);
    }

    /// <summary>The box being dragged: tiles a zone can take in its colour (unmarking: a dark wash), a ✕ on the rest.</summary>
    private void DrawDrag(World w)
    {
        const float T = TileSize;
        ZoneTool tool = _host!.Tool;
        if (!tool.Dragging) return;
        ZoneDef? def = tool.Kind == null ? null : w.Content.TryZone(tool.Kind);
        Color fill = def == null ? new Color(0.08f, 0.06f, 0.05f, 0.35f) : ZoneColor(def).WithAlpha(0.32f);
        var cross = new Color(0.78f, 0.32f, 0.26f, 0.8f);
        SimRect box = tool.Box;
        foreach (SimPoint p in ZoneTool.Tiles(box))
        {
            if (!w.Map.InBounds(p)) continue;
            float x = p.X * T, y = p.Y * T;
            if (def == null || Zones.Check(w, def, p) == null)
            {
                _brush.Rect(x, y, T, T, fill);
                continue;
            }
            _brush.Line(x + T * 0.3f, y + T * 0.3f, x + T * 0.7f, y + T * 0.7f, cross, 1.4f);
            _brush.Line(x + T * 0.7f, y + T * 0.3f, x + T * 0.3f, y + T * 0.7f, cross, 1.4f);
        }
        WorldArt.DashedRect(_brush, WorldArt.TilesRect(box), new Color(0.95f, 0.9f, 0.8f, 0.7f), 1.2f, 4f);
    }

    /// <summary>Above the lighting: each zone's name at its top-left tile while zones are shown, and the hint by the cursor while marking.</summary>
    private void ZoneLabels(World w)
    {
        if (!_host!.ZonesShown) return;
        foreach (Zone z in Zones.Preview(w, _host.Drafts.All))
        {
            if (z.Tiles.Count == 0 || w.Content.TryZone(z.Kind) is not { } def) continue;
            var corner = new Vector2(z.Tiles[0].X * TileSize + 3f, z.Tiles[0].Y * TileSize + 2f);
            Text(_toScreen * corner, Ui.T(def.NameKey), CountFontSize, ZoneColor(def).Lightened(0.35f), Vector2.Zero, 3);
        }
        string hint = ZoneText.Cursor(w, _host.Tool);
        if (hint.Length > 0) Text(GetViewport().GetMousePosition() + new Vector2(16f, 18f), hint, NameFontSize, Palette.Name, Vector2.Zero, 4);
    }

    /// <summary>A kind's colour from zones.json, muted and darkened toward the grounded palette (ADR-0019).</summary>
    private Color ZoneColor(ZoneDef def)
    {
        if (_zoneColors.TryGetValue(def.Id, out Color c)) return c;
        Color raw = Color.FromHtml(def.Color);
        float grey = (raw.R + raw.G + raw.B) / 3f;
        c = new Color(Mathf.Lerp(grey, raw.R, 0.75f), Mathf.Lerp(grey, raw.G, 0.75f), Mathf.Lerp(grey, raw.B, 0.75f)).Darkened(0.1f);
        _zoneColors[def.Id] = c;
        return c;
    }
}
