using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Modding;
using Return.Sim.Stories;
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
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center);

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
        ilex.Pos = w.Map.Stockpile.Center;
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
            // Six days: long enough for seed 3's first hunger (day 5 since beds are claimed, ADR-0027).
            s.Advance(6L * s.World.Content.World.TicksPerDay);
            Assert.Contains(s.World.Characters, c => c.CastKey.StartsWith("ghost", StringComparison.Ordinal));
            return s.StateHash();
        }
        Assert.Equal(Run(), Run());
    }

    /// <summary>
    /// The build design's first story (ADR-0027): a vampire is promised a sound roof; a bedless builder uses the last
    /// of the timber on a bed; the promise breaks citing that, and the analyzer finds the chain with Work in it.
    /// </summary>
    [Fact]
    public void TheLastTimberIsCitedWhenTheRoofPromiseBreaks()
    {
        World w = WorkTests.Only(1, "you", "sera");
        Character you = Cast(w, "you"), sera = Cast(w, "sera"), ilex = Cast(w, "ilex");
        BuildTests.Works(you);
        BuildTests.Works(sera, ("repair_roof", 1), ("build", 1));
        Stock.Take(w, "timber", 99);
        Stock.Add(w, "timber", w.Content.WorkById("repair_roof").TimberCost, w.Map.Stockpile.Center);
        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: ilex.Id)!;
        IncidentSystem.Join(w, ilex, arrived.Id);
        Debt promise = w.Debts.Single(d => d.Kind == DebtKind.PromiseRest);
        BuildTests.Plan(w, "bed", new Rect(BuildPlanTests.ShelterFloor(w).X, BuildPlanTests.ShelterFloor(w).Y, 1, 1));

        // Sera has no bed, so the bed comes before the old roof: and it takes the timber the roof needed.
        BuildTests.Run(w, 3_000, () => w.Log.All.Any(e => e.Key == "event.work.used_last_materials"));
        SimEvent last = w.Log.All.Single(e => e.Key == "event.work.used_last_materials");
        Assert.Equal(new[] { sera.Id, ilex.Id }, last.Actors);
        Assert.Equal("Sera used the last of the timber on a bed.", EventText.Describe(w, last));
        Assert.Contains(ilex.Thoughts, t => t.DefId == "passed_over" && t.AboutId == sera.Id && t.CauseEvent == last.Id);
        Assert.Equal(last.Id, ilex.LastRestEvent);

        w.Tick = promise.DueTick;
        SocialSystem.Hourly(w);
        SimEvent broken = w.Log.Get(promise.ClosedEvent)!;
        Assert.Equal("event.social.promise_broken", broken.Key);
        Assert.Contains(last.Id, broken.Causes);
        Story story = StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true).Single(s => s.Chain.Contains(last.Id));
        Assert.Contains(SystemTag.Work, story.Systems);
        Assert.Contains(SystemTag.World, story.Systems); // your plan heads it
        Assert.True(story.Chain.Count >= 3);
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
