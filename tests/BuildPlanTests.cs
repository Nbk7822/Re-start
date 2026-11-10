using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>Placement rules and the three Build commands (build design 4.3 and 4.4; ADR-0027).</summary>
public class BuildPlanTests
{
    private static Rect Tile(int x, int y) => new(x, y, 1, 1);
    private static Rect Tile(Point p) => new(p.X, p.Y, 1, 1);

    /// <summary>A free tile of the shelter's floor (no bedroll, not by the door).</summary>
    internal static Point ShelterFloor(World w) => new(w.Map.Shelter.X + 2, w.Map.Shelter.Y + 2);

    /// <summary>Open ground east of the hearth, below the covered ruins: inside the tree-free clearing on every map.</summary>
    internal static Point Yard(World w, int dx = 0, int dy = 0) => new(w.Map.Shelter.X + 8 + dx, w.Map.Shelter.Y + 5 + dy);

    private static string Reason(World w, string building, Rect piece) => Building.Check(w, building, new[] { piece })[0].Reason;

    [Fact]
    public void OpenGroundTakesAPlanAndTheCampsPlacesDoNot()
    {
        World w = WorkTests.Only(1, "you");
        Assert.Equal("", Reason(w, "bed", Tile(ShelterFloor(w))));
        Assert.Equal("", Reason(w, "wall", Tile(Yard(w))));
        Assert.Equal(Building.ReasonCamp, Reason(w, "bed", Tile(w.Map.Hearth)));
        Assert.Equal(Building.ReasonCamp, Reason(w, "wall", Tile(w.Map.Stockpile.X, w.Map.Stockpile.Y)));
        Assert.Equal(Building.ReasonCamp, Reason(w, "bed", Tile(w.Map.CoveredRuins.Center)));
        Assert.Equal(Building.ReasonBuilt, Reason(w, "bed", Tile(w.Map.Shelter.X, w.Map.Shelter.Y))); // the old stone wall
        Assert.Equal(Building.ReasonBuilt, Reason(w, "bed", Tile(w.Map.Beds[0].Pos)));
        Assert.Equal(Building.ReasonOffMap, Reason(w, "bed", Tile(-1, 3)));
        Tree tree = w.Map.Trees.First(t => !t.Felled);
        Assert.Equal(Building.ReasonTree, Reason(w, "bed", Tile(tree.Pos)));
        Assert.Equal(Building.ReasonBush, Reason(w, "wall", Tile(w.Map.Bushes[0].Pos)));
        Character body = w.Characters.First(c => c.CastKey == "mira");
        body.Status = CharStatus.Dead;
        body.Pos = Yard(w, 1);
        Assert.Equal(Building.ReasonBody, Reason(w, "bed", Tile(body.Pos)));
    }

    [Fact]
    public void AWallCannotStandUnderARoofButABedMay()
    {
        World w = WorkTests.Only(1, "you");
        Assert.Equal(Building.ReasonUnderRoof, Reason(w, "wall", Tile(ShelterFloor(w)))); // under the old (broken) roof
        Assert.Equal("", Reason(w, "bed", Tile(ShelterFloor(w))));
    }

    [Fact]
    public void ADoorNeedsAWallOnTwoOppositeSides()
    {
        World w = WorkTests.Only(1, "you");
        Point d = Yard(w, 2);
        Assert.Equal(Building.ReasonDoorFrame, Reason(w, "door", Tile(d)));
        var walls = new[] { new PlannedPiece("wall", Tile(d.X - 1, d.Y)), new PlannedPiece("wall", Tile(d.X + 1, d.Y)) };
        Assert.Equal("", Building.Check(w, "door", new[] { Tile(d) }, walls)[0].Reason);
        var corner = new[] { new PlannedPiece("wall", Tile(d.X - 1, d.Y)), new PlannedPiece("wall", Tile(d.X, d.Y + 1)) };
        Assert.Equal(Building.ReasonDoorFrame, Building.Check(w, "door", new[] { Tile(d) }, corner)[0].Reason);
    }

    [Fact]
    public void ARoofNeedsWallsNearbyAndRoomAndKeepsToItsSize()
    {
        World w = WorkTests.Only(1, "you");
        Point at = Yard(w, 0, 0);
        // Too far from any wall: its lower tiles are more than three tiles from the old shelter's east wall.
        var far = new Rect(at.X + 1, at.Y + 2, 2, 2);
        Assert.Equal(Building.ReasonTooFar, Reason(w, "roof", far));
        var ring = new List<PlannedPiece>();
        for (int x = far.X - 1; x <= far.X + far.W; x++) { ring.Add(new("wall", Tile(x, far.Y - 1))); ring.Add(new("wall", Tile(x, far.Y + far.H))); }
        Assert.True(Building.Check(w, "roof", new[] { far }, ring)[0].Ok);
        Assert.Equal(Building.ReasonTooBig, Reason(w, "roof", new Rect(at.X, at.Y, 5, 2)));
        // Over the old shelter's broken roof: a roof is already over it, mended or not.
        Assert.Equal(Building.ReasonRoofed, Reason(w, "roof", new Rect(w.Map.Shelter.X + 1, w.Map.Shelter.Y + 1, 2, 2)));
        // A wall inside it.
        Assert.Equal(Building.ReasonWallBelow, Building.Check(w, "roof", new[] { new Rect(far.X - 1, far.Y - 1, 2, 2) }, ring)[0].Reason);
        Assert.Equal(3, Building.Check(w, "roof", new[] { new Rect(far.X, far.Y, 2, 3) }, ring)[0].Cost);
    }

    [Fact]
    public void ARoofGoesOverABedPlanAndABedUnderARoofPlan()
    {
        World w = WorkTests.Only(1, "you");
        Point at = ShelterFloor(w);
        // Inside a ring planned in the yard, the hut's roof is placed before its beds (the stand-in player's camp plan).
        Point c = Yard(w, 2, 1);
        var ring = new List<PlannedPiece>();
        for (int x = c.X - 2; x <= c.X + 2; x++) { ring.Add(new("wall", Tile(x, c.Y - 1))); ring.Add(new("wall", Tile(x, c.Y + 2))); }
        ring.Add(new("wall", Tile(c.X - 2, c.Y))); ring.Add(new("wall", Tile(c.X - 2, c.Y + 1)));
        ring.Add(new("wall", Tile(c.X + 2, c.Y))); ring.Add(new("wall", Tile(c.X + 2, c.Y + 1)));
        var roof = new PlannedPiece("roof", new Rect(c.X - 1, c.Y, 3, 2));
        Assert.True(Building.Check(w, "roof", new[] { roof.Area }, ring)[0].Ok);
        Assert.True(Building.Check(w, "bed", new[] { Tile(c.X - 1, c.Y) }, ring.Append(roof).ToList())[0].Ok);
        Assert.True(Building.Check(w, "bed", new[] { Tile(at) })[0].Ok);
        // But not two beds, or a wall on a bed.
        Assert.Equal(Building.ReasonPlanned, Building.Check(w, "bed", new[] { Tile(at), Tile(at) })[1].Reason);
        Assert.Equal(Building.ReasonUnderRoof, Building.Check(w, "wall", new[] { Tile(c.X, c.Y) }, ring.Append(roof).ToList())[0].Reason);
    }

    [Fact]
    public void PlacingMarksOutOnePlanAndSaysHowManyInTheSecondPerson()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        Point a = ShelterFloor(w);
        s.Enqueue(new PlaceBlueprint(0, "bed", new[] { Tile(a), Tile(a.X + 1, a.Y), Tile(-5, -5) }));
        s.Step();
        Assert.Equal(2, w.Blueprints.Count);
        Assert.True(w.Blueprints[0].Id < w.Blueprints[1].Id);
        SimEvent planned = w.Log.All.Single(e => e.Key == "event.world.planned");
        Assert.All(w.Blueprints, bp => Assert.Equal(planned.Id, bp.PlanEvent));
        Assert.Equal(SystemTag.World, planned.System);
        Assert.Equal(new[] { w.PlayerId }, planned.Actors);
        Assert.Equal("You marked out 2 beds.", EventText.Describe(w, planned));

        s.Enqueue(new PlaceBlueprint(w.Tick, "wall", new[] { Tile(Yard(w)) }));
        s.Step();
        Assert.Equal("You marked out a wall.", EventText.Describe(w, w.Log.All.Last(e => e.Key == "event.world.planned")));
    }

    [Fact]
    public void APlanThatCannotGoAnywhereSaysWhy()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        s.Enqueue(new PlaceBlueprint(0, "bed", new[] { Tile(w.Map.Hearth) }));
        s.Step();
        Assert.Empty(w.Blueprints);
        SimEvent failed = w.Log.All.Single(e => e.Key == "event.world.plan_failed");
        Assert.Equal("Your plan for a bed could not be placed: the camp needs that spot.", EventText.Describe(w, failed));
    }

    /// <summary>A plan drafted while paused applies later and is checked again then: two plans for one tile in one tick, and the second is dropped.</summary>
    [Fact]
    public void TwoPlansForOneTileInOneTickKeepOnlyTheFirst()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        Point a = ShelterFloor(w);
        s.Enqueue(new PlaceBlueprint(5, "bed", new[] { Tile(a) }));
        s.Enqueue(new PlaceBlueprint(5, "bed", new[] { Tile(a) }));
        s.Advance(6);
        Assert.Single(w.Blueprints);
        Assert.Single(w.Log.All, e => e.Key == "event.world.planned");
        Assert.Single(w.Log.All, e => e.Key == "event.world.plan_failed");
        Assert.Equal(2, w.CommandLog.Count(c => c is PlaceBlueprint));
    }

    [Fact]
    public void CallingOffAnUntouchedPlanLeavesNoTraceButOneWithTimberDropsItOnTheSite()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        Point a = ShelterFloor(w), b = new(a.X + 1, a.Y);
        s.Enqueue(new PlaceBlueprint(0, "bed", new[] { Tile(a) }));
        s.Enqueue(new PlaceBlueprint(0, "bed", new[] { Tile(b) }));
        s.Step();
        Blueprint first = w.Blueprints[0], second = w.Blueprints[1];
        second.Delivered = 2;
        s.Enqueue(new CancelBlueprints(w.Tick, new[] { first.Id }));
        s.Step();
        Assert.Single(w.Blueprints);
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.world.plan_called_off" || e.Causes.Contains(first.PlanEvent));
        Assert.DoesNotContain(w.Items, i => i.Pos == a);

        s.Enqueue(new CancelBlueprints(w.Tick, new[] { second.Id }));
        s.Step();
        Assert.Empty(w.Blueprints);
        ItemStack dropped = w.Items.Single(i => i.Pos == b);
        Assert.Equal(("timber", 2), (dropped.DefId, dropped.Count));
        SimEvent off = w.Log.All.Single(e => e.Key == "event.world.plan_called_off");
        Assert.Equal(new[] { second.PlanEvent }, off.Causes);
        Assert.Equal("You called off the plan for a bed.", EventText.Describe(w, off));
    }

    [Fact]
    public void MarkingSaysSoOncePerGestureAndTheOldCampsRoofCannotBeMarked()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        var refs = w.Map.Beds.Take(2).Select(b => new BuiltRef(TargetKind.Bed, b.Id)).ToList();
        refs.Add(new BuiltRef(TargetKind.Roof, w.Map.Roof[0].Id));
        s.Enqueue(new MarkTakeDown(0, refs, true));
        s.Step();
        SimEvent marked = w.Log.All.Single(e => e.Key == "event.world.marked_take_down");
        Assert.Equal("You marked 2 beds to be taken down.", EventText.Describe(w, marked));
        Assert.Equal(marked.Id, w.Map.Beds[0].MarkedEvent);
        Assert.Equal(0, w.Map.Roof[0].MarkedEvent);
        Assert.NotEqual(0, w.Map.Roof[0].Id);

        // Marking them again says nothing new; unmarking says nothing at all.
        s.Enqueue(new MarkTakeDown(w.Tick, refs, true));
        s.Enqueue(new MarkTakeDown(w.Tick, refs.Take(1).ToList(), false));
        s.Step();
        Assert.Single(w.Log.All, e => e.Key.StartsWith("event.world.marked_take_down", StringComparison.Ordinal));
        Assert.Equal(0, w.Map.Beds[0].MarkedEvent);
        Assert.Equal(marked.Id, w.Map.Beds[1].MarkedEvent);
    }

    [Fact]
    public void BuildCommandsRoundTripThroughSnapshots()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        s.Enqueue(new PlaceBlueprint(100, "bed", new[] { Tile(ShelterFloor(w)) }));
        s.Enqueue(new MarkTakeDown(100, new[] { new BuiltRef(TargetKind.Bed, w.Map.Beds[0].Id) }, true));
        s.Enqueue(new CancelBlueprints(200, new[] { 12345 }));
        Simulation r = Simulation.Restore(TestContent.Db, s.Snapshot());
        Assert.Equal(s.StateHash(), r.StateHash());
        s.Advance(300);
        r.Advance(300);
        Assert.Equal(s.StateHash(), r.StateHash());
        Assert.Single(r.World.Blueprints);
    }
}
