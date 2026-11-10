using Return.Sim.Systems.Culture;

namespace Return.Sim.Systems.Jobs;

/// <summary>Jobs culture asks of people (ADR-0023): gathering at the hearth for a festival.</summary>
public static class CultureJobs
{
    /// <summary>Walk to the hearth, join every festival under way, and stay until they end.</summary>
    public static bool Feast(World w, Character c, Job j)
    {
        if (w.Culture.Festivals.Count == 0) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            Festivals.Attend(w, c);
            c.Facing = TravelSystem.FacingFor(c.Pos, w.Map.Hearth);
            j.Stage = 1;
        }
        return false;
    }
}
