namespace Return.Sim.Events;

/// <summary>
/// Append-only event history. Ids run 1, 2, 3… with no gaps, so an event's id is its position + 1.
/// A cause must name an earlier event; 0 means "no cause" and is dropped.
/// </summary>
public sealed class EventLog
{
    public List<SimEvent> All { get; set; } = new();
    public long NextId { get; set; } = 1;

    public SimEvent? Get(long id) => id >= 1 && id < NextId ? All[(int)(id - 1)] : null;

    public SimEvent Emit(World w, SystemTag system, string key, IReadOnlyList<int> actors, IReadOnlyList<long> causes,
        IReadOnlyList<Effect>? effects = null, params EventArg[] args)
    {
        var causeList = new List<long>(causes.Count);
        foreach (long c in causes)
        {
            if (c == 0) continue;
            if (c < 0 || c >= NextId)
                throw new InvalidOperationException($"Event '{key}' cites cause {c}, which is not an earlier event (next id is {NextId}).");
            if (!causeList.Contains(c)) causeList.Add(c);
        }
        var e = new SimEvent
        {
            Id = NextId++,
            Tick = w.Tick,
            System = system,
            Key = key,
            Actors = new List<int>(actors),
            Causes = causeList,
            Effects = effects == null ? new List<Effect>() : new List<Effect>(effects),
            Args = new List<EventArg>(args),
        };
        All.Add(e);
        return e;
    }
}
