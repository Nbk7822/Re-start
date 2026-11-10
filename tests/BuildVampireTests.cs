using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Vampires and the camp as it is built (ADR-0027): a second covered place is open ground away from the first, and by
/// day a vampire must not walk out into it on the way to its bed, nor go out to the bushes after a refusal. These are
/// the branch's two vampire fixes, which had no tests until its review.
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
        Assert.False(UnderCoverAllTheWay(w, w.Map.CoveredRuins.Center, BuildPlanTests.ShelterFloor(w)), "the ruins and the shelter should be apart, with open ground between");
        return w;
    }

    private static void Night(World w) => w.Tick = 23L * w.Content.World.TicksPerHour;

    /// <summary>A walk from one tile to another over covered, passable tiles only (8 ways).</summary>
    private static bool UnderCoverAllTheWay(World w, Point from, Point to)
    {
        var seen = new HashSet<Point> { from };
        var queue = new Queue<Point>(new[] { from });
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            if (p == to) return true;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var q = new Point(p.X + dx, p.Y + dy);
                    if (w.Map.Passable(q) && w.Map.Covered(q) && seen.Add(q)) queue.Enqueue(q);
                }
        }
        return false;
    }

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
}
