using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;

namespace Return.Game;

/// <summary>
/// Hover a tile for a moment to see what is on it: terrain, contents and who stands there, from
/// StateApi.Location, and what is drafted there while paused. Hidden while the mouse is over the bar
/// or a panel, or outside the map. With a Build tool in hand it is the hint by the cursor instead
/// (build design 3.3): what the gesture marks and costs, and why a tile cannot take it.
/// </summary>
public partial class TileTooltip : PanelContainer
{
    private const double DelaySeconds = 0.45;
    private const double RefreshSeconds = 0.25;
    private static readonly Vector2 MouseGap = new(16, 20);

    private SimHost? _host;
    private MapView? _map;
    private Label _text = null!;
    private Vector2I _tile = new(-1, -1);
    private double _hovered;
    private double _refresh;
    private bool _mouseInWindow = true;

    public void Bind(SimHost host, MapView map)
    {
        _host = host;
        _map = map;
        Theme = Ui.Theme;
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", Ui.Box(Ui.PanelBg, Ui.Edge, 1, 3, 8, 5));
        _text = Ui.MakeLabel("", Ui.SmallFontSize);
        AddChild(_text);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMMouseExit)
        {
            _mouseInWindow = false;
            Visible = false;
        }
        else if (what == NotificationWMMouseEnter)
        {
            _mouseInWindow = true;
        }
    }

    public override void _Process(double delta)
    {
        if (_host == null || _map == null) return;
        if (_host.Build.Active)
        {
            ShowBuildHint();
            return;
        }
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2I tile = _map.TileUnderMouse();
        bool overUi = GetViewport().GuiGetHoveredControl() != null || OverPanel(mouse);
        bool inWindow = _mouseInWindow && GetViewportRect().HasPoint(mouse);
        if (!inWindow || overUi || !_map.InBounds(tile))
        {
            _tile = new Vector2I(-1, -1);
            Visible = false;
            return;
        }
        if (tile != _tile)
        {
            _tile = tile;
            _hovered = 0;
            Visible = false;
            return;
        }
        _hovered += delta;
        if (_hovered < DelaySeconds) return;
        _refresh -= delta;
        if (!Visible || _refresh <= 0)
        {
            _refresh = RefreshSeconds;
            Fill();
            Visible = true;
        }
        PlaceNearMouse();
    }

    /// <summary>
    /// True when the point is over the bar or an open panel. Checked by rectangle because Godot
    /// only tracks the hovered control after the mouse moves, and the cursor may start over the bar.
    /// </summary>
    private bool OverPanel(Vector2 point)
    {
        Node layer = GetParent();
        for (int i = 0; i < layer.GetChildCount(); i++)
            if (layer.GetChild(i) is Control { Visible: true } c && c != this &&
                c.MouseFilter != MouseFilterEnum.Ignore && c.GetGlobalRect().HasPoint(point))
                return true;
        return false;
    }

    private void Fill()
    {
        World w = _host!.World;
        LocationState s = StateApi.Location(w, _tile.X, _tile.Y);
        var lines = new List<string>(ViewText.TileLines(w, s));
        lines.AddRange(BuildTool.DraftLines(w, _host.Drafts, _tile.X, _tile.Y));
        _text.Text = string.Join("\n", lines);
    }

    /// <summary>The Build tool's hint, by the tile under its cursor (where a check hook's gesture ended, too), at once and kept current.</summary>
    private void ShowBuildHint()
    {
        BuildTool tool = _host!.Build;
        _tile = new Vector2I(-1, -1);
        IReadOnlyList<string> lines = tool.Hint(_host.World, _host.Drafts, _host.Clock.Paused);
        if (lines.Count == 0 || !_mouseInWindow && !tool.Pinned)
        {
            Visible = false;
            return;
        }
        string text = string.Join("\n", lines);
        if (text != _text.Text) _text.Text = text;
        Visible = true;
        float tile = _map!.TileOnScreen;
        PlaceNear(_map.ScreenOfTile(tool.Cursor) + new Vector2(tile * 0.5f, tile * 0.5f) - MouseGap);
    }

    private void PlaceNearMouse() => PlaceNear(GetViewport().GetMousePosition());

    private void PlaceNear(Vector2 anchor)
    {
        Vector2 vp = GetViewportRect().Size;
        Vector2 size = GetCombinedMinimumSize();
        Size = size;
        Vector2 pos = anchor + MouseGap;
        float bottom = vp.Y - _host!.BarHeight;
        if (pos.X + size.X > vp.X - 4) pos.X = anchor.X - size.X - 8;
        if (pos.Y + size.Y > bottom - 4) pos.Y = anchor.Y - size.Y - 8;
        Position = new Vector2(Mathf.Max(4, pos.X), Mathf.Max(4, pos.Y));
    }
}
