using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;
using Return.Sim.Systems.Jobs;

namespace Return.Tests;

/// <summary>The stockpile as a zone (ADR-0028): what counts as stores, where goods go, and that moving goods never loses them.</summary>
public class StoresTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    [Fact]
    public void TheCampStartsWithItsStockpileAsAZoneKeepingEveryItem()
    {
        World w = WorldGen.Create(TestContent.Db, 3);
        Zone pile = Assert.Single(w.Map.Zones, z => z.Kind == "stockpile");
        Assert.Equal(Zones.FromRect(w.Map.StartStockpile), pile.Tiles);
        Assert.Equal(new[] { "berries", "timber" }, pile.Items);
        Assert.Equal(0, pile.MadeEvent);
        Assert.All(w.Items, s => Assert.True(s.Stored, $"start stack {s.DefId} should be stores"));
        Assert.True(Zones.Accepts(w, w.Map.StartStockpile.Center, "berries"));
        Assert.False(Zones.Accepts(w, w.Map.Hearth, "berries"));
    }

    [Fact]
    public void BerriesPutDownOnAStockpileAreStoresAndLooseOnesAreNot()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        int before = Stock.Count(w, "berries");
        ItemStack kept = Stock.Add(w, "berries", 2, w.Map.StartStockpile.Center);
        ItemStack loose = Stock.Add(w, "berries", 3, new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3));
        Assert.True(kept.Stored);
        Assert.False(loose.Stored);
        Assert.Equal(before + 2, Stock.Count(w, "berries"));
    }

    [Fact]
    public void AStoredLoadDroppedOnTheWayStaysStoresAndNeverMergesWithLooseGoods()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        var off = new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3);
        ItemStack loose = Stock.Add(w, "timber", 1, off);
        ItemStack carried = Stock.Add(w, "timber", 2, off, stored: true);
        Assert.NotSame(loose, carried);
        Assert.True(carried.Stored);
        Assert.False(loose.Stored);
    }

    [Fact]
    public void AHaulerCarryingStoresKeepsThemStoresWhenTheLoadIsDropped()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        var off = new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3);
        ItemStack waiting = Stock.Add(w, "timber", 2, off, stored: true);
        oren.Pos = off;
        oren.Job = new Job { Kind = JobKind.Haul, TargetId = waiting.Id, Target = off };
        Assert.False(CampJobs.Haul(w, oren, oren.Job));
        Assert.True(oren.Carrying is { Stored: true, DefId: "timber", Count: 2 });
        int stores = Stock.Count(w, "timber");
        WorkSystem.Drop(w, oren);
        Assert.Equal(stores + 2, Stock.Count(w, "timber"));
        Assert.Contains(w.Items, s => s.Pos == off && s.DefId == "timber" && s.Stored);
    }

    [Fact]
    public void CanonicalRunsAreSortedMergedAndFreeOfDuplicates()
    {
        var tiles = new[] { new Point(3, 2), new Point(1, 1), new Point(2, 1), new Point(2, 1), new Point(5, 2), new Point(4, 2) };
        Assert.Equal(new[] { new TileRun(1, 1, 2), new TileRun(2, 3, 3) }, Zones.Canonical(tiles));
        Assert.Equal(5, Zones.Count(Zones.Canonical(tiles)));
        Assert.Equal(new Rect(1, 1, 5, 2), Zones.Bounds(Zones.Canonical(tiles)));
    }

    [Fact]
    public void ZonesAndStoresSurviveASnapshot()
    {
        var sim = Simulation.Create(TestContent.Db, 6);
        sim.Advance(3_000);
        Simulation r = Simulation.Restore(TestContent.Db, sim.Snapshot());
        Assert.Equal(sim.World.Map.Zones.Count, r.World.Map.Zones.Count);
        Assert.Equal(sim.World.Map.Zones[0].Tiles, r.World.Map.Zones[0].Tiles);
        Assert.Equal(sim.World.Items.Select(s => s.Stored), r.World.Items.Select(s => s.Stored));
        Assert.True(Zones.Accepts(r.World, r.World.Map.StartStockpile.Center, "timber"));
        Assert.Equal(sim.StateHash(), r.StateHash());
    }

    private static long Hours(World w, int h) => (long)h * w.Content.World.TicksPerHour;

    private static int StartId(World w) => w.Map.Zones.First(z => z.Kind == "stockpile").Id;

    /// <summary>Moves the camp's stockpile into the covered ruins and leaves <paramref name="berries"/> berries there, and no other food.</summary>
    private static SimEvent MoveToRuins(World w, int berries)
    {
        new MarkZone(w.Tick, StartId(w), "stockpile", Zones.FromRect(w.Map.CoveredRuins)).Apply(w);
        Stock.Take(w, "berries", 999);
        if (berries > 0) Stock.Add(w, "berries", berries, w.Map.CoveredRuins.Center);
        return w.Log.All.Last(e => e.Key == "event.world.zone_moved");
    }

    /// <summary>A night thief standing at <paramref name="at"/>, heading for <paramref name="target"/>, as the incident makes one.</summary>
    private static Character Thief(World w, Point at, Point target)
    {
        var thief = new Character
        {
            Id = w.NewId(), NameKey = "npc.thief.name", SpeciesId = "human", Named = false, Status = CharStatus.Hostile,
            Health = w.Content.IncidentById("night_thief").Param("health"), Food = 1000, Rest = 1000, Social = 1000, Traits = new List<string>(), Pos = at,
        };
        w.Characters.Add(thief);
        SimEvent raid = w.Log.Emit(w, SystemTag.Incident, "event.incident.night_thief", new[] { thief.Id }, Array.Empty<long>(), null, EventArg.Char("a", thief.Id));
        thief.Job = new Job { Kind = JobKind.Raid, Target = target, StartedTick = w.Tick, CauseEvent = raid.Id };
        TravelSystem.Start(w, thief, target);
        return thief;
    }

    private static void RunCombat(World w, int ticks, Func<bool> until)
    {
        for (int t = 0; t < ticks && !until(); t++)
        {
            TravelSystem.Step(w);
            CombatSystem.Step(w);
            w.Tick++;
        }
    }

    private static void Watch(World w, Character c)
    {
        c.Pos = w.Map.WatchPost;
        c.Job = new Job { Kind = JobKind.KeepWatch, Target = w.Map.WatchPost, Stage = 1, StartedTick = w.Tick };
    }

    [Fact]
    public void TheDampSparesStoresUnderCover()
    {
        World w = WorkTests.Only(1, "you");
        MoveToRuins(w, 8);
        Assert.True(w.Map.Covered(w.Map.CoveredRuins.Center));
        Assert.Null(IncidentSystem.Fire(w, "food_spoils"));
        Assert.Equal(8, Stock.Count(w, "berries"));

        var open = new Point(w.Map.StartStockpile.X, w.Map.StartStockpile.Y);
        Stock.Add(w, "berries", 6, open, stored: true); // stores waiting to be carried, out in the open
        Assert.NotNull(IncidentSystem.Fire(w, "food_spoils"));
        Assert.Equal(8, w.Items.Where(s => s.DefId == "berries" && s.Pos == w.Map.CoveredRuins.Center).Sum(s => s.Count));
        Assert.InRange(6 - w.Items.Where(s => s.DefId == "berries" && s.Pos == open).Sum(s => s.Count),
            w.Content.IncidentById("food_spoils").Param("lossMin"), w.Content.IncidentById("food_spoils").Param("lossMax"));
    }

    [Fact]
    public void AThiefTakesOnlyFromThePileItReached()
    {
        World w = WorkTests.Only(1);
        w.Tick = Hours(w, 23);
        new MarkZone(w.Tick, 0, "stockpile", Zones.FromRect(w.Map.CoveredRuins)).Apply(w);
        Stock.Take(w, "berries", 999);
        var near = new Point(w.Map.StartStockpile.X, w.Map.StartStockpile.Y);
        Stock.Add(w, "berries", 5, near);
        Stock.Add(w, "berries", 5, w.Map.CoveredRuins.Center);
        Character thief = Thief(w, new Point(near.X - 2, near.Y), near);
        RunCombat(w, 3_000, () => thief.Job is { Kind: JobKind.Flee });
        Assert.Equal(5 - w.Content.IncidentById("night_thief").Param("stealMax"), w.Items.Where(s => s.Pos == near && s.DefId == "berries").Sum(s => s.Count));
        Assert.Equal(5, w.Items.Where(s => s.Pos == w.Map.CoveredRuins.Center).Sum(s => s.Count));
        Assert.Contains(w.Log.All, e => e.Key == "event.incident.stolen");
    }

    [Fact]
    public void AThiefTheWatchNeverSawNamesTheWatchAndTheStockpileThePlayerPutThere()
    {
        World w = WorkTests.Only(1, "mira", "nia");
        w.Tick = Hours(w, 23);
        Character nia = Cast(w, "nia");
        SimEvent moved = MoveToRuins(w, 5);
        Watch(w, Cast(w, "mira"));
        Rect ruins = w.Map.CoveredRuins;
        Character thief = Thief(w, new Point(ruins.X + ruins.W, ruins.Y), Places.FoodTile(w, "berries"));
        SimEvent raid = w.Log.All[^1];
        RunCombat(w, 3_000, () => thief.Job is { Kind: JobKind.Flee });
        SimEvent stolen = Assert.Single(w.Log.All, e => e.Key.StartsWith("event.incident.stolen", StringComparison.Ordinal));
        Assert.Equal("event.incident.stolen_unseen", stolen.Key);
        Assert.Equal(new[] { raid.Id, moved.Id }, stolen.Causes);
        Assert.Equal("A thief made off with 3 berries from the stockpile, out of sight of Mira on watch.", EventText.Describe(w, stolen));
        Assert.Equal(stolen.Id, nia.LastFoodEvent);
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.combat.thief_spotted");
    }

    [Fact]
    public void AnUnseenTheftFromTheCampsFirstStockpileCitesNoChoice()
    {
        World w = WorkTests.Only(1, "mira");
        w.Tick = Hours(w, 23);
        Watch(w, Cast(w, "mira"));
        Point pile = Places.FoodTile(w, "berries");
        Assert.True(pile.ChebyshevTo(w.Map.WatchPost) > CombatSystem.SightRange);
        Character thief = Thief(w, new Point(pile.X - 2, pile.Y), pile);
        SimEvent raid = w.Log.All[^1];
        RunCombat(w, 3_000, () => thief.Job is { Kind: JobKind.Flee });
        SimEvent stolen = Assert.Single(w.Log.All, e => e.Key == "event.incident.stolen_unseen");
        Assert.Equal(new[] { raid.Id }, stolen.Causes);
    }

    [Fact]
    public void ABerriesOnlyStockpileTurnsTimberAwayAndGoodsGoToTheNearestStockpile()
    {
        World w = WorldGen.Create(TestContent.Db, 2);
        new MarkZone(w.Tick, 0, "stockpile", Zones.FromRect(w.Map.CoveredRuins), new[] { "berries" }).Apply(w);
        Assert.False(Stock.Add(w, "timber", 1, w.Map.CoveredRuins.Center).Stored);
        Assert.True(w.Map.StartStockpile.Contains(Stock.DropTile(w, "timber", w.Map.CoveredRuins.Center)));
        Stock.Take(w, "berries", 999);
        var fromRuins = new Point(w.Map.CoveredRuins.X + w.Map.CoveredRuins.W, w.Map.CoveredRuins.Y + 1);
        Assert.True(w.Map.CoveredRuins.Contains(Stock.DropTile(w, "berries", fromRuins)));
        var fromStart = new Point(w.Map.StartStockpile.X - 1, w.Map.StartStockpile.Y);
        Assert.True(w.Map.StartStockpile.Contains(Stock.DropTile(w, "berries", fromStart)));
        Stock.Add(w, "berries", 1, w.Map.CoveredRuins.Center);
        Assert.Equal(w.Map.CoveredRuins.Center, Stock.DropTile(w, "berries", fromStart)); // a tile already holding berries comes first
    }

    [Fact]
    public void FoodWaitingToBeCarriedCanStillBeEatenAndSharedOut()
    {
        World w = WorkTests.Only(1, "you");
        Character you = Cast(w, "you");
        Point old = Places.FoodTile(w, "berries");
        int berries = Stock.Count(w, "berries");
        new MarkZone(w.Tick, StartId(w), "stockpile", Zones.FromRect(w.Map.CoveredRuins)).Apply(w);
        Assert.Equal(old, Places.FoodTile(w, "berries"));
        you.Food = NeedsSystem.HungryBelow - 10;
        Job? eat = JobPicker.Choose(w, you);
        Assert.Equal(JobKind.Eat, eat?.Kind);
        Assert.Equal(old, eat!.Target);
        you.Pos = old;
        Assert.NotNull(NeedsSystem.Eat(w, you, 0));
        Assert.Equal(berries - 1, Stock.Count(w, "berries"));
    }

    /// <summary>
    /// Story 1 of the zones design: the stockpile moved into the ruins, out of the watch's sight; a thief
    /// takes the berries unseen; the hungry go without, and the Desperate take a meal set aside for
    /// someone else. The analyzer finds one story whose chain starts with the player's tidy-up.
    /// </summary>
    [Fact]
    public void TheDryStoresStoryRunsFromTheMoveToTheGrudge()
    {
        World w = WorkTests.Only(1, "mira", "nia", "bram");
        w.Tick = Hours(w, 23);
        Character nia = Cast(w, "nia"), bram = Cast(w, "bram");
        SimEvent moved = MoveToRuins(w, 4);
        Watch(w, Cast(w, "mira"));
        Rect ruins = w.Map.CoveredRuins;
        Character thief = Thief(w, new Point(ruins.X + ruins.W, ruins.Y), Places.FoodTile(w, "berries"));
        RunCombat(w, 3_000, () => thief.Job is { Kind: JobKind.Flee });
        Assert.Equal(1, Stock.Count(w, "berries"));

        Bed bed = w.Map.Beds[0];
        bram.Pos = bed.Pos;
        bram.Rest = 500;
        bram.Asleep = true;
        bram.Job = new Job { Kind = JobKind.Sleep, Target = bed.Pos, Stage = 1, StartedTick = w.Tick };
        bram.Food = NeedsSystem.StarvingBelow + 10;
        nia.Food = Traits.Max(w, nia, t => t.StealFoodBelow) - 20;
        Assert.True(nia.Food > bram.Food);
        Rations.Assign(w);
        for (int t = 0; t < 3_000 && !w.Log.All.Any(e => e.Key == "event.social.took_meal"); t++)
        {
            TravelSystem.Step(w);
            WorkSystem.Step(w);
            w.Tick++;
        }
        SimEvent grudge = Assert.Single(w.Log.All, e => e.Key == "event.social.grudge_formed");
        Assert.Equal(new[] { bram.Id, nia.Id }, grudge.Actors);
        Story story = Assert.Single(StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true), s => s.TerminalEventId == grudge.Id);
        Assert.Equal(moved.Id, story.Chain[0]);
        Assert.Equal(new[] { SystemTag.World, SystemTag.Incident, SystemTag.Needs, SystemTag.Social }, story.Systems);
    }
}
