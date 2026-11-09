using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Stories;
using static Return.Runner.LocText;

namespace Return.Runner;

/// <summary>What to soak: worlds with seeds FirstSeed, FirstSeed + 1, …, each run for Years years, logs written to OutDir.</summary>
public sealed class SoakOptions
{
    public required int Worlds { get; init; }
    public required int Years { get; init; }
    public required string OutDir { get; init; }

    /// <summary>The most worlds run at once. Each world runs on one thread, so this never changes a log.</summary>
    public int Parallel { get; init; } = Environment.ProcessorCount;

    public ulong FirstSeed { get; init; } = 1;
}

/// <summary>A key and how often it came up.</summary>
public sealed record Tally(string Key, int Count);

/// <summary>One world's totals.</summary>
public sealed record WorldTally(ulong Seed, int Events, int Stories);

/// <summary>A world whose run or log threw, and where.</summary>
public sealed record SoakFailure(ulong Seed, long Tick, string Error);

/// <summary>The soak's results; summary.json holds the same fields.</summary>
public sealed class SoakSummary
{
    public required int Worlds { get; init; }
    public required int Years { get; init; }
    public required ulong FirstSeed { get; init; }
    public required int Parallel { get; init; }
    public required int Stories { get; init; }
    public required double StoriesPerWorld { get; init; }
    public required double StoriesPerWorldYear { get; init; }

    /// <summary>Stories per set of systems, keyed like "Incident+Needs+Social"; keys in ordinal order.</summary>
    public required IReadOnlyList<Tally> BySystemSet { get; init; }

    /// <summary>Stories per terminal event key, most common first, ties in ordinal key order.</summary>
    public required IReadOnlyList<Tally> TopTerminalKinds { get; init; }

    public required IReadOnlyList<WorldTally> PerWorld { get; init; }
    public required IReadOnlyList<SoakFailure> Failures { get; init; }
    public required double ElapsedSeconds { get; init; }
    public required string OutDir { get; init; }
}

/// <summary>
/// Runs many worlds and counts their stories. Each world writes world-&lt;seed&gt;.txt with exactly the
/// log `run` prints for that seed; summary.json totals them. Worlds share only the ContentDb, which is
/// read-only after load. Everything else a world touches is its own, so the logs never depend on
/// --parallel.
/// </summary>
public static class Soak
{
    public const string SummaryFile = "summary.json";

    private const int TopLines = 5;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>What one world contributes: a system-set key and a terminal event key per story, or a failure.</summary>
    internal sealed record WorldResult(ulong Seed, int Events, IReadOnlyList<string> SystemSets, IReadOnlyList<string> TerminalKeys, SoakFailure? Failure);

    public static SoakSummary Run(SoakOptions options) => Run(options, ContentDb.Load(ContentDb.FindContentDir()));

    internal static SoakSummary Run(SoakOptions options, ContentDb content)
    {
        Validate(options);
        var clock = Stopwatch.StartNew();
        string outDir = Path.GetFullPath(options.OutDir);
        Directory.CreateDirectory(outDir);

        var results = new WorldResult[options.Worlds];
        Parallel.For(0, options.Worlds, new ParallelOptions { MaxDegreeOfParallelism = options.Parallel },
            i => results[i] = RunWorld(content, options.FirstSeed + (ulong)i, options.Years, outDir));
        clock.Stop();

        SoakSummary summary = Summarize(options, outDir, results, clock.Elapsed);
        WriteSummaryJson(summary, Path.Combine(outDir, SummaryFile));
        return summary;
    }

    /// <summary>"Incident+Needs+Social": a story's distinct systems in ordinal order, so chain order does not matter.</summary>
    internal static string SystemSetKey(IReadOnlyList<SystemTag> systems)
    {
        var names = new List<string>(systems.Count);
        foreach (SystemTag t in systems)
        {
            string name = t.ToString();
            if (!names.Contains(name)) names.Add(name);
        }
        names.Sort(StringComparer.Ordinal);
        return string.Join("+", names);
    }

    /// <summary>Totals the per-world results, which arrive in seed order.</summary>
    internal static SoakSummary Summarize(SoakOptions options, string outDir, IReadOnlyList<WorldResult> results, TimeSpan elapsed)
    {
        int stories = 0;
        var bySet = new Dictionary<string, int>(StringComparer.Ordinal);
        var byEnding = new Dictionary<string, int>(StringComparer.Ordinal);
        var perWorld = new List<WorldTally>(results.Count);
        var failures = new List<SoakFailure>();
        foreach (WorldResult r in results)
        {
            stories += r.SystemSets.Count;
            foreach (string key in r.SystemSets) bySet[key] = bySet.GetValueOrDefault(key) + 1;
            foreach (string key in r.TerminalKeys) byEnding[key] = byEnding.GetValueOrDefault(key) + 1;
            perWorld.Add(new WorldTally(r.Seed, r.Events, r.SystemSets.Count));
            if (r.Failure != null) failures.Add(r.Failure);
        }

        List<Tally> sets = Tallies(bySet);
        sets.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        List<Tally> endings = Tallies(byEnding);
        endings.Sort(MostFirst);

        int worlds = results.Count;
        return new SoakSummary
        {
            Worlds = worlds,
            Years = options.Years,
            FirstSeed = options.FirstSeed,
            Parallel = options.Parallel,
            Stories = stories,
            StoriesPerWorld = Ratio(stories, worlds),
            StoriesPerWorldYear = Ratio(stories, (long)worlds * options.Years),
            BySystemSet = sets,
            TopTerminalKinds = endings,
            PerWorld = perWorld,
            Failures = failures,
            ElapsedSeconds = Math.Round(elapsed.TotalSeconds, 2, MidpointRounding.AwayFromZero),
            OutDir = outDir,
        };
    }

    /// <summary>Writes summary.json with keys in the order the summary lists them; '+' in keys stays literal.</summary>
    internal static void WriteSummaryJson(SoakSummary s, string path)
    {
        var options = new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        using FileStream file = File.Create(path);
        using (var json = new Utf8JsonWriter(file, options))
        {
            json.WriteStartObject();
            json.WriteNumber("worlds", s.Worlds);
            json.WriteNumber("years", s.Years);
            json.WriteNumber("firstSeed", s.FirstSeed);
            json.WriteNumber("parallel", s.Parallel);
            json.WriteNumber("stories", s.Stories);
            json.WriteNumber("storiesPerWorld", s.StoriesPerWorld);
            json.WriteNumber("storiesPerWorldYear", s.StoriesPerWorldYear);
            WriteTallies(json, "bySystemSet", s.BySystemSet);
            WriteTallies(json, "topTerminalKinds", s.TopTerminalKinds);
            json.WriteStartArray("perWorld");
            foreach (WorldTally t in s.PerWorld)
            {
                json.WriteStartObject();
                json.WriteNumber("seed", t.Seed);
                json.WriteNumber("events", t.Events);
                json.WriteNumber("stories", t.Stories);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteStartArray("failures");
            foreach (SoakFailure f in s.Failures)
            {
                json.WriteStartObject();
                json.WriteNumber("seed", f.Seed);
                json.WriteNumber("tick", f.Tick);
                json.WriteString("error", f.Error);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteNumber("elapsedSeconds", s.ElapsedSeconds);
            json.WriteEndObject();
        }
        file.WriteByte((byte)'\n');
    }

    /// <summary>The short human summary printed after a soak.</summary>
    internal static void Report(SoakSummary s, LocTable loc, TextWriter o)
    {
        Line(o, Format(loc, "log.soak.done",
            ("seconds", s.ElapsedSeconds.ToString("0.00", Inv)), ("worlds", Num(s.Worlds)),
            ("first", Num(s.FirstSeed)), ("last", Num(s.FirstSeed + (ulong)s.Worlds - 1)),
            ("years", Num(s.Years)), ("threads", Num(s.Parallel))));
        Line(o, Format(loc, "log.soak.stories", ("stories", Num(s.Stories)), ("per_world", s.StoriesPerWorld.ToString("0.00", Inv))));
        if (s.BySystemSet.Count > 0)
        {
            var bySize = new List<Tally>(s.BySystemSet);
            bySize.Sort(MostFirst);
            Line(o, loc.Get("log.soak.by_system_set"));
            WriteTop(o, loc, bySize);
        }
        if (s.TopTerminalKinds.Count > 0)
        {
            Line(o, loc.Get("log.soak.endings"));
            WriteTop(o, loc, s.TopTerminalKinds);
        }
        if (s.Failures.Count > 0)
            Line(o, Format(loc, "log.soak.crashed_list", ("seeds", List(loc, s.Failures.Select(f => Num(f.Seed))))));
        Line(o, Format(loc, "log.soak.written", ("dir", s.OutDir)));
    }

    /// <summary>
    /// Runs one world on the calling thread and writes its log. A world that throws becomes a failure
    /// in the summary (its file says where it stopped) instead of ending the whole soak.
    /// </summary>
    private static WorldResult RunWorld(ContentDb content, ulong seed, int years, string outDir)
    {
        string path = Path.Combine(outDir, "world-" + Num(seed) + ".txt");
        Simulation? sim = null;
        try
        {
            sim = Simulation.Create(content, seed);
            sim.Advance(StoryLog.TicksFor(content.World, years));
            World w = sim.World;
            LogAnalysis analysis = StoryLog.Analyze(w);
            var log = new StringWriter(Inv);
            StoryLog.Render(w, analysis, log);
            File.WriteAllText(path, log.ToString(), Cli.Utf8);

            var sets = new List<string>(analysis.Stories.Count);
            var endings = new List<string>(analysis.Stories.Count);
            foreach (Story story in analysis.Stories)
            {
                sets.Add(SystemSetKey(story.Systems));
                endings.Add(w.Log.Get(story.TerminalEventId)?.Key ?? "");
            }
            return new WorldResult(seed, w.Log.All.Count, sets, endings, null);
        }
        catch (Exception ex)
        {
            long tick = sim?.World.Tick ?? 0;
            string error = ex.GetType().Name + ": " + ex.Message;
            LocTable loc = content.Loc;
            var text = new StringWriter(Inv);
            Line(text, Format(loc, "log.header.seed", ("seed", Num(seed))));
            Line(text, Format(loc, "log.soak.crashed", ("tick", Num(tick)), ("error", error)));
            Line(text);
            Line(text, ex.ToString().ReplaceLineEndings("\n"));
            File.WriteAllText(path, text.ToString(), Cli.Utf8);
            return new WorldResult(seed, sim?.World.Log.All.Count ?? 0, Array.Empty<string>(), Array.Empty<string>(), new SoakFailure(seed, tick, error));
        }
    }

    private static void Validate(SoakOptions o)
    {
        if (o.Worlds < 1) throw new ArgumentOutOfRangeException(nameof(o.Worlds), o.Worlds, "A soak needs at least one world.");
        if (o.Years < 1) throw new ArgumentOutOfRangeException(nameof(o.Years), o.Years, "A soak needs at least one year.");
        if (o.Parallel < 1) throw new ArgumentOutOfRangeException(nameof(o.Parallel), o.Parallel, "Parallel must be at least 1.");
        if (string.IsNullOrWhiteSpace(o.OutDir)) throw new ArgumentException("A soak needs an output folder.", nameof(o.OutDir));
        if (o.FirstSeed > ulong.MaxValue - (ulong)(o.Worlds - 1))
            throw new ArgumentOutOfRangeException(nameof(o.FirstSeed), o.FirstSeed, "The last seed would overflow.");
    }

    /// <summary>Copies counts out of a dictionary; callers sort the list, so dictionary order never reaches the output.</summary>
    private static List<Tally> Tallies(Dictionary<string, int> counts)
    {
        var list = new List<Tally>(counts.Count);
        foreach (KeyValuePair<string, int> kv in counts) list.Add(new Tally(kv.Key, kv.Value));
        return list;
    }

    private static int MostFirst(Tally a, Tally b) => a.Count != b.Count ? b.Count.CompareTo(a.Count) : string.CompareOrdinal(a.Key, b.Key);

    private static double Ratio(long count, long per) => per == 0 ? 0 : Math.Round(count / (double)per, 2, MidpointRounding.AwayFromZero);

    private static void WriteTallies(Utf8JsonWriter json, string name, IReadOnlyList<Tally> tallies)
    {
        json.WriteStartObject(name);
        foreach (Tally t in tallies) json.WriteNumber(t.Key, t.Count);
        json.WriteEndObject();
    }

    private static void WriteTop(TextWriter o, LocTable loc, IReadOnlyList<Tally> tallies)
    {
        for (int i = 0; i < tallies.Count && i < TopLines; i++)
            Line(o, "  " + Format(loc, "log.soak.tally", ("key", tallies[i].Key), ("count", Num(tallies[i].Count))));
    }
}
