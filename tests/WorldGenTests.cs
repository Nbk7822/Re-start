using System.Text.Json;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Tests;

public class CalendarTests
{
    private static readonly WorldDef W = TestContent.Db.World;

    [Fact]
    public void CalendarMath()
    {
        Assert.Equal(0, Calendar.Day(0, W));
        Assert.Equal(0, Calendar.Hour(0, W));
        Assert.Equal(1, Calendar.Day(7_200 + 300, W));
        Assert.Equal(1, Calendar.Hour(7_200 + 300, W));
        Assert.Equal(1, Calendar.Year(288_000, W));
        Assert.Equal(0, Calendar.Year(287_999, W));
        Assert.Equal(1, Calendar.Season(72_000, W));
        Assert.False(Calendar.IsDaylight(5 * 300, W));
        Assert.True(Calendar.IsDaylight(6 * 300, W));
        Assert.False(Calendar.IsDaylight(19 * 300, W));
    }
}

public class WorldGenTests
{
    private static ContentDb Db => TestContent.Db;

    [Fact]
    public void SameSeedSameWorld()
    {
        Assert.Equal(JsonSerializer.Serialize(WorldGen.Create(Db, 11)), JsonSerializer.Serialize(WorldGen.Create(Db, 11)));
    }

    [Fact]
    public void DifferentSeedDifferentMap()
    {
        Assert.NotEqual(WorldGen.Create(Db, 11).Map.Terrain, WorldGen.Create(Db, 12).Map.Terrain);
    }

    [Fact]
    public void CastShape()
    {
        World w = WorldGen.Create(Db, 11);
        Assert.Equal(10, w.Characters.Count);
        Assert.Equal(new[] { "mira", "oren", "you" }, w.Members().Select(c => c.CastKey).OrderBy(k => k, StringComparer.Ordinal));
        foreach (Character c in w.Characters)
        {
            Assert.Equal(2, c.Traits.Count);
            Assert.NotEqual(c.Traits[0], c.Traits[1]);
        }
        Assert.Equal(7, w.Arrivals.Count);
        Assert.All(w.Arrivals, a => Assert.InRange(Calendar.Day(a.Tick, w.Content.World), 1, 12));
    }

    [Fact]
    public void NaturesAreLazyAndOrderIndependent()
    {
        var before = Db.Cast.Reverse().ToDictionary(c => c.Key, c => WorldGen.NatureOf(Db, 11, c.Key).SecondaryTrait);
        World w = WorldGen.Create(Db, 11);
        foreach (Character c in w.Characters)
        {
            Assert.Equal(before[c.CastKey], c.Traits[1]);
            Assert.Equal(WorldGen.NatureOf(Db, 11, c.CastKey).SecondaryTrait, c.Traits[1]);
        }
    }

    [Fact]
    public void VampiresArriveAtNight()
    {
        World w = WorldGen.Create(Db, 11);
        foreach (Arrival a in w.Arrivals)
            if (w.Get(a.CharacterId).SpeciesId == "vampire")
                Assert.False(Calendar.IsDaylight(a.Tick, w.Content.World));
    }

    [Fact]
    public void CampIsUsable()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            World w = WorldGen.Create(Db, seed);
            Map m = w.Map;
            Assert.Equal(48, m.Width);
            Assert.Equal(32, m.Height);
            Assert.All(w.Members(), c => Assert.True(m.Passable(c.Pos), $"seed {seed}: {c.CastKey} starts on a blocked tile {c.Pos}"));
            bool[] reach = Flood(m, w.Get(1).Pos);
            Assert.True(reach[Idx(m, m.Stockpile.Center)], $"seed {seed}: stockpile unreachable");
            Assert.True(reach[Idx(m, m.Hearth with { Y = m.Hearth.Y + 1 })], $"seed {seed}: hearth unreachable");
            foreach (RoofSection s in m.Roof)
                Assert.True(reach[Idx(m, s.Area.Center)], $"seed {seed}: roof section {s.Index} interior unreachable");
            Assert.Equal(3, m.Roof.Count);
            Assert.All(m.Roof, s => Assert.False(s.Sound));
            Assert.True(m.Covered(m.CoveredRuins.Center), $"seed {seed}: ruins should be covered");
            Assert.True(m.Trees.Count >= 20, $"seed {seed}: only {m.Trees.Count} trees");
            Assert.True(m.Bushes.Count >= 6, $"seed {seed}: only {m.Bushes.Count} bushes");
            int berries = w.Items.Where(i => i.DefId == "berries").Sum(i => i.Count);
            Assert.Equal(Db.World.StartBerries, berries);
        }
    }

    private static int Idx(Map m, Point p) => p.Y * m.Width + p.X;

    private static bool[] Flood(Map m, Point start)
    {
        var seen = new bool[m.Width * m.Height];
        var queue = new Queue<Point>();
        queue.Enqueue(start);
        seen[Idx(m, start)] = true;
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var n = new Point(p.X + dx, p.Y + dy);
                if (!m.InBounds(n) || seen[Idx(m, n)] || !m.Passable(n)) continue;
                seen[Idx(m, n)] = true;
                queue.Enqueue(n);
            }
        }
        return seen;
    }
}

internal static class TestContent
{
    private static readonly Lazy<ContentDb> Lazy = new(() => ContentDb.Load(Path.Combine(TestPaths.Root, "content")));
    public static ContentDb Db => Lazy.Value;
}
