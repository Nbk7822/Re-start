using System.Diagnostics;
using Return.Sim;
using Return.Sim.Events;
using Return.Sim.Modding;

namespace Return.Tests;

public class DeterminismTests
{
    private static int MiraId(Simulation s) => s.World.Characters.First(c => c.CastKey == "mira").Id;

    private static Simulation Scripted(ulong seed, RationPolicy policy)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        s.Enqueue(new SetRationPolicy(1_000, policy));
        s.Enqueue(new SetWorkPriority(2_000, MiraId(s), "forage", 1));
        return s;
    }

    [Fact]
    public void SameSeedAndCommandsSameHash()
    {
        Simulation a = Scripted(42, RationPolicy.PromisesFirst), b = Scripted(42, RationPolicy.PromisesFirst);
        a.Advance(20_000);
        b.Advance(20_000);
        Assert.Equal(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void DifferentCommandsDifferentHash()
    {
        Simulation a = Scripted(42, RationPolicy.PromisesFirst), b = Scripted(42, RationPolicy.SelfFirst);
        a.Advance(20_000);
        b.Advance(20_000);
        Assert.NotEqual(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void HookSeesEvents()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        var hook = new RecordingHook();
        s.AddHook(hook);
        s.Enqueue(new SetRationPolicy(10, RationPolicy.SelfFirst));
        s.Advance(20);
        Assert.Equal(1, hook.Created);
        Assert.Equal(s.World.Log.All.Select(e => e.Id), hook.Seen.Select(e => e.Id));
        Assert.Contains(hook.Seen, e => e.Key == "event.world.ration_policy");
    }

    [Fact]
    public void EmitRejectsForwardCause()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        long next = s.World.Log.NextId;
        Assert.Throws<InvalidOperationException>(() =>
            s.World.Log.Emit(s.World, SystemTag.World, "event.world.ration_policy", Array.Empty<int>(), new[] { next }));
    }

    private sealed class RecordingHook : IModHook
    {
        public int Created;
        public readonly List<SimEvent> Seen = new();
        public void OnWorldCreated(World w) => Created++;
        public void OnEvent(World w, SimEvent e) => Seen.Add(e);
    }
}

public class SnapshotTests
{
    [Fact]
    public void SnapshotRestoreContinueMatchesStraightRun()
    {
        var a = Simulation.Create(TestContent.Db, 7);
        a.Advance(20_000);

        var b = Simulation.Create(TestContent.Db, 7);
        b.Advance(7_000);
        Simulation c = Simulation.Restore(TestContent.Db, b.Snapshot());
        c.Advance(13_000);

        Assert.Equal(a.StateHash(), c.StateHash());
    }

    [Fact]
    public void PendingCommandsSurviveSnapshot()
    {
        // To ticks, not step counts: in seed 9 you die early and go back (ADR-0030), so ten thousand steps end before tick 9,000.
        var straight = Simulation.Create(TestContent.Db, 9);
        straight.Enqueue(new SetRationPolicy(9_000, RationPolicy.SelfFirst));
        straight.AdvanceTo(10_000);

        var s = Simulation.Create(TestContent.Db, 9);
        s.Enqueue(new SetRationPolicy(9_000, RationPolicy.SelfFirst));
        s.AdvanceTo(5_000);
        Simulation r = Simulation.Restore(TestContent.Db, s.Snapshot());
        Assert.Single(r.World.PendingCommands);
        r.AdvanceTo(10_000);

        Assert.Equal(RationPolicy.SelfFirst, r.World.Policy);
        Assert.Single(r.World.CommandLog);
        Assert.Empty(r.World.PendingCommands);
        Assert.Equal(straight.StateHash(), r.StateHash());
    }

    /// <summary>Review Focus 2: snapshots taken mid-walk, mid-job, mid-fight restore to exactly the same future.</summary>
    [Fact]
    public void MidActivitySnapshotsRestoreExactly()
    {
        const int every = 1_777, after = 3_000;
        var straight = Simulation.Create(TestContent.Db, 5);
        long end = 3L * straight.World.Content.World.TicksPerDay;
        var snaps = new List<(long Tick, byte[] Snap)>();
        var expected = new Dictionary<long, string>();
        while (straight.World.Tick <= end + after)
        {
            long t = straight.World.Tick;
            if (t > 0 && t % every == 0 && t <= end) snaps.Add((t, straight.Snapshot()));
            if (t > after && (t - after) % every == 0 && t - after <= end) expected[t] = straight.StateHash();
            straight.Step();
        }
        Assert.Equal(12, snaps.Count);
        foreach ((long tick, byte[] snap) in snaps)
        {
            Simulation r = Simulation.Restore(TestContent.Db, snap);
            Assert.Equal(tick, r.World.Tick);
            r.Advance(after);
            Assert.True(expected[tick + after] == r.StateHash(), $"restore from tick {tick} diverged by tick {tick + after}");
        }
    }

    [Fact]
    public void RestoreUnderOneSecondAt200Characters()
    {
        var s = Simulation.Create(TestContent.Db, 4);
        WorldGen.AddSyntheticCharacters(s.World, 200);
        Assert.Equal(200, s.World.Characters.Count);
        s.Advance(2 * 7_200);
        byte[] snap = s.Snapshot();

        var times = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var sw = Stopwatch.StartNew();
            Simulation r = Simulation.Restore(TestContent.Db, snap);
            sw.Stop();
            Assert.Equal(200, r.World.Characters.Count);
            times.Add(sw.ElapsedMilliseconds);
        }
        times.Sort();
        Assert.True(times[2] < 1000, $"median restore took {times[2]} ms for a {snap.Length / 1024} KiB snapshot");
    }
}

public class PerformanceTests
{
    [Fact]
    public void TickCostUnderBudget()
    {
        var s = Simulation.Create(TestContent.Db, 3);
        s.Advance(7_200);
        int characters = s.World.Characters.Count;
        var sw = Stopwatch.StartNew();
        s.Advance(7_200);
        sw.Stop();
        double nsPerCharTick = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / (7_200.0 * characters);
        Assert.True(nsPerCharTick <= 2_000, $"tick cost {nsPerCharTick:F0} ns per character per tick exceeds the 2,000 ns budget (ADR-0005)");
    }
}
