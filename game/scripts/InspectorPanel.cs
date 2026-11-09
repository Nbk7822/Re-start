using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim.Api;

namespace Return.Game;

/// <summary>
/// The person inspector, opened by clicking someone on the map: name, species, status, current
/// job, needs and mood as small bars with numbers, traits, opinions, grudges, debts and recent
/// thoughts, all from StateApi.Character. It floats near the right edge, refreshes a few times a
/// second, and closes with its × button, Esc, or a click on empty map.
/// </summary>
public partial class InspectorPanel : PanelContainer
{
    private const float ContentWidth = 320f;
    private const float EdgeGap = 12f;
    private const double RefreshSeconds = 0.25;

    private static readonly Color LowFill = new(0.80f, 0.38f, 0.30f);
    private static readonly Color MidFill = new(0.86f, 0.68f, 0.36f);
    private static readonly Color HighFill = new(0.50f, 0.70f, 0.42f);

    private SimHost? _host;
    private double _refresh;
    private Label _name = null!;
    private Label _subtitle = null!;
    private Label _job = null!;
    private Label _traits = null!;
    private GridContainer _bars = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _content = null!;
    private VBoxContainer _opinions = null!;
    private VBoxContainer _grudges = null!;
    private VBoxContainer _debts = null!;
    private VBoxContainer _thoughts = null!;
    private readonly List<BarRow> _barRows = new();
    private readonly StyleBoxFlat[] _fills = new StyleBoxFlat[3];

    public void Bind(SimHost host)
    {
        _host = host;
        Theme = Ui.Theme;
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        _fills[0] = Ui.Box(LowFill, LowFill, 0, 2, 0, 0);
        _fills[1] = Ui.Box(MidFill, MidFill, 0, 2, 0, 0);
        _fills[2] = Ui.Box(HighFill, HighFill, 0, 2, 0, 0);
        Build();
        host.SelectionChanged += OnSelectionChanged;
    }

    public override void _Process(double delta)
    {
        if (_host == null || !Visible) return;
        _refresh -= delta;
        if (_refresh <= 0)
        {
            _refresh = RefreshSeconds;
            Refresh();
        }
        Place();
    }

    private void OnSelectionChanged()
    {
        Visible = _host != null && _host.SelectedId != 0;
        if (!Visible) return;
        _scroll.ScrollVertical = 0;
        _refresh = RefreshSeconds;
        Refresh();
        Place();
    }

    private void Build()
    {
        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 4);
        AddChild(outer);

        var header = new HBoxContainer();
        outer.AddChild(header);
        _name = Ui.MakeLabel("", Ui.TitleFontSize);
        _name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(_name);
        Button close = Ui.MakeButton(Ui.T("ui.inspector.close"), Ui.T("ui.inspector.close_hint"));
        close.Flat = true;
        close.Pressed += () => _host?.Select(0);
        header.AddChild(close);

        _subtitle = Ui.MakeLabel("", Ui.FontSize, Ui.Dim);
        outer.AddChild(_subtitle);
        _job = Ui.MakeLabel("");
        outer.AddChild(_job);
        outer.AddChild(new HSeparator());

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        outer.AddChild(_scroll);
        _content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 3);
        _scroll.AddChild(_content);

        AddTitle(Ui.T("ui.inspector.needs"));
        _bars = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _bars.AddThemeConstantOverride("h_separation", 8);
        _bars.AddThemeConstantOverride("v_separation", 2);
        _content.AddChild(_bars);

        AddTitle(Ui.T("ui.inspector.traits"));
        _traits = WrappingLabel();
        _content.AddChild(_traits);

        _opinions = AddList(Ui.T("ui.inspector.opinions"));
        _grudges = AddList(Ui.T("ui.inspector.grudges"));
        _debts = AddList(Ui.T("ui.inspector.debts"));
        _thoughts = AddList(Ui.T("ui.inspector.thoughts"));
    }

    private void AddTitle(string text)
    {
        Label title = Ui.MakeLabel(text, Ui.SmallFontSize, Ui.Accent);
        title.AddThemeConstantOverride("line_spacing", 0);
        var spacer = new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore };
        _content.AddChild(spacer);
        _content.AddChild(title);
    }

    private VBoxContainer AddList(string title)
    {
        AddTitle(title);
        var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 1);
        _content.AddChild(list);
        return list;
    }

    private static Label WrappingLabel()
    {
        Label label = Ui.MakeLabel("", Ui.SmallFontSize);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return label;
    }

    private void Refresh()
    {
        if (_host == null || _host.SelectedId == 0) return;
        CharacterState s = StateApi.Character(_host.World, _host.SelectedId);
        _name.Text = s.Name;
        _subtitle.Text = Ui.F("ui.inspector.subtitle", ("species", s.Species), ("status", s.Status));
        _job.Text = Ui.F("ui.inspector.job", ("job", s.Job));

        for (int i = 0; i < s.Needs.Count; i++) SetBar(i, s.Needs[i].Label, s.Needs[i].Value, 1000);
        SetBar(s.Needs.Count, Ui.T("ui.inspector.mood"), s.Mood, 100);
        for (int i = 0; i < _barRows.Count; i++) _barRows[i].SetVisible(i <= s.Needs.Count);

        _traits.Text = ViewText.JoinList(s.Traits);

        var lines = new List<string>();
        foreach (OpinionState o in s.Opinions) lines.Add(ViewText.OpinionLine(o));
        SetLines(_opinions, lines);

        lines.Clear();
        foreach (GrudgeState g in s.Grudges) lines.Add(ViewText.GrudgeLine(g));
        SetLines(_grudges, lines);

        lines.Clear();
        foreach (DebtState d in s.Debts) lines.Add(ViewText.DebtLine(_host.World, d));
        SetLines(_debts, lines);

        lines.Clear();
        for (int i = s.RecentThoughts.Count - 1; i >= 0; i--) lines.Add(s.RecentThoughts[i]);
        SetLines(_thoughts, lines);
    }

    private void SetBar(int row, string label, int value, int max)
    {
        while (_barRows.Count <= row) _barRows.Add(new BarRow(_bars));
        _barRows[row].Set(label, value, max, _fills);
    }

    private static void SetLines(VBoxContainer list, IReadOnlyList<string> lines)
    {
        int rows = Math.Max(1, lines.Count);
        while (list.GetChildCount() < rows) list.AddChild(WrappingLabel());
        for (int i = 0; i < list.GetChildCount(); i++)
        {
            var label = list.GetChild<Label>(i);
            label.Visible = i < rows;
            label.Text = lines.Count == 0 ? Ui.T("ui.inspector.none") : i < lines.Count ? lines[i] : "";
            label.Modulate = lines.Count == 0 ? Ui.Dim : Colors.White;
        }
    }

    /// <summary>Floats near the top-right corner; the list area scrolls when it would reach the bar.</summary>
    private void Place()
    {
        Vector2 vp = GetViewportRect().Size;
        float maxPanel = Mathf.Max(160f, vp.Y - _host!.BarHeight - 2 * EdgeGap);
        float header = GetCombinedMinimumSize().Y - _scroll.GetCombinedMinimumSize().Y;
        float want = _content.GetCombinedMinimumSize().Y;
        _scroll.CustomMinimumSize = new Vector2(ContentWidth, Mathf.Clamp(want, 40f, Mathf.Max(40f, maxPanel - header)));
        Vector2 size = GetCombinedMinimumSize();
        Size = size;
        Position = new Vector2(Mathf.Max(EdgeGap, vp.X - size.X - EdgeGap), EdgeGap);
    }

    /// <summary>One need: name, bar and number.</summary>
    private sealed class BarRow
    {
        private readonly Label _name;
        private readonly ProgressBar _bar;
        private readonly Label _value;
        private int _band = -1;

        public BarRow(GridContainer grid)
        {
            _name = Ui.MakeLabel("", Ui.SmallFontSize);
            _bar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = 1,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(150, 9),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _value = Ui.MakeLabel("", Ui.SmallFontSize);
            _value.HorizontalAlignment = HorizontalAlignment.Right;
            _value.CustomMinimumSize = new Vector2(38, 0);
            grid.AddChild(_name);
            grid.AddChild(_bar);
            grid.AddChild(_value);
        }

        public void Set(string label, int value, int max, StyleBoxFlat[] fills)
        {
            _name.Text = label;
            _bar.MaxValue = Math.Max(1, max);
            _bar.Value = value;
            _value.Text = Ui.N(value);
            float fraction = max > 0 ? (float)value / max : 0f;
            int band = fraction < 0.3f ? 0 : fraction < 0.6f ? 1 : 2;
            if (band == _band) return;
            _band = band;
            _bar.AddThemeStyleboxOverride("fill", fills[band]);
        }

        public void SetVisible(bool visible)
        {
            _name.Visible = visible;
            _bar.Visible = visible;
            _value.Visible = visible;
        }
    }
}
