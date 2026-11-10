using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Return.Sim;
using Return.Sim.Api;
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

    /// <summary>The stand-in player's zone plan (Autoplay.ZonePlans): none, dry or mixed (ADR-0028).</summary>
    public string Zones { get; init; } = "none";

    /// <summary>The stand-in player's schedule plan (Autoplay.DaysPlans): default, nowatch, nightwatcher or mixed (ADR-0029).</summary>
    public string Days { get; init; } = "default";
}

/// <summary>
/// One schedule plan's worlds (ADR-0029; design 8.2): their stories and those whose chain runs through the watch or
/// overwork (with their worlds); humans' median hours asleep a day and how many night samples (01:00-05:00) find a
/// human awake off watch; how much of the watch hours someone held the post and the 90th percentile of a turn; thieves
/// and thefts; worlds with a refusal, with a cover that repaid a debt and with someone worn out (each also over the
/// worlds whose undead is bound to forage, which the design's acceptance reads); and insults.
/// </summary>
public sealed record DaysPlanTally(string Plan, int Worlds, int Stories, int WatchStories, int WatchStoryWorlds,
    double MedianHoursAsleep, double NightAwakePct, double WatchKeptPct, double TurnP90Hours, int Thieves, int Thefts,
    int RefusalWorlds, int CoverRepaidWorlds, int WornOutWorlds, int ForageWorlds, int ForageRefusalWorlds, int ForageCoverRepaidWorlds, int Insults);

/// <summary>
/// One zone plan's worlds (ADR-0028): their stories; stories whose chain holds a zone event (a zone marked or
/// moved, an unseen theft, someone keeping away, a breach or a stay of the home area) and those that run through a
/// zone the player laid out, with the worlds that have one; and what the stores went through (thefts, unseen thefts,
/// spoilage), how often people kept away from the fire, insults and accidents, all as totals over the plan's worlds.
/// The home area's (zones step 3; design 9.2): stories through a home-area line (out after dark, stayed in) and their
/// worlds; breaches by excuse (starving, to help, their own way for any reason); hungry stays; nights nobody bound
/// would keep the watch; walks home at dusk; rule-keepers who saw a breach, by the trait or by their people's ways;
/// and accidents in the dark and worn out.
/// </summary>
public sealed record ZonePlanTally(string Plan, int Worlds, int Stories, int ZoneStories, int ZoneStoryWorlds, int LaidOutStories, int LaidOutWorlds,
    int Thefts, int UnseenThefts, int Spoiled, int KeptAway, int KeptAwayWorlds, int Insults, int Accidents,
    int HomeStories = 0, int HomeStoryWorlds = 0, int OutStarving = 0, int OutHelp = 0, int OutOwnWay = 0, int StayedInHungry = 0, int StayedInWatch = 0,
    int WalksHome = 0, int WitnessTrait = 0, int WitnessWays = 0, int AccidentsDark = 0, int AccidentsWorn = 0);

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

    /// <summary>Stories whose chain holds a Culture event (ADR-0023), their share of all stories in percent, and how many worlds had one.</summary>
    public int CultureStories { get; init; }
    public double CultureStoryShare { get; init; }
    public int CultureWorlds { get; init; }

    /// <summary>Culture stories by the family of their first Culture event: a custom id, or omen_anger, omen_blessing, word_spread; most first.</summary>
    public IReadOnlyList<Tally> ByCultureLink { get; init; } = Array.Empty<Tally>();

    /// <summary>Named people who left or died, by species id; most first (the vampire guard of ADR-0023 reads it).</summary>
    public IReadOnlyList<Tally> DeparturesAndDeathsBySpecies { get; init; } = Array.Empty<Tally>();

    /// <summary>The zone plan the soak ran (--zones) and its measures per plan a world ran under, plans in ordinal order (ADR-0028).</summary>
    public string Zones { get; init; } = "none";
    public IReadOnlyList<ZonePlanTally> ByZonePlan { get; init; } = Array.Empty<ZonePlanTally>();

    /// <summary>The schedule plan the soak ran (--days) and its measures per plan a world ran under, plans in ordinal order (ADR-0029).</summary>
    public string Days { get; init; } = "default";
    public IReadOnlyList<DaysPlanTally> ByDaysPlan { get; init; } = Array.Empty<DaysPlanTally>();
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

    /// <summary>
    /// What one world contributes: a system-set key and a terminal event key per story, or a failure;
    /// per story its culture link ("" for none); and the species of each named person who left or died.
    /// </summary>
    internal sealed record WorldResult(ulong Seed, int Events, IReadOnlyList<string> SystemSets, IReadOnlyList<string> TerminalKeys, SoakFailure? Failure,
        IReadOnlyList<string>? CultureLinks = null, IReadOnlyList<string>? Losses = null, ZoneWorld? Zones = null, DaysWorld? Days = null);

    /// <summary>One world's schedule measures, folded into <see cref="DaysPlanTally"/> by plan.</summary>
    internal sealed record DaysWorld(string Plan, int WatchStories, DaysMeter Meter, int Thieves, int Thefts, bool Refused, bool CoverRepaid, bool WornOut,
        bool ForageUndead, int Insults);

    /// <summary>One world's zone measures, folded into <see cref="ZonePlanTally"/> by plan.</summary>
    internal sealed record ZoneWorld(string Plan, int ZoneStories, int LaidOutStories, int Thefts, int UnseenThefts, int Spoiled, int KeptAway, int Insults, int Accidents,
        int HomeStories = 0, int OutStarving = 0, int OutHelp = 0, int OutOwnWay = 0, int StayedInHungry = 0, int StayedInWatch = 0,
        int WalksHome = 0, int WitnessTrait = 0, int WitnessWays = 0, int AccidentsDark = 0, int AccidentsWorn = 0);

    /// <summary>
    /// An event a zone story runs through: a zone or the home area marked, moved, changed or removed, a theft out of the
    /// watch's sight, someone keeping away from a meeting place, going out after dark or staying in (zones step 3).
    /// </summary>
    internal static bool IsZoneStoryEvent(string key) =>
        key.StartsWith("event.world.zone_", StringComparison.Ordinal) || key.StartsWith("event.world.home_", StringComparison.Ordinal)
        || key == "event.incident.stolen_unseen" || key.StartsWith("event.mood.kept_away", StringComparison.Ordinal) || IsHomeStoryEvent(key);

    /// <summary>A line of the home area's that the design's acceptance counts stories by (9.2): out after dark, or stayed in (hungry, or off the watch).</summary>
    internal static bool IsHomeStoryEvent(string key) =>
        key.StartsWith("event.work.out_after_dark", StringComparison.Ordinal) || key.StartsWith("event.work.stayed_in_", StringComparison.Ordinal);

    /// <summary>The family of a Culture event: the custom id in "event.culture.&lt;id&gt;.&lt;moment&gt;", or the generic key's last part (omen_anger, word_spread); "" for any other key.</summary>
    internal static string CultureLink(string key)
    {
        const string prefix = "event.culture.";
        if (!key.StartsWith(prefix, StringComparison.Ordinal)) return "";
        string rest = key[prefix.Length..];
        int dot = rest.IndexOf('.');
        return dot < 0 ? rest : rest[..dot];
    }

    public static SoakSummary Run(SoakOptions options) => Run(options, ContentDb.Load(ContentDb.FindContentDir()));

    internal static SoakSummary Run(SoakOptions options, ContentDb content)
    {
        Validate(options);
        var clock = Stopwatch.StartNew();
        string outDir = Path.GetFullPath(options.OutDir);
        Directory.CreateDirectory(outDir);

        var results = new WorldResult[options.Worlds];
        Parallel.For(0, options.Worlds, new ParallelOptions { MaxDegreeOfParallelism = options.Parallel },
            i => results[i] = RunWorld(content, options.FirstSeed + (ulong)i, options.Years, outDir, options.Zones, options.Days));
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
        int cultureStories = 0, cultureWorlds = 0;
        var byLink = new Dictionary<string, int>(StringComparer.Ordinal);
        var losses = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (WorldResult r in results)
        {
            stories += r.SystemSets.Count;
            foreach (string key in r.SystemSets) bySet[key] = bySet.GetValueOrDefault(key) + 1;
            foreach (string key in r.TerminalKeys) byEnding[key] = byEnding.GetValueOrDefault(key) + 1;
            perWorld.Add(new WorldTally(r.Seed, r.Events, r.SystemSets.Count));
            if (r.Failure != null) failures.Add(r.Failure);
            int inWorld = 0;
            foreach (string link in r.CultureLinks ?? Array.Empty<string>())
            {
                if (link.Length == 0) continue;
                inWorld++;
                byLink[link] = byLink.GetValueOrDefault(link) + 1;
            }
            cultureStories += inWorld;
            if (inWorld > 0) cultureWorlds++;
            foreach (string species in r.Losses ?? Array.Empty<string>()) losses[species] = losses.GetValueOrDefault(species) + 1;
        }

        List<Tally> sets = Tallies(bySet);
        sets.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        List<Tally> endings = Tallies(byEnding);
        endings.Sort(MostFirst);
        List<Tally> links = Tallies(byLink);
        links.Sort(MostFirst);
        List<Tally> lost = Tallies(losses);
        lost.Sort(MostFirst);

        var plans = new List<ZonePlanTally>();
        foreach (WorldResult r in results)
        {
            if (r.Zones is not { } z) continue;
            int at = plans.FindIndex(p => p.Plan == z.Plan);
            ZonePlanTally p = at >= 0 ? plans[at] : new ZonePlanTally(z.Plan, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            p = p with
            {
                Worlds = p.Worlds + 1, Stories = p.Stories + r.SystemSets.Count,
                ZoneStories = p.ZoneStories + z.ZoneStories, ZoneStoryWorlds = p.ZoneStoryWorlds + (z.ZoneStories > 0 ? 1 : 0),
                LaidOutStories = p.LaidOutStories + z.LaidOutStories, LaidOutWorlds = p.LaidOutWorlds + (z.LaidOutStories > 0 ? 1 : 0),
                Thefts = p.Thefts + z.Thefts, UnseenThefts = p.UnseenThefts + z.UnseenThefts, Spoiled = p.Spoiled + z.Spoiled,
                KeptAway = p.KeptAway + z.KeptAway, KeptAwayWorlds = p.KeptAwayWorlds + (z.KeptAway > 0 ? 1 : 0),
                Insults = p.Insults + z.Insults, Accidents = p.Accidents + z.Accidents,
                HomeStories = p.HomeStories + z.HomeStories, HomeStoryWorlds = p.HomeStoryWorlds + (z.HomeStories > 0 ? 1 : 0),
                OutStarving = p.OutStarving + z.OutStarving, OutHelp = p.OutHelp + z.OutHelp, OutOwnWay = p.OutOwnWay + z.OutOwnWay,
                StayedInHungry = p.StayedInHungry + z.StayedInHungry, StayedInWatch = p.StayedInWatch + z.StayedInWatch, WalksHome = p.WalksHome + z.WalksHome,
                WitnessTrait = p.WitnessTrait + z.WitnessTrait, WitnessWays = p.WitnessWays + z.WitnessWays,
                AccidentsDark = p.AccidentsDark + z.AccidentsDark, AccidentsWorn = p.AccidentsWorn + z.AccidentsWorn,
            };
            if (at >= 0) plans[at] = p;
            else plans.Add(p);
        }
        plans.Sort((a, b) => string.CompareOrdinal(a.Plan, b.Plan));

        var dayPlans = new List<string>();
        foreach (WorldResult r in results)
            if (r.Days is { } d && !dayPlans.Contains(d.Plan)) dayPlans.Add(d.Plan);
        dayPlans.Sort(StringComparer.Ordinal);
        var byDays = new List<DaysPlanTally>(dayPlans.Count);
        foreach (string plan in dayPlans) byDays.Add(DaysTally(plan, results));

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
            CultureStories = cultureStories,
            CultureStoryShare = stories == 0 ? 0 : Math.Round(cultureStories * 100.0 / stories, 1, MidpointRounding.AwayFromZero),
            CultureWorlds = cultureWorlds,
            ByCultureLink = links,
            DeparturesAndDeathsBySpecies = lost,
            Zones = options.Zones,
            ByZonePlan = plans,
            Days = options.Days,
            ByDaysPlan = byDays,
        };
    }

    /// <summary>Folds one schedule plan's worlds into its tally (histograms summed, then read).</summary>
    private static DaysPlanTally DaysTally(string plan, IReadOnlyList<WorldResult> results)
    {
        var asleep = new int[241];
        var turns = new int[241];
        int worlds = 0, stories = 0, watchStories = 0, watchWorlds = 0, thieves = 0, thefts = 0, refused = 0, covered = 0, worn = 0, forage = 0, forageRefused = 0, forageCovered = 0, insults = 0;
        long night = 0, nightAwake = 0, watch = 0, kept = 0;
        foreach (WorldResult r in results)
        {
            if (r.Days is not { } d || d.Plan != plan) continue;
            worlds++;
            stories += r.SystemSets.Count;
            watchStories += d.WatchStories;
            if (d.WatchStories > 0) watchWorlds++;
            for (int i = 0; i < asleep.Length; i++) { asleep[i] += d.Meter.AsleepTenths[i]; turns[i] += d.Meter.TurnTenths[i]; }
            night += d.Meter.NightSamples;
            nightAwake += d.Meter.NightAwakeOffWatch;
            watch += d.Meter.WatchSamples;
            kept += d.Meter.WatchKept;
            thieves += d.Thieves;
            thefts += d.Thefts;
            if (d.Refused) refused++;
            if (d.CoverRepaid) covered++;
            if (d.WornOut) worn++;
            insults += d.Insults;
            if (!d.ForageUndead) continue;
            forage++;
            if (d.Refused) forageRefused++;
            if (d.CoverRepaid) forageCovered++;
        }
        return new DaysPlanTally(plan, worlds, stories, watchStories, watchWorlds, DaysMeter.Quantile(asleep, 0.5),
            Math.Round(nightAwake * 100.0 / Math.Max(1, night), 1, MidpointRounding.AwayFromZero), Math.Round(kept * 100.0 / Math.Max(1, watch), 1, MidpointRounding.AwayFromZero),
            DaysMeter.Quantile(turns, 0.9), thieves, thefts, refused, covered, worn, forage, forageRefused, forageCovered, insults);
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
            json.WriteNumber("cultureStories", s.CultureStories);
            json.WriteNumber("cultureStoryShare", s.CultureStoryShare);
            json.WriteNumber("cultureWorlds", s.CultureWorlds);
            WriteTallies(json, "byCultureLink", s.ByCultureLink);
            WriteTallies(json, "departuresAndDeathsBySpecies", s.DeparturesAndDeathsBySpecies);
            json.WriteString("zones", s.Zones);
            json.WriteStartArray("byZonePlan");
            foreach (ZonePlanTally p in s.ByZonePlan)
            {
                json.WriteStartObject();
                json.WriteString("plan", p.Plan);
                json.WriteNumber("worlds", p.Worlds);
                json.WriteNumber("stories", p.Stories);
                json.WriteNumber("zoneStories", p.ZoneStories);
                json.WriteNumber("zoneStoryWorlds", p.ZoneStoryWorlds);
                json.WriteNumber("laidOutStories", p.LaidOutStories);
                json.WriteNumber("laidOutWorlds", p.LaidOutWorlds);
                json.WriteNumber("thefts", p.Thefts);
                json.WriteNumber("unseenThefts", p.UnseenThefts);
                json.WriteNumber("spoiled", p.Spoiled);
                json.WriteNumber("keptAway", p.KeptAway);
                json.WriteNumber("keptAwayWorlds", p.KeptAwayWorlds);
                json.WriteNumber("insults", p.Insults);
                json.WriteNumber("accidents", p.Accidents);
                json.WriteNumber("homeStories", p.HomeStories);
                json.WriteNumber("homeStoryWorlds", p.HomeStoryWorlds);
                json.WriteNumber("outStarving", p.OutStarving);
                json.WriteNumber("outHelp", p.OutHelp);
                json.WriteNumber("outOwnWay", p.OutOwnWay);
                json.WriteNumber("stayedInHungry", p.StayedInHungry);
                json.WriteNumber("stayedInWatch", p.StayedInWatch);
                json.WriteNumber("walksHome", p.WalksHome);
                json.WriteNumber("witnessTrait", p.WitnessTrait);
                json.WriteNumber("witnessWays", p.WitnessWays);
                json.WriteNumber("accidentsDark", p.AccidentsDark);
                json.WriteNumber("accidentsWorn", p.AccidentsWorn);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteString("days", s.Days);
            json.WriteStartArray("byDaysPlan");
            foreach (DaysPlanTally p in s.ByDaysPlan)
            {
                json.WriteStartObject();
                json.WriteString("plan", p.Plan);
                json.WriteNumber("worlds", p.Worlds);
                json.WriteNumber("stories", p.Stories);
                json.WriteNumber("watchStories", p.WatchStories);
                json.WriteNumber("watchStoryWorlds", p.WatchStoryWorlds);
                json.WriteNumber("medianHoursAsleep", p.MedianHoursAsleep);
                json.WriteNumber("nightAwakePct", p.NightAwakePct);
                json.WriteNumber("watchKeptPct", p.WatchKeptPct);
                json.WriteNumber("turnP90Hours", p.TurnP90Hours);
                json.WriteNumber("thieves", p.Thieves);
                json.WriteNumber("thefts", p.Thefts);
                json.WriteNumber("refusalWorlds", p.RefusalWorlds);
                json.WriteNumber("coverRepaidWorlds", p.CoverRepaidWorlds);
                json.WriteNumber("wornOutWorlds", p.WornOutWorlds);
                json.WriteNumber("forageWorlds", p.ForageWorlds);
                json.WriteNumber("forageRefusalWorlds", p.ForageRefusalWorlds);
                json.WriteNumber("forageCoverRepaidWorlds", p.ForageCoverRepaidWorlds);
                json.WriteNumber("insults", p.Insults);
                json.WriteEndObject();
            }
            json.WriteEndArray();
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
        Line(o, Format(loc, "log.soak.culture", ("stories", Num(s.CultureStories)), ("share", s.CultureStoryShare.ToString("0.0", Inv)),
            ("worlds", Num(s.CultureWorlds)), ("total", Num(s.Worlds))));
        if (s.ByCultureLink.Count > 0)
        {
            Line(o, loc.Get("log.soak.culture_links"));
            WriteTop(o, loc, s.ByCultureLink);
        }
        foreach (ZonePlanTally p in s.ByZonePlan)
        {
            Line(o, Format(loc, "log.soak.zone_plan", ("plan", p.Plan), ("worlds", Num(p.Worlds)), ("stories", Num(p.Stories)),
                ("zone", Num(p.ZoneStories)), ("zone_worlds", Num(p.ZoneStoryWorlds)), ("laid_out", Num(p.LaidOutStories)), ("laid_out_worlds", Num(p.LaidOutWorlds)),
                ("thefts", Num(p.Thefts)), ("unseen", Num(p.UnseenThefts)), ("spoiled", Num(p.Spoiled)), ("kept_away", Num(p.KeptAway)),
                ("kept_away_worlds", Num(p.KeptAwayWorlds)), ("insults", Num(p.Insults)), ("accidents", Num(p.Accidents))));
            Line(o, "  " + Format(loc, "log.soak.zone_plan_dark", ("dark", Num(p.AccidentsDark)), ("worn", Num(p.AccidentsWorn)), ("accidents", Num(p.Accidents))));
            if (p.Plan is "home" or "short" or "both")
                Line(o, "  " + Format(loc, "log.soak.home_plan", ("home", Num(p.HomeStories)), ("home_worlds", Num(p.HomeStoryWorlds)), ("worlds", Num(p.Worlds)),
                    ("starving", Num(p.OutStarving)), ("help", Num(p.OutHelp)), ("own_way", Num(p.OutOwnWay)), ("hungry", Num(p.StayedInHungry)),
                    ("watch", Num(p.StayedInWatch)), ("walks", Num(p.WalksHome)), ("witness_trait", Num(p.WitnessTrait)), ("witness_ways", Num(p.WitnessWays))));
        }
        foreach (DaysPlanTally p in s.ByDaysPlan)
            Line(o, Format(loc, "log.soak.days_plan", ("plan", p.Plan), ("worlds", Num(p.Worlds)), ("stories", Num(p.Stories)),
                ("watch", Num(p.WatchStories)), ("watch_worlds", Num(p.WatchStoryWorlds)), ("asleep", p.MedianHoursAsleep.ToString("0.0", Inv)),
                ("night_awake", p.NightAwakePct.ToString("0.0", Inv)), ("kept", p.WatchKeptPct.ToString("0.0", Inv)), ("turn_p90", p.TurnP90Hours.ToString("0.0", Inv)),
                ("thieves", Num(p.Thieves)), ("thefts", Num(p.Thefts)), ("refusals", Num(p.RefusalWorlds)), ("forage_refusals", Num(p.ForageRefusalWorlds)),
                ("forage", Num(p.ForageWorlds)), ("covers", Num(p.CoverRepaidWorlds)), ("forage_covers", Num(p.ForageCoverRepaidWorlds)),
                ("worn", Num(p.WornOutWorlds)), ("insults", Num(p.Insults))));
        if (s.Failures.Count > 0)
            Line(o, Format(loc, "log.soak.crashed_list", ("seeds", List(loc, s.Failures.Select(f => Num(f.Seed))))));
        Line(o, Format(loc, "log.soak.written", ("dir", s.OutDir)));
    }

    /// <summary>
    /// Runs one world on the calling thread and writes its log. A world that throws becomes a failure
    /// in the summary (its file says where it stopped) instead of ending the whole soak.
    /// </summary>
    private static WorldResult RunWorld(ContentDb content, ulong seed, int years, string outDir, string zones, string days)
    {
        string path = Path.Combine(outDir, "world-" + Num(seed) + ".txt");
        Simulation? sim = null;
        try
        {
            sim = Simulation.Create(content, seed);
            Autoplay.Begin(sim, zones, days);
            var meter = new DaysMeter();
            var home = new HomeMeter();
            long ticks = StoryLog.TicksFor(content.World, years);
            for (long t = 0; t < ticks; t += DaysMeter.EveryTicks)
            {
                sim.Advance(Math.Min(DaysMeter.EveryTicks, ticks - t));
                meter.Sample(sim.World);
                home.Sample(sim.World);
            }
            meter.Finish();
            World w = sim.World;
            LogAnalysis analysis = StoryLog.Analyze(w);
            var log = new StringWriter(Inv);
            StoryLog.Render(w, analysis, log);
            File.WriteAllText(path, log.ToString(), Cli.Utf8);

            var sets = new List<string>(analysis.Stories.Count);
            var endings = new List<string>(analysis.Stories.Count);
            var links = new List<string>(analysis.Stories.Count);
            foreach (Story story in analysis.Stories)
            {
                sets.Add(SystemSetKey(story.Systems));
                endings.Add(w.Log.Get(story.TerminalEventId)?.Key ?? "");
                string link = "";
                foreach (long id in story.Chain)
                    if (w.Log.Get(id) is { System: SystemTag.Culture } e) { link = CultureLink(e.Key); break; }
                links.Add(link);
            }
            var losses = new List<string>();
            foreach (SimEvent e in w.Log.All)
                foreach (Effect f in e.Effects)
                    if (f.Kind is EffectKind.Departure or EffectKind.Death && w.Find(f.A) is { Named: true } who) losses.Add(who.SpeciesId);
            return new WorldResult(seed, w.Log.All.Count, sets, endings, null, links, losses, MeasureZones(w, analysis, Autoplay.ZonePlanFor(zones, seed), home),
                MeasureDays(w, analysis, Autoplay.DaysPlanFor(days, seed), meter));
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

    /// <summary>A world's zone measures: stories through a zone event, a zone the player laid out or a home-area line, and counts of the events zones touch.</summary>
    private static ZoneWorld MeasureZones(World w, LogAnalysis analysis, string plan, HomeMeter home)
    {
        int zoneStories = 0, laidOut = 0, homeStories = 0;
        foreach (Story story in analysis.Stories)
        {
            bool zone = false, laid = false, atHome = false;
            foreach (long id in story.Chain)
                if (w.Log.Get(id) is { } e)
                {
                    zone |= IsZoneStoryEvent(e.Key);
                    laid |= StoryLog.IsZoneEvent(e);
                    atHome |= IsHomeStoryEvent(e.Key);
                }
            if (zone) zoneStories++;
            if (laid) laidOut++;
            if (atHome) homeStories++;
        }
        int thefts = 0, unseen = 0, spoiled = 0, keptAway = 0, insults = 0, accidents = 0, dark = 0, worn = 0;
        int starving = 0, help = 0, ownWay = 0, hungry = 0, watch = 0;
        foreach (SimEvent e in w.Log.All)
        {
            switch (e.Key)
            {
                case "event.incident.stolen": case "event.incident.stolen_unwatched": thefts++; break;
                case "event.incident.stolen_unseen": thefts++; unseen++; break;
                case "event.incident.food_spoils": spoiled++; break;
                case "event.social.insulted": insults++; break;
                case "event.incident.work_accident": accidents++; break;
                case "event.incident.work_accident_tired": accidents++; worn++; break;
                case "event.incident.work_accident_dark": accidents++; dark++; break;
                case "event.incident.work_accident_tired_dark": accidents++; dark++; worn++; break;
                case "event.work.out_after_dark_starving": starving++; break;
                case "event.work.out_after_dark_help": help++; break;
                case "event.work.out_after_dark_own_way": case "event.work.out_after_dark_dislike":
                case "event.work.out_after_dark_mood": case "event.work.out_after_dark_ways": ownWay++; break;
                case "event.work.stayed_in_hungry": hungry++; break;
                case "event.work.stayed_in_watch": watch++; break;
            }
            if (e.Key.StartsWith("event.mood.kept_away", StringComparison.Ordinal)) keptAway++;
        }
        return new ZoneWorld(plan, zoneStories, laidOut, thefts, unseen, spoiled, keptAway, insults, accidents,
            homeStories, starving, help, ownWay, hungry, watch, home.WalksHome, home.WitnessTrait, home.WitnessWays, dark, worn);
    }

    /// <summary>A world's schedule measures: stories through the watch or overwork, thefts, refusals, covers that repaid a debt, wear, insults.</summary>
    private static DaysWorld MeasureDays(World w, LogAnalysis analysis, string plan, DaysMeter meter)
    {
        int watchStories = 0;
        foreach (Story story in analysis.Stories)
            foreach (long id in story.Chain)
                if (w.Log.Get(id) is { } e && StateApi.IsScheduleLine(e.Key)) { watchStories++; break; }
        int thieves = 0, thefts = 0, insults = 0;
        bool refused = false, coverRepaid = false, worn = false;
        foreach (SimEvent e in w.Log.All)
        {
            switch (e.Key)
            {
                case "event.incident.night_thief": thieves++; break;
                case "event.incident.stolen": case "event.incident.stolen_unseen": case "event.incident.stolen_unwatched": thefts++; break;
                case "event.social.insulted": insults++; break;
                case "event.needs.worn_out": worn = true; break;
            }
            if (e.Key.StartsWith("event.social.watch_refused", StringComparison.Ordinal)) refused = true;
            foreach (Effect f in e.Effects)
                if (f.Kind == EffectKind.DebtRepaid && e.Causes.Exists(c => w.Log.Get(c)?.Key == "event.work.covered_watch")) coverRepaid = true;
        }
        bool forage = false;
        foreach (Character c in w.Characters)
            if (Return.Sim.Systems.NatureNeeds.Get(c, Return.Sim.Systems.Purpose.NeedId) is { BondWork: "forage" }) forage = true;
        return new DaysWorld(plan, watchStories, meter, thieves, thefts, refused, coverRepaid, worn, forage, insults);
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
