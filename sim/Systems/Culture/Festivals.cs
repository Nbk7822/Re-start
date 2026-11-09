namespace Return.Sim.Systems.Culture;

/// <summary>Seasonal festivals (S1, S2, F3; ADR-0023).</summary>
public static class Festivals
{
    /// <summary>Are both at a festival under way right now?</summary>
    public static bool BothAttend(World w, int a, int b)
    {
        foreach (FestivalRun run in w.Culture.Festivals)
            if (run.Attendees.BinarySearch(a) >= 0 && run.Attendees.BinarySearch(b) >= 0) return true;
        return false;
    }
}
