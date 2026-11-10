using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>The meeting place (zones step 2, ADR-0028): where the idle gather, who they gather with, and who keeps away.</summary>
public class MeetingPlaceTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    private static Zone Meeting(World w) => w.Map.Zones.First(z => z.Kind == "meeting_place");

    /// <summary>Nothing to do: fed, rested, good company, no work wanted, by day.</summary>
    private static World Idle(ulong seed, params string[] keys)
    {
        World w = WorkTests.Only(seed, keys);
        w.Tick = 10L * w.Content.World.TicksPerHour;
        foreach (Character c in w.Members())
            foreach (WorkPriority p in c.Priorities) p.Priority = 0;
        return w;
    }

    [Fact]
    public void TheCampStartsWithAMeetingPlaceOnTheOpenGroundRoundTheHearth()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            World w = WorldGen.Create(TestContent.Db, seed);
            Zone m = Meeting(w);
            Assert.Equal(24, Zones.Count(m.Tiles));
            Assert.All(Zones.Each(m.Tiles), p => Assert.InRange(p.ChebyshevTo(w.Map.Hearth), 1, 2));
            Assert.Equal(0, m.MadeEvent);
            Assert.True(Zones.ByTheFire(w, m));
        }
    }

    [Fact]
    public void TheIdleGoToTheMeetingPlace()
    {
        World w = Idle(1, "mira", "oren");
        Character mira = Cast(w, "mira");
        mira.Pos = new Point(w.Map.Hearth.X - 6, w.Map.Hearth.Y + 1);
        Job? job = JobPicker.Choose(w, mira);
        Assert.Equal(JobKind.Wander, job?.Kind);
        Assert.Equal(Meeting(w).Id, Zones.UseZoneAt(w, job!.Target)?.Id);
        Assert.Contains("Meeting place", StateApi.Location(w, job.Target.X, job.Target.Y).Contents);
    }

    [Fact]
    public void TheyGatherWhereThePeopleTheyLikeAreSitting()
    {
        World w = Idle(2, "mira", "oren", "bram");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren"), bram = Cast(w, "bram");
        Rect ruins = w.Map.CoveredRuins;
        new MarkZone(w.Tick, 0, "meeting_place", Zones.FromRect(ruins)).Apply(w);
        Zone second = w.Map.Zones.Last();
        Relations.ChangeOpinion(w, mira, oren.Id, 50, 0);
        Relations.ChangeOpinion(w, mira, bram.Id, -10, 0);
        oren.Pos = ruins.Center;
        bram.Pos = Zones.Each(Meeting(w).Tiles).First();
        Assert.Equal(second.Id, Zones.MeetingPlaceFor(w, mira).Place?.Id);
        oren.Pos = Zones.Each(Meeting(w).Tiles).Last();
        bram.Pos = ruins.Center;
        Assert.Equal(Meeting(w).Id, Zones.MeetingPlaceFor(w, mira).Place?.Id);
        oren.Asleep = true; // the sleeping keep nobody company
        Assert.Equal(Meeting(w).Id, Zones.MeetingPlaceFor(w, mira).Place?.Id); // 0 against -10
    }

    [Fact]
    public void SomeoneWithAGrudgeKeepsAwayFromTheFireWhileItsTargetSitsThere()
    {
        World w = Idle(3, "sera", "oren");
        Character sera = Cast(w, "sera"), oren = Cast(w, "oren");
        SimEvent wrong = w.Log.Emit(w, SystemTag.Social, "event.social.insulted", new[] { oren.Id, sera.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", oren.Id), EventArg.Char("b", sera.Id));
        Grudge g = Relations.FormGrudge(w, sera, oren, 40, wrong.Id);
        oren.Pos = Zones.Each(Meeting(w).Tiles).First();
        sera.Pos = new Point(w.Map.Hearth.X - 6, w.Map.Hearth.Y + 1);

        Job? job = JobPicker.Choose(w, sera);
        Assert.Equal(JobKind.Wander, job?.Kind);
        Assert.NotEqual(Meeting(w).Id, Zones.UseZoneAt(w, job!.Target)?.Id ?? 0);
        SimEvent kept = Assert.Single(w.Log.All, e => e.Key.StartsWith("event.mood.kept_away", StringComparison.Ordinal));
        Assert.Equal("event.mood.kept_away_fire", kept.Key);
        Assert.Equal(SystemTag.Mood, kept.System);
        Assert.Equal(new[] { g.FormedEvent }, kept.Causes);
        Assert.Equal("Sera kept away from the fire while Oren sat there.", EventText.Describe(w, kept));
        Assert.Contains(sera.Thoughts, t => t.DefId == "sat_apart" && t.CauseEvent == kept.Id && t.AboutId == oren.Id);

        JobPicker.Choose(w, sera); // the same day: no second line
        Assert.Single(w.Log.All, e => e.Key.StartsWith("event.mood.kept_away", StringComparison.Ordinal));
        w.Tick += w.Content.World.TicksPerDay;
        JobPicker.Choose(w, sera);
        Assert.Equal(2, w.Log.All.Count(e => e.Key.StartsWith("event.mood.kept_away", StringComparison.Ordinal)));

        oren.Pos = new Point(w.Map.Hearth.X + 8, w.Map.Hearth.Y + 3); // he leaves the fire; she goes
        Assert.Equal(Meeting(w).Id, Zones.MeetingPlaceFor(w, sera).Place?.Id);
    }

    [Fact]
    public void AWandererIsReceivedAtTheMeetingPlace()
    {
        World w = WorkTests.Only(1, "you");
        Character wren = Cast(w, "wren");
        IncidentSystem.Fire(w, "wanderer_arrives", targetId: wren.Id);
        Assert.Equal(JobKind.Arrive, wren.Job?.Kind);
        Assert.Equal(Meeting(w).Id, Zones.UseZoneAt(w, wren.Job!.Target)?.Id);
    }

    [Fact]
    public void WithNoMeetingPlaceTheIdleWanderAsTheyAlwaysDid()
    {
        World w = Idle(1, "mira");
        new RemoveZone(w.Tick, Meeting(w).Id).Apply(w);
        Assert.DoesNotContain(w.Map.Zones, z => z.Kind == "meeting_place");
        Assert.Equal("You took away the meeting place by the hearth.", EventText.Describe(w, w.Log.All[^1]));
        Character mira = Cast(w, "mira");
        Point from = mira.Pos;
        Job? job = JobPicker.Choose(w, mira);
        Assert.Equal(JobKind.Wander, job?.Kind);
        Assert.InRange(job!.Target.ChebyshevTo(from), 0, 4);
    }
}
