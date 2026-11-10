namespace Return.Sim;

/// <summary>A run of tiles on one row: <see cref="Length"/> tiles eastward from (X, Y).</summary>
public readonly record struct TileRun(int Y, int X, int Length);

/// <summary>
/// A painted area on the grid (ADR-0028): a stockpile or a meeting place. It is an intention the
/// player set, never an order; people read it and decide. Tiles are kept canonical (sorted by y then
/// x, merged, never overlapping), so equal tile sets serialise identically.
/// </summary>
public sealed class Zone
{
    public int Id { get; set; }

    /// <summary>A zones.json id ("stockpile").</summary>
    public string Kind { get; set; } = "";

    public List<TileRun> Tiles { get; set; } = new();

    /// <summary>For a stockpile: the item ids it keeps, sorted ordinally; never empty. Empty for other kinds.</summary>
    public List<string> Items { get; set; } = new();

    /// <summary>The event that last marked, moved or reshaped it; 0 for the zones the world starts with.</summary>
    public long MadeEvent { get; set; }

    /// <summary>Who decided: the player now, a delegate at kingdom scale; 0 for the zones the world starts with.</summary>
    public int MadeBy { get; set; }
}
