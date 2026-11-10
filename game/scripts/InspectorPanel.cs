using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;

namespace Return.Game;

/// <summary>
/// The person inspector, opened by clicking someone on the map: name, species, status, where they
/// belong (culture and faith in plain words, with their ways and gods in its tooltip), current
/// job, needs and mood as small bars with numbers, what their nature binds them to and their
/// bargains (ADR-0022), traits (each with where it came from), what they want, work priorities
/// (click to change: an order through SimHost.Order; work their nature refuses is shown but cannot
/// be picked), their day as planned and as lived (ADR-0029; paint to change), opinions, grudges,
/// debts and recent thoughts, all from StateApi. It floats near the right edge, refreshes a few times a
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
    private Label _belonging = null!;
    // Your own lines (ADR-0030): your clock, your gift, the lives you lost and what you remember.
    private Label _life = null!;
    private GridContainer _fate = null!;
    private Label _gift = null!;
    private Label _livesLost = null!;
    private Label _remembers = null!;
    private Label _job = null!;
    private Label _traits = null!;
    private Label _wants = null!;
    private GridContainer _bars = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _content = null!;
    private VBoxContainer _opinions = null!;
    private VBoxContainer _grudges = null!;
    private VBoxContainer _debts = null!;
    private VBoxContainer _thoughts = null!;
    private VBoxContainer _natureSection = null!;
    private VBoxContainer _nature = null!;
    private VBoxContainer _workSection = null!;
    private GridContainer _workGrid = null!;
    private VBoxContainer _daySection = null!;
    private DayStripControl _planned = null!;
    private DayStripControl _lived = null!;
    private Label _dayNote = null!;
    private Label _dayLatest = null!;
    private Label _dayPending = null!;
    private Button _daySame = null!;
    private readonly List<(DayBlock Block, Button Button)> _dayBrushes = new();
    private DayBlock _dayBrush = DayBlock.Sleep;
    private PersonDayState? _day;
    private readonly List<(Label Name, HBoxContainer Picker, List<(int Value, Button Button)> Buttons)> _workRows = new();
    private IReadOnlyList<WorkPriorityState> _workState = Array.Empty<WorkPriorityState>();
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
        _belonging = Ui.MakeLabel("", Ui.SmallFontSize, Ui.Dim);
        _belonging.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _belonging.CustomMinimumSize = new Vector2(ContentWidth, 0);
        _belonging.MouseFilter = MouseFilterEnum.Stop;
        outer.AddChild(_belonging);
        _life = Ui.MakeLabel("", Ui.SmallFontSize, Ui.Dim);
        _life.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _life.CustomMinimumSize = new Vector2(ContentWidth, 0);
        outer.AddChild(_life);
        _fate = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _fate.AddThemeConstantOverride("h_separation", 8);
        _fate.AddThemeConstantOverride("v_separation", 2);
        _gift = FateRow(Ui.T("life.gift"));
        _gift.MouseFilter = MouseFilterEnum.Stop;
        _livesLost = FateRow(Ui.T("life.lives_lost"));
        _remembers = FateRow(Ui.T("life.remembers"));
        outer.AddChild(_fate);
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

        // What a person's nature binds them to, and their bargains: only for those whose nature has a need.
        _natureSection = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _natureSection.AddThemeConstantOverride("separation", 1);
        _natureSection.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
        _natureSection.AddChild(Ui.MakeLabel(Ui.T("ui.inspector.nature"), Ui.SmallFontSize, Ui.Accent));
        _nature = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _nature.AddThemeConstantOverride("separation", 1);
        _natureSection.AddChild(_nature);
        _content.AddChild(_natureSection);

        AddTitle(Ui.T("ui.inspector.traits"));
        _traits = WrappingLabel();
        _content.AddChild(_traits);

        AddTitle(Ui.T("ui.inspector.wants"));
        _wants = WrappingLabel();
        _content.AddChild(_wants);

        _workSection = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _workSection.AddThemeConstantOverride("separation", 2);
        _workSection.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
        _workSection.AddChild(Ui.MakeLabel(Ui.T("ui.inspector.work"), Ui.SmallFontSize, Ui.Accent));
        _workGrid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _workGrid.AddThemeConstantOverride("h_separation", 8);
        _workGrid.AddThemeConstantOverride("v_separation", 2);
        _workSection.AddChild(_workGrid);
        _content.AddChild(_workSection);
        BuildDay();

        _opinions = AddList(Ui.T("ui.inspector.opinions"));
        _grudges = AddList(Ui.T("ui.inspector.grudges"));
        _debts = AddList(Ui.T("ui.inspector.debts"));
        _thoughts = AddList(Ui.T("ui.inspector.thoughts"));
    }

    private Label FateRow(string title)
    {
        _fate.AddChild(Ui.MakeLabel(title, Ui.SmallFontSize, Ui.Accent));
        Label value = Ui.MakeLabel("", Ui.SmallFontSize);
        value.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        value.CustomMinimumSize = new Vector2(ContentWidth - 80f, 0);
        _fate.AddChild(value);
        return value;
    }

    /// <summary>
    /// The Day section (ADR-0029): their hours as planned (their own bright, the camp's dim, the sun's hatched) to paint
    /// with a brush, the last 24 hours as lived (a notch where an hour bent), "Same as the camp", the plain source of
    /// their day and the latest schedule line about them.
    /// </summary>
    private void BuildDay()
    {
        _daySection = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _daySection.AddThemeConstantOverride("separation", 2);
        _daySection.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = MouseFilterEnum.Ignore });
        _daySection.AddChild(Ui.MakeLabel(Ui.T("ui.inspector.day"), Ui.SmallFontSize, Ui.Accent));
        _daySection.AddChild(Ui.MakeLabel(Ui.T("ui.inspector.day_planned"), Ui.SmallFontSize, Ui.Dim));
        _planned = new DayStripControl { Picks = true };
        _planned.Picked += PaintDay;
        _daySection.AddChild(_planned);
        _daySection.AddChild(Ui.MakeLabel(Ui.T("ui.inspector.day_lived"), Ui.SmallFontSize, Ui.Dim));
        _lived = new DayStripControl();
        _daySection.AddChild(_lived);
        var paint = new HBoxContainer();
        paint.AddThemeConstantOverride("separation", 3);
        var group = new ButtonGroup();
        foreach (DayBlock b in Enum.GetValues<DayBlock>())
        {
            DayBlock block = b;
            string id = Return.Sim.Systems.ScheduleSystem.BlockId(b);
            Button brush = Ui.MakeButton(Ui.T("block." + id), Ui.T("block." + id + "_hint"));
            brush.ToggleMode = true;
            brush.ButtonGroup = group;
            brush.AddThemeFontSizeOverride("font_size", Ui.SmallFontSize);
            brush.Pressed += () => _dayBrush = block;
            paint.AddChild(brush);
            _dayBrushes.Add((b, brush));
        }
        _daySame = Ui.MakeButton(Ui.T("ui.inspector.day_same"), Ui.T("ui.inspector.day_same_hint"));
        _daySame.AddThemeFontSizeOverride("font_size", Ui.SmallFontSize);
        _daySame.Pressed += () =>
        {
            if (_host == null || _host.SelectedId == 0) return;
            _host.Order(new SetPersonDay(_host.World.Tick, _host.SelectedId, new DayBlock?[DayStrip.Hours]));
            Refresh();
        };
        paint.AddChild(_daySame);
        _daySection.AddChild(paint);
        _dayNote = WrappingLabel();
        _daySection.AddChild(_dayNote);
        _dayLatest = WrappingLabel();
        _dayLatest.Modulate = Ui.Dim;
        _daySection.AddChild(_dayLatest);
        _dayPending = Ui.MakeLabel(Ui.T("ui.inspector.day_pending"), Ui.SmallFontSize, Ui.Accent);
        _daySection.AddChild(_dayPending);
        _content.AddChild(_daySection);
    }

    private void RefreshDay()
    {
        World w = _host!.World;
        Character? c = w.Find(_host.SelectedId);
        _daySection.Visible = c is { Status: CharStatus.Member };
        if (!_daySection.Visible) return;
        PersonDayState d = StateApi.Day(w, c!.Id, _host.Drafts.All);
        _day = d;
        var planned = new List<DayStripControl.Cell>(d.Planned.Count);
        foreach (HourState h in d.Planned) planned.Add(new DayStripControl.Cell(DayColors.Of(h.Block), h.Source == "camp", h.Sun, false, false, h.Note));
        _planned.SetCells(planned, d.Pending);
        var lived = new List<DayStripControl.Cell>(d.Lived.Count);
        foreach (HourState h in d.Lived) lived.Add(new DayStripControl.Cell(DayColors.Of(h.Block), false, false, h.Differs, h.Block == "watched", h.Note));
        _lived.SetCells(lived, false);
        foreach ((DayBlock block, Button button) in _dayBrushes)
        {
            bool off = block == DayBlock.Sleep && !d.Sleeps;
            button.Disabled = off;
            button.TooltipText = off ? Ui.T("ui.schedule.no_sleep_brush") : Ui.T("block." + Return.Sim.Systems.ScheduleSystem.BlockId(block) + "_hint");
            button.SetPressedNoSignal(block == _dayBrush);
        }
        _daySame.Disabled = d.SameAsCamp;
        _dayNote.Text = d.Note;
        _dayLatest.Visible = d.Latest.Length > 0;
        _dayLatest.Text = d.Latest.Length > 0 ? Ui.F("ui.inspector.day_latest", ("line", d.Latest)) : "";
        _dayPending.Visible = d.Pending;
    }

    /// <summary>Paints the picked hours of the open person's day with the brush, as their own (those who never sleep take no sleep brush).</summary>
    private void PaintDay(int press, int release)
    {
        (int first, int last) = DayStrip.Dragged(press, release);
        PaintDayCells(first, last);
    }

    private void PaintDayCells(int first, int last)
    {
        if (_host == null || _host.SelectedId == 0) return;
        PersonDayState d = StateApi.Day(_host.World, _host.SelectedId, _host.Drafts.All);
        if (_dayBrush == DayBlock.Sleep && !d.Sleeps) return;
        var own = new DayBlock?[DayStrip.Hours];
        foreach (HourState h in d.Planned)
            if (h.Source == "own") own[h.Hour] = Return.Sim.Systems.ScheduleSystem.ParseBlock(h.Block);
        _host.Order(new SetPersonDay(_host.World.Tick, _host.SelectedId, DayStrip.PaintOwn(own, first, last, _dayBrush)));
        Refresh();
    }

    /// <summary>Presses a brush (0 sleep, 1 work, 2 free time) and paints the open person's hours from <paramref name="from"/> to <paramref name="to"/> (exclusive, wrapping) through the panel's own code (RETURN_PRESS checks).</summary>
    public bool PressDay(int block, int from, int to)
    {
        if (_host == null || _host.SelectedId == 0) return false;
        Refresh();
        if (!_daySection.Visible || block < 0 || block >= _dayBrushes.Count || DayStrip.FromTo(from, to) is not { } cells) return false;
        _dayBrushes[block].Button.EmitSignal(BaseButton.SignalName.Pressed);
        PaintDayCells(cells.First, cells.Last);
        return true;
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
        _belonging.Text = s.Belonging.Line;
        _belonging.TooltipText = ViewText.BelongingTooltip(s.Belonging);
        bool you = s.Id == _host.World.PlayerId && _host.World.Fate.PeopleId.Length > 0;
        _life.Visible = you;
        _fate.Visible = you;
        if (you)
        {
            FateText.PlayerLines p = FateText.Player(_host.World);
            _subtitle.Text = p.Subtitle;
            _subtitle.TooltipText = FateText.Opening(_host.World) is { Count: > 1 } opening ? opening[1] : "";
            _life.Text = p.Life;
            _gift.Text = p.Gift.Length == 0 ? Ui.T("ui.inspector.none") : p.Gift;
            _gift.TooltipText = p.GiftTooltip;
            _livesLost.Text = p.LivesLost;
            _remembers.Text = p.Remembers.Count == 0 ? Ui.T("life.remembers_none") : string.Join("\n", p.Remembers);
        }
        else _subtitle.TooltipText = "";
        _job.Text = Ui.F("ui.inspector.job", ("job", s.Job));

        for (int i = 0; i < s.Needs.Count; i++) SetBar(i, s.Needs[i].Label, s.Needs[i].Value, 1000);
        SetBar(s.Needs.Count, Ui.T("ui.inspector.mood"), s.Mood, 100);
        for (int i = 0; i < _barRows.Count; i++) _barRows[i].SetVisible(i <= s.Needs.Count);

        _traits.Text = ViewText.TraitsText(s.Growth);
        _wants.Text = ViewText.WantLine(s.Growth);
        RefreshWork();
        RefreshDay();

        var lines = new List<string>();
        foreach (NatureNeedState n in StateApi.NatureNeeds(_host.World, s.Id)) lines.Add(ViewText.NatureLine(n));
        lines.AddRange(ViewText.BargainLines(_host.World, s.Id));
        _natureSection.Visible = lines.Count > 0;
        if (lines.Count > 0) SetLines(_nature, lines);

        lines.Clear();
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

    /// <summary>One row per kind of work: its name and a picker 1 2 3 4 – (1 is done first; – means never).</summary>
    private void RefreshWork()
    {
        World w = _host!.World;
        Character? c = w.Find(_host.SelectedId);
        _workSection.Visible = c is { Status: CharStatus.Member };
        if (!_workSection.Visible) return;
        _workState = StateApi.WorkPriorities(w, c!.Id);
        while (_workRows.Count < _workState.Count)
        {
            int row = _workRows.Count;
            Label name = Ui.MakeLabel("", Ui.SmallFontSize);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var picker = new HBoxContainer();
            picker.AddThemeConstantOverride("separation", 2);
            var group = new ButtonGroup();
            var buttons = new List<(int Value, Button Button)>();
            foreach (int value in ViewText.PriorityValues)
            {
                int v = value;
                Button b = Ui.MakeButton(ViewText.PriorityLabel(v), Ui.T("ui.inspector.priority_hint"));
                b.ToggleMode = true;
                b.ButtonGroup = group;
                b.CustomMinimumSize = new Vector2(24, 0);
                b.Pressed += () => ChoosePriority(row, v);
                picker.AddChild(b);
                buttons.Add((v, b));
            }
            _workGrid.AddChild(name);
            _workGrid.AddChild(picker);
            _workRows.Add((name, picker, buttons));
        }
        for (int i = 0; i < _workRows.Count; i++)
        {
            bool visible = i < _workState.Count;
            _workRows[i].Name.Visible = visible;
            _workRows[i].Picker.Visible = visible;
            if (!visible) continue;
            WorkPriorityState s = _workState[i];
            int? draft = _host.Drafts.Priority(c.Id, s.WorkId);
            int shown = draft ?? s.Priority;
            bool pending = s.Pending || (draft != null && draft != JobPickerPriority(c, s.WorkId));
            // Work their nature refuses (a dryad's axe) shows as never, and cannot be picked.
            bool refused = ViewText.RefusesByNature(w, c, s.WorkId);
            _workRows[i].Name.Text = refused ? ViewText.WorkRefusedLabel(s.Work) : pending ? Ui.F("ui.inspector.work_pending", ("work", s.Work)) : s.Work;
            foreach ((int value, Button button) in _workRows[i].Buttons)
            {
                button.Disabled = refused;
                button.SetPressedNoSignal(refused ? value == 0 : value == shown);
            }
        }
    }

    private static int JobPickerPriority(Character c, string workId) => Return.Sim.Systems.JobPicker.PriorityOf(c, workId);

    /// <summary>Picks a priority through the button's own signal (RETURN_PRESS checks): row and value (0 = never).</summary>
    public bool PressPriority(int row, int value)
    {
        if (_host == null || _host.SelectedId == 0) return false;
        Refresh();
        if (row < 0 || row >= _workRows.Count || !_workSection.Visible) return false;
        foreach ((int v, Button button) in _workRows[row].Buttons)
            if (v == value) { button.EmitSignal(BaseButton.SignalName.Pressed); return true; }
        return false;
    }

    private void ChoosePriority(int row, int value)
    {
        if (_host == null || _host.SelectedId == 0 || row >= _workState.Count) return;
        if (_host.World.Find(_host.SelectedId) is not { Status: CharStatus.Member }) { Refresh(); return; }
        WorkPriorityState s = _workState[row];
        if (ViewText.RefusesByNature(_host.World, _host.World.Get(_host.SelectedId), s.WorkId)) { Refresh(); return; }
        int shown = _host.Drafts.Priority(_host.SelectedId, s.WorkId) ?? s.Priority;
        if (value != shown) _host.Order(new SetWorkPriority(_host.World.Tick, _host.SelectedId, s.WorkId, value));
        Refresh();
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
