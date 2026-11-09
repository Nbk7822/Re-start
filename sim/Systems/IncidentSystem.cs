using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>Stub until Task 13 fills it in.</summary>
public static class IncidentSystem
{
    public static void Hourly(World w)
    {
    }

    /// <summary>Fires an incident by id. targetId names the person it happens to, when there is one.</summary>
    public static SimEvent? Fire(World w, string incidentId, long cause = 0, int targetId = 0) => null;
}
