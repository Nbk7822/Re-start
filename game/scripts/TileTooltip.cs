using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;

namespace Return.Game;

/// <summary>
/// Hover a tile for a moment to see what is on it: terrain, contents and who stands there, from
/// StateApi.Location. Hidden while the mouse is over the bar or a panel, or outside the map.
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
        Vector2I tile = _map.TileUnderMouse();
        bool overUi = GetViewport().GuiGetHoveredControl() != null;
        bool inWindow = _mouseInWindow && GetViewportRect().HasPoint(GetViewport().GetMousePosition());
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

    private void Fill()
    {
        World w = _host!.World;
        LocationState s = StateApi.Location(w, _tile.X, _tile.Y);
        _text.Text = string.Join("\n", ViewText.TileLines(w, s));
    }

    private void PlaceNearMouse()
    {
        Vector2 vp = GetViewportRect().Size;
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 size = GetCombinedMinimumSize();
        Size = size;
        Vector2 pos = mouse + MouseGap;
        float bottom = vp.Y - _host!.BarHeight;
        if (pos.X + size.X > vp.X - 4) pos.X = mouse.X - size.X - 8;
        if (pos.Y + size.Y > bottom - 4) pos.Y = mouse.Y - size.Y - 8;
        Position = new Vector2(Mathf.Max(4, pos.X), Mathf.Max(4, pos.Y));
    }
}
