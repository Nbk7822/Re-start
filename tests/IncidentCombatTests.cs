using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

public class IncidentCombatTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static long Hours(World w, int h) => (long)h * w.Content.World.TicksPerHour;
    private static int Param(World w, string incident, string name) => w.Content.IncidentById(incident).Param(name);
    private static int AllItems(World w, string defId) => w.Items.Where(s => s.DefId == defId).Sum(s => s.Count);

    /// <summary>Per-tick movement and fighting; work too unless the test pins everyone's job itself.</summary>
    private static void Run(World w, int ticks, Func<bool> until, bool work = true)
    {
        for (int t = 0; t < ticks; t++)
        {
            TravelSystem.Step(w);
            CombatSystem.Step(w);
            if (work) WorkSystem.Step(w);
            w.Tick++;
            if (until()) return;
        }
    }

    /// <summary>The nearest passable tile to <paramref name="near"/> that is open to the sky and not a bed.</summary>
    private static Point OpenTile(World w, Point near, Point skip = default)
    {
        for (int r = 1; r < 20; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    var p = new Point(near.X + dx, near.Y + dy);
                    if (p == skip || !w.Map.Passable(p) || w.Map.Covered(p) || w.Map.Beds.Any(b => b.Pos == p)) continue;
                    return p;
                }
        throw new InvalidOperationException("no open tile");
    }

    private static Job Watch(World w, Character c, Point at)
    {
        c.Pos = at;
        return c.Job = new Job { Kind = JobKind.KeepWatch, Target = at, Stage = 1, StartedTick = w.Tick };
    }

    [Fact]
    public void RainSoaksOnlyUncovered()
    {
        World w = WorkTests.Only(1, "you", "mira", "oren");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren");
        you.Pos = w.Map.CoveredRuins.Center;
        mira.Pos = OpenTile(w, w.Map.Hearth);
        oren.Pos = OpenTile(w, w.Map.Hearth, skip: mira.Pos);
        oren.Asleep = true;
        int orenRest = oren.Rest;

        SimEvent rain = IncidentSystem.Fire(w, "rain")!;
        Assert.Equal("event.incident.rain", rain.Key);
        Assert.Equal(SystemTag.Incident, rain.System);
        Assert.Null(IncidentSystem.Fire(w, "rain"));

        w.Tick += Hours(w, 1);
        IncidentSystem.Hourly(w);
        Assert.Contains(mira.Thoughts, t => t.DefId == "soaked" && t.CauseEvent == rain.Id);
        Assert.Contains(oren.Thoughts, t => t.DefId == "soaked" && t.CauseEvent == rain.Id);
        Assert.DoesNotContain(you.Thoughts, t => t.DefId == "soaked");
        Assert.Equal(orenRest - Param(w, "rain", "soakRestPerHour"), oren.Rest);
        Assert.Equal(rain.Id, oren.LastRestEvent);
    }

    [Fact]
    public void SpoilageSetsFoodCause()
    {
        World w = WorkTests.Only(1, "you", "ilex");
        Character you = Cast(w, "you"), ilex = Cast(w, "ilex");
        Stock.Add(w, "berries", 6, w.Map.Stockpile.Center);
        int before = Stock.Count(w, "berries");

        SimEvent spoil = IncidentSystem.Fire(w, "food_spoils")!;
        Assert.Equal(SystemTag.Incident, spoil.System);
        Assert.InRange(before - Stock.Count(w, "berries"), Param(w, "food_spoils", "lossMin"), Param(w, "food_spoils", "lossMax"));
        Assert.Equal(spoil.Id, you.LastFoodEvent);
        Assert.NotEqual(spoil.Id, ilex.LastFoodEvent);

        you.Food = NeedsSystem.HungryBelow + 10;
        NeedsSystem.Hourly(w);
        SimEvent hungry = w.Log.All.Single(e => e.Key == "event.needs.hungry" && e.Actors.Contains(you.Id));
        Assert.Contains(spoil.Id, hungry.Causes);
    }

    [Fact]
    public void SpoilageNeedsAStock()
    {
        World w = WorkTests.Only(1, "you");
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", Param(w, "food_spoils", "minBerries") - 1, w.Map.Stockpile.Center);
        Assert.Null(IncidentSystem.Fire(w, "food_spoils"));
    }

    [Fact]
    public void WandererJoinsWithPromise()
    {
        World w = WorkTests.Only(1, "you");
        Character you = Cast(w, "you"), wren = Cast(w, "wren");
        Assert.Equal(CharStatus.OffMap, wren.Status);
        Stock.Take(w, "berries", 99);

        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: wren.Id)!;
        Assert.Equal(SystemTag.Incident, arrived.System);
        Assert.Equal(CharStatus.Member, wren.Status);
        Assert.Equal(JobKind.Arrive, wren.Job?.Kind);

        Run(w, 6_000, () => w.Debts.Any(d => d.CreditorId == wren.Id));
        Debt promise = Assert.Single(w.Debts, d => d.CreditorId == wren.Id);
        Assert.Equal(DebtKind.PromiseMeal, promise.Kind);
        Assert.Equal(you.Id, promise.DebtorId);
        Assert.Equal(DebtStatus.Open, promise.Status);
        Assert.Equal(promise.FormedTick + Hours(w, w.Content.World.PromiseDueHours), promise.DueTick);

        SimEvent joined = w.Log.Get(promise.FormedEvent)!;
        Assert.Equal("event.social.joined_promised_meal", joined.Key);
        Assert.Contains(arrived.Id, joined.Causes);
        Assert.Contains(joined.Effects, f => f.Kind == EffectKind.Recruitment && f.A == wren.Id);
        Assert.Contains(joined.Effects, f => f.Kind == EffectKind.DebtFormed && f.A == you.Id && f.B == wren.Id && f.RefId == promise.Id);
    }

    [Fact]
    public void WandererIsWelcomedWithoutPromiseWhenFoodIsSpare()
    {
        World w = WorkTests.Only(1, "you");
        Character you = Cast(w, "you"), wren = Cast(w, "wren");
        Stock.Add(w, "berries", 5, w.Map.Stockpile.Center);
        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: wren.Id)!;
        SimEvent joined = IncidentSystem.Join(w, wren, arrived.Id);
        Assert.Equal("event.social.joined", joined.Key);
        Assert.Equal("You welcomed Wren into the camp.", Return.Sim.Api.EventText.Describe(w, joined));
        Assert.Contains(joined.Effects, f => f.Kind == EffectKind.Recruitment && f.A == wren.Id && f.B == you.Id);
        Assert.DoesNotContain(joined.Effects, f => f.Kind == EffectKind.DebtFormed);
        Assert.Empty(w.Debts);
    }

    [Fact]
    public void RoofPromiseIsKeptWhenTheRoofIsMended()
    {
        World w = WorkTests.Only(1, "you", "oren");
        w.Tick = Hours(w, 20);
        Character you = Cast(w, "you"), oren = Cast(w, "oren"), vale = Cast(w, "vale");
        Assert.DoesNotContain(w.Map.Roof, r => r.Sound);
        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: vale.Id)!;
        SimEvent joined = IncidentSystem.Join(w, vale, arrived.Id);
        Debt promise = Assert.Single(w.Debts);
        Assert.Equal(DebtKind.PromiseRest, promise.Kind);
        Assert.Equal(you.Id, promise.DebtorId);

        Stock.Add(w, "timber", w.Content.WorkById("repair_roof").TimberCost, w.Map.Stockpile.Center);
        oren.Priorities.RemoveAll(p => p.WorkId == "repair_roof");
        oren.Priorities.Add(new WorkPriority { WorkId = "repair_roof", Priority = 1 });
        Run(w, 6_000, () => promise.Status != DebtStatus.Open);

        Assert.Equal(DebtStatus.Repaid, promise.Status);
        SimEvent kept = w.Log.Get(promise.ClosedEvent)!;
        Assert.Equal("event.social.promise_kept", kept.Key);
        SimEvent mended = w.Log.All.First(e => e.Key == "event.work.roof_repaired");
        Assert.Contains(mended.Id, kept.Causes);
        Assert.Contains(joined.Id, mended.Causes);
        Assert.Contains(arrived.Id, NeedsMoodTests.Ancestors(w, kept.Id));
    }

    [Fact]
    public void VampireIsWelcomedWithoutPromiseUnderASoundRoof()
    {
        World w = WorkTests.Only(1, "you");
        w.Map.Roof[0].Sound = true;
        Character ilex = Cast(w, "ilex");
        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: ilex.Id)!;
        Assert.Equal("event.social.joined", IncidentSystem.Join(w, ilex, arrived.Id).Key);
        Assert.Empty(w.Debts);
    }

    [Fact]
    public void VampireWandererIsPromisedRest()
    {
        World w = WorkTests.Only(1, "you");
        Character ilex = Cast(w, "ilex");
        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: ilex.Id)!;
        SimEvent joined = IncidentSystem.Join(w, ilex, arrived.Id);
        Assert.Equal("event.social.joined_promised_rest", joined.Key);
        Assert.Equal(DebtKind.PromiseRest, Assert.Single(w.Debts, d => d.CreditorId == ilex.Id).Kind);
        Assert.Contains(ilex.Thoughts, t => t.DefId == "joined_camp");
    }

    [Fact]
    public void AccidentWoundsWithCause()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        SimEvent felling = w.Log.Emit(w, SystemTag.Work, "event.work.felled_tree", new[] { oren.Id }, Array.Empty<long>(), null, EventArg.Char("a", oren.Id));
        int before = oren.Health;

        SimEvent hurt = IncidentSystem.Fire(w, "work_accident", felling.Id, oren.Id)!;
        Assert.Equal(SystemTag.Incident, hurt.System);
        Assert.Contains(felling.Id, hurt.Causes);
        Assert.InRange(before - oren.Health, Param(w, "work_accident", "damageMin"), Param(w, "work_accident", "damageMax"));
        Assert.Equal(hurt.Id, oren.LastHealthEvent);
        Assert.Contains(oren.Thoughts, t => t.DefId == "wounded" && t.CauseEvent == hurt.Id);
    }

    [Fact]
    public void WindfallLandsByTheRiver()
    {
        World w = WorkTests.Only(1, "you");
        int before = AllItems(w, "berries") + AllItems(w, "timber");
        SimEvent e = IncidentSystem.Fire(w, "windfall")!;
        Assert.Equal(SystemTag.Incident, e.System);
        Assert.Equal(before + Param(w, "windfall", "amount"), AllItems(w, "berries") + AllItems(w, "timber"));
        Assert.Contains(Cast(w, "you").Thoughts, t => t.DefId == "windfall" && t.CauseEvent == e.Id);
    }

    [Fact]
    public void ThiefStealsWithoutWatch()
    {
        World w = WorkTests.Only(1, "you");
        Character you = Cast(w, "you");
        w.Tick = Hours(w, 23);
        you.Pos = w.Map.CoveredRuins.Center;
        you.Asleep = true;
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 5, w.Map.Stockpile.Center);

        SimEvent raid = IncidentSystem.Fire(w, "night_thief")!;
        Assert.Equal(SystemTag.Incident, raid.System);
        Character thief = w.Characters.Single(c => c.Status == CharStatus.Hostile);
        Assert.False(thief.Named);

        Run(w, 4_000, () => thief.Status != CharStatus.Hostile, work: false);
        SimEvent stolen = w.Log.All.Single(e => e.Key == "event.incident.stolen");
        Assert.Contains(raid.Id, NeedsMoodTests.Ancestors(w, stolen.Id));
        Assert.Equal(5 - Param(w, "night_thief", "stealMax"), Stock.Count(w, "berries"));
        Assert.Equal(stolen.Id, you.LastFoodEvent);
        Assert.Contains(you.Thoughts, t => t.DefId == "robbed");
        Assert.Equal(CharStatus.Left, thief.Status);
        Assert.DoesNotContain(w.Log.All, e => e.System == SystemTag.Combat);
    }

    [Fact]
    public void ThiefFightsWatch()
    {
        World w = WorkTests.Only(1, "mira");
        Character mira = Cast(w, "mira");
        w.Tick = Hours(w, 23);
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 5, w.Map.Stockpile.Center);
        Watch(w, mira, w.Map.Stockpile.Center);

        SimEvent raid = IncidentSystem.Fire(w, "night_thief")!;
        Character thief = w.Characters.Single(c => c.Status == CharStatus.Hostile);
        Run(w, 4_000, () => thief.Status != CharStatus.Hostile);

        SimEvent spotted = w.Log.All.Single(e => e.Key == "event.combat.thief_spotted");
        Assert.Equal(SystemTag.Combat, spotted.System);
        Assert.Contains(raid.Id, spotted.Causes);
        SimEvent fight = w.Log.All.First(e => e.Key == "event.combat.fight");
        Assert.Contains(spotted.Id, fight.Causes);
        Assert.Contains(mira.Id, fight.Actors);
        Assert.Contains(w.Log.All, e => e.Key is "event.combat.thief_fled" or "event.combat.thief_killed");
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.incident.stolen");
        Assert.Equal(5, Stock.Count(w, "berries"));
        Assert.Contains(mira.Thoughts, t => t.DefId == "thief_driven_off");
        Assert.NotEqual(CharStatus.Hostile, thief.Status);
    }

    [Fact]
    public void AnxiousGuardFleesAndIsResented()
    {
        World w = WorkTests.Only(1, "mira", "wren");
        Character mira = Cast(w, "mira"), wren = Cast(w, "wren");
        w.Tick = Hours(w, 23);
        Point post = w.Map.Stockpile.Center;
        Watch(w, mira, post);
        Watch(w, wren, OpenTile(w, post));
        wren.Health = Traits.Max(w, wren, t => t.FleeHealthBelow) - 50;

        IncidentSystem.Fire(w, "night_thief");
        Run(w, 4_000, () => w.Log.All.Any(e => e.Key == "event.combat.fled"));

        SimEvent fled = w.Log.All.Single(e => e.Key == "event.combat.fled");
        Assert.Equal(new[] { wren.Id }, fled.Actors);
        Grudge g = Assert.Single(w.Grudges);
        Assert.Equal(mira.Id, g.HolderId);
        Assert.Equal(wren.Id, g.TargetId);
        Assert.Equal(fled.Id, g.CauseEvent);
    }

    [Fact]
    public void RescueCreatesDebt()
    {
        World w = WorkTests.Only(1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        w.Tick = Hours(w, 12);
        oren.Pos = OpenTile(w, w.Map.Hearth);
        SimEvent hurt = w.Log.Emit(w, SystemTag.Incident, "event.test.hurt", new[] { oren.Id }, Array.Empty<long>());
        oren.Health = 100;
        oren.Downed = true;
        oren.LastHealthEvent = hurt.Id;

        Run(w, 4_000, () => w.Log.All.Any(e => e.Key == "event.work.rescued"));
        SimEvent rescued = w.Log.All.Single(e => e.Key == "event.work.rescued");
        Assert.Equal(SystemTag.Work, rescued.System);
        Assert.Equal(new[] { mira.Id, oren.Id }, rescued.Actors);
        Assert.Contains(hurt.Id, rescued.Causes);
        Debt debt = Assert.Single(w.Debts, d => d.Kind == DebtKind.Rescue);
        Assert.Equal(oren.Id, debt.DebtorId);
        Assert.Equal(mira.Id, debt.CreditorId);
        Assert.Contains(w.Map.Beds, b => b.Pos == oren.Pos);
        Assert.Contains(oren.Thoughts, t => t.DefId == "rescued" && t.AboutId == mira.Id);
    }

    [Fact]
    public void DirectorSpacesMajors()
    {
        var s = Simulation.Create(TestContent.Db, 3);
        World w = s.World;
        s.Advance(30L * w.Content.World.TicksPerDay);
        long gap = Hours(w, w.Content.World.Director.MinHoursBetweenMajors);
        List<long> majors = w.Log.All.Where(e => e.Key is "event.incident.food_spoils" or "event.incident.night_thief").Select(e => e.Tick).ToList();
        Assert.True(majors.Count >= 2, $"only {majors.Count} major incidents in 30 days");
        for (int i = 1; i < majors.Count; i++)
            Assert.True(majors[i] - majors[i - 1] >= gap, $"majors at ticks {majors[i - 1]} and {majors[i]} are closer than {gap}");
    }
}
