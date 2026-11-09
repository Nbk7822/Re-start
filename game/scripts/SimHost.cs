using System;
using System.Globalization;
using System.IO;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Content;
using SimPoint = Return.Sim.Core.Point;

namespace Return.Game;

/// <summary>
/// Owns the Simulation and runs it in real time: world.json's 20 ticks per second at ×1 (×3 and ×6
/// from its speed multipliers), fractional ticks carried over, at most 400 ticks per frame. Space
/// pauses or resumes, 1/2/3 pick a speed, Esc closes the open panel. The view reads World and calls
/// Simulation.Step / Enqueue only; it never changes World directly.
///
/// Environment hooks for checks and debugging:
///   RETURN_SEED=n        world seed (default 1)
///   RETURN_TICKS=n       advance n ticks before the first frame
///   RETURN_SELECT=key    open the inspector for that cast key (e.g. "you")
///   RETURN_OPEN=name     open a bar list at start: orders, work, people or chronicle
///   RETURN_PAUSE=1       start paused
///   RETURN_PRESS=list    after 1 s, press buttons as a player would: "policy:N" (the Nth ration
///                        policy in Orders) and "priority:N" (the Nth work priority in the open
///                        inspector), comma-separated
///   RETURN_SCREENSHOT=p  save the window to PNG file p about 3 s after start, then quit
/// </summary>
public partial class SimHost : Node
{
    private const double ScreenshotDelaySeconds = 3.0;

    private MapView _map = null!;
    private CameraRig _camera = null!;
    private BottomBar _bar = null!;
    private InspectorPanel _inspector = null!;
    private TileTooltip _tooltip = null!;
    private bool _running;

    public Simulation Sim { get; private set; } = null!;

    public World World => Sim.World;

    public ContentDb Content { get; private set; } = null!;

    public TickClock Clock { get; private set; } = null!;

    /// <summary>The character whose inspector is open; 0 when none.</summary>
    public int SelectedId { get; private set; }

    /// <summary>The left edge of the open inspector in screen pixels, or the window's width when it is closed.</summary>
    public float InspectorLeft => _inspector.Visible ? _inspector.GlobalPosition.X : _inspector.GetViewportRect().Size.X;

    /// <summary>Height of the bottom menu bar in screen pixels (the map is visible above it).</summary>
    public float BarHeight => Mathf.Max(_bar.Size.Y, BottomBar.Height);

    /// <summary>
    /// The only way the view changes the world: a tick-stamped command for the next tick, so it is
    /// recorded, replayable and part of the determinism test like any other input.
    /// </summary>
    public void Order(Command command) => Sim.Enqueue(command);

    public event Action? SpeedChanged;

    public event Action? SelectionChanged;

    public override void _Ready()
    {
        string contentDir = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "content"));
        try
        {
            Content = ContentDb.Load(contentDir);
        }
        catch (Exception ex) when (ex is ContentException or IOException)
        {
            GD.PushError("Return: could not load content from " + contentDir + ": " + ex.Message);
            GetTree().Quit(1);
            return;
        }

        MatchScreenScale();
        Ui.Init(Content.Loc);
        Sim = Simulation.Create(Content, ReadSeed());
        Clock = new TickClock(Content.World.TicksPerSecond, Content.World.SpeedMultipliers);
        long warmUp = ReadTicks();
        if (warmUp > 0) Sim.Advance(warmUp);

        _map = GetNode<MapView>("MapView");
        _camera = GetNode<CameraRig>("Camera");
        _bar = GetNode<BottomBar>("Ui/BottomBar");
        _inspector = GetNode<InspectorPanel>("Ui/Inspector");
        _tooltip = GetNode<TileTooltip>("Ui/Tooltip");
        _bar.Bind(this);
        _map.Bind(this);
        _camera.Bind(this);
        _inspector.Bind(this);
        _tooltip.Bind(this, _map);
        _tooltip.GetParent().MoveChild(_tooltip, -1);
        _running = true;

        string? select = Env("RETURN_SELECT");
        if (select != null)
        {
            Character? c = FindByCastKey(select);
            if (c == null) GD.PushWarning("Return: RETURN_SELECT names no cast member '" + select + "'.");
            else Select(c.Id);
        }

        string? open = Env("RETURN_OPEN");
        if (open != null && !_bar.Open(open)) GD.PushWarning("Return: RETURN_OPEN names no bar list '" + open + "'.");
        if (Env("RETURN_PAUSE") == "1") { Clock.SetSpeed(0); SpeedChanged?.Invoke(); }
        string? press = Env("RETURN_PRESS");
        if (press != null) GetTree().CreateTimer(1.0).Timeout += () => PressForCheck(press);

        string? screenshot = Env("RETURN_SCREENSHOT");
        if (screenshot != null) ScreenshotAndQuit(ResolvePath(screenshot));
    }

    public override void _Process(double delta)
    {
        if (!_running) return;
        int ticks = Clock.Advance(delta);
        for (int i = 0; i < ticks; i++) Sim.Step();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (!_running || e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Space:
                Clock.TogglePause();
                SpeedChanged?.Invoke();
                break;
            case Key.Escape:
                if (!_bar.ClosePopups()) Select(0);
                break;
            default:
                int speed = SpeedForKey(key.Keycode);
                if (speed == 0) speed = SpeedForKey(key.PhysicalKeycode);
                if (speed == 0) return;
                SetSpeed(speed);
                break;
        }
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Opens the inspector for a character (0 closes it).</summary>
    public void Select(int characterId)
    {
        if (characterId != 0 && World.Find(characterId) == null) characterId = 0;
        if (characterId == SelectedId) return;
        SelectedId = characterId;
        SelectionChanged?.Invoke();
    }

    public void SetSpeed(int speed)
    {
        Clock.SetSpeed(speed);
        SpeedChanged?.Invoke();
    }

    public void FocusCamera(SimPoint tile) => _camera.FocusOn(tile);

    /// <summary>
    /// On a HiDPI screen (a Retina Mac reports scale 2) the 1280×800 window would open at half its
    /// size. Open it at the project size in points instead; the canvas_items stretch then draws the
    /// same layout at the screen's scale, so text stays crisp.
    /// </summary>
    private void MatchScreenScale()
    {
        if (DisplayServer.GetName() == "headless") return;
        Window window = GetWindow();
        if (window.Mode != Window.ModeEnum.Windowed) return;
        int screen = window.CurrentScreen;
        float scale = DisplayServer.ScreenGetScale(screen);
        if (scale <= 1f) return;
        var wanted = (Vector2I)((Vector2)window.ContentScaleSize * scale);
        Rect2I usable = DisplayServer.ScreenGetUsableRect(screen);
        var size = new Vector2I(Math.Min(wanted.X, usable.Size.X), Math.Min(wanted.Y, usable.Size.Y));
        if (size.X <= window.Size.X && size.Y <= window.Size.Y) return;
        window.Size = size;
        window.Position = usable.Position + (usable.Size - size) / 2;
    }

    /// <summary>Keys 1..9 select speeds 1..N (×1, ×3, ×6); anything else is 0.</summary>
    private int SpeedForKey(Key k)
    {
        int n = 0;
        if (k >= Key.Key1 && k <= Key.Key9) n = (int)(k - Key.Key1) + 1;
        else if (k >= Key.Kp1 && k <= Key.Kp9) n = (int)(k - Key.Kp1) + 1;
        return n < Clock.SpeedCount ? n : 0;
    }

    private Character? FindByCastKey(string castKey)
    {
        foreach (Character c in World.Characters)
            if (string.Equals(c.CastKey, castKey, StringComparison.Ordinal)) return c;
        return null;
    }

    /// <summary>Presses UI buttons through their real signals, for RETURN_PRESS checks.</summary>
    private void PressForCheck(string list)
    {
        foreach (string item in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = item.Split(':');
            bool ok = parts.Length == 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && parts[0] switch
            {
                "policy" => _bar.PressPolicy(n),
                "priority" => _inspector.PressPriority(n),
                _ => false,
            };
            if (!ok) GD.PushWarning("Return: RETURN_PRESS could not press '" + item + "'.");
        }
    }

    private async void ScreenshotAndQuit(string path)
    {
        try
        {
            await ToSignal(GetTree().CreateTimer(ScreenshotDelaySeconds), SceneTreeTimer.SignalName.Timeout);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Image? image = GetViewport().GetTexture()?.GetImage();
            if (image == null || image.IsEmpty())
            {
                GD.PushWarning("Return: no rendered frame to save (headless run?); screenshot skipped.");
            }
            else
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                Error err = image.SavePng(path);
                if (err == Error.Ok) GD.Print("Return: saved screenshot ", path, " (", image.GetWidth(), "x", image.GetHeight(), ")");
                else GD.PushError("Return: could not save screenshot " + path + ": " + err);
            }
        }
        catch (Exception ex)
        {
            GD.PushError("Return: screenshot failed: " + ex.Message);
        }
        GetTree().Quit();
    }

    private static string? Env(string name)
    {
        string? value = System.Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ulong ReadSeed()
    {
        string? s = Env("RETURN_SEED");
        if (s == null) return 1;
        if (ulong.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed)) return seed;
        GD.PushWarning("Return: RETURN_SEED='" + s + "' is not a whole number; using seed 1.");
        return 1;
    }

    private static long ReadTicks()
    {
        string? s = Env("RETURN_TICKS");
        if (s == null) return 0;
        if (long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out long ticks)) return ticks;
        GD.PushWarning("Return: RETURN_TICKS='" + s + "' is not a whole number; not advancing.");
        return 0;
    }

    /// <summary>Relative paths are taken from the directory the game was launched from.</summary>
    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        string baseDir = Env("PWD") ?? System.Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, path));
    }
}
