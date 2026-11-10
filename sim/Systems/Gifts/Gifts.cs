using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>Your one gift acting in the world (the player design §4.2–4.4, ADR-0030).</summary>
public static class Gifts
{
    /// <summary>Hourly, before food is shared out.</summary>
    public static void Hourly(World w)
    {
    }

    /// <summary>At a return, before the dying world is let go: what your gift carries back.</summary>
    public static void KeepEchoes(World dying, FateBook fate)
    {
    }

    /// <summary>After a return, once the world gone back to is in place and the return is logged.</summary>
    public static void OnReturned(World w, SimEvent back)
    {
    }
}
