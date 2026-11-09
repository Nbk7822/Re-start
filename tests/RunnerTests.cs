using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Return.Runner;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Events;

namespace Return.Tests;

public class RunnerTests
{
    /// <summary>One `run --seed 1 --years 1` through the CLI, shared by the tests that only read it.</summary>
    private static readonly Lazy<string> Seed1Year1 = new(() => RunCli("run", "--seed", "1", "--years", "1"));

    [Fact]
    public void RunPrintsHeaderAndStoriesSection()
    {
        string log = Seed1Year1.Value;
        string[] lines = log.Split('\n');
        Assert.Contains("Seed 1", log);
        Assert.Contains("Seed 1", lines);
        Assert.Contains("Stories", lines);
        Assert.Contains("Years simulated: 1", lines);
        Assert.Contains(lines, l => l.StartsWith("  Mira (Human): Protective, ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("  Ilex (Vampire): Proud, ", StringComparison.Ordinal));
        Assert.Contains("The camp at the end", lines);
        Assert.DoesNotContain("⟦", log);
        Assert.DoesNotContain("\r", log);
        Assert.EndsWith("\n", log);
    }

    [Fact]
    public void RunIsDeterministic()
    {
        string dir = TempDir();
        try
        {
            string file = Path.Combine(dir, "nested", "seed-1.txt");
            string confirmation = RunCli("run", "--seed", "1", "--years", "1", "--out", file);
            Assert.Contains(file, confirmation);
            // A second seed-1 run, written to a file this time: the same bytes as the first run's stdout.
            Assert.Equal(Encoding.UTF8.GetBytes(Seed1Year1.Value), File.ReadAllBytes(file));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void StoryLogListsNotableEventsAndStories()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        int mira = w.Characters.First(c => c.CastKey == "mira").Id;
        int oren = w.Characters.First(c => c.CastKey == "oren").Id;

        // A player decision: a World event with no effect, in no story, so not notable.
        sim.Enqueue(new SetRationPolicy(10, RationPolicy.SelfFirst));
        sim.Advance(8 * 300);
        SimEvent rain = w.Log.Emit(w, SystemTag.Incident, "incident.rain", Array.Empty<int>(), Array.Empty<long>());
        sim.Advance(300);
        SimEvent soaked = w.Log.Emit(w, SystemTag.Needs, "thought.soaked", new[] { mira }, new[] { rain.Id });
        sim.Advance(7_200);
        w.Log.Emit(w, SystemTag.Social, "thought.insulted", new[] { mira, oren }, new[] { soaked.Id },
            new[] { new Effect { Kind = EffectKind.GrudgeFormed, A = mira, B = oren } });
        // A persistent change on its own: notable, but no story.
        w.Log.Emit(w, SystemTag.Social, "thought.cared_for", new[] { oren }, Array.Empty<long>(),
            new[] { new Effect { Kind = EffectKind.DebtFormed, A = oren, B = mira } });

        var text = new StringWriter();
        StoryLog.Write(sim, text);
        string log = text.ToString();

        Assert.Contains(
            "What happened\n" +
            "-------------\n" +
            "  Day 1, 08:00 — Rain\n" +
            "  Day 1, 09:00 — Soaked through\n" +
            "\n" +
            "  Day 2, 09:00 — Was insulted\n" +
            "  Day 2, 09:00 — Someone tended my wounds\n" +
            "\n", log);
        Assert.Contains(
            "Stories\n" +
            "-------\n" +
            "  Story 1\n" +
            "    1. Day 1, 08:00 — Rain\n" +
            "    2. Day 1, 09:00 — Soaked through\n" +
            "    3. Day 2, 09:00 — Was insulted\n" +
            "    Systems: Incident → Needs → Social\n" +
            "    People: Mira, Oren\n" +
            "\n", log);
        Assert.DoesNotContain("You decided", log);
        Assert.DoesNotContain("No stories this time.", log);
        Assert.EndsWith("Events logged: 5. Notable: 4. Stories: 1.\n", log);
        Assert.Contains("  When food runs short, you eat first.\n", log);
        Assert.DoesNotContain("⟦", log);
    }

    [Fact]
    public void StoryLogHandlesAnEmptyCamp()
    {
        var sim = Simulation.Create(TestContent.Db, 2);
        foreach (Character c in sim.World.Characters) c.Status = CharStatus.Dead;
        sim.Advance(14_400);
        var text = new StringWriter();
        StoryLog.Write(sim, text);
        string log = text.ToString();
        Assert.Contains("  Nobody lives there any more.\n", log);
        Assert.Contains("  Nothing notable happened.\n", log);
        Assert.Contains("Stories\n-------\n  No stories this time.\n", log);
        Assert.Contains("  Mira (Human): Protective, ", log);
        Assert.DoesNotContain("⟦", log);
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("run")]
    [InlineData("run --seed 1")]
    [InlineData("run --years 1")]
    [InlineData("run --seed x --years 1")]
    [InlineData("run --seed -1 --years 1")]
    [InlineData("run --seed 1 --years 0")]
    [InlineData("run --seed 1 --years 1 --parallel 2")]
    [InlineData("run --seed 1 --seed 2 --years 1")]
    [InlineData("run --seed 1 --years")]
    [InlineData("run --seed --years 1")]
    [InlineData("run --seed 1 --years 1 stray")]
    public void BadArgumentsPrintUsageAndExitWith2(string argLine)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        string[] args = argLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, Cli.Run(args, stdout, stderr));
        Assert.Contains("usage: return-runner run", stderr.ToString());
        Assert.Equal("", stdout.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("--help")]
    [InlineData("-h")]
    public void HelpPrintsUsageAndExitsWith0(string argLine)
    {
        var stdout = new StringWriter();
        string[] args = argLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(0, Cli.Run(args, stdout, new StringWriter()));
        Assert.Contains("usage: return-runner run", stdout.ToString());
    }

    [Fact]
    public void LocCoversEveryKeyTheRunnerUses()
    {
        var used = new SortedSet<string>(StringComparer.Ordinal);
        var literal = new Regex("\"(log\\.[a-z0-9_.]+)\"");
        string sep = Path.DirectorySeparatorChar.ToString();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestPaths.Root, "tools", "Return.Runner"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(sep + "obj" + sep) || file.Contains(sep + "bin" + sep)) continue;
            foreach (Match m in literal.Matches(File.ReadAllText(file)))
                if (!m.Groups[1].Value.EndsWith(".", StringComparison.Ordinal)) used.Add(m.Groups[1].Value);
        }
        foreach (SystemTag t in Enum.GetValues<SystemTag>()) used.Add("log.system." + Names.Snake(t.ToString()));

        var missing = used.Where(k => !TestContent.Db.Loc.Has(k)).ToList();
        Assert.True(missing.Count == 0, "Missing from content/loc/en.log.json: " + string.Join(", ", missing));

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestPaths.Root, "content", "loc", "en.log.json")));
        var defined = doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.All(defined, k => Assert.StartsWith("log.", k));
        var unused = defined.Where(k => !used.Contains(k)).ToList();
        Assert.True(unused.Count == 0, "Unused keys in content/loc/en.log.json: " + string.Join(", ", unused));
    }

    private static string RunCli(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        int code = Cli.Run(args, stdout, stderr);
        Assert.True(code == 0, $"return-runner {string.Join(' ', args)} exited {code}: {stderr}");
        return stdout.ToString();
    }

    private static string TempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "return-runner-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
