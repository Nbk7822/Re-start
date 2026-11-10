using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>The Build menu's text-state reads (build design 6.4; ADR-0027): structured, localised, and never changing state.</summary>
public class BuildApiTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static Rect Tile(Point p) => new(p.X, p.Y, 1, 1);

    [Fact]
    public void TheBuildListComesFromContentWithCostsInWords()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        IReadOnlyList<BuildingState> list = StateApi.Buildings(w);
        Assert.Equal(new[] { "wall", "door", "roof", "bed" }, list.Select(b => b.Id));
        Assert.Equal("1 timber each", list[0].Cost);
        Assert.Equal("2 timber", list[1].Cost);
        Assert.Equal("1 timber per 2 tiles", list[2].Cost);
        Assert.Equal(("Bed", "rest"), (list[3].Name, list[3].Category));
        Assert.All(list, b => Assert.True(b.Available));
        Assert.DoesNotContain("⟦", StateApi.Json(list));
    }

    [Fact]
    public void CanPlaceGivesEachTileItsReasonAndTheCostAgainstTheStock()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Point floor = BuildPlanTests.ShelterFloor(w);
        CanPlaceState ok = StateApi.CanPlace(w, "bed", new[] { Tile(floor), Tile(new Point(floor.X + 1, floor.Y)) });
        Assert.True(ok.Ok);
        Assert.Equal(4, ok.Cost);
        Assert.Equal("timber", ok.Item);
        Assert.Equal(Stock.Count(w, "timber"), ok.InStore);
        CanPlaceState bad = StateApi.CanPlace(w, "bed", new[] { Tile(w.Map.Hearth) });
        Assert.False(bad.Ok);
        TileVerdictState t = bad.Pieces.Single().Tiles.Single();
        Assert.Equal((Building.ReasonCamp, "the camp needs that spot"), (t.ReasonKey, t.Reason));
        Assert.Equal(0, bad.Cost);
    }

    [Fact]
    public void CanPlaceAlsoChecksPiecesAskedForButNotYetOnTheMap()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Point yard = BuildPlanTests.Yard(w);
        Assert.True(StateApi.CanPlace(w, "bed", new[] { Tile(yard) }).Ok);
        // A view's drafts (or placements queued this tick) are passed in; the check stays a function of its arguments.
        CanPlaceState clash = StateApi.CanPlace(w, "bed", new[] { Tile(yard) }, new[] { new PlannedPiece("wall", Tile(yard)) });
        Assert.False(clash.Ok);
        Assert.Equal(Building.ReasonPlanned, clash.Pieces.Single().Tiles.Single().ReasonKey);
        Assert.True(StateApi.CanPlace(w, "roof", new[] { new Rect(yard.X, yard.Y + 1, 2, 2) }, new[] { new PlannedPiece("wall", Tile(yard)) }).Ok);
        Assert.Empty(w.Blueprints);

        // A placed plan asked to be called off (a draft, or a call-off queued this tick) leaves its tile free to plan on.
        Blueprint wall = BuildTests.Plan(w, "wall", Tile(yard)).Single();
        Assert.Equal(Building.ReasonPlanned, StateApi.CanPlace(w, "bed", new[] { Tile(yard) }).Pieces.Single().Tiles.Single().ReasonKey);
        Assert.True(StateApi.CanPlace(w, "bed", new[] { Tile(yard) }, exceptPlans: new[] { wall.Id }).Ok);
    }

    [Fact]
    public void BlueprintsSayHowEachPlanStandsAndShowQueuedPlacements()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        Point floor = BuildPlanTests.ShelterFloor(w);
        s.Enqueue(new PlaceBlueprint(0, "bed", new[] { Tile(floor) }));
        s.Step();
        Blueprint bp = w.Blueprints.Single();
        bp.Delivered = 1;
        bp.Contributors.Add(Cast(w, "mira").Id);
        s.Enqueue(new PlaceBlueprint(w.Tick + 5, "wall", new[] { Tile(BuildPlanTests.Yard(w)) }));
        IReadOnlyList<BlueprintState> plans = StateApi.Blueprints(w);
        BlueprintState bed = plans[0];
        Assert.Equal((bp.Id, "bed", "waiting_for_timber", "waiting for timber"), (bed.Id, bed.BuildingId, bed.StatusKey, bed.Status));
        Assert.Equal((1, 2), (bed.Delivered, bed.Needed));
        Assert.Equal(new[] { "Mira" }, bed.ContributorNames);
        Assert.False(bed.Pending);
        BlueprintState queued = plans[1];
        Assert.True(queued.Pending);
        Assert.Equal(("pending", "takes effect when time moves"), (queued.StatusKey, queued.Status));
        bp.Delivered = 2;
        Assert.Equal("ready", StateApi.Blueprints(w)[0].StatusKey);
        string before = s.StateHash();
        _ = StateApi.Blueprints(w);
        _ = StateApi.Location(w, floor.X, floor.Y);
        Assert.Equal(before, s.StateHash()); // reads never change state
    }

    /// <summary>A wall ready to build with someone, or a body, on its tile says so before any builder tries it: nobody will while they are there.</summary>
    [Fact]
    public void AWallWithSomeoneOnItsTileSaysTheyAreInTheWay()
    {
        World w = WorkTests.Only(1, "you", "mira");
        Point at = BuildPlanTests.Yard(w, 1, 1);
        Blueprint wall = BuildTests.Plan(w, "wall", Tile(at)).Single();
        string Status() => StateApi.Blueprints(w).Single(b => b.Id == wall.Id).StatusKey;
        Character mira = Cast(w, "mira");
        mira.Pos = at;
        Assert.Equal("waiting_for_timber", Status()); // the timber still has to come, whoever stands there
        wall.Delivered = 1;
        Assert.Equal("in_the_way", Status());
        Assert.Equal(BlockedReason.None, wall.Blocked); // no builder has tried it
        mira.Pos = new Point(at.X + 2, at.Y);
        Assert.Equal("ready", Status());
        mira.Pos = at;
        mira.Status = CharStatus.Dead;
        Assert.Equal(("in_the_way", "someone is in the way"), (Status(), StateApi.Blueprints(w).Single(b => b.Id == wall.Id).Status));
    }

    [Fact]
    public void ARoofPlanWaitsForWallsAndARefusedWallSaysSo()
    {
        World w = WorkTests.Only(1, "you");
        Point c = BuildPlanTests.Yard(w, 2, 2);
        var roof = new Rect(c.X - 1, c.Y, 3, 2);
        var walls = new List<Rect>();
        for (int x = roof.X - 1; x <= roof.X + roof.W; x++) { walls.Add(Tile(new Point(x, roof.Y - 1))); walls.Add(Tile(new Point(x, roof.Y + roof.H))); }
        BuildTests.Plan(w, "wall", walls.ToArray());
        Blueprint roofPlan = BuildTests.Plan(w, "roof", roof).Single();
        Assert.Equal("waiting_for_walls", StateApi.Blueprints(w).Single(b => b.Id == roofPlan.Id).StatusKey);
        Blueprint wall = w.Blueprints[0];
        wall.Blocked = BlockedReason.WouldShutIn;
        wall.BlockedRevision = w.Map.Revision;
        Assert.Equal(("shut_in", "would shut people in"), (StateApi.Blueprints(w)[0].StatusKey, StateApi.Blueprints(w)[0].Status));
        w.Map.Changed();
        Assert.NotEqual("shut_in", StateApi.Blueprints(w)[0].StatusKey);
        Assert.Contains(StateApi.Location(w, roof.X, roof.Y).Contents, l => l == "Plan for a roof section · 0 of 3 timber here · waiting for walls");
    }

    [Fact]
    public void BuiltThingsTellWhoBuiltOwnsAndMendedThem()
    {
        World w = WorkTests.Only(1, "oren", "mira");
        Character oren = Cast(w, "oren"), mira = Cast(w, "mira");
        BuildTests.Works(oren, ("build", 1));
        BuildTests.Works(mira);
        Stock.Add(w, "timber", 3, w.Map.Stockpile.Center);
        Point floor = BuildPlanTests.ShelterFloor(w), at = BuildPlanTests.Yard(w, 1, 2);
        BuildTests.Plan(w, "bed", Tile(floor));
        BuildTests.Plan(w, "wall", Tile(at));
        BuildTests.Run(w, 8_000, () => w.Blueprints.Count == 0);
        Bed bed = Building.BedAt(w, floor)!;
        bed.OwnerId = mira.Id;
        StructureState b = StateApi.Structure(w, TargetKind.Bed, bed.Id)!;
        Assert.Equal(("Bed", "Oren", "Mira", false), (b.Building, b.BuiltByNames, b.Owner, b.OldCamp));
        Assert.NotEqual("", b.BuiltWhen);
        Assert.Contains("Bed — Mira's · built by Oren", StateApi.Location(w, floor.X, floor.Y).Contents);
        Assert.Contains("Wall · built by Oren", StateApi.Location(w, at.X, at.Y).Contents);
        Assert.Equal("Wall", StateApi.Location(w, at.X, at.Y).Terrain);

        RoofSection old = w.Map.Roof[0];
        old.Sound = true;
        old.MendedBy = oren.Id;
        StructureState r = StateApi.Structure(w, TargetKind.Roof, old.Id)!;
        Assert.True(r.OldCamp);
        Assert.Equal("Oren", r.MendedByName);
        Assert.Contains("Under a sound roof · mended by Oren", StateApi.Location(w, old.Area.X, old.Area.Y).Contents);
        Assert.Null(StateApi.Structure(w, TargetKind.Bed, 123456));

        Assert.Equal("the bed Oren built", StateApi.Character(w, mira.Id).Bed);
        Assert.Equal("none", StateApi.Character(w, oren.Id).Bed);
        bed.OwnerId = oren.Id;
        Assert.Equal("the bed they built", StateApi.Character(w, oren.Id).Bed);
        bed.OwnerId = w.PlayerId;
        Assert.Equal("Bed — yours · built by Oren", StateApi.Location(w, floor.X, floor.Y).Contents.Single(l => l.StartsWith("Bed", StringComparison.Ordinal)));
        Assert.Equal("none", StateApi.Character(w, mira.Id).Bed);
        w.Map.Beds[0].OwnerId = mira.Id; // an old bedroll reads plainly
        Assert.Equal("Bed — Mira's", StateApi.Location(w, w.Map.Beds[0].Pos.X, w.Map.Beds[0].Pos.Y).Contents.Single(l => l.StartsWith("Bed", StringComparison.Ordinal)));
        Assert.Equal("a bed", StateApi.Character(w, mira.Id).Bed);
    }

    [Fact]
    public void TheCampCountsItsBedsPlansAndTrees()
    {
        var s = Simulation.Create(TestContent.Db, 2);
        World w = s.World;
        w.Map.Beds[0].OwnerId = w.PlayerId;
        s.Enqueue(new PlaceBlueprint(0, "bed", new[] { Tile(BuildPlanTests.ShelterFloor(w)) }));
        s.Step();
        FactionState camp = StateApi.Faction(w);
        Assert.Equal((4, 1, 1), (camp.Beds, camp.BedsClaimed, camp.OpenPlans));
        Assert.Equal(w.Map.Trees.Count(t => !t.Felled), camp.TreesStanding);
        Assert.Equal(3, camp.RoofTotal);
        w.Map.Roof[0].Removed = true;
        Assert.Equal(2, StateApi.Faction(w).RoofTotal);
    }
}
