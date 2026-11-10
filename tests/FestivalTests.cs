using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using Return.Sim.Systems.Jobs;
using static Return.Tests.CultureHookTests;

namespace Return.Tests;

/// <summary>Seasonal festivals (S1, S2, F3; ADR-0023).</summary>
public class FestivalTests
{
    /// <summary>Mira and Oren of the Hearthfolk keep the harvest fire, on day 4 of autumn; Nia and you are there too.</summary>
    private static (World W, Character You, Character Mira, Character Oren, Character Nia, long Dusk) HarvestCamp()
    {
        World w = Camp(TestContent.Db, 1, "you", "mira", "oren", "nia");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren"), nia = Cast(w, "nia");
        Belong(mira, "hearth"); Belong(oren, "hearth"); Belong(nia, "reed");
        Hold(w, "hearth", "clasp_greeting", "harvest_fire");
        Hold(w, "reed", "no_touch_greeting");
        w.Culture.FestivalDates.Clear();
        w.Culture.FestivalDates.Add(new FestivalDate { CustomId = "harvest_fire", HolderId = "hearth", Season = 2, Day = 3 });
        WorldDef def = w.Content.World;
        long dusk = (2L * def.DaysPerSeason + 3) * def.TicksPerDay + (long)def.DuskHour * def.TicksPerHour;
        Stock.Take(w, "berries", 99);
        return (w, you, mira, oren, nia, dusk);
    }

    [Fact]
    public void FestivalsBeginOnTheirDayAndForgiveOneGrudgeEach()
    {
        (World w, Character you, Character mira, Character oren, Character nia, long dusk) = HarvestCamp();
        Stock.Add(w, "berries", 5, w.Map.Stockpile.Center);
        foreach (Character c in new[] { you, mira, oren, nia }) c.Food = 600;
        mira.Food = 200;

        w.Tick = dusk - w.Content.World.TicksPerHour;
        Festivals.Hourly(w);
        Assert.Empty(w.Culture.Festivals);

        w.Tick = dusk;
        Festivals.Hourly(w);
        FestivalRun run = Assert.Single(w.Culture.Festivals);
        SimEvent begins = w.Log.All.Single(e => e.Key == "event.culture.harvest_fire.begins");
        Assert.Equal(SystemTag.Culture, begins.System);
        Assert.Equal(new[] { mira.Id, oren.Id }, begins.Actors);
        Assert.Empty(begins.Causes);
        Assert.True(run.Feasted);
        Assert.Equal(1, Stock.Count(w, "berries")); // four eaters awake, one berry each
        Assert.Equal(200 + w.Content.ItemById("berries").Nutrition, mira.Food);
        string hearthName = w.Content.Loc.Get(Ways.CultureById(w, "hearth")!.NameKey);
        Assert.Equal("At dusk " + hearthName + " lit the harvest fire and shared 4 berries among the camp.", Return.Sim.Api.EventText.Describe(w, begins));

        // Everyone comes to the hearth; festival-goers talk more warmly.
        foreach (Character c in new[] { you, mira, oren, nia })
        {
            Job? job = JobPicker.Choose(w, c);
            Assert.Equal(JobKind.Feast, job?.Kind);
            c.Pos = job!.Target;
            c.Job = job;
            Assert.False(CultureJobs.Feast(w, c, job));
        }
        Assert.Equal(new[] { you.Id, mira.Id, oren.Id, nia.Id }.OrderBy(x => x), run.Attendees);
        Assert.Equal(100 + CultureTuning.FestivalWarmth, Greetings.Warmth(w, nia, you, 0));
        Assert.Contains("Hearth (keeping the harvest fire)", Return.Sim.Api.StateApi.Location(w, w.Map.Hearth.X, w.Map.Hearth.Y).Contents);

        // Mira holds two grudges against festival-goers: only the slighter is set down.
        SimEvent why = w.Log.Emit(w, SystemTag.Social, "event.test.why", new[] { mira.Id }, Array.Empty<long>());
        Grudge slight = Relations.FormGrudge(w, mira, oren, 20, why.Id);
        Grudge deep = Relations.FormGrudge(w, mira, nia, 50, why.Id);
        w.Tick = run.EndsTick;
        Festivals.Hourly(w);
        Assert.Empty(w.Culture.Festivals);
        Assert.False(slight.Active);
        Assert.True(deep.Active);
        Assert.Equal(50, deep.Intensity);
        SimEvent forgave = w.Log.All.Single(e => e.Key == "event.culture.harvest_fire.forgave");
        Assert.Equal(new[] { slight.FormedEvent, begins.Id }, forgave.Causes);
        Effect ended = Assert.Single(forgave.Effects);
        Assert.Equal(EffectKind.GrudgeEnded, ended.Kind);
        Assert.Equal(slight.Id, ended.RefId);
        Assert.Contains(mira.Thoughts, t => t.DefId == "grudge_set_down");
        Assert.All(new[] { you, mira, oren, nia }, c => Assert.Contains(c.Thoughts, t => t.DefId == "festival"));
        Assert.True(CultureJobs.Feast(w, you, you.Job!)); // the festival is over: the job ends
    }

    [Fact]
    public void WithNothingToSpareTheFireIsLean()
    {
        (World w, _, Character mira, _, _, long dusk) = HarvestCamp();
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center); // fewer than the two keepers
        w.Tick = dusk;
        Festivals.Hourly(w);
        Assert.Single(w.Log.All, e => e.Key == "event.culture.harvest_fire.begins_lean");
        FestivalRun run = Assert.Single(w.Culture.Festivals);
        Assert.False(run.Feasted);
        Assert.Equal(1, Stock.Count(w, "berries"));
        Festivals.Attend(w, mira);
        w.Tick = run.EndsTick;
        Festivals.Hourly(w);
        Assert.Contains(mira.Thoughts, t => t.DefId == "festival_lean");
    }

    [Fact]
    public void NoKeeperInCampNoFestival()
    {
        (World w, _, Character mira, Character oren, _, long dusk) = HarvestCamp();
        mira.Status = CharStatus.OffMap;
        oren.Status = CharStatus.Left;
        w.Tick = dusk;
        Festivals.Hourly(w);
        Assert.Empty(w.Culture.Festivals);
        Assert.DoesNotContain(w.Log.All, e => e.System == SystemTag.Culture);
    }

    [Fact]
    public void SnapshotMidFestivalRestoresExactly()
    {
        // Run seed 2 until a festival is under way, then snapshot and compare a straight run with a restored one.
        var straight = Simulation.Create(TestContent.Db, 2);
        while (straight.World.Culture.Festivals.Count == 0 || straight.World.Culture.Festivals[0].Attendees.Count == 0)
        {
            straight.Step();
            Assert.True(straight.World.Tick < straight.World.Content.World.TicksPerYear, "seed 2 holds no festival in its first year");
        }
        byte[] snap = straight.Snapshot();
        Simulation restored = Simulation.Restore(TestContent.Db, snap);
        Assert.Equal(straight.StateHash(), restored.StateHash());
        straight.Advance(4_000);
        restored.Advance(4_000);
        Assert.Equal(straight.StateHash(), restored.StateHash());
    }
}
