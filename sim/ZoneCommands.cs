namespace Return.Sim;

// The zone commands (ADR-0028). Each is an intention the player states for the camp, tick-stamped and
// recorded like the ration policy; each checks itself again when it applies, because a draft can apply
// a tick or more after it was made. Tile sets are absolute, so a later command for the same zone simply
// replaces an earlier one. A change that would leave an item with no stockpile is ignored whole.

/// <summary>
/// Marks tiles as a zone of a kind: ZoneId 0 makes a new zone, otherwise that zone's tiles become
/// exactly <see cref="Tiles"/>. Items (stockpiles only) sets what it keeps; null keeps what it kept
/// (every item, for a new stockpile).
/// </summary>
public sealed record MarkZone(long Tick, int ZoneId, string Kind, IReadOnlyList<TileRun> Tiles, IReadOnlyList<string>? Items = null) : Command(Tick)
{
    internal override void Apply(World w) => Systems.Zones.ApplyMark(w, this);
}

/// <summary>Takes a zone away.</summary>
public sealed record RemoveZone(long Tick, int ZoneId) : Command(Tick)
{
    internal override void Apply(World w) => Systems.Zones.ApplyRemove(w, this);
}

/// <summary>Sets which items a stockpile keeps (never none).</summary>
public sealed record SetZoneItems(long Tick, int ZoneId, IReadOnlyList<string> Items) : Command(Tick)
{
    internal override void Apply(World w) => Systems.Zones.ApplySetItems(w, this);
}
