using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

public class TravelTests
{
    [Fact]
    public void PathGoesAroundWall()
    {
        Map m = TestMaps.FromAscii(
            ".....",
            ".###.",
            ".....");
        List<Point>? path = Pathfinder.Find(m, new Point(2, 0), new Point(2, 2));
        Assert.NotNull(path);
        Assert.Equal(new Point(2, 2), path![^1]);
        Assert.DoesNotContain(new Point(2, 1), path);
        Assert.All(path, p => Assert.True(m.Passable(p)));
        Assert.DoesNotContain(new Point(2, 0), path);
    }

    [Fact]
    public void NoCornerCutting()
    {
        Map m = TestMaps.FromAscii(
            ".#",
            "#.");
        Assert.Null(Pathfinder.Find(m, new Point(0, 0), new Point(1, 1)));

        Map half = TestMaps.FromAscii(
            "..",
            "#.");
        List<Point>? path = Pathfinder.Find(half, new Point(0, 0), new Point(1, 1));
        Assert.Equal(new[] { new Point(1, 0), new Point(1, 1) }, path);
    }

    [Fact]
    public void UnreachableReturnsNull()
    {
        Map m = TestMaps.FromAscii(
            "..~",
            "..~");
        Assert.Null(Pathfinder.Find(m, new Point(0, 0), new Point(2, 1)));
    }

    [Fact]
    public void SameTileIsAnEmptyPath()
    {
        Map m = TestMaps.FromAscii("...");
        Assert.Empty(Pathfinder.Find(m, new Point(1, 0), new Point(1, 0))!);
    }

    [Fact]
    public void PathIsDeterministic()
    {
        World w = WorldGen.Create(TestContent.Db, 6);
        Point from = w.Members().First().Pos, to = w.Map.Bushes[0].Pos;
        List<Point> first = Pathfinder.Find(w.Map, from, to)!;
        for (int i = 0; i < 100; i++) Assert.Equal(first, Pathfinder.Find(w.Map, from, to));
    }

    [Fact]
    public void MovementTakesTicksPerTile()
    {
        (World w, Character c) = Walker(".......", new Point(0, 0));
        Assert.True(TravelSystem.Start(w, c, new Point(5, 0)));
        for (int t = 0; t < 29; t++) TravelSystem.Step(w);
        Assert.Equal(new Point(4, 0), c.Pos);
        TravelSystem.Step(w);
        Assert.Equal(new Point(5, 0), c.Pos);
        Assert.Empty(c.Path);
    }

    [Fact]
    public void DiagonalsAndBushesAreSlower()
    {
        (World w, Character c) = Walker("..\n..", new Point(0, 0));
        Assert.Equal(6, TravelSystem.TicksFor(w, c, new Point(0, 0), new Point(1, 0)));
        Assert.Equal(8, TravelSystem.TicksFor(w, c, new Point(0, 0), new Point(1, 1)));
        (World wb, Character cb) = Walker(".b", new Point(0, 0));
        Assert.Equal(12, TravelSystem.TicksFor(wb, cb, new Point(0, 0), new Point(1, 0)));
    }

    [Fact]
    public void FacingFollowsDirection()
    {
        (World w, Character c) = Walker(".....\n.....\n.....", new Point(0, 2));
        TravelSystem.Start(w, c, new Point(3, 2));
        TravelSystem.Step(w);
        Assert.Equal(Facing.E, c.Facing);
        for (int t = 0; t < 30; t++) TravelSystem.Step(w);
        TravelSystem.Start(w, c, new Point(3, 0));
        TravelSystem.Step(w);
        Assert.Equal(Facing.N, c.Facing);
    }

    [Fact]
    public void BlockedStepClearsThePath()
    {
        (World w, Character c) = Walker(".....", new Point(0, 0));
        TravelSystem.Start(w, c, new Point(4, 0));
        w.Map.Set(new Point(1, 0), TerrainKind.Wall);
        w.Map.RebuildCaches();
        for (int t = 0; t < 10; t++) TravelSystem.Step(w);
        Assert.Equal(new Point(0, 0), c.Pos);
        Assert.Empty(c.Path);
    }

    /// <summary>A world whose map is replaced by an ASCII map, with only You active.</summary>
    internal static (World, Character) Walker(string ascii, Point start)
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        w.Map = TestMaps.FromAscii(ascii.Split('\n'));
        foreach (Character other in w.Characters) other.Status = CharStatus.OffMap;
        Character c = w.Characters.First(x => x.Otherworlder);
        c.Status = CharStatus.Member;
        c.Pos = start;
        return (w, c);
    }
}

internal static class TestMaps
{
    /// <summary>'.' grass, ',' soil, '#' wall, '~' water, '^' rock, 'T' tree, 'b' bush, '_' floor, '+' door.</summary>
    public static Map FromAscii(params string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        var m = new Map { Width = w, Height = h, Terrain = new byte[w * h], Hearth = Point.None, WatchPost = Point.None };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Point(x, y);
                switch (rows[y][x])
                {
                    case ',': m.Set(p, TerrainKind.Soil); break;
                    case '#': m.Set(p, TerrainKind.Wall); break;
                    case '~': m.Set(p, TerrainKind.Water); break;
                    case '^': m.Set(p, TerrainKind.Rock); break;
                    case '_': m.Set(p, TerrainKind.Floor); break;
                    case '+': m.Set(p, TerrainKind.Door); break;
                    case 'T': m.Trees.Add(new Tree { Id = 1000 + m.Trees.Count, Pos = p }); break;
                    case 'b': m.Bushes.Add(new Bush { Id = 2000 + m.Bushes.Count, Pos = p, Berries = 4 }); break;
                }
            }
        m.RebuildCaches();
        return m;
    }
}
