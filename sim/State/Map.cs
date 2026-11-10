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

    // The Build menu (ADR-0027). New sections are appended; one taken down stays in the list, unsound and
    // Removed, so Index stays its position.
    public int Id { get; set; }
    /// <summary>Who raised it, whoever finished it first; empty for the old camp's sections.</summary>
    public List<int> BuiltBy { get; set; } = new();
    public long BuiltEvent { get; set; }
    /// <summary>The last person to repair it; 0 if nobody has.</summary>
    public int MendedBy { get; set; }
    /// <summary>The event that marked it to be taken down; 0 if it is not marked.</summary>
    public long MarkedEvent { get; set; }
    /// <summary>Taken down: never repaired, never counted, gives no cover.</summary>
    public bool Removed { get; set; }
}

public sealed class Bed
{
    public int Id { get; set; }
    public Point Pos { get; set; }
    public int OwnerId { get; set; }

    // The Build menu (ADR-0027). New beds are appended; one taken down is removed.
    /// <summary>Who built it, whoever finished it first; empty for the old camp's bedrolls.</summary>
    public List<int> BuiltBy { get; set; } = new();
    public long BuiltEvent { get; set; }
    /// <summary>The event in which its owner claimed it; 0 while unclaimed.</summary>
    public long ClaimEvent { get; set; }
    /// <summary>Whose it was, once its owner left or died; 0 if nobody's.</summary>
    public int PrevOwnerId { get; set; }
    /// <summary>The event that marked it to be taken down; 0 if it is not marked.</summary>
    public long MarkedEvent { get; set; }
}

/// <summary>
/// The tile grid and fixed camp features. Passability is cached in arrays that are rebuilt after
/// generation, after load, and whenever a blocking feature changes (e.g. a tree is felled).
/// </summary>
public sealed class Map : IJsonOnDeserialized
{
    private byte[] _blocked = Array.Empty<byte>();
    private byte[] _bush = Array.Empty<byte>();

    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Terrain { get; set; } = Array.Empty<byte>();
    public List<Tree> Trees { get; set; } = new();
    public List<Bush> Bushes { get; set; } = new();
    public List<RoofSection> Roof { get; set; } = new();
    public List<Bed> Beds { get; set; } = new();
    public Rect Shelter { get; set; }
    public Rect CoveredRuins { get; set; }
    public Rect Stockpile { get; set; }
    public Point Hearth { get; set; }
    public Point WatchPost { get; set; }

    /// <summary>Walls and doors built in this playthrough (graves later), in id order (ADR-0027).</summary>
    public List<Structure> Structures { get; set; } = new();

    /// <summary>Goes up whenever terrain, roofs, beds or structures change: the view watches it, and blocked plans try again (ADR-0027).</summary>
    public int Revision { get; set; }

    /// <summary>Records a change to terrain, roofs, beds or structures: bumps <see cref="Revision"/> and rebuilds the passability caches.</summary>
    public void Changed()
    {
        Revision++;
        RebuildCaches();
    }

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
    }

    public void OnDeserialized() => RebuildCaches();
}
