using Return.Sim.Events;

namespace Return.Sim.Api;

/// <summary>
/// Folds runs of the same line that differ only in one person ("Ilex asked Nia, Nia refused; Ilex asked Mira, Mira
/// refused…"), so the story log and the Chronicle read "… and the same with Mira, Bram, until Day 37, 03:00" (zones design
/// 5.6: the runner and the view share one fold). A pure read over events.
/// </summary>
public static class EventFold
{
    /// <summary>
    /// The run of identical steps starting at <paramref name="start"/>: a unit of one or two events that repeats with only
    /// one character changing. Returns the unit length, how many times it occurs (1 = no run worth folding) and the
    /// placeholder that changes. A run is folded only from three occurrences on.
    /// </summary>
    public static (int Unit, int Repeats, string Arg) Repetition(IReadOnlyList<SimEvent> events, int start)
    {
        for (int unit = 1; unit <= 2; unit++)
        {
            if (start + 3 * unit > events.Count) break;
            string? arg = ChangingArg(events, start, start + unit, unit);
            if (arg == null || !AboutOnePerson(events, start, unit, arg) || !AboutOnePerson(events, start + unit, unit, arg)) continue;
            int repeats = 2;
            while (start + (repeats + 1) * unit <= events.Count && ChangingArg(events, start, start + repeats * unit, unit) == arg
                   && AboutOnePerson(events, start + repeats * unit, unit, arg)) repeats++;
            if (repeats >= 3) return (unit, repeats, arg);
        }
        return (1, 1, "");
    }

    /// <summary>The value of a named argument ("c:3"), or "" if the event has none.</summary>
    public static string ArgValue(SimEvent e, string name)
    {
        foreach (EventArg a in e.Args)
            if (a.Name == name) return a.Value;
        return "";
    }

    /// <summary>The one character placeholder whose value differs between two units (the same one in every event of the unit), or null.</summary>
    private static string? ChangingArg(IReadOnlyList<SimEvent> events, int a, int b, int unit)
    {
        string? changing = null;
        for (int u = 0; u < unit; u++)
        {
            SimEvent x = events[a + u], y = events[b + u];
            if (x.Key != y.Key || x.Args.Count != y.Args.Count) return null;
            for (int i = 0; i < x.Args.Count; i++)
            {
                if (x.Args[i].Name != y.Args[i].Name) return null;
                if (x.Args[i].Value == y.Args[i].Value) continue;
                if (!x.Args[i].Value.StartsWith("c:", StringComparison.Ordinal)) return null;
                if (changing != null && changing != x.Args[i].Name) return null;
                changing = x.Args[i].Name;
            }
        }
        return changing;
    }

    /// <summary>Every event in the unit names the same person in the changing placeholder ("Ilex asked Nia", "Nia refused").</summary>
    private static bool AboutOnePerson(IReadOnlyList<SimEvent> events, int start, int unit, string arg)
    {
        string first = ArgValue(events[start], arg);
        for (int u = 1; u < unit; u++)
            if (ArgValue(events[start + u], arg) != first) return false;
        return true;
    }
}
