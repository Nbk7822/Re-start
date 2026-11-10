using System.Globalization;
using System.Text;
using Return.Sim;
using Return.Sim.Content;

namespace Return.Runner;

/// <summary>
/// The command line. `run` prints one world's story log; `soak` runs many worlds, writes their
/// logs and a summary, and prints a short report. Exit codes: 0 done; 1 failed (content would not
/// load, a file could not be written, a soak world crashed); 2 bad arguments (usage goes to stderr).
/// </summary>
public static class Cli
{
    public const string Usage =
        "usage: return-runner run --seed <n> --years <y> [--out <file>] [--people <id>] [--gift <id|none>] [--fates] [--plan none|beds|camp|mixed]\n" +
        "           [--zones none|dry|home|short|both|mixed] [--days default|nowatch|nightwatcher|mixed]\n" +
        "       return-runner soak --worlds <n> --years <y> --out <dir> [--parallel <p>] [--people <id>] [--gift <id|none>] [--fates]\n" +
        "           [--plan none|beds|camp|mixed] [--zones none|dry|home|short|both|mixed] [--days default|nowatch|nightwatcher|mixed]\n" +
        "       --people and --gift choose who you are reborn as, for testing (the log says so); --fates kills you once\n" +
        "       in your first timeline, on a day from 10 to 35 hashed from the seed, so every world shows a return.\n" +
        "       --plan gives the stand-in player a building plan: none, beds, camp, or mixed (one of the three by seed).\n" +
        "       --zones lays out its zones and --days sets the camp's days; mixed picks one plan per world by seed.\n";

    /// <summary>UTF-8 without a byte-order mark, for every file the runner writes.</summary>
    internal static readonly Encoding Utf8 = new UTF8Encoding(false);

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            stdout.Write(Usage);
            return 0;
        }
        try
        {
            return args[0] switch
            {
                "run" => RunWorld(ParseRun(args), stdout),
                "soak" => RunSoak(ParseSoak(args), stdout),
                _ => throw new UsageException($"unknown command '{args[0]}'"),
            };
        }
        catch (UsageException ex)
        {
            stderr.Write("error: " + ex.Message + "\n" + Usage);
            return 2;
        }
        catch (Exception ex) when (ex is RunFailure or IOException or UnauthorizedAccessException)
        {
            stderr.Write("error: " + ex.Message + "\n");
            return 1;
        }
    }

    /// <summary>Writes text as UTF-8 without a BOM, creating the folder if needed. Returns the full path.</summary>
    internal static string WriteFile(string file, string text)
    {
        string path = Path.GetFullPath(file);
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, text, Utf8);
        return path;
    }

    private static int RunWorld(RunArgs a, TextWriter stdout)
    {
        ContentDb content = LoadContent();
        CheckChoice(content, a.Setup.Choice);
        Simulation sim = StoryLog.Simulate(content, a.Seed, a.Years, a.Setup, a.Plan, a.Zones, a.Days);
        var log = new StringWriter(CultureInfo.InvariantCulture);
        StoryLog.Write(sim, log);
        if (a.OutFile == null)
        {
            stdout.Write(log.ToString());
            return 0;
        }
        string path = WriteFile(a.OutFile, log.ToString());
        LocText.Line(stdout, LocText.Format(content.Loc, "log.run.wrote", ("seed", LocText.Num(a.Seed)), ("path", path)));
        return 0;
    }

    private static int RunSoak(SoakOptions options, TextWriter stdout)
    {
        ContentDb content = LoadContent();
        CheckChoice(content, options.Setup.Choice);
        SoakSummary summary = Soak.Run(options, content);
        Soak.Report(summary, content.Loc, stdout);
        return summary.Failures.Count == 0 ? 0 : 1;
    }

    /// <summary>Content that fails to load is reported as a plain error; a failure mid-run keeps its stack trace.</summary>
    private static ContentDb LoadContent()
    {
        try
        {
            return ContentDb.Load(ContentDb.FindContentDir());
        }
        catch (ContentException ex)
        {
            throw new RunFailure(ex.Message, ex);
        }
    }

    private sealed record RunArgs(ulong Seed, int Years, string? OutFile, RunSetup Setup, string Plan, string Zones, string Days);

    private static readonly string[] Flags = { "--fates" };

    private static RunArgs ParseRun(string[] args)
    {
        Dictionary<string, string> o = Options(args, "--seed", "--years", "--out", "--people", "--gift", "--fates", "--plan", "--zones", "--days");
        return new RunArgs(Seed(o, "--seed"), AtLeastOne(o, "--years"), o.GetValueOrDefault("--out"), Setup(o), Plan(o), ZonePlan(o), DaysPlan(o));
    }

    /// <summary>The stand-in player's building plan (ADR-0027); "none" when not given.</summary>
    private static string Plan(Dictionary<string, string> options)
    {
        string plan = options.GetValueOrDefault("--plan") ?? "none";
        return Autoplay.Plans.Contains(plan) ? plan : throw new UsageException($"--plan must be one of {string.Join(", ", Autoplay.Plans)}, not '{plan}'");
    }

    private static SoakOptions ParseSoak(string[] args)
    {
        Dictionary<string, string> o = Options(args, "--worlds", "--years", "--out", "--parallel", "--people", "--gift", "--fates", "--plan", "--zones", "--days");
        return new SoakOptions
        {
            Worlds = AtLeastOne(o, "--worlds"),
            Years = AtLeastOne(o, "--years"),
            OutDir = Required(o, "--out"),
            Parallel = o.ContainsKey("--parallel") ? AtLeastOne(o, "--parallel") : Environment.ProcessorCount,
            Setup = Setup(o),
            Plan = Plan(o),
            Zones = ZonePlan(o),
            Days = DaysPlan(o),
        };
    }

    /// <summary>Who you are reborn as, if chosen for testing, and whether --fates forces a death (ADR-0030).</summary>
    private static RunSetup Setup(Dictionary<string, string> o)
    {
        string? people = o.GetValueOrDefault("--people"), gift = o.GetValueOrDefault("--gift");
        RebirthChoice? choice = people == null && gift == null ? null : new RebirthChoice(people, gift);
        return new RunSetup(choice, o.ContainsKey("--fates"));
    }

    /// <summary>A chosen people or gift must exist; a bad one is a usage error, not a crash mid-soak.</summary>
    private static void CheckChoice(ContentDb content, RebirthChoice? choice)
    {
        if (choice?.People is { } p && content.Rebirth.People(p) == null)
            throw new UsageException($"--people '{p}' is not a people you can be reborn as ({string.Join(", ", content.Rebirth.Peoples.Select(x => x.Species))})");
        if (choice?.Gift is { } g && g != RebirthChoice.NoGift && content.TryGift(g) == null)
            throw new UsageException($"--gift '{g}' is not a gift ({string.Join(", ", content.Gifts.Select(x => x.Id))}, or none)");
    }

    /// <summary>The stand-in player's schedule plan (ADR-0029); default when not given.</summary>
    private static string DaysPlan(Dictionary<string, string> options)
    {
        string plan = options.GetValueOrDefault("--days") ?? "default";
        return Autoplay.DaysPlans.Contains(plan)
            ? plan
            : throw new UsageException($"--days must be one of {string.Join(", ", Autoplay.DaysPlans)}, not '{plan}'");
    }

    /// <summary>The stand-in player's zone plan (ADR-0028); none when not given.</summary>
    private static string ZonePlan(Dictionary<string, string> options)
    {
        string plan = options.GetValueOrDefault("--zones") ?? "none";
        return Autoplay.ZonePlans.Contains(plan)
            ? plan
            : throw new UsageException($"--zones must be one of {string.Join(", ", Autoplay.ZonePlans)}, not '{plan}'");
    }

    /// <summary>Reads the "--name value" pairs (and bare flags such as --fates) after the command. Every name must be allowed and given once.</summary>
    private static Dictionary<string, string> Options(string[] args, params string[] allowed)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 1; i < args.Length; i += 2)
        {
            string name = args[i];
            if (Array.IndexOf(allowed, name) < 0) throw new UsageException($"unexpected argument '{name}'");
            if (options.ContainsKey(name)) throw new UsageException($"{name} is given twice");
            if (Array.IndexOf(Flags, name) >= 0)
            {
                options[name] = "";
                i--;
                continue;
            }
            if (i + 1 >= args.Length || args[i + 1].Length == 0 || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                throw new UsageException($"{name} needs a value");
            options[name] = args[i + 1];
        }
        return options;
    }

    private static string Required(Dictionary<string, string> options, string name) =>
        options.TryGetValue(name, out string? value) ? value : throw new UsageException($"{name} is required");

    private static ulong Seed(Dictionary<string, string> options, string name)
    {
        string value = Required(options, name);
        return ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed)
            ? seed
            : throw new UsageException($"{name} must be a whole number, not '{value}'");
    }

    private static int AtLeastOne(Dictionary<string, string> options, string name)
    {
        string value = Required(options, name);
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n >= 1
            ? n
            : throw new UsageException($"{name} must be a whole number of at least 1, not '{value}'");
    }

    private sealed class UsageException : Exception
    {
        public UsageException(string message) : base(message) { }
    }

    private sealed class RunFailure : Exception
    {
        public RunFailure(string message, Exception inner) : base(message, inner) { }
    }
}
