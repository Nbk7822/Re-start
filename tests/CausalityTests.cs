using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Modding;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Review findings from the M0 exit check: an event cites only what actually brought it about, and
/// nothing ends a story after its people are gone.
/// </summary>
public class CausalityTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static SimEvent Mark(World w, string key, params int[] actors) => w.Log.Emit(w, SystemTag.Incident, key, actors, Array.Empty<long>());

    [Fact]
    public void AGoodMoodedInsultDoesNotCiteSomeoneElsesTrouble()
    {
        World w = WorkTests.Only(1, "wren", "oren", "mira");
        Character wren = Cast(w, "wren"), oren = Cast(w, "oren"), mira = Cast(w, "mira");
        SimEvent rain = Mark(w, "event.test.rain");
        MoodSystem.AddThought(w, wren, "soaked", 0, rain.Id);
        wren.Mood = 48;
        SimEvent insult = SocialSystem.Insult(w, wren, oren);
        Assert.Empty(insult.Causes);

        SimEvent slight = Mark(w, "event.test.slight", oren.Id, wren.Id);
        MoodSystem.AddThought(w, wren, "insulted", oren.Id, slight.Id);
        Assert.Equal(new[] { slight.Id }, SocialSystem.Insult(w, wren, oren).Causes);
        Assert.Empty(SocialSystem.Insult(w, wren, mira).Causes);

        wren.Mood = SocialSystem.LowMoodBelow - 1;
        Assert.Single(SocialSystem.Insult(w, wren, mira).Causes);
    }

    [Fact]
    public void GrudgesLeaveWithTheirHolder()
    {
        World w = WorkTests.Only(1, "you", "mira", "oren");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren");
        SimEvent why = Mark(w, "event.test.slight", oren.Id, mira.Id);
        Grudge g = Relations.FormGrudge(w, mira, oren, 3, why.Id);
        SocialSystem.Leave(w, mira, why.Id);
        Assert.False(g.Active);
        w.Tick = 5L * w.Content.World.TicksPerDay;
        for (int d = 0; d < 5; d++) { SocialSystem.Hourly(w); w.Tick += w.Content.World.TicksPerDay; }
        Assert.DoesNotContain(w.Log.All, e => e.Effects.Any(f => f.Kind == EffectKind.GrudgeEnded));

        Grudge h = Relations.FormGrudge(w, you, oren, 3, why.Id);
        Lifecycle.Die(w, you, SystemTag.Needs, "event.needs.died", why.Id);
        Assert.False(h.Active);
    }

    [Fact]
    public void GoingWithoutCitesThePolicyOnlyWhenItChangedWhoAte()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        foreach (Character c in w.Characters) if (c.CastKey is not ("you" or "mira" or "oren")) c.Status = CharStatus.OffMap;
        s.Enqueue(new SetRationPolicy(0, RationPolicy.SelfFirst));
        s.Step();
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren");
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 1, w.Map.StartStockpile.Center);

        // You are the hungriest anyway: "you eat first" changed nothing for Mira.
        you.Food = 100; mira.Food = 200; oren.Food = 900;
        Rations.Assign(w);
        SimEvent first = w.Log.All.Last(e => e.Key == "event.needs.went_without");
        Assert.DoesNotContain(w.PolicyEvent, first.Causes);

        // Mira is hungrier than you: "you eat first" is why she goes without.
        mira.SkippedRation = false; mira.LastFoodEvent = 0;
        you.Food = 300; mira.Food = 100;
        Rations.Assign(w);
        SimEvent second = w.Log.All.Last(e => e.Key == "event.needs.went_without");
        Assert.Contains(w.PolicyEvent, second.Causes);
    }

    [Fact]
    public void ASunburnDoesNotCiteAnOldWound()
    {
        World w = WorkTests.Only(1, "ilex");
        Character ilex = Cast(w, "ilex");
        SimEvent old = Mark(w, "event.test.accident", ilex.Id);
        ilex.LastHealthEvent = old.Id;
        w.Tick = 3L * w.Content.World.TicksPerDay + 12L * w.Content.World.TicksPerHour;
        ilex.Pos = w.Map.StartStockpile.Center;
        NeedsSystem.Hourly(w);
        SimEvent burn = w.Log.All.Single(e => e.Key == "event.needs.sun_burn");
        Assert.Empty(burn.Causes);
        w.Tick += w.Content.World.TicksPerHour;
        NeedsSystem.Hourly(w);
        Assert.Equal(new[] { burn.Id }, w.Log.All.Last(e => e.Key == "event.needs.sun_burn").Causes);
    }

    [Fact]
    public void ALeaverWhoIsNotHungryDoesNotCiteFood()
    {
        World w = WorkTests.Only(1, "you", "mira");
        Character mira = Cast(w, "mira");
        SimEvent hunger = Mark(w, "event.test.hunger", mira.Id);
        SimEvent why = Mark(w, "event.test.slight", mira.Id);
        mira.LastFoodEvent = hunger.Id;
        mira.HungerStage = 0;
        Assert.Equal(new[] { why.Id }, SocialSystem.Leave(w, mira, why.Id).Causes);
    }

    /// <summary>Zones design 5.5's cause fix: a forage chosen for hunger carries it, and the picking cites it; a forage for the camp cites nothing.</summary>
    [Fact]
    public void AForageForHungerCarriesTheHungerToThePicking()
    {
        World w = WorkTests.Only(1, "you");
        Stock.Take(w, "berries", 99);
        Character you = Cast(w, "you");
        SimEvent hunger = Mark(w, "event.test.hunger", you.Id);
        you.Food = 250;
        you.LastFoodEvent = hunger.Id;
        w.Tick = 10L * w.Content.World.TicksPerHour;
        Job? job = JobPicker.Choose(w, you);
        Assert.Equal(JobKind.Forage, job?.Kind);
        Assert.Equal(hunger.Id, job!.CauseEvent);
        WorkSystem.Assign(w, you);
        for (int t = 0; t < 4_000 && !w.Log.All.Any(e => e.Key == "event.work.foraged"); t++)
        {
            TravelSystem.Step(w);
            WorkSystem.Step(w);
            w.Tick++;
        }
        Assert.Equal(new[] { hunger.Id }, w.Log.All.First(e => e.Key == "event.work.foraged").Causes);

        // Fed, they forage for the camp's stores: no hunger of theirs is why.
        World v = WorkTests.Only(1, "you");
        Stock.Take(v, "berries", 99);
        v.Tick = 10L * v.Content.World.TicksPerHour;
        Character fed = Cast(v, "you");
        fed.LastFoodEvent = Mark(v, "event.test.hunger", fed.Id).Id;
        Job? work = JobPicker.Choose(v, fed);
        Assert.Equal(JobKind.Forage, work?.Kind);
        Assert.Equal(0, work!.CauseEvent);
    }

    [Fact]
    public void PromiseTextTakesItsLengthFromTheWorld()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        int wren = Cast(w, "wren").Id;
        string Say(int days) => EventText.Describe(w, new SimEvent
        {
            Key = "event.social.joined_promised_meal",
            Args = { EventArg.Char("a", wren), EventArg.Char("b", w.PlayerId), EventArg.Num("n", days) },
        });
        Assert.Equal("Wren agreed to stay after you promised them a meal within 2 days.", Say(2));
        Assert.Equal("Wren agreed to stay after you promised them a meal within a day.", Say(1));
    }

    [Fact]
    public void AHookMayChangeTheWorld()
    {
        string Run()
        {
            var s = Simulation.Create(TestContent.Db, 3);
            s.AddHook(new Meddler());
            s.Advance(3L * s.World.Content.World.TicksPerDay);
            Assert.Contains(s.World.Characters, c => c.CastKey.StartsWith("ghost", StringComparison.Ordinal));
            return s.StateHash();
        }
        Assert.Equal(Run(), Run());
    }

    /// <summary>Adds someone to the world (and a grudge) whenever anyone gets hungry: list changes a hook must be allowed to make.</summary>
    private sealed class Meddler : IModHook
    {
        public void OnWorldCreated(World w) { }

        public void OnEvent(World w, SimEvent e)
        {
            if (e.Key != "event.needs.hungry") return;
            w.Characters.Add(new Character { Id = w.NewId(), CastKey = "ghost" + w.Characters.Count, NameKey = "name.someone", SpeciesId = "human", Status = CharStatus.OffMap, Traits = new List<string>() });
            w.Grudges.Add(new Grudge { Id = w.NewId(), HolderId = e.Actors[0], TargetId = e.Actors[0], Intensity = 1, FormedTick = w.Tick });
        }
    }
}
