using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Return.Game.Art;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;

namespace Return.Game;

/// <summary>
/// The Schedule list on the bottom bar (ADR-0029): the camp's day as a strip to paint with a brush, the night folk's
/// day and the day of those who never sleep while any such live in camp, the watch's hours as a row to drag (or none),
/// who keeps it now, has kept it tonight and kept it last night, and whose day differs (a click opens their day). Every strip is one day;
/// there is never a table of people against hours (fixed decision 2). Choices go out through SimHost.Order, as drafts
/// while paused, and the list shows what will be once they apply.
/// </summary>
public sealed class ScheduleList
{
    private readonly SimHost _host;
    private readonly VBoxContainer _rows;
    private readonly Action _close;
    private DayBlock _brush = DayBlock.Sleep;
    private string _signature = "";
    private readonly List<(string Rhythm, DayStripControl Strip, Label Line, bool Sleeps)> _days = new();
    private readonly List<(DayBlock Block, Button Button)> _brushes = new();
    private DayStripControl? _watch;
    private Label? _watchText, _watchNow, _watchTonight, _watchLast, _pending;
    private Button? _noWatch;

    public ScheduleList(SimHost host, VBoxContainer rows, Action close)
    {
        _host = host;
        _rows = rows;
        _close = close;
    }

    public void Refresh()
    {
        World w = _host.World;
        ScheduleState st = StateApi.Schedule(w, _host.Drafts.All);
        string signature = Signature(st);
        if (signature != _signature)
        {
            _signature = signature;
            Rebuild(w, st);
        }
        Update(w, st);
    }

    /// <summary>The strips shown (a kind of day with someone keeping it; the camp's always) and whose day differs.</summary>
    private static string Signature(ScheduleState st)
    {
        var s = new StringBuilder();
        foreach (DayState d in st.CampDays)
            if (Shown(d)) s.Append(d.Rhythm).Append(':').Append(string.Join(",", d.Members)).Append(';');
        foreach (PersonDaySummary p in st.Different) s.Append(p.Id).Append('|').Append(p.Summary).Append(';');
        return s.ToString();
    }

    private static bool Shown(DayState d) => d.Rhythm == Return.Sim.Systems.ScheduleSystem.Diurnal || d.Members.Count > 0;

    private void Rebuild(World w, ScheduleState st)
    {
        foreach (Node n in _rows.GetChildren())
        {
            _rows.RemoveChild(n);
            n.QueueFree();
        }
        _days.Clear();
        _brushes.Clear();

        var paint = new HBoxContainer();
        paint.AddThemeConstantOverride("separation", 4);
        paint.AddChild(Ui.MakeLabel(Ui.T("ui.schedule.paint"), Ui.SmallFontSize, Ui.Dim));
        var group = new ButtonGroup();
        foreach (DayBlock b in Enum.GetValues<DayBlock>())
        {
            DayBlock block = b;
            Button brush = Ui.MakeButton(Ui.T("block." + Return.Sim.Systems.ScheduleSystem.BlockId(b)), Ui.T("block." + Return.Sim.Systems.ScheduleSystem.BlockId(b) + "_hint"));
            brush.ToggleMode = true;
            brush.ButtonGroup = group;
            brush.Pressed += () => _brush = block;
            paint.AddChild(brush);
            _brushes.Add((b, brush));
        }
        _rows.AddChild(paint);
        _rows.AddChild(Ui.MakeLabel(Ui.T("ui.schedule.brush_hint"), Ui.SmallFontSize, Ui.Dim));

        foreach (DayState d in st.CampDays)
        {
            if (!Shown(d)) continue;
            string title = d.Rhythm == Return.Sim.Systems.ScheduleSystem.Diurnal
                ? d.Title
                : Ui.F("ui.schedule.day_of", ("day", d.Title), ("names", ViewText.JoinList(d.Members.Select(id => EventText.Name(w, id)).ToList())));
            _rows.AddChild(Ui.MakeLabel(title, Ui.SmallFontSize, Ui.Accent));
            var strip = new DayStripControl { Picks = true };
            string rhythm = d.Rhythm;
            bool sleeps = d.Rhythm != Return.Sim.Systems.ScheduleSystem.Sleepless;
            strip.Picked += (press, release) => PaintDay(rhythm, sleeps, press, release);
            _rows.AddChild(strip);
            Label line = Ui.MakeLabel("", Ui.SmallFontSize, Ui.Dim);
            line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _rows.AddChild(line);
            _days.Add((rhythm, strip, line, sleeps));
        }

        _rows.AddChild(new HSeparator());
        var watchHead = new HBoxContainer();
        Label watchTitle = Ui.MakeLabel(Ui.T("ui.schedule.watch"), Ui.SmallFontSize, Ui.Accent);
        watchTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        watchHead.AddChild(watchTitle);
        _noWatch = Ui.MakeButton(Ui.T("ui.schedule.no_watch"), Ui.T("ui.schedule.no_watch_hint"));
        _noWatch.Pressed += () => _host.Order(new SetWatchHours(_host.World.Tick, 0, 0));
        watchHead.AddChild(_noWatch);
        _rows.AddChild(watchHead);
        _watch = new DayStripControl { Picks = true, TooltipText = Ui.T("ui.schedule.watch_hint") };
        _watch.Picked += (press, release) =>
        {
            (int from, int to) = DayStrip.WatchHours(press, release);
            _host.Order(new SetWatchHours(_host.World.Tick, from, to));
        };
        _rows.AddChild(_watch);
        _watchText = Wrapping("", Ui.Ink);
        _watchNow = Wrapping("", Ui.Ink);
        _watchTonight = Wrapping("", Ui.Dim);
        _watchLast = Wrapping("", Ui.Dim);
        Wrapping(Ui.T("ui.schedule.asked_first"), Ui.Dim);

        _rows.AddChild(new HSeparator());
        _rows.AddChild(Ui.MakeLabel(Ui.T("ui.schedule.different"), Ui.SmallFontSize, Ui.Accent));
        if (st.Different.Count == 0) _rows.AddChild(Ui.MakeLabel(Ui.T("ui.schedule.different_none"), Ui.SmallFontSize, Ui.Dim));
        foreach (PersonDaySummary p in st.Different)
        {
            int id = p.Id;
            Button row = Ui.MakeButton(Ui.F("ui.schedule.different_row", ("name", p.Name), ("summary", p.Summary)), Ui.T("ui.schedule.different_hint"));
            row.Flat = true;
            row.Alignment = HorizontalAlignment.Left;
            row.ClipText = true;
            row.Pressed += () =>
            {
                if (_host.World.Find(id) is not { } c) return;
                _host.Select(id);
                _host.FocusCamera(c.Pos);
            };
            _rows.AddChild(row);
        }
        _pending = Ui.MakeLabel(Ui.T("ui.schedule.pending"), Ui.SmallFontSize, Ui.Accent);
        _rows.AddChild(_pending);
    }

    private Label Wrapping(string text, Color color)
    {
        Label label = Ui.MakeLabel(text, Ui.SmallFontSize, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _rows.AddChild(label);
        return label;
    }

    private void Update(World w, ScheduleState st)
    {
        foreach ((DayBlock block, Button button) in _brushes) button.SetPressedNoSignal(block == _brush);
        bool waiting = _host.Drafts.HasScheduleDrafts || st.Watch.Pending;
        foreach ((string rhythm, DayStripControl strip, Label line, bool _) in _days)
        {
            DayState d = st.CampDays.First(x => x.Rhythm == rhythm);
            var cells = new List<DayStripControl.Cell>(d.Hours.Count);
            foreach (HourState h in d.Hours) cells.Add(new DayStripControl.Cell(DayColors.Of(h.Block), false, false, false, false, h.Note));
            strip.SetCells(cells, d.Pending);
            line.Text = DayStrip.Summary(w, d.Hours);
            waiting |= d.Pending;
        }
        if (_watch != null)
        {
            bool[] kept = DayStrip.WatchCells(st.Watch.From, st.Watch.To);
            var cells = new List<DayStripControl.Cell>(DayStrip.Hours);
            for (int h = 0; h < DayStrip.Hours; h++)
                cells.Add(new DayStripControl.Cell(kept[h] ? Palette.DayWatch.Darkened(0.35f) : new Color(0.17f, 0.16f, 0.14f), false, false, false, kept[h], Ui.T("ui.schedule.watch_hint")));
            _watch.SetCells(cells, st.Watch.Pending);
        }
        if (_watchText != null) _watchText.Text = st.Watch.Text;
        if (_watchNow != null) _watchNow.Text = st.Watch.Now;
        if (_watchTonight != null)
        {
            _watchTonight.Text = st.Watch.TonightText; // in the watch hours, the turns already kept tonight
            _watchTonight.Visible = st.Watch.TonightText.Length > 0;
        }
        if (_watchLast != null)
        {
            _watchLast.Text = st.Watch.LastNightText;
            _watchLast.Visible = st.Watch.LastNightText.Length > 0;
        }
        if (_noWatch != null) _noWatch.Disabled = st.Watch.From == st.Watch.To;
        if (_pending != null) _pending.Visible = waiting;
    }

    /// <summary>Paints the picked hours of a kind of day's camp day with the brush (those who never sleep take no sleep brush).</summary>
    private void PaintDay(string rhythm, bool sleeps, int press, int release)
    {
        if (_brush == DayBlock.Sleep && !sleeps) return;
        (int first, int last) = DayStrip.Dragged(press, release);
        Paint(rhythm, first, last);
    }

    private void Paint(string rhythm, int first, int last)
    {
        DayState d = StateApi.Schedule(_host.World, _host.Drafts.All).CampDays.First(x => x.Rhythm == rhythm);
        var hours = d.Hours.Select(h => Return.Sim.Systems.ScheduleSystem.ParseBlock(h.Block) ?? DayBlock.Work).ToList();
        _host.Order(new SetCampDay(_host.World.Tick, rhythm, DayStrip.Paint(hours, first, last, _brush)));
        Refresh();
    }

    /// <summary>Presses a brush and paints hours from <paramref name="from"/> to <paramref name="to"/> (exclusive, wrapping) of a kind of day (RETURN_PRESS checks).</summary>
    public bool PressDay(string rhythm, int block, int from, int to)
    {
        Refresh();
        if (block < 0 || block >= _brushes.Count || DayStrip.FromTo(from, to) is not { } cells) return false;
        _brushes[block].Button.EmitSignal(BaseButton.SignalName.Pressed);
        if (!_days.Exists(d => d.Rhythm == rhythm)) return false;
        Paint(rhythm, cells.First, cells.Last);
        return true;
    }

    /// <summary>Sets the watch as a drag along its row from <paramref name="from"/> to the hour before <paramref name="to"/> would (RETURN_PRESS checks).</summary>
    public bool PressWatch(int from, int to)
    {
        Refresh();
        if (_watch == null) return false;
        _watch.Pick(from, (to + DayStrip.Hours - 1) % DayStrip.Hours);
        Refresh();
        return true;
    }
}

/// <summary>The colour of a block or of an hour as lived (Palette's muted day colours).</summary>
public static class DayColors
{
    public static Color Of(string block) => block switch
    {
        "sleep" or "slept" => Palette.DaySleep,
        "work" or "worked" => Palette.DayWork,
        "watched" => Palette.DayWatch,
        "needs" => Palette.DayNeeds,
        _ => Palette.DayFree,
    };
}
