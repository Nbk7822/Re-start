using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
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
        if (n > 0) Stock.Add(w, "timber", n, new Point(w.Map.StartStockpile.X + 1, w.Map.StartStockpile.Y));
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
        Assert.DoesNotContain(w.Items, i => i.DefId == "timber" && !w.Map.StartStockpile.Contains(i.Pos)); // half of one timber is none
        // Whoever built the walls thinks less of the player who marked them, once for the whole run; past the line, a grudge.
        int[] builders = w.Map.Structures.Select(s => s.BuiltBy[0]).Distinct().ToArray();
        foreach (int b in builders)
        {
            Character builder = w.Get(b);
            Assert.Single(builder.Thoughts, t => t.DefId == "work_torn_down");
            Assert.NotNull(Relations.ActiveGrudge(w, b, w.PlayerId));
        }
    }

    /// <summary>
    /// A gesture over walls and a door is told once for all of them, in words that fit both (review of ADR-0027: it named
    /// the whole gesture after whichever piece came down last, "took down 3 doors").
    /// </summary>
    [Fact]
    public void WallsAndADoorTakenDownInOneGestureAreToldAsThings()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        Works(oren, ("build", 1));
        SetTimber(w, 4);
        Point at = BuildPlanTests.Yard(w, 0, 2);
        Plan(w, "wall", Tile(at.X - 1, at.Y), Tile(at.X + 1, at.Y));
        Plan(w, "door", Tile(at));
        Run(w, 10_000, () => w.Map.Structures.Count == 3);
        Building.Mark(w, w.Map.Structures.Select(s => new BuiltRef(TargetKind.Structure, s.Id)).ToList(), marked: true);
        Run(w, 10_000, () => w.Map.Structures.All(s => s.Removed));
        SimEvent took = w.Log.All.Single(e => e.Key.StartsWith("event.work.took_down", StringComparison.Ordinal));
        Assert.Equal("Oren took down 3 things.", EventText.Describe(w, took));
    }

    /// <summary>
    /// Marked walls, some already down when the player leaves the rest standing: what came down is still told, once, and its
    /// builder still sees it torn down (review of ADR-0027: the line, the blame and the story ending were lost).
    /// </summary>
    [Fact]
    public void WallsLeftStandingAfterSomeCameDownStillTellWhatCameDown()
    {
        World w = WorkTests.Only(1, "oren", "sera");
        Character oren = Cast(w, "oren"), sera = Cast(w, "sera");
        Works(oren, ("build", 1));
        Works(sera);
        SetTimber(w, 3);
        Point at = BuildPlanTests.Yard(w, 0, 2);
        Plan(w, "wall", Tile(at), Tile(at.X + 1, at.Y), Tile(at.X + 2, at.Y));
        Run(w, 10_000, () => w.Map.Structures.Count == 3);
        List<BuiltRef> all = w.Map.Structures.Select(s => new BuiltRef(TargetKind.Structure, s.Id)).ToList();
        Building.Mark(w, all, marked: true);
        Run(w, 10_000, () => w.Map.Structures.Any(s => s.Removed));
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.work.took_down", StringComparison.Ordinal)); // told when the gesture is done
        Building.Mark(w, all, marked: false); // the player leaves the rest standing
        SimEvent took = w.Log.All.Single(e => e.Key.StartsWith("event.work.took_down", StringComparison.Ordinal));
        Assert.Equal("Oren took down a wall.", EventText.Describe(w, took));
        Assert.Contains(oren.Thoughts, t => t.DefId == "work_torn_down" && t.CauseEvent == took.Id);
        Run(w, 3_000);
        Assert.Equal(1, w.Map.Structures.Count(s => s.Removed)); // the rest stand
        Assert.Single(w.Log.All, e => e.Key.StartsWith("event.work.took_down", StringComparison.Ordinal));
    }

    [Fact]
    public void ABuilderClaimsTheBedTheyBuilt()
    {
        World w = WorkTests.Only(1, "oren");
        w.Tick = 10L * w.Content.World.TicksPerHour; // working hours: at midnight Oren would sleep first, in a bed of the shelter's (ADR-0029)
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
        w.Tick = 10L * w.Content.World.TicksPerHour; // working hours: at midnight the camp is in bed (ADR-0029)
        Character sera = Cast(w, "sera");
        Works(sera, ("repair_roof", 1), ("build", 1));
        sera.Traits = new List<string> { "protective", "proud" }; // no lean to any work, and no Independent halving the right's weight
        SetTimber(w, 3);
        Plan(w, "bed", Tile(BuildPlanTests.ShelterFloor(w)));
        Assert.Equal(JobKind.Build, JobPicker.Choose(w, sera)?.Kind);

        w.Map.Beds[0].OwnerId = sera.Id; // with a bed of her own, the old roof comes first at equal priority
        Assert.Equal(JobKind.RepairRoof, JobPicker.Choose(w, sera)?.Kind);
    }

    /// <summary>"... repaired a section of the shelter roof (1 of 3 now sound)": a section raised elsewhere, or one taken down, is not the shelter's (review of ADR-0027).</summary>
    [Fact]
    public void MendingTheShelterRoofCountsOnlyTheShelterRoof()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        Works(oren, ("repair_roof", 1));
        Point yard = BuildPlanTests.Yard(w);
        w.Map.Roof.Add(new RoofSection { Index = w.Map.Roof.Count, Id = w.NewId(), DefId = "roof", Area = new Rect(yard.X, yard.Y, 2, 2), Sound = true, BuiltBy = new List<int> { oren.Id } });
        w.Map.Roof.Add(new RoofSection { Index = w.Map.Roof.Count, Id = w.NewId(), DefId = "roof", Area = new Rect(yard.X, yard.Y + 3, 2, 2), Removed = true, BuiltBy = new List<int> { oren.Id } });
        SetTimber(w, 3);
        Run(w, 4_000, () => w.Log.All.Any(e => e.Key == "event.work.roof_repaired"));
        SimEvent mended = w.Log.All.Single(e => e.Key == "event.work.roof_repaired");
        Assert.Equal("Oren repaired a section of the shelter roof (1 of 3 now sound).", EventText.Describe(w, mended));
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

    /// <summary>A roof promise open on someone's account: the debt the joining would have made, without the arrival.</summary>
    private static Debt PromiseRoof(World w, Character debtor, Character creditor)
    {
        SimEvent formed = w.Log.Emit(w, SystemTag.Social, "event.social.promised_rest", new[] { debtor.Id, creditor.Id }, Array.Empty<long>());
        var d = new Debt
        {
            Id = w.NewId(), DebtorId = debtor.Id, CreditorId = creditor.Id, Kind = DebtKind.PromiseRest, Weight = 1,
            FormedEvent = formed.Id, FormedTick = w.Tick, DueTick = w.Tick + 48L * w.Content.World.TicksPerHour, Status = DebtStatus.Open,
        };
        w.Debts.Add(d);
        return d;
    }

    /// <summary>
    /// Timber taken from the stores for a plan is still the camp's stores while it is carried: a deliverer who drops it on the
    /// way (starving, spent, or caught by the sun) leaves stores where it fell, counted and carried on later, so nobody fells
    /// another tree for timber the camp still has (merge review).
    /// </summary>
    [Fact]
    public void TimberADelivererDropsOnTheWayIsStillStores()
    {
        World w = WorkTests.Only(1, "mira");
        Character mira = Cast(w, "mira");
        Works(mira, ("haul", 1));
        SetTimber(w, 2);
        Blueprint bp = Plan(w, "bed", Tile(BuildPlanTests.ShelterFloor(w))).Single();
        Run(w, 3_000, () => mira.Carrying != null && !Zones.Accepts(w, mira.Pos, "timber"));
        Assert.Equal(JobKind.Deliver, mira.Job?.Kind);
        Assert.Equal(0, Stock.Count(w, "timber")); // committed to the plan: it left the stock
        mira.Food = NeedsSystem.StarvingBelow - 1; // starving, she drops what she carries where she stands
        Run(w, WorkSystem.InterruptEveryTicks, () => mira.Carrying == null);
        Assert.Null(mira.Carrying);
        Assert.Equal(0, bp.Delivered);
        ItemStack dropped = w.Items.Single(s => s.DefId == "timber");
        Assert.False(Zones.Accepts(w, dropped.Pos, "timber"));
        Assert.True(dropped.Stored);
        Assert.Equal(2, Stock.Count(w, "timber"));
    }

    /// <summary>While a roof promise is open, deliverers leave a roof section's timber in stock; above it, they deliver.</summary>
    [Fact]
    public void DeliverersLeaveTheRoofsTimberWhileARoofPromiseIsOpen()
    {
        World w = WorkTests.Only(1, "mira", "ilex");
        Character mira = Cast(w, "mira");
        Works(mira, ("haul", 1));
        Works(Cast(w, "ilex")); // nobody mends the roof meanwhile
        PromiseRoof(w, Cast(w, "you"), Cast(w, "ilex"));
        int reserve = w.Content.WorkById("repair_roof").TimberCost;
        SetTimber(w, reserve);
        Blueprint wall = Plan(w, "wall", Tile(BuildPlanTests.Yard(w))).Single();
        Assert.Null(Building.DeliverJob(w, mira));
        SetTimber(w, reserve + 1);
        Assert.Equal(JobKind.Deliver, Building.DeliverJob(w, mira)?.Kind);
        Run(w, 3_000, () => wall.Delivered == 1);
        Assert.Equal(1, wall.Delivered);
        Assert.Equal(reserve, Stock.Count(w, "timber"));
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.work.used_last_materials"); // a deliverer leaves it, so nobody is passed over
    }

    /// <summary>An Oathbound debtor of a promised roof raises a planned section when there is nothing to mend; the new section keeps the promise.</summary>
    [Fact]
    public void AnOathboundDebtorRaisesAPlannedRoofAndKeepsTheirPromise()
    {
        World w = WorkTests.Only(1, "oren", "ilex");
        w.Tick = 10L * w.Content.World.TicksPerHour; // working hours: at midnight the camp is in bed (ADR-0029)
        Character oren = Cast(w, "oren"), ilex = Cast(w, "ilex");
        Works(oren); // no work at all: only the promise moves him
        Works(ilex);
        foreach (RoofSection r in w.Map.Roof) r.Sound = true; // nothing left to mend
        Debt promise = PromiseRoof(w, oren, ilex);
        SetTimber(w, 3);
        Rect s = w.Map.Shelter;
        Blueprint roof = Plan(w, "roof", new Rect(s.X + 1, s.Y + s.H, 2, 2)).Single(); // just south of the old shelter, held up by its wall
        Job? job = JobPicker.Choose(w, oren);
        Assert.Equal((JobKind.Build, roof.Id, promise.FormedEvent), (job?.Kind, job?.TargetId, job?.CauseEvent));

        Run(w, 4_000, () => promise.Status != DebtStatus.Open);
        Assert.Equal(DebtStatus.Repaid, promise.Status);
        RoofSection raised = w.Map.Roof.Single(r => r.Id == roof.Id);
        SimEvent kept = w.Log.Get(promise.ClosedEvent)!;
        Assert.Equal("event.social.promise_kept", kept.Key);
        Assert.Contains(raised.BuiltEvent, kept.Causes);
        Assert.Contains(promise.FormedEvent, w.Log.Get(raised.BuiltEvent)!.Causes); // the section cites the promise it keeps
    }

    /// <summary>
    /// Someone without a bed counts on a bed plan only while no bed stands free for them: with a bedroll to lie down in
    /// tonight, the last timber going into a wall passes nobody over (review of ADR-0027; it named 41 of 200 people
    /// passed over in 100 camp worlds). One who burns in daylight counts only covered beds as free.
    /// </summary>
    [Fact]
    public void NobodyCountsOnABedPlanWhileABedStandsFreeForThem()
    {
        World w = WorkTests.Only(1, "oren", "you", "ilex");
        Character oren = Cast(w, "oren"), you = Cast(w, "you"), ilex = Cast(w, "ilex");
        SetTimber(w, 2);
        Plan(w, "bed", Tile(BuildPlanTests.ShelterFloor(w)));
        Blueprint wall = Plan(w, "wall", Tile(BuildPlanTests.Yard(w))).Single();
        BuildingDef def = w.Content.BuildingById("wall");
        // The old shelter's roof is broken: its bedrolls are free, but not to one who burns by day.
        Assert.Equal(new[] { ilex.Id }, Building.PassedOver(w, wall, def, 1, oren.Id).Select(c => c.Id));
        foreach (RoofSection r in w.Map.Roof) r.Sound = true;
        Assert.Empty(Building.PassedOver(w, wall, def, 1, oren.Id)); // four bedrolls stand free under a sound roof

        foreach (Bed b in w.Map.Beds) b.OwnerId = 999; // every bed is someone's now
        Assert.Equal(new[] { you.Id, ilex.Id }, Building.PassedOver(w, wall, def, 1, oren.Id).Select(c => c.Id));

        // A free bed out in the open is no bed at all to one who burns by day.
        Bed open = w.Map.Beds[0];
        open.OwnerId = 0;
        open.Pos = BuildPlanTests.Yard(w, 0, 3);
        Assert.False(w.Map.Covered(open.Pos));
        Assert.Equal(new[] { ilex.Id }, Building.PassedOver(w, wall, def, 1, oren.Id).Select(c => c.Id));
    }

    [Fact]
    public void AGenerousPersonWillNotTakeTheLastTimberSomeoneIsCountingOn()
    {
        World w = WorkTests.Only(1, "bram", "you");
        w.Tick = 10L * w.Content.World.TicksPerHour; // working hours: at midnight the camp is in bed (ADR-0029)
        Character bram = Cast(w, "bram");
        Works(bram, ("build", 1));
        SetTimber(w, 2);
        foreach (Bed b in w.Map.Beds) b.OwnerId = 999; // no bed stands free: whoever has none counts on a bed plan
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
