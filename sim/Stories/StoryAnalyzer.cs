using Return.Sim.Events;

namespace Return.Sim.Stories;

/// <summary>A detected story: the causal chain (event ids, oldest first) ending in a persistent change.</summary>
public sealed record Story(long TerminalEventId, IReadOnlyList<long> Chain, IReadOnlyList<SystemTag> Systems, IReadOnlyList<int> Characters);

/// <summary>
/// Finds stories as CLAUDE.md defines them: a chain of at least 3 causally linked events across at
/// least 2 systems, involving a named character, ending in a persistent state change.
/// For every event it keeps the longest backward cause path per set of systems touched (a bitmask
/// over <see cref="SystemTag"/>), so a short multi-system chain is not hidden behind a longer
/// single-system one. Each event with a persistent effect yields at most one story: the longest
/// qualifying chain, then the one touching more systems, then the lowest system mask.
/// </summary>
public static class StoryAnalyzer
{
    public const int MinEvents = 3;
    public const int MinSystems = 2;

    private readonly record struct Entry(int Mask, int Len, int PrevIndex, int PrevMask);

    public static IReadOnlyList<Story> Find(IReadOnlyList<SimEvent> events, Func<int, bool> isNamed)
    {
        int n = events.Count;
        var index = new Dictionary<long, int>(n);
        for (int i = 0; i < n; i++) index[events[i].Id] = i;

        var best = new List<Entry>[n];
        var stories = new List<Story>();
        for (int i = 0; i < n; i++)
        {
            SimEvent e = events[i];
            int bit = 1 << (int)e.System;
            var entries = new List<Entry> { new(bit, 1, -1, 0) };
            foreach (long cause in e.Causes)
            {
                if (!index.TryGetValue(cause, out int j) || j >= i) continue;
                foreach (Entry prev in best[j]) Upsert(entries, new Entry(prev.Mask | bit, prev.Len + 1, j, prev.Mask));
            }
            entries.Sort((a, b) => a.Mask.CompareTo(b.Mask));
            best[i] = entries;
            if (e.Effects.Count == 0) continue;

            var candidates = new List<Entry>();
            foreach (Entry en in entries)
                if (en.Len >= MinEvents && System.Numerics.BitOperations.PopCount((uint)en.Mask) >= MinSystems) candidates.Add(en);
            candidates.Sort((a, b) =>
            {
                int byLen = b.Len.CompareTo(a.Len);
                if (byLen != 0) return byLen;
                int bySystems = System.Numerics.BitOperations.PopCount((uint)b.Mask).CompareTo(System.Numerics.BitOperations.PopCount((uint)a.Mask));
                return bySystems != 0 ? bySystems : a.Mask.CompareTo(b.Mask);
            });
            foreach (Entry candidate in candidates)
            {
                Story? story = Build(events, best, i, candidate.Mask, isNamed);
                if (story == null) continue;
                stories.Add(story);
                break;
            }
        }
        return stories;
    }

    private static void Upsert(List<Entry> entries, Entry entry)
    {
        for (int k = 0; k < entries.Count; k++)
        {
            if (entries[k].Mask != entry.Mask) continue;
            if (entry.Len > entries[k].Len) entries[k] = entry;
            return;
        }
        entries.Add(entry);
    }

    private static Story? Build(IReadOnlyList<SimEvent> events, List<Entry>[] best, int terminal, int mask, Func<int, bool> isNamed)
    {
        var chainIdx = new List<int>();
        int idx = terminal, m = mask;
        while (idx >= 0)
        {
            chainIdx.Add(idx);
            Entry? found = null;
            foreach (Entry en in best[idx])
                if (en.Mask == m) { found = en; break; }
            if (found == null) break;
            idx = found.Value.PrevIndex;
            m = found.Value.PrevMask;
        }
        chainIdx.Reverse();

        var chain = new List<long>(chainIdx.Count);
        var systems = new List<SystemTag>();
        var characters = new List<int>();
        bool named = false;
        foreach (int ci in chainIdx)
        {
            SimEvent e = events[ci];
            chain.Add(e.Id);
            if (!systems.Contains(e.System)) systems.Add(e.System);
            foreach (int a in e.Actors)
            {
                if (!characters.Contains(a)) characters.Add(a);
                if (isNamed(a)) named = true;
            }
        }
        return named ? new Story(events[terminal].Id, chain, systems, characters) : null;
    }
}
