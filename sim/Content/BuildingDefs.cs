using Return.Sim.Core;

namespace Return.Sim.Content;

// The Build menu's buildings (content/buildings.json; ADR-0027). Read-only after load.

/// <summary>What a building costs: <see cref="Count"/> of an item per piece, or, with <see cref="PerTiles"/>, per that many tiles rounded up (roofs).</summary>
public sealed class MaterialDef
{
    public string Item { get; init; } = "";
    public int Count { get; init; }
    public int PerTiles { get; init; }
}

/// <summary>
/// A building the player can plan. <see cref="Kind"/> names the behaviour the simulation gives it
/// (wall, door, roof, bed), so a mod can add a building of an existing kind without code.
/// </summary>
public sealed class BuildingDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public string DescKey { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Category { get; init; } = "";
    public List<MaterialDef> Materials { get; init; } = new();

    /// <summary>Work ticks to finish; per tile for a roof section.</summary>
    public int WorkTicks { get; init; }
    public int TakeDownPct { get; init; }
    public List<string> PlaceOn { get; init; } = new();
    public int MaxSide { get; init; }
    public int SupportRange { get; init; }
    public int RestBonusPct { get; init; }
    public List<string> Tags { get; init; } = new();

    /// <summary>"a bed": used in event text where one piece is meant.</summary>
    public string AKey => "building." + Id + ".a";

    /// <summary>"beds": used in event text where several are meant.</summary>
    public string PluralKey => "building." + Id + ".plural";

    /// <summary>The one material this version builds it from (ContentDb checks there is exactly one).</summary>
    public MaterialDef Material => Materials[0];

    /// <summary>Only a roof section covers more than one tile.</summary>
    public bool Spans => Kind == Systems.Building.Roof;

    /// <summary>How much of its material one piece over this area costs.</summary>
    public int Cost(Rect area)
    {
        MaterialDef m = Material;
        int tiles = area.W * area.H;
        return m.PerTiles > 0 ? (m.Count * tiles + m.PerTiles - 1) / m.PerTiles : m.Count;
    }

    /// <summary>Work ticks to build one piece over this area (a roof's work is per tile).</summary>
    public int WorkFor(Rect area) => Spans ? WorkTicks * area.W * area.H : WorkTicks;

    /// <summary>Taking down takes half the work of building.</summary>
    public int TakeDownWorkFor(Rect area) => Math.Max(1, WorkFor(area) / 2);

    /// <summary>What taking it down returns: <see cref="TakeDownPct"/> of its cost, rounded down.</summary>
    public int ReturnedOnTakeDown(Rect area) => Cost(area) * TakeDownPct / 100;
}
