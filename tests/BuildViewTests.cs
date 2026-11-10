using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// The Build menu's view, slice 1b (build design 3.3 to 3.8, 8.2; ADR-0027): the Godot-free half (BuildTool,
/// OrderDrafts, CheckPresses, Poses), compiled into the tests from game/scripts/Logic.
/// </summary>
public class BuildViewTests
{
    public BuildViewTests()
    {
        Ui.Init(TestContent.Db.Loc);
    }

    private static Rect Tile(Point p) => new(p.X, p.Y, 1, 1);

    private static Rect Tile(int x, int y) => new(x, y, 1, 1);

    /// <summary>A tile that can take a bed in seed 1's shelter.</summary>
    private static Point ShelterFloor(World w) => BuildPlanTests.ShelterFloor(w);

    // ---- Gestures (build design 3.3) ----

    [Fact]
    public void ADragPlacesALineAlongItsLongerAxis()
    {
        Assert.Equal(new[] { new Point(2, 3), new Point(3, 3), new Point(4, 3), new Point(5, 3), new Point(6, 3), new Point(7, 3) },
            BuildTool.Line(new Point(2, 3), new Point(7, 5)));
        Assert.Equal(new[] { new Point(2, 3), new Point(2, 4), new Point(2, 5), new Point(2, 6) }, BuildTool.Line(new Point(2, 6), new Point(4, 3)));
        Assert.Equal(new[] { new Point(4, 4) }, BuildTool.Line(new Point(4, 4), new Point(4, 4)));
        // A drag back toward the west runs west to east; an equal drag runs east-west.
        Assert.Equal(BuildTool.Line(new Point(2, 3), new Point(7, 3)), BuildTool.Line(new Point(7, 3), new Point(2, 3)));
        Assert.Equal(3, BuildTool.Line(new Point(0, 0), new Point(2, 2)).Count(p => p.Y == 0));
    }

    [Fact]
    public void AShiftDragPlacesARoomsOutline()
    {
        List<Point> ring = BuildTool.Outline(new Point(14, 13), new Point(10, 10));
        Assert.Equal(14, ring.Count); // a 5×4 room
        Assert.All(ring, p => Assert.True(p.X is 10 or 14 || p.Y is 10 or 13));
        Assert.Equal(ring.OrderBy(p => p.Y).ThenBy(p => p.X), ring);
        Assert.Equal(ring.Count, ring.Distinct().Count());
        Assert.Equal(BuildTool.Line(new Point(3, 3), new Point(3, 7)), BuildTool.Outline(new Point(3, 3), new Point(3, 7)));
    }

    [Fact]
    public void ARoofSplitsIntoSectionsOfAtMostFourByFour()
    {
        for (int w = 1; w <= 13; w++)
            for (int h = 1; h <= 9; h++)
            {
                var area = new Rect(5, 7, w, h);
                List<Rect> sections = BuildTool.Split(area, 4);
                Assert.All(sections, s => Assert.True(s.W is >= 1 and <= 4 && s.H is >= 1 and <= 4, $"{w}x{h}: {s}"));
                var covered = new List<Point>();
                foreach (Rect s in sections)
                    for (int y = s.Y; y < s.Y + s.H; y++)
                        for (int x = s.X; x < s.X + s.W; x++) covered.Add(new Point(x, y));
                Assert.Equal(w * h, covered.Count);
                Assert.Equal(w * h, covered.Distinct().Count());
                Assert.All(covered, p => Assert.True(area.Contains(p)));
            }
        // As even as it goes: 6 wide is two sections of 3 (the old shelter's interior), 10 is 4, 3 and 3.
        Assert.Equal(new[] { new Rect(0, 0, 3, 3), new Rect(3, 0, 3, 3) }, BuildTool.Split(new Rect(0, 0, 6, 3), 4));
        Assert.Equal(new[] { 4, 3, 3 }, BuildTool.Split(new Rect(0, 0, 10, 2), 4).Select(s => s.W));
        Assert.Single(BuildTool.Split(new Rect(0, 0, 4, 4), 4));
    }

    [Fact]
    public void TheToolTurnsAGestureIntoPiecesOfItsBuilding()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        var tool = new BuildTool();
        tool.Place("wall");
        tool.Hover(new Point(5, 5));
        Assert.Equal(new[] { Tile(5, 5) }, tool.Pieces(w));
        tool.Press(new Point(5, 5));
        tool.Hover(new Point(9, 6));
        Assert.Equal(5, tool.Pieces(w).Count);
        tool.Shift = true;
        Assert.Equal(10, tool.Pieces(w).Count); // the 5×2 outline
        tool.Place("roof");
        tool.Press(new Point(20, 20));
        tool.Hover(new Point(25, 22));
        Assert.Equal(new[] { new Rect(20, 20, 3, 3), new Rect(23, 20, 3, 3) }, tool.Pieces(w));
        tool.Stop();
        Assert.False(tool.Active);
        Assert.Empty(tool.Pieces(w));
    }

    // ---- Drafts while paused (build design 3.3, 3.8) ----

    [Fact]
    public void PlacingAndCallingOffWhilePausedLeavesNoTrace()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        Point floor = ShelterFloor(w);
        var tool = new BuildTool();
        tool.Place("bed");
        tool.Hover(floor);
        tool.Press(floor);
        Assert.True(tool.Release(w, drafts, c => drafts.Add(c)));
        Assert.Single(drafts.Placements);
        Assert.True(tool.Active); // the tool stays in hand until right-click or Esc

        tool.CallOff();
        tool.Hover(floor);
        tool.Press(floor);
        Assert.True(tool.Release(w, drafts, c => drafts.Add(c)));
        Assert.Equal(0, drafts.Count);
        drafts.Flush(sim);
        Assert.Empty(w.PendingCommands);
        sim.Step();
        var untouched = Simulation.Create(TestContent.Db, 1);
        untouched.Step();
        Assert.Equal(untouched.StateHash(), sim.StateHash());
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.world.plan", StringComparison.Ordinal));
    }

    [Fact]
    public void ADraftCallsOffPieceByPieceAndAPlanThatCannotGoIsNeverSent()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        Point yard = BuildPlanTests.Yard(w);
        int id = drafts.Add(new PlaceBlueprint(w.Tick, "wall", new[] { Tile(yard), Tile(yard.X + 1, yard.Y), Tile(yard.X + 2, yard.Y) }));
        Assert.True(drafts.CallOff(id, Tile(yard.X + 1, yard.Y)));
        Assert.Equal(2, drafts.Placements.Single().Plan.Pieces.Count);
        Assert.False(drafts.CallOff(id, Tile(yard.X + 1, yard.Y)));
        drafts.Add(new PlaceBlueprint(w.Tick, "bed", new[] { Tile(w.Map.Hearth) })); // nothing in it can go
        drafts.Flush(sim);
        Assert.Single(w.PendingCommands);
        sim.Step();
        Assert.Equal(new[] { Tile(yard), Tile(yard.X + 2, yard.Y) }, w.Blueprints.Select(b => b.Area));
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.world.plan_failed", StringComparison.Ordinal));
        Assert.Equal("You marked out 2 walls.", EventText.Describe(w, w.Log.All.Single(e => e.Key.StartsWith("event.world.planned", StringComparison.Ordinal))));
    }

    [Fact]
    public void DraftedPlansSeeEachOtherAndTheSimChecksThemAgain()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        Point yard = BuildPlanTests.Yard(w);
        drafts.Add(new PlaceBlueprint(w.Tick, "wall", new[] { Tile(yard) }));
        // The tool's ghost sees the first draft: a second plan over the same tile cannot go there.
        CanPlaceState check = StateApi.CanPlace(w, "wall", new[] { Tile(yard), Tile(yard.X + 1, yard.Y) }, OrderDrafts.Planned(w, drafts));
        Assert.Equal(new[] { false, true }, check.Pieces.Select(p => p.Ok));
        Assert.Equal(Building.ReasonPlanned, check.Pieces[0].Tiles[0].ReasonKey);
        drafts.Add(new PlaceBlueprint(w.Tick, "wall", new[] { Tile(yard), Tile(yard.X + 1, yard.Y) }));
        drafts.Flush(sim);
        Assert.Equal(2, w.PendingCommands.Count);
        // Queued placements count too, for a gesture made the same tick.
        Assert.False(StateApi.CanPlace(w, "bed", new[] { Tile(yard) }, OrderDrafts.Planned(w, new OrderDrafts())).Ok);
        sim.Step();
        Assert.Equal(new[] { Tile(yard), Tile(yard.X + 1, yard.Y) }, w.Blueprints.Select(b => b.Area));
    }

    [Fact]
    public void MarkingAndUnmarkingTheSameThingReplaceEachOther()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        var bed = new BuiltRef(TargetKind.Bed, w.Map.Beds[0].Id);
        var other = new BuiltRef(TargetKind.Bed, w.Map.Beds[1].Id);
        drafts.Add(new MarkTakeDown(w.Tick, new[] { bed, other }, true));
        drafts.Add(new MarkTakeDown(w.Tick, new[] { bed }, false));
        Assert.False(drafts.Marked(bed));
        Assert.True(drafts.Marked(other));
        Assert.Equal(2, drafts.Count);
        drafts.Flush(sim);
        MarkTakeDown sent = Assert.IsType<MarkTakeDown>(w.PendingCommands.Single()); // unmarking an unmarked bed changes nothing
        Assert.Equal(new[] { other }, sent.Targets);
        sim.Step();
        Assert.NotEqual(0, w.Map.Beds[1].MarkedEvent);
        Assert.Equal(0, w.Map.Beds[0].MarkedEvent);
    }

    [Fact]
    public void AClickOnAMarkedThingLeavesItStanding()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        Bed bed = w.Map.Beds[0];
        var tool = new BuildTool();
        tool.TakeDown();
        tool.Hover(bed.Pos);
        tool.Press(bed.Pos);
        var sent = new List<Command>();
        Assert.True(tool.Release(w, drafts, sent.Add));
        Assert.True(Assert.IsType<MarkTakeDown>(sent.Single()).Marked);
        sim.Enqueue(sent[0]);
        sim.Step();
        Assert.NotEqual(0, bed.MarkedEvent);
        tool.Hover(bed.Pos);
        tool.Press(bed.Pos);
        sent.Clear();
        Assert.True(tool.Release(w, drafts, sent.Add));
        Assert.False(Assert.IsType<MarkTakeDown>(sent.Single()).Marked);
    }

    [Fact]
    public void TheOldCampsStoneAndRoofCannotBeMarked()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Rect shelter = w.Map.Shelter;
        Assert.Empty(BuildTool.BuiltIn(w, Tile(shelter.X, shelter.Y), click: true));
        Assert.DoesNotContain(BuildTool.BuiltIn(w, new Rect(shelter.X + 1, shelter.Y + 1, 2, 3), click: false), t => t.Kind == TargetKind.Roof);
        var tool = new BuildTool();
        tool.TakeDown();
        tool.Hover(new Point(shelter.X, shelter.Y));
        Assert.Equal("The old camp's stone walls and roof stay until there is stone to work", tool.Hint(w, new OrderDrafts(), paused: false)[0]);
    }

    [Fact]
    public void AClickTakesTheBedNotTheRoofOverItAndADragOverTheWholeRoofTakesBoth()
    {
        World w = WorkTests.Only(1, "you");
        Point c = BuildPlanTests.Yard(w, 2, 2);
        var walls = new List<Rect>();
        for (int x = c.X - 2; x <= c.X + 2; x++) walls.Add(Tile(x, c.Y - 2));
        Building.Place(w, "wall", walls, 0);
        var roof = new Rect(c.X - 1, c.Y - 1, 3, 2);
        Building.Place(w, "roof", new[] { roof }, 0);
        Building.Place(w, "bed", new[] { Tile(c) }, 0);
        Assert.Equal(7, w.Blueprints.Count);
        var drafts = new OrderDrafts();
        List<PlanPiece> click = BuildTool.PlansIn(w, drafts, Tile(c), click: true);
        Assert.Equal("bed", Building.BlueprintById(w, click.Single().BlueprintId)!.DefId);
        List<PlanPiece> roofClick = BuildTool.PlansIn(w, drafts, Tile(c.X - 1, c.Y - 1), click: true);
        Assert.Equal("roof", Building.BlueprintById(w, roofClick.Single().BlueprintId)!.DefId);
        List<PlanPiece> drag = BuildTool.PlansIn(w, drafts, new Rect(roof.X, roof.Y, roof.W, roof.H), click: false);
        Assert.Equal(new[] { "roof", "bed" }.OrderBy(s => s), drag.Select(p => Building.BlueprintById(w, p.BlueprintId)!.DefId).OrderBy(s => s));
        // A drag that clips the roof takes only what lies wholly in it.
        Assert.DoesNotContain(BuildTool.PlansIn(w, drafts, new Rect(c.X, c.Y - 1, 2, 2), click: false), p => Building.BlueprintById(w, p.BlueprintId)!.DefId == "roof");
    }

    [Fact]
    public void CallingOffPlacedPlansIsOneCommandAndHidesThemFromTheNextGesture()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        Point yard = BuildPlanTests.Yard(w);
        sim.Enqueue(new PlaceBlueprint(w.Tick, "wall", new[] { Tile(yard), Tile(yard.X + 1, yard.Y) }));
        sim.Step();
        var drafts = new OrderDrafts();
        var tool = new BuildTool();
        tool.CallOff();
        tool.Press(yard);
        tool.Hover(new Point(yard.X + 1, yard.Y));
        Assert.True(tool.Release(w, drafts, c => drafts.Add(c)));
        Assert.Equal(1, drafts.Count);
        Assert.All(w.Blueprints, b => Assert.True(drafts.CallingOff(b.Id)));
        Assert.Empty(BuildTool.PlansIn(w, drafts, Tile(yard), click: true));
        drafts.Flush(sim);
        CancelBlueprints cancel = Assert.IsType<CancelBlueprints>(Assert.Single(w.PendingCommands));
        Assert.Equal(w.Blueprints.Select(b => b.Id), cancel.BlueprintIds);
        sim.Step();
        Assert.Empty(w.Blueprints);
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.world.plan_called_off", StringComparison.Ordinal)); // nothing was delivered or built
    }

    // ---- The list and the hint (build design 3.2, 3.3) ----

    [Fact]
    public void EveryBuildingHasAListEntryAndAHint()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        IReadOnlyList<BuildingState> list = StateApi.Buildings(w);
        Assert.Equal(TestContent.Db.Buildings.Count, list.Count);
        var tool = new BuildTool();
        foreach (BuildingState b in list)
        {
            foreach (string text in new[] { b.Name, b.Description, b.Cost, Ui.T("ui.build.category." + b.Category) })
                Assert.DoesNotContain("⟦", text);
            tool.Place(b.Id);
            tool.Hover(BuildPlanTests.Yard(w));
            IReadOnlyList<string> hint = tool.Hint(w, new OrderDrafts(), paused: true);
            Assert.True(hint.Count >= 2, b.Id);
            Assert.All(hint, line => Assert.DoesNotContain("⟦", line));
            Assert.Equal("Takes effect when time moves", hint[^1]);
        }
        // Categories are named by composed keys, so check every one the schema allows (the grave's "rites" too).
        foreach (string category in RequiredContent.BuildingCategories) Assert.True(TestContent.Db.Loc.Has("ui.build.category." + category), category);
    }

    [Fact]
    public void TheHintGivesWhatIsMarkedItsCostTheStockAndWhyATileCannotTakeIt()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Point hearth = w.Map.Hearth;
        var tool = new BuildTool();
        tool.Place("wall");
        tool.Press(new Point(hearth.X - 2, hearth.Y));
        tool.Hover(new Point(hearth.X + 3, hearth.Y));
        IReadOnlyList<string> hint = tool.Hint(w, new OrderDrafts(), paused: false);
        int store = Stock.Count(w, "timber");
        Assert.Equal($"Wall · 5 marked · 5 timber · {store} in store; people will fell trees for the rest", hint[0]);
        Assert.Equal("Can't go here: the camp needs that spot", hint[1]);
        Assert.StartsWith("Click for one", hint[2]);
        tool.Hover(new Point(hearth.X - 2, hearth.Y + 1));
        tool.CancelDrag();
        Assert.Equal($"Wall · 1 marked · 1 timber · {store} in store", tool.Hint(w, new OrderDrafts(), paused: false)[0]);
        tool.Hover(hearth);
        Assert.Equal("Wall · nothing can go here", tool.Hint(w, new OrderDrafts(), paused: false)[0]);
        // A gesture that marks nothing sends nothing.
        tool.Press(hearth);
        var sent = new List<Command>();
        Assert.False(tool.Release(w, new OrderDrafts(), sent.Add));
        Assert.Empty(sent);
    }

    [Fact]
    public void AfterAGestureTheHintSaysWhatItDidUntilThePointerMovesOn()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        var drafts = new OrderDrafts();
        Point hearth = w.Map.Hearth;
        var tool = new BuildTool();
        tool.Place("wall");
        tool.Press(new Point(hearth.X - 3, hearth.Y));
        tool.Hover(new Point(hearth.X + 2, hearth.Y));
        Assert.True(tool.Release(w, drafts, c => drafts.Add(c)));
        // The release tile is now planned; the hint still tells the gesture, not "nothing can go here".
        Assert.True(tool.Settled);
        IReadOnlyList<string> hint = tool.Hint(w, drafts, paused: true);
        Assert.StartsWith("Wall · 5 marked · 5 timber", hint[0]);
        Assert.Equal("Can't go here: the camp needs that spot", hint[1]);
        Assert.Equal("Takes effect when time moves", hint[^1]);
        tool.Hover(new Point(hearth.X + 2, hearth.Y));
        Assert.True(tool.Settled);
        tool.Hover(new Point(hearth.X + 2, hearth.Y + 1));
        Assert.False(tool.Settled);
        Assert.StartsWith("Wall · 1 marked", tool.Hint(w, drafts, paused: true)[0]);
    }

    [Fact]
    public void TheTooltipNamesWhatIsDraftedAtATile()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        var drafts = new OrderDrafts();
        Point floor = ShelterFloor(w);
        drafts.Add(new PlaceBlueprint(w.Tick, "bed", new[] { Tile(floor) }));
        Assert.Equal(new[] { "Plan for a bed · takes effect when time moves" }, BuildTool.DraftLines(w, drafts, floor.X, floor.Y));
        Bed bed = w.Map.Beds[0];
        drafts.Add(new MarkTakeDown(w.Tick, new[] { new BuiltRef(TargetKind.Bed, bed.Id) }, true));
        Assert.Equal(new[] { "To be taken down when time moves" }, BuildTool.DraftLines(w, drafts, bed.Pos.X, bed.Pos.Y));
        drafts.Add(new MarkTakeDown(w.Tick, new[] { new BuiltRef(TargetKind.Bed, bed.Id) }, false));
        Assert.Equal(new[] { "To be left standing when time moves" }, BuildTool.DraftLines(w, drafts, bed.Pos.X, bed.Pos.Y));
        Assert.Empty(BuildTool.DraftLines(w, drafts, 0, 0));
    }

    // ---- Check hooks and poses ----

    [Fact]
    public void CheckPressesKeepEachPressesCoordinatesWithIt()
    {
        Assert.Equal(new[] { "build:wall:19,16,6,1", "policy:1", "priority:0:1", "takedown:4,5", "wait:600", "stop" },
            CheckPresses.Split("build:wall:19,16,6,1, policy:1,priority:0:1,takedown:4,5,wait:600,stop"));
        Assert.True(CheckPresses.TryGesture("19,16,6,1", out Point from, out Point to));
        Assert.Equal((new Point(19, 16), new Point(24, 16)), (from, to));
        Assert.True(CheckPresses.TryGesture("3,4", out from, out to));
        Assert.Equal(from, to);
        Assert.False(CheckPresses.TryGesture("3,4,0,1", out _, out _));
        Assert.False(CheckPresses.TryGesture("3", out _, out _));
    }

    [Theory]
    [InlineData(JobKind.Build, 0, Pose.Stand)]  // fetching what the plan lacks
    [InlineData(JobKind.Build, 1, Pose.Stand)]  // carrying it to where they will stand
    [InlineData(JobKind.Build, 2, Pose.Swing)]  // at work
    [InlineData(JobKind.TakeDown, 0, Pose.Stand)]
    [InlineData(JobKind.TakeDown, 1, Pose.Swing)]
    [InlineData(JobKind.Deliver, 1, Pose.Stand)]
    public void BuildingAndTakingDownSwingAHammer(JobKind job, int stage, Pose expected)
    {
        Assert.Equal(expected, Poses.For(new PoseInput(CharStatus.Member, false, false, job, stage, false, false)));
        var c = new Character { Status = CharStatus.Member, Job = new Job { Kind = job, Stage = stage } };
        Assert.Equal(expected == Pose.Swing ? HeldTool.Hammer : HeldTool.None, Poses.ToolFor(c, expected));
    }
}
