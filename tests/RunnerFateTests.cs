using System.Text;
using System.Text.Json;
using Return.Runner;
using Return.Sim;

namespace Return.Tests;

/// <summary>
/// The runner's side of the player design (ADR-0030): --people, --gift and --fates; a return as a divider in
/// What happened and the Lost futures section after the log; soak runs with returns byte-identical whatever the thread
/// count; and the fate summary in summary.json.
/// </summary>
public class RunnerFateTests
{
    private static (int Code, string Out, string Err) Cli(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        int code = Return.Runner.Cli.Run(args, stdout, stderr);
        return (code, stdout.ToString(), stderr.ToString());
    }

    private static string TempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "return-fates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void AFatesRunShowsTheReturnAndItsLostFuture()
    {
        (int code, string log, string err) = Cli("run", "--seed", "2", "--years", "1", "--fates", "--people", "fae", "--gift", "familiar_faces");
        Assert.True(code == 0, err);
        string[] lines = log.Split('\n');
        Assert.Contains("You were reborn here as one of the fae, with a gift: Familiar faces.", lines);
        Assert.Contains("Your people and gift were chosen for testing, not rolled.", lines);
        Assert.Contains(lines, l => l.StartsWith("Lives lost: ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("  You (Fae, reborn here; gift: Familiar faces): ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("~~ You died on Day ", StringComparison.Ordinal) && l.Contains(", and the world went back to Day ", StringComparison.Ordinal));
        int lost = Array.IndexOf(lines, "Lost futures");
        Assert.True(lost > Array.IndexOf(lines, "Stories"), "the Lost futures section follows the log's stories");
        Assert.Contains(lines.Skip(lost), l => l.Contains("(soak) You fell dead.", StringComparison.Ordinal));
        Assert.DoesNotContain("⟦", log);
        // A run without a return writes no Lost futures section.
        (_, string quiet, _) = Cli("run", "--seed", "4", "--years", "1", "--people", "human", "--gift", "none");
        if (!quiet.Contains("Lives lost", StringComparison.Ordinal)) Assert.DoesNotContain("\nLost futures\n", quiet);
    }

    [Fact]
    public void ChoosingAPeopleOrGiftThatDoesNotExistIsAUsageError()
    {
        (int code, _, string err) = Cli("run", "--seed", "1", "--years", "1", "--people", "dragon");
        Assert.Equal(2, code);
        Assert.Contains("--people 'dragon'", err);
        (code, _, err) = Cli("soak", "--worlds", "1", "--years", "1", "--out", TempDir(), "--gift", "wishes");
        Assert.Equal(2, code);
        Assert.Contains("--gift 'wishes'", err);
        (code, _, err) = Cli("run", "--seed", "1", "--years", "1", "--fates", "--fates");
        Assert.Equal(2, code);
        Assert.Contains("given twice", err);
    }

    [Fact]
    public void AFatesSoakIsThreadCountIndependentAndCountsItsReturns()
    {
        string serial = TempDir(), threaded = TempDir();
        try
        {
            (int code, string printed, string err) = Cli("soak", "--worlds", "4", "--years", "1", "--out", serial, "--parallel", "1", "--fates");
            Assert.True(code == 0, err);
            SoakSummary summary = Soak.Run(new SoakOptions { Worlds = 4, Years = 1, OutDir = threaded, Parallel = 4, Setup = new RunSetup(null, Fates: true) });
            foreach (string name in new[] { "world-1.txt", "world-2.txt", "world-3.txt", "world-4.txt" })
                Assert.Equal(File.ReadAllBytes(Path.Combine(serial, name)), File.ReadAllBytes(Path.Combine(threaded, name)));
            Assert.Equal(4, summary.Fates.ReturnWorlds); // --fates: every world returns
            Assert.True(summary.Fates.Returns >= 4);
            Assert.Equal(summary.Fates.Returns, summary.Fates.LostFutures);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(serial, Soak.SummaryFile)));
            JsonElement fates = doc.RootElement.GetProperty("fates");
            Assert.Equal(4, fates.GetProperty("returnWorlds").GetInt32());
            foreach (string field in new[] { "naturalReturns", "averageDaysBack", "deepPermille", "lostFuturesWithStory", "fateOrGiftStories", "crossingStories", "companionDeathsPerWorldYear", "giftTargetWorlds" })
                Assert.True(fates.TryGetProperty(field, out _), "summary.json's fates has no " + field);
            Assert.Contains("Returns: ", printed);
            Assert.DoesNotContain("⟦", printed);
        }
        finally
        {
            Directory.Delete(serial, true);
            Directory.Delete(threaded, true);
        }
    }

    /// <summary>
    /// A story crosses a return only if its chain runs through one (the return's line, which every memory and familiar face
    /// cites); a gift's story in a life that never died carries a gift event but crosses nothing. Seed 13: you are a djinn
    /// with Fortune's favourite, never die in the year, and the harm your luck turns is on a story chain.
    /// </summary>
    [Fact]
    public void OnlyAStoryThroughAReturnCrossesOne()
    {
        Simulation quiet = StoryLog.Simulate(Return.Tests.TestContent.Rolled, 13, 1);
        Assert.Equal(0, quiet.World.Fate.Returns);
        FateTally t = SoakFates.Tally(quiet.World, StoryLog.Analyze(quiet.World).Stories);
        Assert.True(t.FateOrGiftStories > 0, "seed 13's luck should reach a story");
        Assert.Equal(0, t.CrossingStories);

        Simulation fated = StoryLog.Simulate(Return.Tests.TestContent.Rolled, 3, 1);
        Assert.True(fated.World.Fate.Returns > 0);
        IReadOnlyList<Return.Sim.Stories.Story> stories = StoryLog.Analyze(fated.World).Stories;
        FateTally f = SoakFates.Tally(fated.World, stories);
        Assert.Equal(stories.Count(s => s.Chain.Any(id => fated.World.Log.Get(id)!.Key == Return.Sim.Systems.Fate.ReturnedKey)), f.CrossingStories);
        Assert.InRange(f.CrossingStories, 1, f.FateOrGiftStories);
    }

    /// <summary>A gift's lines show in the log whatever system tells them: a familiar face knowing you is Social, yet it is the gift at work.</summary>
    [Fact]
    public void EveryGiftLineIsInTheLog()
    {
        var sim = Simulation.Create(Return.Tests.TestContent.Rolled, 9, new RebirthChoice("human", Return.Sim.Systems.Gifts.FamiliarId));
        sim.Advance(7_200 + 6 * 300);
        World dying = sim.World;
        Character mira = dying.Characters.First(c => c.CastKey == "mira");
        Return.Sim.Systems.Relations.ChangeOpinion(dying, mira, dying.PlayerId, 80 - Return.Sim.Systems.Relations.Opinion(dying, mira.Id, dying.PlayerId), 0);
        Assert.True(sim.ForceDeath());
        sim.Step();
        World w = sim.World;
        Return.Sim.Events.SimEvent knew = w.Log.All.Single(e => e.Key == "event.gift.familiar.knew_you");
        Assert.Equal(Return.Sim.Events.SystemTag.Social, knew.System);
        Assert.Contains(knew, StoryLog.Analyze(w).Notable);
    }

    [Fact]
    public void TheForcedDeathComesOnADayFromTenToThirtyFiveHashedFromTheSeed()
    {
        var def = TestContent.Db.World;
        var days = new HashSet<int>();
        for (ulong seed = 1; seed <= 300; seed++)
        {
            long tick = RunSetup.FateTick(seed, def);
            int day = (int)(tick / def.TicksPerDay) + 1;
            Assert.InRange(day, 10, 35);
            days.Add(day);
            Assert.Equal(tick, RunSetup.FateTick(seed, def));
        }
        Assert.Equal(26, days.Count);
        Simulation sim = StoryLog.Simulate(TestContent.Db, 7, 1, new RunSetup(null, Fates: true));
        Assert.Contains(sim.World.Fate.LostFutures, f => f.Lines[f.DeathLine].Key == Return.Sim.Systems.Fate.ForcedDeathKey
            && f.DeathTick == RunSetup.FateTick(7, def));
        Assert.Equal(Encoding.UTF8.GetBytes(Cli("run", "--seed", "7", "--years", "1", "--fates", "--people", "human", "--gift", "none").Out),
            Encoding.UTF8.GetBytes(Cli("run", "--seed", "7", "--years", "1", "--people", "human", "--gift", "none", "--fates").Out));
    }
}
