using Return.Sim.Core;

namespace Return.Sim;

/// <summary>A stack of one item type on a tile. A reserved stack (ReservedFor != 0) is set aside for one person.</summary>
public sealed class ItemStack
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public int Count { get; set; }
    public Point Pos { get; set; }
    public long SpoilTick { get; set; }
    public int ReservedFor { get; set; }
}

/// <summary>What a character is carrying.</summary>
public sealed class Carry
{
    public string DefId { get; set; } = "";
    public int Count { get; set; }
}
