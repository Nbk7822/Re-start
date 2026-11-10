using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>Delivering, building and taking down, through the real jobs (build design 4.5 to 4.7 and 8.1; ADR-0027).</summary>
public class BuildTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static Rect Tile(Point p) => new(p.X, p.Y, 1, 1);
    private static Rect Tile(int x, int y) => new(x, y, 1, 1);

    /// <summary>Only these kinds of work, at these priorities; everything else never.</summary>
    internal static void Works(Character c, params (string Work, int Priority)[] works)
    {
        c.Priorities.Clear();
        foreach (string id in JobPicker.PrioritizedWork) c.Priorities.Add(new WorkPriority { WorkId = id, Priority = 0 });
        foreach ((string id, int p) in works) c.Priorities.Single(x => x.WorkId == id).Priority = p;
    }

    internal static void Run(World w, int ticks, Func<bool>? until = null)
    {
        for (int t = 0; t < ticks; t++)
        {
            TravelSystem.Step(w);
            WorkSystem.Step(w);
            w.Tick++;
            if (until != null && until()) return;
        }
    }

    internal static List<Blueprint> Plan(World w, string building, params Rect[] pieces)
    {
        int before = w.Blueprints.Count;
        Building.Place(w, building, pieces, 0);
        return w.Blueprints.Skip(before).ToList();
    }

    private static void SetTimber(World w, int n)
    {
        Stock.Take(w, "timber", 999);
        if (n > 0) Stock.Add(w, "timber", n, new Point(w.Map.Stockpile.X + 1, w.Map.Stockpile.Y));
    }

    [Fact]
    public void ABuilderBuildsABedFromAPlan()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        Works(oren, ("build", 1));
        SetTimber(w, 3);
        Point at = BuildPlanTests.ShelterFloor(w);
        Blueprint bp = Plan(w, "bed", Tile(at)).Single();
        Run(w, 4_000, () => Building.BedAt(w, at) != null);

        Bed bed = Building.BedAt(w, at)!;
        Assert.Equal(1, Stock.Count(w, "timber"));
        Assert.Empty(w.Blueprints);
        Assert.Equal(bp.Id, bed.Id); // a plan becomes the thing it planned
        Assert.Equal(new[] { oren.Id }, bed.BuiltBy);
        Assert.Equal("bed", bed.DefId);
        SimEvent built = w.Log.Get(bed.BuiltEvent)!;
        Assert.Equal("event.work.built", built.Key);
        Assert.Equal(SystemTag.Work, built.System);
        Assert.Contains(bp.PlanEvent, built.Causes);
        Assert.Empty(built.Effects); // finishing a building is never a story's ending by itself
        Assert.Equal("Oren built a bed.", EventText.Describe(w, built));
        Assert.Contains(oren.Thoughts, t => t.DefId == "built_something");
        Assert.True(w.Map.Revision > 0);
    }

    [Fact]
    public void ADeliverersTimberAndABuildersWorkAreBothRecorded()
    {
        World w = WorkTests.Only(1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        Works(mira, ("haul", 1)); // build at never: she only carries
        Works(oren, ("build", 1));
        oren.Status = CharStatus.OffMap;
        SetTimber(w, 2);
        Point at = BuildPlanTests.ShelterFloor(w);
        Blueprint bp = Plan(w, "bed", Tile(at)).Single();
        Run(w, 3_000, () => bp.Delivered == 2);
        Assert.Equal(2, bp.Delivered);
        Assert.Equal(0, Stock.Count(w, "timber")); // committed: it left the stock and lies on the site
        Assert.Equal(new[] { mira.Id }, bp.Contributors);
        Assert.Null(mira.Carrying);

        oren.Status = CharStatus.Member;
        Run(w, 3_000, () => Building.BedAt(w, at) != null);
        Bed bed = Building.BedAt(w, at)!;
        Assert.Equal(new[] { oren.Id }, bed.BuiltBy);
        Assert.Equal(new[] { mira.Id, oren.Id }, bp.Contributors);
        Assert.Equal(new[] { oren.Id }, bp.Builders);
    }

    [Fact]
    public void AWallNeverFinishesUnderSomeoneStandingOnIt()
    {
        World w = WorkTests.Only(1, "oren", "mira");
        Character oren = Cast(w, "oren"), mira = Cast(w, "mira");
        Works(oren, ("build", 1));
        Works(mira);
        SetTimber(w, 2);
        Point at = BuildPlanTests.Yard(w, 1, 1);
        Blueprint bp = Plan(w, "wall", Tile(at)).Single();
        mira.Pos = new Point(at.X + 3, at.Y + 3);
        // Oren gets to work; then Mira, struck down, falls on the wall's tile.
        Run(w, 3_000, () => oren.Job is { Kind: JobKind.Build, Stage: 2 });
        mira.Pos = at;
        mira.Downed = true;
        Run(w, 1_000, () => oren.Job == null || oren.Job.Kind != JobKind.Build);
        Assert.NotEqual(TerrainKind.Wall, w.Map.At(at));
        Assert.Contains(bp, w.Blueprints);
        Assert.Equal(BlockedReason.InTheWay, bp.Blocked);
        Assert.True(w.Map.Passable(mira.Pos));
        // Once she is up and gone, the wall is finished from where the work left off.
        mira.Downed = false;
        mira.Pos = new Point(at.X + 3, at.Y + 3);
        Run(w, 1_500, () => w.Map.At(at) == TerrainKind.Wall);
        Assert.Equal(TerrainKind.Wall, w.Map.At(at));
        Structure wall = Building.StructureAt(w, at)!;
        Assert.Equal(new[] { oren.Id }, wall.BuiltBy);
        Assert.False(w.Map.Passable(at));
        SimEvent done = w.Log.All.Single(e => e.Key == "event.work.plan_finished");
        Assert.Equal("Oren finished building a wall.", EventText.Describe(w, done));
        Assert.Equal(done.Id, wall.BuiltEvent);
    }

    [Fact]
    public void ARingOfWallsWithNoDoorIsRefusedAndStaysBlockedUntilTheMapChanges()
    {
        World w = WorkTests.Only(1, "oren", "mira");
        Character oren = Cast(w, "oren"), mira = Cast(w, "mira");
        Works(oren, ("build", 1));
        Works(mira);
        SetTimber(w, 10);
        Point c = BuildPlanTests.Yard(w, 2, 1);
        mira.Pos = c;
        mira.Downed = true; // she stays where she is: inside
        var ring = new List<Rect>();
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (dx != 0 || dy != 0) ring.Add(Tile(c.X + dx, c.Y + dy));
        Plan(w, "wall", ring.ToArray());
        Run(w, 20_000, () => w.Log.All.Any(e => e.Key == "event.work.refused_to_shut_in"));
        SimEvent refused = w.Log.All.Single(e => e.Key == "event.work.refused_to_shut_in");
        Assert.Equal("Oren would not finish a wall: it would shut people in.", EventText.Describe(w, refused));
        // The corners do not matter (nobody cuts a corner), so the rest of the ring goes up and one side stays open.
        Run(w, 8_000, () => w.Blueprints.Count == 1 && oren.Job == null);
        Blueprint last = Assert.Single(w.Blueprints);
        Assert.Equal(BlockedReason.WouldShutIn, last.Blocked);
        Assert.Equal(1, last.Area.Center.ManhattanTo(c));
        Assert.Equal(7, w.Map.Structures.Count(s => !s.Removed));
        Assert.Contains(last.PlanEvent, refused.Causes);

        // Each corner going up changed the map, so the open side may have been tried (and refused) again; once the map
        // stops changing, nobody tries again until it does.
        int Refusals() => w.Log.All.Count(e => e.Key == "event.work.refused_to_shut_in");
        Run(w, 2_000);
        int settled = Refusals();
        Run(w, 3_000);
        Assert.Equal(settled, Refusals());
        w.Map.Changed();
        Run(w, 3_000, () => Refusals() == settled + 1);
        Assert.Equal(settled + 1, Refusals()); // the map changed: they look again, and refuse again
        Assert.True(w.Map.Passable(c));
    }

    [Fact]
    public void ARoofWaitsForWallsThenGivesCoverAgainstSunAndRain()
    {
        World w = WorkTests.Only(1, "oren", "ilex", "mira");
        Character oren = Cast(w, "oren"), ilex = Cast(w, "ilex"), mira = Cast(w, "mira");
        Works(oren, ("build", 1));
        Works(mira);
        Works(ilex);
        SetTimber(w, 20);
        Point c = BuildPlanTests.Yard(w, 2, 2);
        var roof = new Rect(c.X - 1, c.Y, 3, 2);
        var walls = new List<Rect>();
        for (int x = roof.X - 1; x <= roof.X + roof.W; x++) { walls.Add(Tile(x, roof.Y - 1)); walls.Add(Tile(x, roof.Y + roof.H)); }
        Assert.Empty(Plan(w, "roof", roof)); // on its own it cannot be placed: no wall, standing or planned, holds it up
        Plan(w, "wall", walls.ToArray());
        Blueprint roofPlan = Plan(w, "roof", roof).Single();
        Assert.False(Building.Supported(w, roof, w.Content.BuildingById("roof"))); // waiting for walls
        Run(w, 40_000, () => w.Map.Roof.Count == 4);
        RoofSection section = w.Map.Roof[3];
        Assert.Equal(roofPlan.Id, section.Id);
        Assert.True(section.Sound);
        Assert.Equal(new[] { oren.Id }, section.BuiltBy);
        Assert.True(w.Map.Covered(new Point(roof.X, roof.Y)));
        Assert.True(w.Map.Structures.Count(s => !s.Removed) > 0);

        // A vampire caught by the dawn beside it takes cover under it.
        w.Tick = 7L * w.Content.World.TicksPerHour + w.Tick / w.Content.World.TicksPerDay * w.Content.World.TicksPerDay;
        ilex.Pos = new Point(roof.X + 1, roof.Y + roof.H + 1);
        ilex.Job = null;
        Job? cover = JobPicker.Choose(w, ilex);
        Assert.Equal(JobKind.SeekCover, cover?.Kind);
        Assert.True(roof.Contains(cover!.Target));

        // Rain soaks those in the open, not those under it.
        mira.Pos = new Point(roof.X, roof.Y);
        oren.Pos = new Point(roof.X - 3, roof.Y - 3);
        mira.Thoughts.Clear();
        oren.Thoughts.Clear();
        Assert.NotNull(IncidentSystem.Fire(w, "rain"));
        IncidentSystem.Hourly(w);
        Assert.DoesNotContain(mira.Thoughts, t => t.DefId == "soaked");
        Assert.Contains(oren.Thoughts, t => t.DefId == "soaked");
    }

    [Fact]
    public void TakingDownReturnsHalfTheTimberAndTheOwnerBlamesWhoeverMarkedIt()
    {
        World w = WorkTests.Only(1, "oren", "mira");
        Character oren = Cast(w, "oren"), mira = Cast(w, "mira");
        Works(oren, ("build", 1));
        Works(mira);
        SetTimber(w, 0);
        Bed bed = w.Map.Beds[0];
        bed.OwnerId = mira.Id;
        Building.Mark(w, new[] { new BuiltRef(TargetKind.Bed, bed.Id) }, marked: true);
        Run(w, 3_000, () => !w.Map.Beds.Contains(bed));
        Assert.DoesNotContain(bed, w.Map.Beds);
        ItemStack back = w.Items.Single(i => i.Pos == bed.Pos);
        Assert.Equal(("timber", 1), (back.DefId, back.Count)); // half of two
        SimEvent took = w.Log.All.Single(e => e.Key == "event.work.took_down_bed_of");
        Assert.Equal("Oren took down Mira's bed.", EventText.Describe(w, took));
        Assert.Contains(bed.MarkedEvent, took.Causes);
        Assert.Contains(mira.Thoughts, t => t.DefId == "lost_bed" && t.AboutId == w.PlayerId && t.CauseEvent == took.Id);
        Assert.Null(Building.OwnedBed(w, mira.Id));
    }

    [Fact]
    public void AWallRunTakenDownIsToldOnceAndItsBuilderSeesItTornDown()
    {
        World w = WorkTests.Only(1, "oren", "sera");
        Character oren = Cast(w, "oren"), sera = Cast(w, "sera");
        Works(oren, ("build", 1));
        Works(sera, ("build", 1));
        SetTimber(w, 3);
        Point at = BuildPlanTests.Yard(w, 0, 2);
        Plan(w, "wall", Tile(at), Tile(at.X + 1, at.Y), Tile(at.X + 2, at.Y));
        Run(w, 10_000, () => w.Map.Structures.Count == 3);
        Assert.Equal(3, w.Map.Structures.Count);
        Relations.ChangeOpinion(w, oren, w.PlayerId, -38, 0);
        Relations.ChangeOpinion(w, sera, w.PlayerId, -38, 0);
        Building.Mark(w, w.Map.Structures.Select(s => new BuiltRef(TargetKind.Structure, s.Id)).ToList(), marked: true);
        Run(w, 10_000, () => w.Map.Structures.All(s => s.Removed));
        Assert.All(w.Map.Structures, s => Assert.True(w.Map.Passable(s.Pos)));
        SimEvent took = w.Log.All.Single(e => e.Key == "event.work.took_down");
        Assert.EndsWith("took down 3 walls.", EventText.Describe(w, took));
        Assert.DoesNotContain(w.Items, i => i.DefId == "timber" && !w.Map.Stockpile.Contains(i.Pos)); // half of one timber is none
        // Whoever built the walls thinks less of the player who marked them, once for the whole run; past the line, a grudge.
        int[] builders = w.Map.Structures.Select(s => s.BuiltBy[0]).Distinct().ToArray();
        foreach (int b in builders)
        {
            Character builder = w.Get(b);
            Assert.Single(builder.Thoughts, t => t.DefId == "work_torn_down");
            Assert.NotNull(Relations.ActiveGrudge(w, b, w.PlayerId));
        }
    }

    [Fact]
    public void ABuilderClaimsTheBedTheyBuilt()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        Works(oren, ("build", 1));
        SetTimber(w, 2);
        Point at = BuildPlanTests.ShelterFloor(w);
        Plan(w, "bed", Tile(at));
        Run(w, 4_000, () => Building.BedAt(w, at) != null);
        Bed built = Building.BedAt(w, at)!;
        oren.Rest = 300;
        w.Tick = 23L * w.Content.World.TicksPerHour;
        Run(w, 2_000, () => oren.Asleep);
        Assert.Equal(at, oren.Pos);
        Assert.Equal(oren.Id, built.OwnerId);
        SimEvent claim = w.Log.Get(built.ClaimEvent)!;
        Assert.Equal("event.social.claimed_own_bed", claim.Key);
        Assert.Equal(new[] { built.BuiltEvent }, claim.Causes);
        Assert.Equal("Oren claimed the bed they built.", EventText.Describe(w, claim));
    }

    [Fact]
    public void ABedlessBuilderBuildsABedBeforeMendingTheRoofButOneWithABedMendsFirst()
    {
        World w = WorkTests.Only(1, "sera");
        Character sera = Cast(w, "sera");
        Works(sera, ("repair_roof", 1), ("build", 1));
        sera.Traits = new List<string> { "protective", "proud" }; // no lean to any work, and no Independent halving the right's weight
        SetTimber(w, 3);
        Plan(w, "bed", Tile(BuildPlanTests.ShelterFloor(w)));
        Assert.Equal(JobKind.Build, JobPicker.Choose(w, sera)?.Kind);

        w.Map.Beds[0].OwnerId = sera.Id; // with a bed of her own, the old roof comes first at equal priority
        Assert.Equal(JobKind.RepairRoof, JobPicker.Choose(w, sera)?.Kind);
    }

    [Fact]
    public void TimberForPlansIsFelledAndTheFellingCitesThePlan()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        Works(oren, ("gather_timber", 1));
        foreach (RoofSection r in w.Map.Roof) r.Sound = true; // nothing else wants timber
        SetTimber(w, 0);
        Assert.Equal(0, Places.TimberNeeded(w));
        Blueprint bp = Plan(w, "bed", Tile(BuildPlanTests.ShelterFloor(w))).Single();
        Assert.Equal(2, Places.TimberNeeded(w));
        Run(w, 3_000, () => w.Log.All.Any(e => e.Key == "event.work.felled_tree"));
        SimEvent felled = w.Log.All.First(e => e.Key == "event.work.felled_tree");
        Assert.Contains(bp.PlanEvent, felled.Causes);
    }

    [Fact]
    public void AGenerousPersonWillNotTakeTheLastTimberSomeoneIsCountingOn()
    {
        World w = WorkTests.Only(1, "bram", "you");
        Character bram = Cast(w, "bram");
        Works(bram, ("build", 1));
        SetTimber(w, 2);
        Building.Place(w, "bed", new[] { Tile(BuildPlanTests.ShelterFloor(w)) }, 0);
        Building.Place(w, "wall", new[] { Tile(BuildPlanTests.Yard(w)) }, 0);
        // You have no bed and the bed plan needs both timber: a wall would take one of them.
        Assert.True(Building.GivesWay(w, bram));
        Job? job = JobPicker.Choose(w, bram);
        Assert.Equal(JobKind.Build, job?.Kind);
        Assert.Equal(w.Blueprints[0].Id, job!.TargetId); // the bed (and the bed plan is the older too)
        w.Blueprints.RemoveAt(0);
        Building.Place(w, "bed", new[] { Tile(BuildPlanTests.ShelterFloor(w)) }, 0); // now newer than the wall
        job = JobPicker.Choose(w, bram);
        Assert.Equal(w.Blueprints.Single(b => b.DefId == "bed").Id, job!.TargetId);
    }
}
