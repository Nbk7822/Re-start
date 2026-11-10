namespace Return.Sim.Systems.Culture;

/// <summary>Culture's hourly upkeep: festivals end and begin; old deeds, readings and guest-bread decisions are forgotten (ADR-0023).</summary>
public static class CultureSystem
{
    public static void Hourly(World w)
    {
        Festivals.Hourly(w);
        Omens.Prune(w);
        Hospitality.Prune(w);
    }
}
