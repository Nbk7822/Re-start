using System.Text.Json.Serialization;
using Return.Sim.Core;

namespace Return.Sim;

public enum TerrainKind : byte
{
    Grass = 0,
    Soil = 1,
    Water = 2,
    Rock = 3,
    Floor = 4,
    Wall = 5,
    Door = 6,
}

public sealed class Tree
{
    public int Id { get; set; }
    public Point Pos { get; set; }
    public bool Felled { get; set; }
}

public sealed class Bush
{
    public int Id { get; set; }
    public Point Pos { get; set; }
    public int Berries { get; set; }
    public long RegrowTick { get; set; }
}

public sealed class RoofSection
{
    public int Index { get; set; }
    public Rect Area { get; set; }
    public bool Sound { get; set; }
    public int Progress { get; set; }
}

public sealed class Bed
{
    public int Id { get; set; }
    public Point Pos { get; set; }
    public int OwnerId { get; set; }
}

/// <summary>
/// The tile grid and fixed camp features. Passability is cached in arrays that are rebuilt after
/// generation, after load, and whenever a blocking feature changes (e.g. a tree is felled).
/// </summary>
public sealed class Map : IJsonOnDeserialized
{
    private byte[] _blocked = Array.Empty<byte>();
    private byte[] _bush = Array.Empty<byte>();

    /// <summary>The use-layer zone id on each tile (0 for none), rebuilt by Systems.Zones when <see cref="ZonesDirty"/>.</summary>
    internal int[] UseZone = Array.Empty<int>();

    /// <summary>1 on each tile of the home area (zones step 3), rebuilt with <see cref="UseZone"/>.</summary>
    internal byte[] HomeAt = Array.Empty<byte>();

    /// <summary>Set whenever the zones or the caches change, so the zone cache is rebuilt before its next read.</summary>
    internal bool ZonesDirty = true;

    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Terrain { get; set; } = Array.Empty<byte>();
    public List<Tree> Trees { get; set; } = new();
    public List<Bush> Bushes { get; set; } = new();
    public List<RoofSection> Roof { get; set; } = new();
    public List<Bed> Beds { get; set; } = new();
    public Rect Shelter { get; set; }
    public Rect CoveredRuins { get; set; }

    /// <summary>
    /// The rectangle WorldGen laid the first stockpile out on. Only a landmark (the generator's reserved
    /// ground, worn ground, tests); what the camp keeps where is <see cref="Zones"/> (ADR-0028).
    /// </summary>
    public Rect StartStockpile { get; set; }
    public Point Hearth { get; set; }
    public Point WatchPost { get; set; }

    /// <summary>Painted areas (stockpiles, meeting places, the home area), in id order (ADR-0028). Change them only through Systems.Zones.</summary>
    public List<Zone> Zones { get; set; } = new();

    /// <summary>Bumped by every change the view may need to redraw for (a zone marked, moved or removed).</summary>
    public long Revision { get; set; }

    public bool InBounds(Point p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

    public int Index(Point p) => p.Y * Width + p.X;

    public TerrainKind At(Point p) => (TerrainKind)Terrain[Index(p)];

    public void Set(Point p, TerrainKind kind) => Terrain[Index(p)] = (byte)kind;

    public bool Passable(Point p) => InBounds(p) && _blocked[Index(p)] == 0;

    public bool IsBush(Point p) => InBounds(p) && _bush[Index(p)] != 0;

    public bool Covered(Point p)
    {
        if (CoveredRuins.Contains(p)) return true;
        foreach (RoofSection s in Roof)
            if (s.Sound && s.Area.Contains(p)) return true;
        return false;
    }

    public Tree? TreeAt(Point p)
    {
        foreach (Tree t in Trees)
            if (!t.Felled && t.Pos == p) return t;
        return null;
    }

    public Bush? BushAt(Point p)
    {
        foreach (Bush b in Bushes)
            if (b.Pos == p) return b;
        return null;
    }

    public void RebuildCaches()
    {
        int n = Width * Height;
        _blocked = new byte[n];
        _bush = new byte[n];
        for (int i = 0; i < n; i++)
        {
            var kind = (TerrainKind)Terrain[i];
            if (kind is TerrainKind.Water or TerrainKind.Rock or TerrainKind.Wall) _blocked[i] = 1;
        }
        foreach (Tree t in Trees)
            if (!t.Felled) _blocked[Index(t.Pos)] = 1;
        foreach (Bush b in Bushes) _bush[Index(b.Pos)] = 1;
        if (InBounds(Hearth)) _blocked[Index(Hearth)] = 1;
        ZonesDirty = true;
    }

    public void OnDeserialized() => RebuildCaches();
}
