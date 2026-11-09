using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;

namespace Return.Game;

/// <summary>
/// The one menu bar along the bottom edge (ADR-0008): Build, Zones and Schedule (shown but disabled
/// until the sim has them), Orders (the ration policy) and Work (who does what first), People and
/// Chronicle, each opening a small list on demand; then the clock, and pause plus the speed buttons
/// with the active one highlighted. Nothing else stays on screen. Choices become commands through
/// SimHost.Order.
/// </summary>
public partial class BottomBar : PanelContainer
{
    public const float Height = 40f;

    private const float PopupGap = 6f;
    private const double RefreshSeconds = 0.25;

    private SimHost? _host;
    private Label _clock = null!;
    private readonly List<Button> _speedButtons = new();
    private ListPopup _orders = null!;
    private ListPopup _work = null!;
    private ListPopup _people = null!;
    private ListPopup _chronicle = null!;
    private ListPopup? _open;
    private readonly List<int> _peopleIds = new();
    private readonly List<int> _workIds = new();
    private readonly List<(RationPolicy Policy, Button Button)> _policyButtons = new();
    private Label? _ordersPending;
    private long _clockTick = -1;
    private double _refresh;

    public void Bind(SimHost host)
    {
        _host = host;
        Theme = Ui.Theme;
        SetAnchorsPreset(LayoutPreset.BottomWide);
        OffsetLeft = 0;
        OffsetRight = 0;
        OffsetTop = -Height;
        OffsetBottom = 0;
        GrowVertical = GrowDirection.Begin;
        CustomMinimumSize = new Vector2(0, Height);
        StyleBoxFlat strip = Ui.Box(Ui.PanelBg, Ui.Edge, 0, 0, 10, 5);
        strip.BorderWidthTop = 1;
        AddThemeStyleboxOverride("panel", strip);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        AddChild(row);

        Node layer = GetParent();
        _orders = new ListPopup(layer, Ui.T("ui.orders.title"), 320f);
        _work = new ListPopup(layer, Ui.T("ui.work.title"), 380f);
        _people = new ListPopup(layer, Ui.T("ui.people.title"), 300f);
        _chronicle = new ListPopup(layer, Ui.T("ui.chronicle.title"), 540f);

        row.AddChild(NotYet("ui.bar.build"));
        row.AddChild(Toggle(_orders, Ui.T("ui.bar.orders"), Ui.T("ui.bar.orders_hint")));
        row.AddChild(NotYet("ui.bar.zones"));
        row.AddChild(Toggle(_work, Ui.T("ui.bar.work"), Ui.T("ui.bar.work_hint")));
        row.AddChild(NotYet("ui.bar.schedule"));
        row.AddChild(new VSeparator());
        row.AddChild(Toggle(_people, Ui.T("ui.bar.people"), Ui.T("ui.bar.people_hint")));
        row.AddChild(Toggle(_chronicle, Ui.T("ui.bar.chronicle"), Ui.T("ui.bar.chronicle_hint")));

        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        _clock = Ui.MakeLabel("");
        _clock.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(_clock);
        row.AddChild(new VSeparator());

        var group = new ButtonGroup();
        for (int s = 0; s < host.Clock.SpeedCount; s++)
        {
            int speed = s;
            int multiplier = host.Clock.MultiplierOf(s);
            string hint = s == 0
                ? Ui.T("ui.speed.pause_hint")
                : Ui.F("ui.speed.multiplier_hint", ("n", Ui.N(multiplier)), ("key", Ui.N(s)));
            Button b = Ui.MakeButton(ViewText.SpeedLabel(multiplier), hint);
            b.ToggleMode = true;
            b.ButtonGroup = group;
            b.CustomMinimumSize = new Vector2(s == 0 ? 64 : 44, 0);
            b.Pressed += () => _host?.SetSpeed(speed);
            row.AddChild(b);
            _speedButtons.Add(b);
        }

        host.SpeedChanged += SyncSpeed;
        SyncSpeed();
        UpdateClock();
    }

    /// <summary>Opens a list by name (orders, work, people, chronicle); false if there is no such list.</summary>
    public bool Open(string name)
    {
        ListPopup? popup = name switch { "orders" => _orders, "work" => _work, "people" => _people, "chronicle" => _chronicle, _ => null };
        if (popup == null) return false;
        SetOpen(popup);
        return true;
    }

    /// <summary>Closes the open list, if any; true when something was closed.</summary>
    public bool ClosePopups()
    {
        if (_open == null) return false;
        SetOpen(null);
        return true;
    }

    public override void _Process(double delta)
    {
        if (_host == null) return;
        if (_host.World.Tick != _clockTick) UpdateClock();
        if (_open == null) return;
        _refresh -= delta;
        if (_refresh <= 0)
        {
            _refresh = RefreshSeconds;
            if (_open == _people) RefreshPeople();
            else if (_open == _chronicle) RefreshChronicle();
            else if (_open == _orders) RefreshOrders();
            else RefreshWork();
        }
        Place(_open);
    }

    private static Button NotYet(string key)
    {
        Button b = Ui.MakeButton(Ui.T(key), Ui.T("ui.bar.not_yet"));
        b.Disabled = true;
        return b;
    }

    private Button Toggle(ListPopup popup, string text, string hint)
    {
        Button b = Ui.MakeButton(text, hint);
        b.ToggleMode = true;
        b.Toggled += on => SetOpen(on ? popup : _open == popup ? null : _open);
        popup.Toggle = b;
        return b;
    }

    private void SetOpen(ListPopup? popup)
    {
        _open = popup;
        foreach (ListPopup p in new[] { _orders, _work, _people, _chronicle })
        {
            bool open = p == popup;
            p.Panel.Visible = open;
            p.Toggle.SetPressedNoSignal(open);
        }
        _refresh = 0;
    }

    private void SyncSpeed()
    {
        if (_host == null) return;
        for (int i = 0; i < _speedButtons.Count; i++) _speedButtons[i].SetPressedNoSignal(i == _host.Clock.Speed);
    }

    private void UpdateClock()
    {
        World w = _host!.World;
        _clockTick = w.Tick;
        _clock.Text = ViewText.Clock(w);
    }

    private void RefreshPeople()
    {
        IReadOnlyList<ViewText.PersonLine> lines = ViewText.People(_host!.World);
        _peopleIds.Clear();
        foreach (ViewText.PersonLine line in lines) _peopleIds.Add(line.Id);
        int rows = Math.Max(1, lines.Count);
        while (_people.Rows.GetChildCount() < rows)
        {
            int index = _people.Rows.GetChildCount();
            Button b = Ui.MakeButton("");
            b.Flat = true;
            b.Alignment = HorizontalAlignment.Left;
            b.Pressed += () => OnPersonPressed(index);
            _people.Rows.AddChild(b);
        }
        for (int i = 0; i < _people.Rows.GetChildCount(); i++)
        {
            var b = _people.Rows.GetChild<Button>(i);
            b.Visible = i < rows;
            b.Disabled = lines.Count == 0;
            b.Text = lines.Count == 0 ? Ui.T("ui.people.empty") : i < lines.Count ? lines[i].Text : "";
        }
    }

    private void OnPersonPressed(int index)
    {
        if (_host == null || index >= _peopleIds.Count) return;
        Character? c = _host.World.Find(_peopleIds[index]);
        if (c == null) return;
        _host.Select(c.Id);
        _host.FocusCamera(c.Pos);
    }

    /// <summary>"When food runs short": the three ration policies, the current one pressed.</summary>
    private void RefreshOrders()
    {
        if (_policyButtons.Count == 0)
        {
            var group = new ButtonGroup();
            foreach (RationPolicy p in Enum.GetValues<RationPolicy>())
            {
                RationPolicy policy = p;
                Button b = Ui.MakeButton(ViewText.PolicyOption(policy), ViewText.PolicyHint(policy));
                b.ToggleMode = true;
                b.ButtonGroup = group;
                b.Alignment = HorizontalAlignment.Left;
                b.Pressed += () => ChoosePolicy(policy);
                _orders.Rows.AddChild(b);
                _policyButtons.Add((policy, b));
            }
            Label note = Ui.MakeLabel(Ui.T("ui.orders.note"), Ui.SmallFontSize, Ui.Dim);
            note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _orders.Rows.AddChild(note);
            _ordersPending = Ui.MakeLabel(Ui.T("ui.orders.pending"), Ui.SmallFontSize, Ui.Accent);
            _orders.Rows.AddChild(_ordersPending);
        }
        PolicyState s = StateApi.Policy(_host!.World);
        foreach ((RationPolicy policy, Button button) in _policyButtons) button.SetPressedNoSignal(policy == s.Effective);
        _ordersPending!.Visible = s.Pending;
    }

    /// <summary>Presses the nth ration policy button through its signal (RETURN_PRESS checks).</summary>
    public bool PressPolicy(int index)
    {
        if (_host == null) return false;
        RefreshOrders();
        if (index < 0 || index >= _policyButtons.Count) return false;
        _policyButtons[index].Button.EmitSignal(BaseButton.SignalName.Pressed);
        RefreshOrders();
        return true;
    }

    private void ChoosePolicy(RationPolicy policy)
    {
        if (_host == null) return;
        if (StateApi.Policy(_host.World).Effective != policy) _host.Order(new SetRationPolicy(_host.World.Tick, policy));
        _refresh = 0;
    }

    /// <summary>Each member and what they do first; clicking one opens their inspector, where priorities are set.</summary>
    private void RefreshWork()
    {
        if (_work.Rows.GetChildCount() == 0)
        {
            Label hint = Ui.MakeLabel(Ui.T("ui.work.hint"), Ui.SmallFontSize, Ui.Dim);
            hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _work.Rows.AddChild(hint);
        }
        IReadOnlyList<ViewText.PersonLine> lines = ViewText.WorkLines(_host!.World);
        _workIds.Clear();
        foreach (ViewText.PersonLine line in lines) _workIds.Add(line.Id);
        int rows = Math.Max(1, lines.Count);
        while (_work.Rows.GetChildCount() - 1 < rows)
        {
            int index = _work.Rows.GetChildCount() - 1;
            Button b = Ui.MakeButton("");
            b.Flat = true;
            b.Alignment = HorizontalAlignment.Left;
            b.Pressed += () => OnWorkPersonPressed(index);
            _work.Rows.AddChild(b);
        }
        for (int i = 1; i < _work.Rows.GetChildCount(); i++)
        {
            var b = _work.Rows.GetChild<Button>(i);
            int k = i - 1;
            b.Visible = k < rows;
            b.Disabled = lines.Count == 0;
            b.Text = lines.Count == 0 ? Ui.T("ui.people.empty") : k < lines.Count ? lines[k].Text : "";
        }
    }

    private void OnWorkPersonPressed(int index)
    {
        if (_host == null || index >= _workIds.Count) return;
        Character? c = _host.World.Find(_workIds[index]);
        if (c == null) return;
        _host.Select(c.Id);
        _host.FocusCamera(c.Pos);
    }

    private void RefreshChronicle()
    {
        IReadOnlyList<string> lines = ViewText.Chronicle(_host!.World);
        int rows = Math.Max(1, lines.Count);
        while (_chronicle.Rows.GetChildCount() < rows)
        {
            Label label = Ui.MakeLabel("", Ui.SmallFontSize);
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _chronicle.Rows.AddChild(label);
        }
        for (int i = 0; i < _chronicle.Rows.GetChildCount(); i++)
        {
            var label = _chronicle.Rows.GetChild<Label>(i);
            label.Visible = i < rows;
            label.Text = lines.Count == 0 ? Ui.T("ui.chronicle.empty") : i < lines.Count ? lines[i] : "";
            label.Modulate = lines.Count == 0 ? Ui.Dim : Colors.White;
        }
    }

    /// <summary>Sits the list just above the bar, under its button, as tall as its rows allow, and clear of an open inspector.</summary>
    private void Place(ListPopup p)
    {
        Vector2 vp = GetViewportRect().Size;
        float bar = Size.Y;
        float maxRows = Mathf.Max(60f, vp.Y - bar - PopupGap - 90f);
        p.Scroll.CustomMinimumSize = new Vector2(p.Width, Mathf.Min(p.Rows.GetCombinedMinimumSize().Y, maxRows));
        Vector2 size = p.Panel.GetCombinedMinimumSize();
        p.Panel.Size = size;
        float right = Mathf.Min(vp.X, _host?.InspectorLeft ?? vp.X);
        float x = Mathf.Clamp(p.Toggle.GlobalPosition.X, PopupGap, Mathf.Max(PopupGap, right - size.X - PopupGap));
        p.Panel.Position = new Vector2(x, vp.Y - bar - PopupGap - size.Y);
    }

    /// <summary>A titled, scrolling list panel that lives beside the bar on the UI layer.</summary>
    private sealed class ListPopup
    {
        public ListPopup(Node layer, string title, float width)
        {
            Width = width;
            Panel = new PanelContainer { Theme = Ui.Theme, Visible = false };
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 6);
            box.AddChild(Ui.MakeLabel(title, Ui.FontSize, Ui.Accent));
            box.AddChild(new HSeparator());
            Scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
            Rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            Rows.AddThemeConstantOverride("separation", 2);
            Scroll.AddChild(Rows);
            box.AddChild(Scroll);
            Panel.AddChild(box);
            layer.AddChild(Panel);
        }

        public float Width { get; }

        public PanelContainer Panel { get; }

        public ScrollContainer Scroll { get; }

        public VBoxContainer Rows { get; }

        public Button Toggle { get; set; } = null!;
    }
}
