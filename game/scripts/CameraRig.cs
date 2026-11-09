using Godot;
using Return.Sim;
using SimPoint = Return.Sim.Core.Point;

namespace Return.Game;

/// <summary>
/// The map camera. WASD or the arrow keys pan; the mouse wheel zooms toward the cursor, clamped
/// between "whole map visible" and 3×. It starts centred on the camp's hearth and keeps the view
/// over the map, so the map fills the window above the bottom bar.
///
/// Check hooks (for screenshots, like SimHost's): RETURN_FOCUS=x,y starts centred on that tile;
/// RETURN_ZOOM=z starts at that zoom (clamped as usual).
/// </summary>
public partial class CameraRig : Camera2D
{
    private const float MaxZoom = 3f;
    private const float PreferredZoom = 1.25f;
    private const float WheelStep = 1.12f;
    private const float PanSpeed = 900f; // screen pixels per second

    private SimHost? _host;
    private Vector2 _mapSize;
    private Vector2 _focus; // map point at the centre of the area above the bar
    private float _zoom = PreferredZoom;

    public void Bind(SimHost host)
    {
        _host = host;
        Map m = host.World.Map;
        _mapSize = new Vector2(m.Width, m.Height) * MapView.TileSize;
        _focus = MapView.TileCenter(m.Hearth);
        _zoom = Mathf.Max(PreferredZoom, CoverZoom());
        string? focus = System.Environment.GetEnvironmentVariable("RETURN_FOCUS");
        string[] xy = (focus ?? "").Split(',');
        if (xy.Length == 2 && int.TryParse(xy[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int fx)
            && int.TryParse(xy[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int fy))
            _focus = MapView.TileCenter(new SimPoint(fx, fy));
        else if (!string.IsNullOrWhiteSpace(focus)) GD.PushWarning("Return: RETURN_FOCUS='" + focus + "' is not x,y; ignored.");
        string? zoom = System.Environment.GetEnvironmentVariable("RETURN_ZOOM");
        if (float.TryParse(zoom, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z) && z > 0f) _zoom = z;
        else if (!string.IsNullOrWhiteSpace(zoom)) GD.PushWarning("Return: RETURN_ZOOM='" + zoom + "' is not a number; ignored.");
        Apply();
    }

    public void FocusOn(SimPoint tile)
    {
        if (_host == null) return;
        _focus = MapView.TileCenter(tile);
        Apply();
    }

    public override void _Process(double delta)
    {
        if (_host == null) return;
        var dir = Vector2.Zero;
        if (Held(Key.A, Key.Left)) dir.X -= 1;
        if (Held(Key.D, Key.Right)) dir.X += 1;
        if (Held(Key.W, Key.Up)) dir.Y -= 1;
        if (Held(Key.S, Key.Down)) dir.Y += 1;
        if (dir != Vector2.Zero) _focus += dir.Normalized() * PanSpeed * (float)delta / _zoom;
        Apply();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_host == null || e is not InputEventMouseButton { Pressed: true } mb) return;
        if (mb.ButtonIndex == MouseButton.WheelUp) ZoomAt(mb.Position, WheelStep);
        else if (mb.ButtonIndex == MouseButton.WheelDown) ZoomAt(mb.Position, 1f / WheelStep);
        else return;
        GetViewport().SetInputAsHandled();
    }

    private static bool Held(Key a, Key b) => Input.IsPhysicalKeyPressed(a) || Input.IsPhysicalKeyPressed(b);

    /// <summary>Size in screen pixels of the map area (the window above the bottom bar).</summary>
    private Vector2 VisibleSize()
    {
        Vector2 vp = GetViewportRect().Size;
        return new Vector2(Mathf.Max(1, vp.X), Mathf.Max(1, vp.Y - _host!.BarHeight));
    }

    private float FitZoom()
    {
        Vector2 v = VisibleSize();
        return Mathf.Min(v.X / _mapSize.X, v.Y / _mapSize.Y);
    }

    private float CoverZoom()
    {
        Vector2 v = VisibleSize();
        return Mathf.Max(v.X / _mapSize.X, v.Y / _mapSize.Y);
    }

    private void ZoomAt(Vector2 screen, float factor)
    {
        Vector2 offset = screen - VisibleSize() / 2;
        Vector2 under = _focus + offset / _zoom;
        _zoom = ClampZoom(_zoom * factor);
        _focus = under - offset / _zoom;
        Apply();
    }

    private float ClampZoom(float zoom) => Mathf.Clamp(zoom, Mathf.Min(FitZoom(), MaxZoom), MaxZoom);

    private void Apply()
    {
        _zoom = ClampZoom(_zoom);
        Vector2 half = VisibleSize() / (2 * _zoom);
        _focus.X = half.X * 2 >= _mapSize.X ? _mapSize.X / 2 : Mathf.Clamp(_focus.X, half.X, _mapSize.X - half.X);
        _focus.Y = half.Y * 2 >= _mapSize.Y ? _mapSize.Y / 2 : Mathf.Clamp(_focus.Y, half.Y, _mapSize.Y - half.Y);
        Zoom = new Vector2(_zoom, _zoom);
        Position = _focus + new Vector2(0, _host!.BarHeight / (2 * _zoom));
    }
}
