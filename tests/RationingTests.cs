using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>Sharing out short food (ADR-0014): who goes without, broken meal promises, the Desperate, winter.</summary>
public class RationingTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    private static World Short(int berries, params string[] keys)
    {
        World w = WorkTests.Only(1, keys);
        Stock.Take(w, "berries", 99);
        if (berries > 0) Stock.Add(w, "berries", berries, w.Map.StartStockpile.Center);
        return w;
    }

    private static Debt Promise(World w, Character debtor, Character creditor)
    {
        var d = new Debt
        {
            Id = w.NewId(), DebtorId = debtor.Id, CreditorId = creditor.Id, Kind = DebtKind.PromiseMeal, Status = DebtStatus.Open,
            FormedTick = w.Tick, DueTick = w.Tick + 48L * w.Content.World.TicksPerHour,
        };
        d.FormedEvent = w.Log.Emit(w, SystemTag.Social, "event.test.promised", new[] { debtor.Id, creditor.Id }, Array.Empty<long>()).Id;
        w.Debts.Add(d);
        return d;
    }

    [Fact]
    public void NobodyGoesWithoutWhenTheStoresAreEmpty()
    {
        World w = Short(0, "you", "mira", "oren");
        foreach (Character c in w.Members()) c.Food = 200;
        Rations.Assign(w);
        Assert.Empty(w.Reservations);
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.needs.went_without");
    }

    [Fact]
    public void FoodReachingTheStockpileIsSharedOutAtOnce()
    {
        World w = Short(0, "you", "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        mira.Food = 100; oren.Food = 200; Cast(w, "you").Food = 300;
        Point at = w.Map.StartStockpile.Center;
        Stock.Add(w, "berries", 1, at);
        Rations.AfterDeposit(w, "berries", at);
        Reservation r = Assert.Single(w.Reservations);
        Assert.Equal(mira.Id, r.CharacterId);
        Assert.Equal(2, w.Log.All.Count(e => e.Key == "event.needs.went_without"));
    }

    [Fact]
    public void LeavingAPromisedNewcomerOutBreaksThePromise()
    {
        World w = Short(1, "you", "mira", "hollis");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), hollis = Cast(w, "hollis");
        you.Food = 900; mira.Food = 100; hollis.Food = 250; hollis.HungerStage = 1;
        Debt promise = Promise(w, you, hollis);

        Rations.Assign(w);
        Assert.Equal(mira.Id, Assert.Single(w.Reservations).CharacterId);
        SimEvent without = w.Log.All.Single(e => e.Key == "event.needs.went_without" && e.Actors.Contains(hollis.Id));
        Assert.Equal(DebtStatus.Broken, promise.Status);
        SimEvent broken = w.Log.Get(promise.ClosedEvent == 0 ? w.Log.All.Single(e => e.Key == "event.social.promise_broken").Id : promise.ClosedEvent)!;
        Assert.Equal("event.social.promise_broken", broken.Key);
        Assert.Contains(without.Id, broken.Causes);
        Assert.Contains(hollis.Thoughts, t => t.DefId == "promise_broken" && t.AboutId == you.Id);
    }

    [Fact]
    public void APromiseStandsWhileItsCreditorIsNotYetHungry()
    {
        World w = Short(1, "you", "mira", "hollis");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), hollis = Cast(w, "hollis");
        you.Food = 900; mira.Food = 100; hollis.Food = 400;
        Debt promise = Promise(w, you, hollis);
        Rations.Assign(w);
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.went_without" && e.Actors.Contains(hollis.Id));
        Assert.Equal(DebtStatus.Open, promise.Status);
    }

    [Fact]
    public void PromisesFirstKeepsTheNewcomerFed()
    {
        World w = Short(1, "you", "mira", "hollis");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), hollis = Cast(w, "hollis");
        w.Policy = RationPolicy.PromisesFirst;
        you.Food = 900; mira.Food = 100; hollis.Food = 400;
        Debt promise = Promise(w, you, hollis);
        Rations.Assign(w);
        Assert.Equal(hollis.Id, Assert.Single(w.Reservations).CharacterId);
        Assert.Equal(DebtStatus.Open, promise.Status);
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.went_without" && e.Actors.Contains(mira.Id));
    }

    [Fact]
    public void DesperateLeftOutGoesForSomeoneElsesShare()
    {
        World w = Short(1, "nia", "mira");
        Character nia = Cast(w, "nia"), mira = Cast(w, "mira");
        nia.Food = 200; mira.Food = 100;
        Rations.Assign(w);
        Assert.Equal(mira.Id, Assert.Single(w.Reservations).CharacterId);
        Assert.Equal(JobKind.Eat, nia.Job?.Kind);
        Assert.Equal(mira.Id, nia.Job!.TargetId);
    }

    [Fact]
    public void TheMerelyHungryDoNotSteal()
    {
        World w = Short(1, "nia", "mira");
        Character nia = Cast(w, "nia"), mira = Cast(w, "mira");
        nia.Food = 280; mira.Food = 100;
        Rations.Assign(w);
        Assert.NotEqual(JobKind.Eat, nia.Job?.Kind);
    }

    [Fact]
    public void WinterFrostStripsTheBushesThenTheyTrickle()
    {
        World w = WorkTests.Only(1, "you");
        WorldDef def = w.Content.World;
        w.Tick = (long)(def.SeasonsPerYear - 1) * def.TicksPerSeason;
        NeedsSystem.Hourly(w);
        Assert.All(w.Map.Bushes, b => Assert.Equal(0, b.Berries));
        Assert.Contains(w.Log.All, e => e.Key == "event.world.winter_begins");

        int day = Calendar.Day(w.Tick, def);
        for (int i = 0; i < Places.WinterRegrowEveryDays; i++)
        {
            w.Tick = (long)(day + i) * def.TicksPerDay;
            WorkSystem.RegrowForTest(w);
        }
        Assert.All(w.Map.Bushes, b => Assert.Equal(1, b.Berries));
    }
}

public class ChemistryTests
{
    [Fact]
    public void ChemistryIsSymmetricStableAndSpread()
    {
        var counts = new int[5];
        for (ulong seed = 1; seed <= 20; seed++)
        {
            World a = WorldGen.Create(TestContent.Db, seed), b = WorldGen.Create(TestContent.Db, seed);
            foreach (Character x in a.Characters)
                foreach (Character y in a.Characters)
                {
                    if (x.Id >= y.Id) continue;
                    int c = Relations.Chemistry(a, x, y);
                    Assert.Equal(c, Relations.Chemistry(a, y, x));
                    Assert.Equal(c, Relations.Chemistry(b, b.Get(x.Id), b.Get(y.Id)));
                    counts[c + 2]++;
                }
        }
        Assert.All(counts, n => Assert.True(n > 0));
        Assert.True(counts[2] > counts[0] && counts[2] > counts[4], "neutral pairs should be the most common");
    }

    [Fact]
    public void NewcomersLeaveSoonerThanOldHands()
    {
        World w = WorkTests.Only(1, "you", "mira", "hollis");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), hollis = Cast(w, "hollis");
        w.Tick = 10L * w.Content.World.TicksPerDay + 5L * w.Content.World.TicksPerHour;
        hollis.ArrivedTick = w.Tick - w.Content.World.TicksPerDay;
        SimEvent why = w.Log.Emit(w, SystemTag.Social, "event.test.slight", new[] { you.Id }, Array.Empty<long>());
        foreach (Character c in new[] { mira, hollis })
        {
            Relations.ChangeOpinion(w, c, you.Id, SocialSystem.LeaveAtOpinion + SocialSystem.NewcomerLeaveSlack - Relations.Opinion(w, c.Id, you.Id), why.Id);
            Relations.FormGrudge(w, c, you, 30, why.Id);
        }
        SocialSystem.Hourly(w);
        Assert.Equal(CharStatus.Left, hollis.Status);
        Assert.Equal(CharStatus.Member, mira.Status);
    }

    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
}
