using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// A year with the stockpile moved into the ruins on the first tick and back out by the hearth in
/// autumn (ADR-0028): every hour the zones stay canonical and apart, every item has a stockpile, stores
/// lie only where they can be reached, nobody stands where they cannot, and every cause names an earlier event.
/// </summary>
public class ZoneYearInvariantTests
{
    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    public void AYearOfMovedStockpilesKeepsTheStoresWhole(ulong seed)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        World w = s.World;
        int first = w.Map.Zones.First(z => z.Kind == "stockpile").Id;
        s.Enqueue(new MarkZone(0, 0, "stockpile", Zones.FromRect(w.Map.CoveredRuins)));
        s.Enqueue(new RemoveZone(0, first));
        long autumn = 2L * w.Content.World.TicksPerSeason;
        s.Enqueue(new MarkZone(autumn, 0, "stockpile", Zones.FromRect(w.Map.StartStockpile), new[] { "berries" }));
        int perHour = w.Content.World.TicksPerHour, checkedEvents = 0;
        for (long hour = 0; hour < w.Content.World.TicksPerYear / perHour; hour++)
        {
            s.Advance(perHour);
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
                if (c.Carrying is { Stored: true }) Assert.Equal(JobKind.Haul, c.Job?.Kind);
            }
            for (; checkedEvents < w.Log.All.Count; checkedEvents++)
            {
                SimEvent e = w.Log.All[checkedEvents];
                foreach (long cause in e.Causes) Assert.True(cause >= 1 && cause < e.Id, $"event {e.Id} ({e.Key}) cites cause {cause}");
            }
        }
        Assert.Contains(w.Log.All, e => e.Key == "event.world.zone_marked");
        Assert.True(w.Map.Zones.Count(z => z.Kind == "stockpile") == 2);
    }
}
