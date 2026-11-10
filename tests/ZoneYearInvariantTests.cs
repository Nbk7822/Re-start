using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// A year with the stockpile moved into the ruins on the first tick and back out by the hearth in
/// autumn (ADR-0028): every hour the zones stay canonical and apart, every item has a stockpile, stores
/// lie only where they can be reached, nobody stands where they cannot, and every cause names an earlier event.
/// A return sends the world back and hands the simulation the restored world (ADR-0030), so, as in
/// YearInvariantTests, the year runs to its last tick and the world is read afresh every hour; one run
/// has you die in summer (forced, as the soak's --fates does) to be sure the checks follow the return.
/// </summary>
public class ZoneYearInvariantTests
{
    [Theory]
    [InlineData(1UL, false)]
    [InlineData(2UL, false)]
    [InlineData(1UL, true)]
    public void AYearOfMovedStockpilesKeepsTheStoresWhole(ulong seed, bool dieInSummer)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        World w = s.World;
        int first = w.Map.Zones.First(z => z.Kind == "stockpile").Id;
        s.Enqueue(new MarkZone(0, 0, "stockpile", Zones.FromRect(w.Map.CoveredRuins)));
        s.Enqueue(new RemoveZone(0, first));
        long autumn = 2L * w.Content.World.TicksPerSeason;
        s.Enqueue(new MarkZone(autumn, 0, "stockpile", Zones.FromRect(w.Map.StartStockpile), new[] { "berries" }));
        int perHour = w.Content.World.TicksPerHour, checkedEvents = 0;
        long year = w.Content.World.TicksPerYear, dieAt = dieInSummer ? w.Content.World.TicksPerSeason + w.Content.World.TicksPerSeason / 2 : -1;
        bool died = false;
        for (long hours = 0; s.World.Tick < year && !s.World.Fate.Ended; hours++)
        {
            Assert.True(hours < 4 * year / perHour, $"seed {seed}: the year never ended");
            if (!died && dieAt >= 0 && s.World.Fate.Timeline == 0 && s.World.Tick >= dieAt) died = s.ForceDeath();
            s.Advance(Math.Min(perHour, year - s.World.Tick));
            if (s.World != w) checkedEvents = 0; // a return: the world gone back to has its own log, read from the start
            w = s.World;
            string when = $"seed {seed}, tick {w.Tick}";
            var used = new Dictionary<Point, int>();
            foreach (Zone z in w.Map.Zones)
            {
                Assert.Equal(Zones.Canonical(z.Tiles), z.Tiles);
                Assert.NotEmpty(z.Tiles);
                foreach (Point p in Zones.Each(z.Tiles))
                {
                    Assert.True(used.TryAdd(p, z.Id), $"{when}: tile {p} is in zones {used.GetValueOrDefault(p)} and {z.Id}");
                    Assert.Null(Zones.Check(w, w.Content.ZoneById(z.Kind), p));
                }
                if (Zones.IsStores(w, z)) Assert.NotEmpty(z.Items);
            }
            Assert.True(Zones.EveryItemKept(w, w.Map.Zones), $"{when}: an item has no stockpile");
            foreach (ItemStack st in w.Items)
            {
                Assert.True(st.Count > 0, $"{when}: stack {st.Id} has count {st.Count}");
                Assert.True(w.Map.Passable(st.Pos), $"{when}: stack {st.Id} lies on impassable {st.Pos}");
                if (!st.Stored) Assert.False(Zones.Accepts(w, st.Pos, st.DefId), $"{when}: loose {st.DefId} on a stockpile that keeps it");
            }
            foreach (Character c in w.Characters)
            {
                if (c.Status is not (CharStatus.Member or CharStatus.Hostile)) continue;
                Assert.True(w.Map.Passable(c.Pos), $"{when}: {c.CastKey} on impassable {c.Pos}");
                if (c.Carrying is { Stored: true }) Assert.True(c.Job?.Kind is JobKind.Haul or JobKind.Deliver or JobKind.Build, $"{when}: {c.CastKey} carries stores at {c.Job?.Kind}"); // stores in hand: hauled, or taken for a plan
            }
            for (; checkedEvents < w.Log.All.Count; checkedEvents++)
            {
                SimEvent e = w.Log.All[checkedEvents];
                foreach (long cause in e.Causes) Assert.True(cause >= 1 && cause < e.Id, $"event {e.Id} ({e.Key}) cites cause {cause}");
            }
        }
        w = s.World;
        Assert.True(w.Tick == year || w.Fate.Ended, $"seed {seed}: stopped at tick {w.Tick}");
        Assert.Contains(w.Log.All, e => e.Key == "event.world.zone_marked");
        Assert.True(w.Map.Zones.Count(z => z.Kind == "stockpile") == 2);
        if (dieInSummer) Assert.True(w.Fate.Returns > 0, $"seed {seed}: you never died and came back");
    }
}
