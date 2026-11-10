using Return.Sim.Events;
using Return.Sim.Stories;

namespace Return.Sim.Systems;

/// <summary>
/// The Chronicle of Lost Futures (the player design §5.5, fixed decision 9): when a death sends the world back, what
/// came after the knot is frozen from the dying world's log into lines that read the same in any later world. It
/// keeps the stories that ended after the knot (the eight longest, then those touching most systems), every line a
/// memory quotes, companions' growth beats and your death. Each event is stored once, as a loc key and arguments:
/// cast ids are stable, and anyone made during play (a thief) is frozen to their name.
/// </summary>
public static class LostFutures
{
    public const int KeptStories = 8;

    /// <summary>
    /// Freezes the lost future. <paramref name="cut"/> is how many events the restored knot's log holds: the dying
    /// log continues it, so everything from there on happened only in the timeline being lost. <paramref name="quoted"/>
    /// lists events memories will quote, which are kept whatever stories they lie on.
    /// </summary>
    public static (LostFuture Future, Dictionary<long, int> LineOf) Build(World dying, int cut, long knotTick, long deathEvent,
        IReadOnlyList<long> quoted)
    {
        List<SimEvent> log = dying.Log.All;
        IReadOnlyList<Story> stories = StoryAnalyzer.Find(log, id => dying.Find(id)?.Named == true);
        var after = new List<Story>();
        foreach (Story s in stories)
            if (s.TerminalEventId > cut) after.Add(s); // event ids are positions + 1, so id > cut means index >= cut
        var kept = new List<Story>(after);
        kept.Sort((a, b) =>
        {
            int byLength = b.Chain.Count.CompareTo(a.Chain.Count);
            if (byLength != 0) return byLength;
            int bySystems = b.Systems.Count.CompareTo(a.Systems.Count);
            return bySystems != 0 ? bySystems : a.TerminalEventId.CompareTo(b.TerminalEventId);
        });
        if (kept.Count > KeptStories) kept.RemoveRange(KeptStories, kept.Count - KeptStories);
        kept.Sort((a, b) => a.TerminalEventId.CompareTo(b.TerminalEventId));

        var ids = new SortedSet<long>();
        foreach (Story s in kept)
            foreach (long id in s.Chain)
                if (id > cut) ids.Add(id);
        foreach (long id in quoted)
            if (id > cut) ids.Add(id);
        var growth = new List<long>();
        for (int i = cut; i < log.Count; i++)
            if (log[i].System == SystemTag.Growth) { ids.Add(log[i].Id); growth.Add(log[i].Id); }
        if (deathEvent > cut) ids.Add(deathEvent);

        var future = new LostFuture
        {
            Timeline = dying.Fate.Timeline,
            KnotTick = knotTick,
            DeathTick = dying.Log.Get(deathEvent)?.Tick ?? dying.Tick,
            StoriesFound = after.Count,
        };
        var lineOf = new Dictionary<long, int>();
        foreach (long id in ids)
        {
            SimEvent e = dying.Log.Get(id)!;
            lineOf[id] = future.Lines.Count;
            future.Lines.Add(Freeze(dying, e));
        }
        foreach (Story s in kept)
        {
            var story = new LostStory { Systems = new List<SystemTag>(s.Systems), Length = s.Chain.Count };
            foreach (long id in s.Chain)
                if (lineOf.TryGetValue(id, out int line)) story.Lines.Add(line);
            future.Stories.Add(story);
        }
        foreach (long id in growth) future.GrowthLines.Add(lineOf[id]);
        future.DeathLine = lineOf.TryGetValue(deathEvent, out int death) ? death : -1;
        return (future, lineOf);
    }

    /// <summary>One event as a line that reads the same in any later world: cast members keep their ids; anyone else becomes their name.</summary>
    public static LostLine Freeze(World w, SimEvent e)
    {
        var line = new LostLine { Tick = e.Tick, System = e.System, Key = e.Key };
        foreach (EventArg a in e.Args)
        {
            if (a.Value.StartsWith("c:", StringComparison.Ordinal) && int.TryParse(a.Value.AsSpan(2), out int id) && !IsCast(w, id))
                line.Args.Add(EventArg.Key(a.Name, w.Find(id)?.NameKey ?? "name.someone"));
            else
                line.Args.Add(new EventArg { Name = a.Name, Value = a.Value });
        }
        foreach (int actor in e.Actors)
            if (IsCast(w, actor)) line.Actors.Add(actor);
        return line;
    }

    /// <summary>A cast member: made by CreateCast in cast.json's order, so their id is the same in every timeline of this world.</summary>
    public static bool IsCast(World w, int id) =>
        w.Find(id) is { } c && c.CastKey.Length > 0 && w.Content.TryCast(c.CastKey) != null;
}
