using Return.Runner;
using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// A year under each of the stand-in's home plans: every cause names an earlier event, the home area is marked, and with
/// the post outside it ("short") the watch's skip says why the post stands empty. The curfew proper, and design 9.2's year
/// invariant that nobody it keeps in starts anything outside after dark, moved to slice 2 with keep-out (ADR-0028).
/// </summary>
public class HomeYearInvariantTests
{
    [Theory]
    [InlineData("home", 1UL)]
    [InlineData("short", 2UL)]
    [InlineData("both", 3UL)]
    public void AYearUnderAHomePlanKeepsItsCausesAndTheShortOneSaysWhyThePostIsEmpty(string plan, ulong seed)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        Autoplay.Begin(s, plan);
        long year = s.World.Content.World.TicksPerYear;
        int checkedEvents = 0;
        // A return hands the simulation the restored world (ADR-0030), so the world is read afresh after every step.
        for (long steps = 0; s.World.Tick < year && !s.World.Fate.Ended; steps++)
        {
            Assert.True(steps < 4 * year, $"seed {seed} {plan}: the year never ended");
            s.Step();
            World w = s.World;
            checkedEvents = Math.Min(checkedEvents, w.Log.All.Count);
            for (; checkedEvents < w.Log.All.Count; checkedEvents++)
            {
                SimEvent e = w.Log.All[checkedEvents];
                foreach (long cause in e.Causes) Assert.True(cause >= 1 && cause < e.Id, $"event {e.Id} ({e.Key}) cites cause {cause}");
            }
        }
        World end = s.World;
        Assert.Contains(end.Log.All, e => e.Key == "event.world.home_marked");
        if (plan == "short") Assert.Contains(end.Log.All, e => e.Key == "event.work.stayed_in_watch");
    }
}

/// <summary>The home area under the determinism rules (zones design 9.2): same seed and the same layout, same world; a snapshot mid-night restores to the same future; it stays under the tick budget.</summary>
public class HomeDeterminismTests
{
    private static Simulation Planned(ulong seed, string plan)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        Autoplay.Begin(s, plan);
        return s;
    }

    [Fact]
    public void SameSeedAndHomeAreaSameWorldAndADifferentLayoutADifferentOne()
    {
        long ticks = 6L * TestContent.Db.World.TicksPerDay;
        Simulation a = Planned(5, "short"), b = Planned(5, "short"), c = Planned(5, "home");
        a.Advance(ticks);
        b.Advance(ticks);
        c.Advance(ticks);
        Assert.Equal(a.StateHash(), b.StateHash());
        Assert.NotEqual(a.StateHash(), c.StateHash());
    }

    /// <summary>A snapshot taken at 20:00 on a night the home area left the post empty (tonight's roll made, its line said) restores to the same future.</summary>
    [Fact]
    public void ASnapshotOnAKeptInNightRestoresToTheSameFuture()
    {
        Simulation straight = Planned(2, "short");
        World w = straight.World;
        long hour = w.Content.World.TicksPerHour, day = w.Content.World.TicksPerDay;
        long snapAt = -1;
        while (snapAt < 0 && w.Tick < 40 * day)
        {
            straight.Step();
            if (w.HomeWatchNight >= 0 && w.Tick % day == 20 * hour && w.HomeWatchNight == Zones.Tonight(w)) snapAt = w.Tick;
        }
        Assert.True(snapAt > 0, "no night kept in by 40 days");
        Assert.Contains(w.Characters, c => c.NightRollDay == Zones.Tonight(w));
        byte[] snap = straight.Snapshot();
        straight.Advance(day);
        Simulation restored = Simulation.Restore(TestContent.Db, snap);
        Assert.Equal(snapAt, restored.World.Tick);
        restored.Advance(day);
        Assert.Equal(straight.StateHash(), restored.StateHash());
    }

    [Fact]
    public void APendingHomeAreaSurvivesASnapshot()
    {
        var straight = Simulation.Create(TestContent.Db, 9);
        Rect clearing = WorldGen.ClearingOf(straight.World.Map.Shelter);
        straight.Enqueue(new MarkZone(9_000, 0, "home", Zones.FromRect(clearing)));
        straight.Advance(12_000);

        var s = Simulation.Create(TestContent.Db, 9);
        s.Enqueue(new MarkZone(9_000, 0, "home", Zones.FromRect(clearing)));
        s.Advance(5_000);
        Simulation r = Simulation.Restore(TestContent.Db, s.Snapshot());
        Assert.Single(r.World.PendingCommands);
        r.Advance(7_000);
        Assert.NotNull(Zones.HomeOf(r.World));
        Assert.Equal(straight.StateHash(), r.StateHash());
    }

    [Fact]
    public void TheTickStaysUnderBudgetWithAHomeArea()
    {
        Simulation s = Planned(3, "short");
        s.Advance(7_200);
        int characters = s.World.Characters.Count;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        s.Advance(7_200);
        sw.Stop();
        double nsPerCharTick = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / (7_200.0 * characters);
        Assert.True(nsPerCharTick <= 2_000, $"tick cost {nsPerCharTick:F0} ns per character per tick exceeds the 2,000 ns budget (ADR-0005)");
    }
}

