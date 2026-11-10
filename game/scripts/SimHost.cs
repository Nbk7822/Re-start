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
///   RETURN_TICKS=n       step n ticks before the first frame (a return sends the clock back, so the
///                        world may then stand earlier than tick n; ADR-0030)
///   RETURN_SELECT=key    open the inspector for that cast key (e.g. "you")
///   RETURN_OPEN=name     open a bar list at start: build, orders, zones, work, schedule, people or chronicle;
///                        "lost" opens the Chronicle on its Lost futures tab (ADR-0030)
///   RETURN_PAUSE=1       start paused
///   RETURN_PRESS=list    after 1 s, press buttons as a player would, comma-separated (a press
///                        begins with a word; the numbers after it are its own):
///                        "policy:N" presses ration policy N in Orders (0-based: 0 = the hungriest
///                        eat first); "priority:R:V" picks value V (1–4, or 0 for never) in work
///                        row R of the open inspector (0-based: 0 = the first kind of work);
///                        "build:ID:x,y[,w,h]" picks building ID in the Build list and drags from
///                        tile (x, y) across w by h tiles through the real tool (a click without
///                        w, h; a wall, door or bed rectangle at least 2 by 2 is a Shift-drag, its
///                        outline); "takedown:x,y[,w,h]" and "calloff:x,y[,w,h]" do the same with
///                        those tools; the tool stays in hand, as it does for a player, until
///                        "stop"; "wait:N" lets N ticks pass at once (drafts go out first, as time
///                        moves), so a plan can be seen part built or finished;
///                        "zone:KIND:X:Y:W:H" picks a kind in Zones and drags a W×H box from tile
///                        (X, Y) with the marking tool; "unzone:X:Y:W:H" does the same with Unmark;
///                        "open:NAME" opens a bar list as its button would (build, orders, zones, ...);
///                        "stop" puts down whichever tool is in hand (Build or zones), as Esc does;
///                        "camp:B:F:T" paints the camp's day with block B (0 sleep, 1 work, 2 free time)
///                        from hour F to T (exclusive, wrapping) in Schedule, "night:B:F:T" the night
///                        folk's day, "person:B:F:T" the open inspector's day, "watch:F:T" sets the
///                        watch from F to T (ADR-0029)
///   RETURN_SCREENSHOT=p  save the window to PNG file p about 3 s after start, then quit
///   RETURN_SCREENSHOT_DELAY=s  seconds before that screenshot (default 3; a return's frames
///                        play over about 3 s from the moment it happens, ADR-0030)
///   RETURN_MODS=a,b      content mod folders loaded over content/, comma-separated (for checks, such as
///                        a short soul's span to see a life end)
/// </summary>
public partial class SimHost : Node
{
    private const double ScreenshotDelaySeconds = 3.0;

    private MapView _map = null!;
    private CameraRig _camera = null!;
    private BottomBar _bar = null!;
    private InspectorPanel _inspector = null!;
    private TileTooltip _tooltip = null!;
    private FateOverlay? _fate;
    private Action<World>? _witness;
    private bool _running;
    private bool _endShown;

    /// <summary>While a return plays, the world that was lost stays on screen until the fold (ADR-0030).</summary>
    private World? _lost;
    private FateReturn? _pendingReturn;

    /// <summary>The seed "A new world" starts next (kept across the scene reload); 0 = none chosen.</summary>
    private static ulong _nextSeed;

    public Simulation Sim { get; private set; } = null!;

    /// <summary>The world the view shows: the simulation's, or, while a return plays, the one that was lost.</summary>
    public World World => _lost ?? Sim.World;

    public ContentDb Content { get; private set; } = null!;

    public TickClock Clock { get; private set; } = null!;

    /// <summary>The character whose inspector is open; 0 when none.</summary>
    public int SelectedId { get; private set; }

    /// <summary>The left edge of the open inspector in screen pixels, or the window's width when it is closed.</summary>
    public float InspectorLeft => _inspector.Visible ? _inspector.GlobalPosition.X : _inspector.GetViewportRect().Size.X;

    /// <summary>Height of the bottom menu bar in screen pixels (the map is visible above it).</summary>
    public float BarHeight => Mathf.Max(_bar.Size.Y, BottomBar.Height);

    /// <summary>Choices made while paused, sent when time moves (see <see cref="OrderDrafts"/>).</summary>
    public OrderDrafts Drafts { get; } = new();

    /// <summary>The Build tool in hand, if any (build design 3.3): the bar's Build list picks it, the map takes its gestures.</summary>
    public BuildTool Build { get; } = new();

    /// <summary>
    /// Ends the Build tool's gesture: the plan, call-off or marks it asked for go out through <see cref="Order"/>. While a
    /// return plays the gesture ends unreleased: it was made in the world that was lost (ADR-0030; the merge review).
    /// </summary>
    public void FinishGesture()
    {
        if (_lost != null)
        {
            Build.CancelDrag();
            return;
        }
        Build.Release(World, Drafts, Order);
    }

    /// <summary>
    /// A check screenshot is being taken (RETURN_SCREENSHOT): the real pointer does not move a cursor that a check hook's
    /// gesture pinned, so the picture does not depend on where the mouse happens to be on the machine (ADR-0027 review).
    /// </summary>
    public bool Checking { get; private set; }

    /// <summary>The zone marking tool (ADR-0028): which kind is being marked, and the box being dragged.</summary>
    public ZoneTool Tool { get; } = new();

    /// <summary>Zones are shown in full while their list is open or a marking tool is in hand.</summary>
    public bool ZonesShown => Tool.Active || _bar.ZonesOpen;

    /// <summary>Picks up the marking tool for a kind of zone (left clicks then mark, and stop selecting people); a Build tool in hand is put down.</summary>
    public void StartZoneTool(string kind)
    {
        Build.Stop();
        Tool.Start(kind);
    }

    /// <summary>Picks up the unmarking tool; a Build tool in hand is put down.</summary>
    public void StartUnmark()
    {
        Build.Stop();
        Tool.StartUnmark();
    }

    public void StopZoneTool() => Tool.Stop();

    /// <summary>Ends a drag: its one change goes out through <see cref="Order"/>, as a draft while paused; none while a return plays, as for the Build tool.</summary>
    public void ReleaseZoneTool()
    {
        if (_lost != null)
        {
            Tool.CancelDrag();
            return;
        }
        foreach (Command c in Tool.Release(World, Return.Sim.Systems.CampPreview.Of(World, Drafts.All), NewZoneId, World.Tick)) Order(c);
    }

    /// <summary>A zone made now: 0 (a new zone) while time runs, a draft's own negative id while paused.</summary>
    private int NewZoneId() => Clock.Paused ? Drafts.NewZoneId() : 0;

    /// <summary>
    /// The only way the view changes the world. While time runs, a choice is a tick-stamped command
    /// for the next tick, recorded and replayable like any other input. While paused it is a draft
    /// that a later choice for the same thing replaces; drafts go out when time moves.
    /// </summary>
    public void Order(Command command)
    {
        if (Clock.Paused) Drafts.Add(command);
        else Sim.Enqueue(command with { Tick = World.Tick });
    }

    public event Action? SpeedChanged;

    public event Action? SelectionChanged;

    public override void _Ready()
    {
        string contentDir = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "content"));
        try
        {
            string[] mods = (Env("RETURN_MODS") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < mods.Length; i++) mods[i] = ResolvePath(mods[i]);
            Content = ContentDb.Load(contentDir, mods);
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
        Sim.Returned += OnReturned;
        Clock = new TickClock(Content.World.TicksPerSecond, Content.World.SpeedMultipliers);
        _map = GetNode<MapView>("MapView");
        // Every tick the view runs, warm-up or live, is witnessed by the map, so blood lands where it fell.
        _witness = _map.Witness;
        for (long warm = ReadTicks(); warm > 0;)
        {
            long ran = ViewStepper.Run(Sim, warm, _witness);
            warm -= ran;
            if (warm == 0 || ran == 0 || _pendingReturn == null) break;
            // A return the warm-up runs on past plays no frames: the view moves on to the world gone back to (ADR-0030).
            _lost = null;
            _pendingReturn = null;
            _map.ResetFor(Sim.World);
            Clock.SetSpeed(1);
        }

        _camera = GetNode<CameraRig>("Camera");
        _bar = GetNode<BottomBar>("Ui/BottomBar");
        _inspector = GetNode<InspectorPanel>("Ui/Inspector");
        _tooltip = GetNode<TileTooltip>("Ui/Tooltip");
        _bar.Bind(this);
        _map.Bind(this);
        _camera.Bind(this);
        _inspector.Bind(this);
        _tooltip.Bind(this, _map);
        _fate = new FateOverlay();
        GetNode("Ui").AddChild(_fate);
        _fate.Bind(this);
        _tooltip.GetParent().MoveChild(_tooltip, -1);
        _running = true;
        if (_pendingReturn != null) // you died during the warm-up
        {
            FocusOnYou(_pendingReturn.Lost);
            _fate.ShowReturn(_pendingReturn);
        }
        else if (Sim.World.Tick == 0)
        {
            Clock.SetSpeed(0);
            SpeedChanged?.Invoke();
            _fate.ShowOpening(Sim.World);
        }

        string? select = Env("RETURN_SELECT");
        if (select != null)
        {
            Character? c = FindByCastKey(select);
            if (c == null) GD.PushWarning("Return: RETURN_SELECT names no cast member '" + select + "'.");
            else Select(c.Id);
        }

        string? open = Env("RETURN_OPEN");
        if (open != null && !_bar.Open(open)) GD.PushWarning("Return: RETURN_OPEN names no bar list '" + open + "'.");
        if (open != null && _fate.Showing && _pendingReturn == null) _fate.Visible = false; // a check that opens a list wants the list, not the opening card
        if (Env("RETURN_PAUSE") == "1") { Clock.SetSpeed(0); SpeedChanged?.Invoke(); }
        string? press = Env("RETURN_PRESS");
        if (press != null) GetTree().CreateTimer(1.0).Timeout += () => PressForCheck(press);

        string? screenshot = Env("RETURN_SCREENSHOT");
        Checking = screenshot != null;
        if (screenshot != null) ScreenshotAndQuit(ResolvePath(screenshot));
    }

    public override void _Process(double delta)
    {
        if (!_running) return;
        if (Drafts.Count > 0 && !Clock.Paused) Drafts.Flush(Sim);
        ViewStepper.Run(Sim, Clock.Advance(delta), _witness);
        if (Sim.World.Fate.Ended && !_endShown && _fate != null && !_fate.Showing)
        {
            _endShown = true;
            Clock.SetSpeed(0);
            SpeedChanged?.Invoke();
            _fate.ShowEnded(Sim.World);
        }
    }

    /// <summary>
    /// You died and the world went back (ADR-0030): time stops, a gesture in hand ends unreleased, choices drafted in the
    /// lost world are dropped, and the return plays over the lost world until the fold. A return during the warm-up plays
    /// once the view is up.
    /// </summary>
    private void OnReturned(FateReturn r)
    {
        ReturnGestures.Begin(Drafts, Tool, Build);
        Clock.SetSpeed(0);
        SpeedChanged?.Invoke();
        _lost = r.Lost;
        if (_fate == null)
        {
            _pendingReturn = r;
            return;
        }
        FocusOnYou(r.Lost);
        _fate.ShowReturn(r);
    }

    /// <summary>The camera goes to you: to where you fell, as a return begins, and to where you stand when it folds back.</summary>
    private void FocusOnYou(World w)
    {
        if (w.Find(w.PlayerId) is { } you && you.Pos != SimPoint.None) FocusCamera(you.Pos);
    }

    /// <summary>The fold of a return: the map shows the world gone back to, with its own blood, trees and places.</summary>
    public void CompleteReturn()
    {
        if (_lost == null) return;
        _lost = null;
        _pendingReturn = null;
        ReturnGestures.Fold(Drafts); // whatever was drafted while the return played was chosen in the lost world
        _map.ResetFor(Sim.World);
        FocusOnYou(Sim.World);
        // Someone made after the knot (the thief who struck you down) is not in the world gone back to: close their inspector.
        SelectedId = FateText.SelectionAfterReturn(Sim.World, SelectedId);
        SelectionChanged?.Invoke();
    }

    /// <summary>Time moves again at the speed it last ran.</summary>
    public void Resume()
    {
        if (Clock.Paused) Clock.TogglePause();
        SpeedChanged?.Invoke();
    }

    /// <summary>Opens the Chronicle, on its Lost futures tab if asked.</summary>
    public void OpenChronicle(bool lost) => _bar.Open(lost ? "lost" : "chronicle");

    /// <summary>Your lifetime ended: start a new world on the next seed.</summary>
    public void NewWorld()
    {
        _nextSeed = Sim.World.Seed + 1;
        GetTree().ReloadCurrentScene();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (!_running || e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Space:
                if (_lost != null) break; // a return is playing: time waits for its last card
                Clock.TogglePause();
                SpeedChanged?.Invoke();
                break;
            case Key.Escape:
                if (Build.Active) Build.Stop();
                else if (Tool.Active) StopZoneTool();
                else if (!_bar.ClosePopups()) Select(0);
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
        if (_lost == null) Clock.SetSpeed(speed); // while a return plays, time waits for its last card
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

    /// <summary>Presses UI buttons through their real signals, and makes Build gestures through the real tool, for RETURN_PRESS checks.</summary>
    private void PressForCheck(string list)
    {
        foreach (string item in CheckPresses.Split(list))
        {
            if (PressZoneForCheck(item)) continue;
            string[] parts = item.Split(':');
            bool ok = parts[0] switch
            {
                "build" when parts.Length == 3 => _bar.PressBuilding(parts[1]) && Gesture(parts[2]),
                "takedown" when parts.Length == 2 => StartTool(BuildMode.TakeDown) && Gesture(parts[1]),
                "calloff" when parts.Length == 2 => StartTool(BuildMode.CallOff) && Gesture(parts[1]),
                "stop" when parts.Length == 1 => StopTool(),
                "wait" when parts.Length == 2 && Numbers(parts, out int[] t) => Wait(t[0]),
                "policy" when parts.Length == 2 && Numbers(parts, out int[] n) => _bar.PressPolicy(n[0]),
                "priority" when parts.Length == 3 && Numbers(parts, out int[] n) => _inspector.PressPriority(n[0], n[1]),
                "camp" when parts.Length == 4 && Numbers(parts, out int[] n) => _bar.PressDay(Return.Sim.Systems.ScheduleSystem.Diurnal, n[0], n[1], n[2]),
                "night" when parts.Length == 4 && Numbers(parts, out int[] n) => _bar.PressDay("nocturnal", n[0], n[1], n[2]),
                "person" when parts.Length == 4 && Numbers(parts, out int[] n) => _inspector.PressDay(n[0], n[1], n[2]),
                "watch" when parts.Length == 3 && Numbers(parts, out int[] n) => _bar.PressWatch(n[0], n[1]),
                _ => false,
            };
            if (!ok) GD.PushWarning("Return: RETURN_PRESS could not press '" + item + "'.");
        }
    }

    /// <summary>The whole numbers after a press's word, or false.</summary>
    private static bool Numbers(string[] parts, out int[] n)
    {
        n = new int[parts.Length - 1];
        for (int i = 1; i < parts.Length; i++)
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i - 1])) return false;
        return true;
    }

    private bool StartTool(BuildMode mode)
    {
        StopZoneTool();
        if (mode == BuildMode.TakeDown) Build.TakeDown();
        else Build.CallOff();
        return true;
    }

    /// <summary>Puts down whichever tool is in hand, Build or zones, as Esc does.</summary>
    private bool StopTool()
    {
        Build.Stop();
        StopZoneTool();
        return true;
    }

    /// <summary>
    /// A press, drag and release of the tool in hand over "x,y[,w,h]", as the mouse would make it; a wall, door or
    /// bed rectangle at least 2 by 2 is made with Shift held (its outline). The cursor stays at the release tile.
    /// </summary>
    private bool Gesture(string coords)
    {
        if (!Build.Active || !CheckPresses.TryGesture(coords, out SimPoint from, out SimPoint to)) return false;
        Build.Shift = Build.Mode == BuildMode.Place && World.Content.TryBuilding(Build.BuildingId) is { Spans: false } && from.X != to.X && from.Y != to.Y;
        Build.Hover(from, pinned: true);
        Build.Press(from);
        Build.Hover(to, pinned: true);
        FinishGesture();
        return true;
    }

    /// <summary>Lets ticks pass at once, as the warm-up does (every tick witnessed by the map); drafts go out first, as time moves.</summary>
    private bool Wait(int ticks)
    {
        if (ticks < 0) return false;
        if (Drafts.Count > 0) Drafts.Flush(Sim);
        ViewStepper.Run(Sim, ticks, _witness);
        return true;
    }

    /// <summary>
    /// "zone:KIND:X:Y:W:H" or "unzone:X:Y:W:H": picks the tool in the Zones list through its button, then
    /// drags the box through the same tool code the mouse uses; "open:NAME" opens a bar list. True when the item
    /// was one of these.
    /// </summary>
    private bool PressZoneForCheck(string item)
    {
        string[] parts = item.Split(':');
        if (parts.Length == 2 && parts[0] == "open")
        {
            if (!_bar.Open(parts[1])) GD.PushWarning("Return: RETURN_PRESS names no bar list '" + parts[1] + "'.");
            return true;
        }
        bool zone = parts.Length == 6 && parts[0] == "zone", unzone = parts.Length == 5 && parts[0] == "unzone";
        if (!zone && !unzone) return false;
        int first = zone ? 2 : 1;
        int[] n = new int[4];
        for (int i = 0; i < 4; i++)
            if (!int.TryParse(parts[first + i], NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i]) || (i >= 2 && n[i] < 1))
            {
                GD.PushWarning("Return: RETURN_PRESS could not press '" + item + "'.");
                return true;
            }
        if (!_bar.PressZoneTool(zone ? parts[1] : null))
        {
            GD.PushWarning("Return: RETURN_PRESS found no zone tool for '" + item + "'.");
            return true;
        }
        Tool.Press(new SimPoint(n[0], n[1]));
        Tool.Move(new SimPoint(n[0] + n[2] - 1, n[1] + n[3] - 1));
        ReleaseZoneTool();
        return true;
    }

    private async void ScreenshotAndQuit(string path)
    {
        try
        {
            double delay = ScreenshotDelaySeconds;
            if (Env("RETURN_SCREENSHOT_DELAY") is { } d && double.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && seconds >= 0)
                delay = seconds;
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
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
        if (_nextSeed != 0) return _nextSeed;
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
