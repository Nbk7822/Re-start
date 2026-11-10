namespace Return.Sim.Systems;

/// <summary>
/// The Build menu (ADR-0027): the kinds of building the simulation has behaviour for, and where they
/// may stand. Plans, placement and the work of building live in the partial files beside this one.
/// </summary>
public static partial class Building
{
    public const string Wall = "wall";
    public const string Door = "door";
    public const string Roof = "roof";
    public const string Bed = "bed";

    /// <summary>The terrain a building may be placed on (buildings.json placeOn).</summary>
    public static readonly IReadOnlyList<string> PlaceableTerrain = new[] { "grass", "soil", "floor" };
}
