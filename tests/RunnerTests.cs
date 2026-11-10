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
        // Where someone belongs varies by seed (ADR-0023), so only the start of the cast line is pinned.
        Assert.Contains(lines, l => l.StartsWith("  Mira (Human, of ", StringComparison.Ordinal) && l.Contains("): Protective, ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("  Ilex (Vampire, of ", StringComparison.Ordinal) && l.Contains("): Proud, ", StringComparison.Ordinal));
        // You are reborn as a rolled people with a rolled gift (ADR-0030): both Truths, so seed 1 is always a human with a gift.
        Assert.Contains(lines, l => l.StartsWith("  You (Human, reborn here; gift: ", StringComparison.Ordinal) && l.Contains("): Practical, ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("You were reborn here as a human, with a gift: ", StringComparison.Ordinal));
        Assert.Contains("Peoples and faiths", lines);
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
    public void StoryLogFoldsARunOfTheSameStepsWithDifferentPeople()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        w.Log = new EventLog();
        int Id(string key) => w.Characters.First(c => c.CastKey == key).Id;
        int ilex = Id("ilex");
        foreach (string key in new[] { "ilex", "bram", "nia", "mira", "oren" }) w.Get(Id(key)).Status = CharStatus.Member;

        w.Tick = 7_200;
        long last = w.Log.Emit(w, SystemTag.Needs, "event.needs.thirsting", new[] { ilex }, Array.Empty<long>(), null, EventArg.Char("a", ilex)).Id;
        foreach (string donor in new[] { "you", "bram", "nia", "mira" })
        {
            w.Tick += 300;
            long ask = w.Log.Emit(w, SystemTag.Social, "event.social.blood_request", new[] { ilex, Id(donor) }, new[] { last }, null,
                EventArg.Char("a", ilex), EventArg.Char("b", Id(donor))).Id;
            last = w.Log.Emit(w, SystemTag.Social, "event.social.blood_refused_hungry", new[] { ilex, Id(donor) }, new[] { ask }, null,
                EventArg.Char("a", ilex), EventArg.Char("b", Id(donor))).Id;
        }
        w.Tick += 300;
        w.Log.Emit(w, SystemTag.Social, "event.social.left_camp", new[] { ilex }, new[] { last },
            new[] { new Effect { Kind = EffectKind.Departure, A = ilex } }, EventArg.Char("a", ilex));

        var text = new StringWriter();
        StoryLog.Write(sim, text);
        Assert.Contains(
            "  Story 1\n" +
            "    1. Day 2, 00:00 — Ilex is thirsting for blood.\n" +
            "    2. Day 2, 01:00 — Ilex asked you for blood.\n" +
            "    3. Day 2, 01:00 — You, hungry yourself, refused to give Ilex blood.\n" +
            "      … and the same with Bram, Nia, Mira, until Day 2, 04:00.\n" +
            "    4. Day 2, 05:00 — Ilex left the camp.\n", text.ToString());
    }

    [Fact]
    public void FoldedListsSayYouMidSentenceAndWhatHappenedFoldsToo()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        w.Log = new EventLog();
        int Id(string key) => w.Characters.First(c => c.CastKey == key).Id;
        int ilex = Id("ilex");
        w.Tick = 7_200;
        long last = w.Log.Emit(w, SystemTag.Needs, "event.needs.thirsting", new[] { ilex }, Array.Empty<long>(), null, EventArg.Char("a", ilex)).Id;
        foreach (string donor in new[] { "bram", "you", "nia", "mira" })
        {
            w.Tick += 300;
            long ask = w.Log.Emit(w, SystemTag.Social, "event.social.blood_request", new[] { ilex, Id(donor) }, new[] { last }, null,
                EventArg.Char("a", ilex), EventArg.Char("b", Id(donor))).Id;
            last = w.Log.Emit(w, SystemTag.Social, "event.social.blood_refused", new[] { ilex, Id(donor) }, new[] { ask }, null,
                EventArg.Char("a", ilex), EventArg.Char("b", Id(donor))).Id;
        }
        w.Tick += 300;
        w.Log.Emit(w, SystemTag.Social, "event.social.left_camp", new[] { ilex }, new[] { last },
            new[] { new Effect { Kind = EffectKind.Departure, A = ilex } }, EventArg.Char("a", ilex));

        var text = new StringWriter();
        StoryLog.Write(sim, text);
        string log = text.ToString();
        Assert.Contains("      … and the same with you, Nia, Mira, until Day 2, 04:00.\n", log);
        Assert.Contains("  Day 2, 01:00 — Bram refused to give Ilex blood.\n    … and the same with you, Nia, Mira, until Day 2, 04:00.\n", log);
    }

    [Fact]
    public void StoryLogFoldsOnlyStepsAboutOnePersonAtATime()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        w.Log = new EventLog();
        int Id(string key) => w.Characters.First(c => c.CastKey == key).Id;
        int ilex = Id("ilex");
        w.Tick = 7_200;
        long last = w.Log.Emit(w, SystemTag.Needs, "event.needs.thirsting", new[] { ilex }, Array.Empty<long>(), null, EventArg.Char("a", ilex)).Id;
        // The chain opens with a refusal, so a two-step unit starting there would straddle two people.
        last = w.Log.Emit(w, SystemTag.Social, "event.social.blood_refused", new[] { ilex, Id("wren") }, new[] { last }, null,
            EventArg.Char("a", ilex), EventArg.Char("b", Id("wren"))).Id;
        foreach (string donor in new[] { "bram", "mira", "oren" })
        {
            w.Tick += 300;
            long ask = w.Log.Emit(w, SystemTag.Social, "event.social.blood_request", new[] { ilex, Id(donor) }, new[] { last }, null,
                EventArg.Char("a", ilex), EventArg.Char("b", Id(donor))).Id;
            last = w.Log.Emit(w, SystemTag.Social, "event.social.blood_refused", new[] { ilex, Id(donor) }, new[] { ask }, null,
                EventArg.Char("a", ilex), EventArg.Char("b", Id(donor))).Id;
        }
        w.Log.Emit(w, SystemTag.Social, "event.social.left_camp", new[] { ilex }, new[] { last },
            new[] { new Effect { Kind = EffectKind.Departure, A = ilex } }, EventArg.Char("a", ilex));

        var text = new StringWriter();
        StoryLog.Write(sim, text);
        Assert.Contains(
            "    2. Day 2, 00:00 — Wren refused to give Ilex blood.\n" +
            "    3. Day 2, 01:00 — Ilex asked Bram for blood.\n" +
            "    4. Day 2, 01:00 — Bram refused to give Ilex blood.\n" +
            "      … and the same with Mira, Oren, until Day 2, 03:00.\n" +
            "    5. Day 2, 03:00 — Ilex left the camp.\n", text.ToString());
    }

    [Fact]
    public void StoryLogListsNotableEventsAndStories()
    {
        // The sim is never stepped and starts from an empty history, so the log holds exactly the
        // events below, whatever the systems do.
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        w.Log = new EventLog();
        int you = w.Characters.First(c => c.Otherworlder).Id;
        int mira = w.Characters.First(c => c.CastKey == "mira").Id;
        int oren = w.Characters.First(c => c.CastKey == "oren").Id;

        // A player decision: a World event with no effect, in no story, so not notable.
        w.Tick = 10;
        w.Policy = RationPolicy.SelfFirst;
        w.Log.Emit(w, SystemTag.World, "event.world.ration_policy", new[] { you }, Array.Empty<long>(), null,
            EventArg.Key("policy", SetRationPolicy.PolicyKey(RationPolicy.SelfFirst)));
        w.Tick = 8 * 300;
        SimEvent rain = w.Log.Emit(w, SystemTag.Incident, "incident.rain", Array.Empty<int>(), Array.Empty<long>());
        w.Tick = 9 * 300;
        SimEvent soaked = w.Log.Emit(w, SystemTag.Needs, "thought.soaked", new[] { mira }, new[] { rain.Id });
        w.Tick = 7_200 + 9 * 300;
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
    public void StoryLogHandlesAnEmptyCampAndAnEmptyHistory()
    {
        // Not stepped, history cleared: the camp is empty and nothing has happened, whatever the systems do.
        var sim = Simulation.Create(TestContent.Db, 2);
        World w = sim.World;
        w.Log = new EventLog();
        w.Tick = 2 * 7_200;
        foreach (Character c in w.Characters) c.Status = CharStatus.Dead;
        var text = new StringWriter();
        StoryLog.Write(sim, text);
        string log = text.ToString();
        Assert.Contains("  Nobody lives there any more.\n", log);
        Assert.Contains("What happened\n-------------\n  Nothing notable happened.\n", log);
        Assert.Contains("Stories\n-------\n  No stories this time.\n", log);
        Assert.Contains("  Mira (Human, of ", log);
        Assert.Contains(". Dead.\n", log);
        Assert.Contains("Years simulated: 0.05\n", log);
        Assert.EndsWith("Events logged: 0. Notable: 0. Stories: 0.\n", log);
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
    [InlineData("soak")]
    [InlineData("soak --worlds 3 --years 1")]
    [InlineData("soak --worlds 3 --out unused")]
    [InlineData("soak --worlds 0 --years 1 --out unused")]
    [InlineData("soak --worlds 3 --years 1 --out unused --parallel 0")]
    [InlineData("soak --worlds 3 --years 1 --out unused --parallel many")]
    [InlineData("soak --worlds 3 --years 1 --out unused --seed 2")]
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
    public void SoakIsThreadCountIndependent()
    {
        string serial = TempDir(), threaded = TempDir();
        try
        {
            // One soak through the CLI and one through Soak.Run, so both entry points must agree too.
            string printed = RunCli("soak", "--worlds", "3", "--years", "1", "--out", serial, "--parallel", "1");
            SoakSummary summary = Soak.Run(new SoakOptions { Worlds = 3, Years = 1, OutDir = threaded, Parallel = 3 });

            string[] expected = { "world-1.txt", "world-2.txt", "world-3.txt" };
            Assert.Equal(expected, WorldFiles(serial));
            Assert.Equal(expected, WorldFiles(threaded));
            foreach (string name in expected)
                Assert.Equal(File.ReadAllBytes(Path.Combine(serial, name)), File.ReadAllBytes(Path.Combine(threaded, name)));
            // A world's file is exactly what `run` prints for its seed.
            Assert.Equal(Encoding.UTF8.GetBytes(Seed1Year1.Value), File.ReadAllBytes(Path.Combine(serial, "world-1.txt")));

            Assert.Equal(3, summary.Worlds);
            Assert.Equal(new ulong[] { 1, 2, 3 }, summary.PerWorld.Select(p => p.Seed));
            Assert.Empty(summary.Failures);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(serial, "summary.json")));
            JsonElement root = doc.RootElement;
            foreach (string field in new[] { "worlds", "years", "stories", "storiesPerWorld", "bySystemSet", "topTerminalKinds", "elapsedSeconds" })
                Assert.True(root.TryGetProperty(field, out _), "summary.json has no " + field);
            Assert.Equal(3, root.GetProperty("worlds").GetInt32());
            Assert.Equal(1, root.GetProperty("years").GetInt32());
            Assert.Equal(summary.Stories, root.GetProperty("stories").GetInt32());
            Assert.Contains("Worlds: 3 (seeds 1–3)", printed);
            Assert.DoesNotContain("⟦", printed);
        }
        finally
        {
            Directory.Delete(serial, true);
            Directory.Delete(threaded, true);
        }
    }

    [Fact]
    public void SoakSummaryTalliesSystemSetsAndEndings()
    {
        Assert.Equal("Incident+Needs+Social", Soak.SystemSetKey(new[] { SystemTag.Social, SystemTag.Incident, SystemTag.Needs, SystemTag.Social }));
        Assert.Equal(Soak.SystemSetKey(new[] { SystemTag.Needs, SystemTag.Work }), Soak.SystemSetKey(new[] { SystemTag.Work, SystemTag.Needs }));

        string dir = TempDir();
        try
        {
            var results = new[]
            {
                new Soak.WorldResult(1, 40, new[] { "Needs+Social", "Incident+Needs+Social" }, new[] { "event.social.grudge_formed", "event.needs.death" }, null),
                new Soak.WorldResult(2, 30, new[] { "Incident+Needs+Social", "Combat+Social" }, new[] { "event.social.grudge_formed", "event.combat.rescue" }, null),
                new Soak.WorldResult(3, 20, Array.Empty<string>(), Array.Empty<string>(), new SoakFailure(3, 99, "InvalidOperationException: boom")),
            };
            var options = new SoakOptions { Worlds = 3, Years = 1, OutDir = dir, Parallel = 2 };
            SoakSummary s = Soak.Summarize(options, dir, results, TimeSpan.FromSeconds(1.234));

            Assert.Equal(4, s.Stories);
            Assert.Equal(1.33, s.StoriesPerWorld);
            Assert.Equal(1.23, s.ElapsedSeconds);
            Assert.Equal(new[] { new Tally("Combat+Social", 1), new Tally("Incident+Needs+Social", 2), new Tally("Needs+Social", 1) }, s.BySystemSet);
            Assert.Equal(new[] { new Tally("event.social.grudge_formed", 2), new Tally("event.combat.rescue", 1), new Tally("event.needs.death", 1) }, s.TopTerminalKinds);
            Assert.Equal(new ulong[] { 3 }, s.Failures.Select(f => f.Seed));

            string path = Path.Combine(dir, "summary.json");
            Soak.WriteSummaryJson(s, path);
            string json = File.ReadAllText(path);
            Assert.Contains("\"Incident+Needs+Social\": 2", json);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.Equal(new[] { "Combat+Social", "Incident+Needs+Social", "Needs+Social" },
                root.GetProperty("bySystemSet").EnumerateObject().Select(p => p.Name));
            Assert.Equal(new[] { "event.social.grudge_formed", "event.combat.rescue", "event.needs.death" },
                root.GetProperty("topTerminalKinds").EnumerateObject().Select(p => p.Name));
            Assert.Equal(2, root.GetProperty("topTerminalKinds").GetProperty("event.social.grudge_formed").GetInt32());
            Assert.Equal(4, root.GetProperty("stories").GetInt32());
            Assert.Equal(1.33, root.GetProperty("storiesPerWorld").GetDouble());
            Assert.Equal(3UL, root.GetProperty("failures")[0].GetProperty("seed").GetUInt64());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
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

    private static string[] WorldFiles(string dir) =>
        Directory.GetFiles(dir, "world-*.txt").Select(f => Path.GetFileName(f)).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    private static string TempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "return-runner-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
