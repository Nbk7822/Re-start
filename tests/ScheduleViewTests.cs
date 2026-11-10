using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>The Godot-free half of the Schedule view (ADR-0029; design test 15): the day strip's arithmetic, the lines under each strip, and the lantern.</summary>
public class ScheduleViewTests
{
    private static readonly ContentDb Content = TestContent.Db;

    public ScheduleViewTests()
    {
        Ui.Init(Content.Loc);
    }

    [Fact]
    public void AStripMapsThePointerToAnHour()
    {
        Assert.Equal(0, DayStrip.HourAt(0f, 240f));
        Assert.Equal(0, DayStrip.HourAt(9.9f, 240f));
        Assert.Equal(1, DayStrip.HourAt(10f, 240f));
        Assert.Equal(23, DayStrip.HourAt(239f, 240f));
        Assert.Equal(23, DayStrip.HourAt(400f, 240f)); // past the end: the last hour
        Assert.Equal(0, DayStrip.HourAt(-5f, 240f));
    }

    [Fact]
    public void ADragPaintsBothEndsAndWhatLiesBetween()
    {
        Assert.Equal((3, 7), DayStrip.Dragged(7, 3));
        Assert.Equal((0, 23), DayStrip.Dragged(0, 23)); // the whole day
        List<DayBlock> camp = ScheduleSystem.CampDayFor(WorldGen.Create(Content, 1), "diurnal")!.Hours;
        List<DayBlock> painted = DayStrip.Paint(camp, 20, 21, DayBlock.Sleep);
        Assert.Equal(DayBlock.Sleep, painted[20]);
        Assert.Equal(DayBlock.Sleep, painted[21]);
        Assert.Equal(DayBlock.Free, painted[19]);
        Assert.Equal(DayBlock.Free, camp[20]); // the camp's day itself is untouched until a command applies
        Assert.All(DayStrip.Paint(camp, 0, 23, DayBlock.Work), b => Assert.Equal(DayBlock.Work, b));
    }

    [Fact]
    public void PaintingWrapsPastMidnightAndRangesAreFromToExclusive()
    {
        Assert.Equal(new[] { 22, 23, 0, 1 }, DayStrip.Cells(22, 1));
        Assert.Equal((21, 5), DayStrip.FromTo(21, 6)); // camp:0:21:6 paints 21:00 up to 06:00
        Assert.Null(DayStrip.FromTo(6, 6));
        DayBlock?[] own = DayStrip.PaintOwn(new DayBlock?[24], 22, 6, DayBlock.Work);
        Assert.Equal(DayBlock.Work, own[23]);
        Assert.Equal(DayBlock.Work, own[3]);
        Assert.Null(own[7]);
        Assert.Null(own[21]);
    }

    [Fact]
    public void TheWatchRowSetsWhereTheWatchBeginsAndEnds()
    {
        Assert.Equal((19, 6), DayStrip.WatchHours(19, 5)); // dragged leftward from 19 to 5: across midnight
        Assert.Equal((6, 19), DayStrip.WatchHours(6, 18));
        bool[] cells = DayStrip.WatchCells(19, 6);
        Assert.Equal(11, cells.Count(x => x));
        Assert.True(cells[23] && cells[0] && cells[5]);
        Assert.False(cells[6] || cells[18]);
        Assert.DoesNotContain(true, DayStrip.WatchCells(0, 0));
    }

    [Fact]
    public void TheLineUnderAStripReadsAsTheSketchDoes()
    {
        World w = WorldGen.Create(Content, 1);
        ScheduleState st = StateApi.Schedule(w);
        Assert.Equal("Sleep 22:00–06:00 · Work 06:00–19:00 · Free time 19:00–22:00", DayStrip.Summary(w, st.CampDays[0].Hours));
        Assert.Equal("Work 19:00–06:00 · Sleep 06:00–14:00 · Free time 14:00–19:00", DayStrip.Summary(w, st.CampDays[1].Hours));
        var split = ScheduleSystem.CampDayFor(w, "diurnal")!.Hours.Select((b, h) => h == 12 ? DayBlock.Free : b).ToList();
        var drafts = new List<Command> { new SetCampDay(0, "diurnal", split) };
        Assert.Equal("Sleep 22:00–06:00 · Work 06:00–12:00 and 13:00–19:00 · Free time 12:00–13:00 and 19:00–22:00",
            DayStrip.Summary(w, StateApi.Schedule(w, drafts).CampDays[0].Hours));
    }

    [Fact]
    public void TheLanternBurnsOnlyForAWatcherWhoIsAwake()
    {
        World w = WorkTests.Only(1, "vale");
        Character vale = w.Characters.First(c => c.CastKey == "vale");
        Assert.False(ViewText.LanternLit(w));
        WatchTests.OnWatch(w, vale, w.Tick + 1200);
        Assert.True(ViewText.LanternLit(w));
        vale.Asleep = true; // nodding off at the post: the lantern goes dark
        Assert.False(ViewText.LanternLit(w));
    }

    [Fact]
    public void EveryScheduleViewKeyExists()
    {
        foreach (string key in new[]
        {
            "ui.bar.schedule_hint", "ui.schedule.title", "ui.schedule.paint", "ui.schedule.brush_hint", "ui.schedule.day_of", "ui.schedule.range_block",
            "ui.schedule.ranges_sep", "ui.schedule.no_sleep_brush", "ui.schedule.watch", "ui.schedule.watch_hint", "ui.schedule.no_watch",
            "ui.schedule.no_watch_hint", "ui.schedule.asked_first", "ui.schedule.different", "ui.schedule.different_row", "ui.schedule.different_none",
            "ui.schedule.different_hint", "ui.schedule.pending", "ui.inspector.day", "ui.inspector.day_planned", "ui.inspector.day_lived",
            "ui.inspector.day_same", "ui.inspector.day_same_hint", "ui.inspector.day_latest", "ui.inspector.day_pending",
        })
            Assert.True(Content.Loc.Has(key), key);
    }
}
