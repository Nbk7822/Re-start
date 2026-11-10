using Return.Sim.Core;

namespace Return.Sim;

/// <summary>A stack of one item type on a tile. SpoilTick 0 = never spoils.</summary>
public sealed class ItemStack
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public int Count { get; set; }
    public Point Pos { get; set; }
    public long SpoilTick { get; set; }

    /// <summary>
    /// Put away in a stockpile: the camp's stores, wherever it lies now. Moving a stockpile leaves its
    /// goods stored where they are, waiting to be carried, so they can still be eaten, shared out and
    /// stolen (ADR-0028). Loose stacks (a windfall, a load dropped on the way) are not stores.
    /// </summary>
    public bool Stored { get; set; }
}

/// <summary>A claim on stockpile food set aside for one person while food is short (the ration policy).</summary>
public sealed class Reservation
{
    public int CharacterId { get; set; }
    public int Count { get; set; }
}

/// <summary>What a character is carrying.</summary>
public sealed class Carry
{
    public string DefId { get; set; } = "";
    public int Count { get; set; }

    /// <summary>A hauler carrying stores keeps them stores, even if the load is dropped on the way (ADR-0028).</summary>
    public bool Stored { get; set; }
}
