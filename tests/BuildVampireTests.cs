using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Vampires and the camp as it is built (ADR-0027 and its review): a second covered place (the hut) is open ground away
/// from the first, and by day a vampire must not cross it, to its bed, to the bushes or to a donor.
/// </summary>
public class BuildVampireTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>Seed 1 at noon on the first day, the old shelter's roof mended: the shelter and the ruins are both under cover, with open ground between.</summary>
    private static World Noon(params string[] keys)
    {
        World w = WorkTests.Only(1, keys);
        foreach (RoofSection r in w.Map.Roof) r.Sound = true;
        w.Tick = 12L * w.Content.World.TicksPerHour;
        Assert.True(Places.SunDanger(w));
        bool[] fromRuins = Places.CoveredReach(w, w.Map.CoveredRuins.Center)!;
        Assert.False(fromRuins[w.Map.Index(BuildPlanTests.ShelterFloor(w))], "the ruins and the shelter should be apart, with open ground between");
        return w;
    }

    private static void Night(World w) => w.Tick = 23L * w.Content.World.TicksPerHour;

    [Fact]
    public void ByDayAVampireGoesToItsBedOnlyIfTheWayThereIsUnderCover()
    {
        World w = Noon("ilex", "you");
        Character ilex = Cast(w, "ilex");
        Bed bed = w.Map.Beds[0];
        bed.OwnerId = ilex.Id;
        ilex.Pos = w.Map.CoveredRuins.Center;
        Point spot = Places.SleepSpot(w, ilex);
        Assert.NotEqual(bed.Pos, spot);
        Assert.True(w.Map.Covered(spot));
        Assert.True(w.Map.CoveredRuins.Contains(spot)); // it stays where it is under cover

        ilex.Pos = BuildPlanTests.ShelterFloor(w); // in the shelter, the way to the bed is under the roof
        Assert.Equal(bed.Pos, Places.SleepSpot(w, ilex));
        ilex.Pos = w.Map.CoveredRuins.Center;
        Night(w);
        Assert.Equal(bed.Pos, Places.SleepSpot(w, ilex)); // by night the yard is no danger
    }

    [Fact]
    public void ATurnedDownVampireDoesNotGoOutToTheBushesByDay()
    {
        World w = Noon("ilex", "you");
        Character ilex = Cast(w, "ilex");
        ilex.Pos = w.Map.CoveredRuins.Center;
        ilex.Food = 100;
        MoodSystem.AddThought(w, ilex, "was_refused", Cast(w, "you").Id, 0);
        Assert.True(Return.Sim.Systems.Jobs.BloodJobs.RecentlyRebuffed(w, ilex));
        Assert.NotEqual(JobKind.Forage, JobPicker.Choose(w, ilex)?.Kind);
        Night(w);
        MoodSystem.AddThought(w, ilex, "was_refused", Cast(w, "you").Id, 0); // turned down again, just now
        Assert.Equal(JobKind.Forage, JobPicker.Choose(w, ilex)?.Kind); // after dark it picks berries for the camp, as before
    }

    [Fact]
    public void ByDayAVampireAsksOnlySomeoneItCanReachUnderCover()
    {
        World w = Noon("ilex", "you", "mira");
        Character ilex = Cast(w, "ilex"), you = Cast(w, "you"), mira = Cast(w, "mira");
        ilex.Pos = w.Map.CoveredRuins.Center;
        ilex.Food = 100;
        you.Pos = BuildPlanTests.ShelterFloor(w);
        mira.Pos = new Point(you.Pos.X + 1, you.Pos.Y);
        Relations.ChangeOpinion(w, ilex, you.Id, 40, 0);
        // Both are under the shelter's roof, across open ground from the ruins: nobody to ask until dark.
        Assert.NotEqual(JobKind.RequestBlood, JobPicker.Choose(w, ilex)?.Kind);

        // Mira comes into the ruins: Ilex asks her, though it thinks better of you.
        mira.Pos = new Point(ilex.Pos.X, ilex.Pos.Y + 1);
        Assert.True(w.Map.Covered(mira.Pos));
        Job? ask = JobPicker.Choose(w, ilex);
        Assert.Equal((JobKind.RequestBlood, mira.Id), (ask?.Kind, ask?.TargetId));

        // After dark it asks whom it thinks best of, wherever they are.
        Night(w);
        ask = JobPicker.Choose(w, ilex);
        Assert.Equal((JobKind.RequestBlood, you.Id), (ask?.Kind, ask?.TargetId));
    }
}
