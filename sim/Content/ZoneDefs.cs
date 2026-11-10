namespace Return.Sim.Content;

/// <summary>
/// A kind of zone the player can paint (zones.json, ADR-0028): a stockpile, a meeting place. The
/// behaviour names code (as an incident's id does); the layer says whether it may overlap others.
/// </summary>
public sealed class ZoneDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";

    /// <summary>One line saying what the zone asks ("where the camp keeps its goods").</summary>
    public string HintKey { get; init; } = "";

    /// <summary>"stores" or "gather" (and "home" from step 3).</summary>
    public string Behaviour { get; init; } = "";

    /// <summary>"use": never overlaps another use zone; "area": overlaps anything.</summary>
    public string Layer { get; init; } = "use";

    /// <summary>At most one zone of this kind: a second marking joins the first.</summary>
    public bool Single { get; init; }

    /// <summary>A view hint for the zone's fill and edge ("#c8b07a").</summary>
    public string Color { get; init; } = "#888888";
}
