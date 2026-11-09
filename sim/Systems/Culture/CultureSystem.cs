namespace Return.Sim.Systems.Culture;

/// <summary>Culture's hourly upkeep: old deeds and readings are forgotten (ADR-0023).</summary>
public static class CultureSystem
{
    public static void Hourly(World w)
    {
        Omens.Prune(w);
    }
}
