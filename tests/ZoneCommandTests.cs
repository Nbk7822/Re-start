using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>Marking, reshaping and removing zones (ADR-0028): what each command does, what it says, and what it refuses.</summary>
public class ZoneCommandTests
{
    private static Zone Start(World w) => w.Map.Zones.First(z => z.Kind == "stockpile");

    private static List<TileRun> Ruins(World w) => Zones.FromRect(w.Map.CoveredRuins);

    private static List<SimEvent> ZoneEvents(World w) => w.Log.All.Where(e => e.Key.StartsWith("event.world.zone_", StringComparison.Ordinal)).ToList();

    [Fact]
    public void MarkingAStockpileMakesAZoneAndSaysWhere()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        new MarkZone(0, 0, "stockpile", Ruins(w)).Apply(w);
        Zone dry = w.Map.Zones.Last();
        Assert.NotEqual(Start(w).Id, dry.Id);
        Assert.Equal(9, Zones.Count(dry.Tiles));
        Assert.Equal(new[] { "berries", "timber" }, dry.Items);
        SimEvent e = Assert.Single(ZoneEvents(w));
        Assert.Equal("You marked a stockpile in the covered ruins (9 tiles).", EventText.Describe(w, e));
        Assert.Equal(SystemTag.World, e.System);
        Assert.Equal(e.Id, dry.MadeEvent);
        Assert.Equal(w.PlayerId, dry.MadeBy);
        Assert.True(Zones.Accepts(w, w.Map.CoveredRuins.Center, "berries"));
    }

    [Fact]
    public void ReshapingIsAbsoluteAndAMoveSaysSo()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        int id = Start(w).Id;
        Rect r = w.Map.StartStockpile;
        new MarkZone(0, id, "stockpile", Zones.FromRect(new Rect(r.X, r.Y, r.W + 1, r.H))).Apply(w);
        Assert.Equal(15, Zones.Count(Start(w).Tiles));
        Assert.Equal("You changed the stockpile by the hearth; it now covers 15 tiles.", EventText.Describe(w, ZoneEvents(w)[^1]));
        new MarkZone(0, id, "stockpile", Ruins(w)).Apply(w);
        Assert.Equal(Ruins(w), Start(w).Tiles);
        Assert.Equal("You moved the stockpile; it now lies in the covered ruins.", EventText.Describe(w, ZoneEvents(w)[^1]));
        Assert.Equal(ZoneEvents(w)[^1].Id, Start(w).MadeEvent);
    }

    [Fact]
    public void TilesThatCannotHoldAZoneAreDroppedAndAChangeOfNothingSaysNothing()
    {
        World w = WorldGen.Create(TestContent.Db, 2);
        Point hearth = w.Map.Hearth, bush = w.Map.Bushes[0].Pos, bed = w.Map.Beds[0].Pos, open = new Point(hearth.X + 1, hearth.Y + 1);
        var tiles = Zones.Canonical(new[] { hearth, bush, bed, open });
        IReadOnlyList<ZoneTileState> verdicts = StateApi.CanZone(w, "stockpile", new[] { hearth, bush, bed, open });
        Assert.Equal(new[] { false, false, false, true }, verdicts.Select(v => v.Ok));
        Assert.Equal("The hearth", verdicts[0].Reason);
        new MarkZone(0, 0, "stockpile", tiles).Apply(w);
        Zone made = w.Map.Zones.Last();
        Assert.Equal(new[] { new TileRun(open.Y, open.X, 1) }, made.Tiles);
        Assert.Equal("You marked a stockpile by the hearth (1 tile).", EventText.Describe(w, ZoneEvents(w)[^1]));

        int events = w.Log.All.Count, zones = w.Map.Zones.Count;
        new MarkZone(0, made.Id, "stockpile", tiles).Apply(w); // the same tiles again
        new MarkZone(0, 0, "stockpile", Zones.Canonical(new[] { hearth, bush })).Apply(w); // nothing survives
        new MarkZone(0, 99_999, "stockpile", tiles).Apply(w); // no such zone
        Assert.Equal(events, w.Log.All.Count);
        Assert.Equal(zones, w.Map.Zones.Count);
    }

    [Fact]
    public void UseZonesTakeTilesFromEachOtherAndOneLeftEmptyIsGoneWithoutAWord()
    {
        World w = WorldGen.Create(TestContent.Db, 3);
        Rect r = w.Map.StartStockpile;
        int start = Start(w).Id;
        new MarkZone(0, 0, "stockpile", Zones.FromRect(new Rect(r.X, r.Y, 2, r.H))).Apply(w);
        Assert.Equal(6, Zones.Count(Zones.ById(w, start)!.Tiles));
        Assert.False(Zones.Contains(Zones.ById(w, start)!.Tiles, new Point(r.X, r.Y)));
        Assert.Equal(w.Map.Zones.Last().Id, Zones.UseZoneAt(w, new Point(r.X, r.Y))!.Id);

        new MarkZone(0, 0, "stockpile", Zones.FromRect(new Rect(r.X - 1, r.Y - 1, r.W + 2, r.H + 2))).Apply(w);
        Assert.Null(Zones.ById(w, start));
        Assert.Single(w.Map.Zones);
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.world.zone_removed");
    }

    [Fact]
    public void TheCampAlwaysHasSomewhereToKeepEachItem()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        int start = Start(w).Id, events = w.Log.All.Count;
        new RemoveZone(0, start).Apply(w);
        new SetZoneItems(0, start, new[] { "timber" }).Apply(w);
        new SetZoneItems(0, start, Array.Empty<string>()).Apply(w);
        Assert.Equal(events, w.Log.All.Count);
        Assert.Equal(new[] { "berries", "timber" }, Start(w).Items);
        Assert.False(Zones.CanRemove(w, w.Map.Zones, start));

        new MarkZone(0, 0, "stockpile", Ruins(w), new[] { "berries" }).Apply(w);
        Assert.Equal("You chose to keep no timber in the stockpile in the covered ruins.", EventText.Describe(w, ZoneEvents(w)[^1]));
        new SetZoneItems(0, start, new[] { "timber" }).Apply(w);
        Assert.Equal(new[] { "timber" }, Start(w).Items);
        Assert.Equal("You chose to keep no berries in the stockpile by the hearth.", EventText.Describe(w, ZoneEvents(w)[^1]));
        Assert.False(Zones.Accepts(w, w.Map.StartStockpile.Center, "berries"));
        Assert.False(Zones.CanRemove(w, w.Map.Zones, start)); // the only stockpile keeping timber

        new SetZoneItems(0, w.Map.Zones.Last().Id, new[] { "berries", "timber" }).Apply(w);
        new RemoveZone(0, start).Apply(w);
        Assert.Null(Zones.ById(w, start));
        Assert.Equal("You took away the stockpile by the hearth.", EventText.Describe(w, ZoneEvents(w)[^1]));
    }

    [Fact]
    public void StoresLeftBehindByAMoveStayStoresAndAreCarriedFirst()
    {
        World w = WorkTests.Only(4, "oren");
        int berries = Stock.Count(w, "berries"), timber = Stock.Count(w, "timber");
        Point old = w.Items.First(s => s.DefId == "berries").Pos;
        Stock.Add(w, "timber", 2, new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3)); // a loose load
        new MarkZone(0, Start(w).Id, "stockpile", Ruins(w)).Apply(w);
        Assert.Equal(berries, Stock.Count(w, "berries"));
        Assert.Equal(timber, Stock.Count(w, "timber"));
        Assert.Contains("berries ×" + berries + ", waiting to be carried to a stockpile", StateApi.Location(w, old.X, old.Y).Contents);

        Character oren = w.Characters.First(c => c.CastKey == "oren");
        Job? haul = JobPicker.WorkCandidate(w, oren, "haul", sunBlocked: false);
        Assert.NotNull(haul);
        Assert.True(w.Items.First(s => s.Id == haul!.TargetId).Stored, "stores waiting to be carried go before loose goods");
    }

    [Fact]
    public void PaintingAStockpileOverLooseGoodsMakesThemStores()
    {
        World w = WorldGen.Create(TestContent.Db, 5);
        var spot = new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3);
        Assert.Null(Zones.Check(w, w.Content.ZoneById("stockpile"), spot));
        int before = Stock.Count(w, "timber");
        Stock.Add(w, "timber", 2, spot);
        Assert.Equal(before, Stock.Count(w, "timber"));
        new MarkZone(0, 0, "stockpile", Zones.Canonical(new[] { spot })).Apply(w);
        Assert.Equal(before + 2, Stock.Count(w, "timber"));
    }

    [Fact]
    public void ZonesReadsCountQueuedCommandsAndDrafts()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        int start = Start(w).Id;
        sim.Enqueue(new MarkZone(5, start, "stockpile", Ruins(w)));
        ZoneState queued = Assert.Single(StateApi.Zones(w));
        Assert.True(queued.Pending);
        Assert.Equal("in the covered ruins", queued.Place);
        Assert.Equal(new[] { "berries", "timber" }, queued.Items);

        var drafts = new OrderDrafts();
        int draft = drafts.NewZoneId();
        drafts.Add(new MarkZone(0, draft, "stockpile", Zones.FromRect(w.Map.StartStockpile)));
        IReadOnlyList<ZoneState> shown = StateApi.Zones(w, drafts.All);
        Assert.Equal(2, shown.Count);
        Assert.Equal(draft, shown[1].Id);
        Assert.Single(w.Map.Zones);
    }

    [Fact]
    public void ChangingYourMindAboutZonesWhilePausedLeavesNoTrace()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        int draft = drafts.NewZoneId();
        drafts.Add(new MarkZone(w.Tick, draft, "stockpile", Ruins(w)));
        drafts.Add(new SetZoneItems(w.Tick, draft, new[] { "berries" }));
        Assert.Equal(new[] { "berries" }, ((MarkZone)Assert.Single(drafts.All)).Items);
        drafts.Add(new RemoveZone(w.Tick, draft));
        Zone start = Start(w);
        var grown = Zones.Canonical(Zones.Each(start.Tiles).Append(new Point(w.Map.StartStockpile.X - 1, w.Map.StartStockpile.Y)));
        drafts.Add(new MarkZone(w.Tick, start.Id, "stockpile", grown));
        drafts.Add(new MarkZone(w.Tick, start.Id, "stockpile", start.Tiles)); // and back again
        Assert.Equal(1, drafts.Count);
        drafts.Flush(sim);
        Assert.Equal(0, drafts.Count);
        Assert.Empty(w.PendingCommands);
    }

    [Fact]
    public void AZoneDraftedWhilePausedIsMadeWhenTimeMoves()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        drafts.Add(new MarkZone(w.Tick, drafts.NewZoneId(), "stockpile", Ruins(w)));
        drafts.Flush(sim);
        MarkZone sent = Assert.IsType<MarkZone>(Assert.Single(w.PendingCommands));
        Assert.Equal(0, sent.ZoneId);
        sim.Step();
        Assert.Equal(2, w.Map.Zones.Count);
    }
}

/// <summary>Zone commands are inputs like any other: replayable, snapshotted, part of the hash (ADR-0004, ADR-0028).</summary>
public class ZoneDeterminismTests
{
    private static Simulation Scripted(ulong seed, bool moveToRuins)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        World w = s.World;
        int start = w.Map.Zones[0].Id;
        Rect r = w.Map.StartStockpile;
        s.Enqueue(new MarkZone(1_000, 0, "stockpile", Zones.FromRect(new Rect(r.X, r.Y + r.H, 2, 1)), new[] { "timber" }));
        if (moveToRuins) s.Enqueue(new MarkZone(2_000, start, "stockpile", Zones.FromRect(w.Map.CoveredRuins)));
        else s.Enqueue(new MarkZone(2_000, start, "stockpile", Zones.FromRect(new Rect(r.X, r.Y, r.W - 1, r.H))));
        s.Enqueue(new SetZoneItems(3_000, start, new[] { "berries" }));
        return s;
    }

    [Fact]
    public void SameZoneCommandsSameWorld()
    {
        Simulation a = Scripted(42, true), b = Scripted(42, true);
        a.Advance(30_000);
        b.Advance(30_000);
        Assert.Equal(a.StateHash(), b.StateHash());
        Assert.Equal(2, a.World.Map.Zones.Count);
    }

    [Fact]
    public void ADifferentLayoutMakesADifferentWorld()
    {
        Simulation a = Scripted(42, true), b = Scripted(42, false);
        a.Advance(30_000);
        b.Advance(30_000);
        Assert.NotEqual(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void PendingZoneCommandsSurviveASnapshot()
    {
        Simulation straight = Scripted(9, true);
        straight.Advance(12_000);
        Simulation s = Scripted(9, true);
        s.Advance(1_500);
        Simulation r = Simulation.Restore(TestContent.Db, s.Snapshot());
        Assert.Equal(2, r.World.PendingCommands.Count);
        r.Advance(10_500);
        Assert.Equal(straight.StateHash(), r.StateHash());
    }

    [Fact]
    public void ASnapshotMidHaulRestoresToTheSameFuture()
    {
        var a = Simulation.Create(TestContent.Db, 8);
        a.Enqueue(new MarkZone(100, a.World.Map.Zones[0].Id, "stockpile", Zones.FromRect(a.World.Map.CoveredRuins)));
        long tick = 0;
        for (int i = 0; i < 40_000 && tick == 0; i++)
        {
            a.Step();
            if (a.World.Characters.Any(c => c.Carrying is { Stored: true } && c.Job is { Kind: JobKind.Haul })) tick = a.World.Tick;
        }
        Assert.True(tick > 0, "someone carries the stores left behind");
        Simulation r = Simulation.Restore(TestContent.Db, a.Snapshot());
        a.Advance(5_000);
        r.Advance(5_000);
        Assert.Equal(a.StateHash(), r.StateHash());
    }
}
