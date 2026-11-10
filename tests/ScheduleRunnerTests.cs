using Return.Runner;

namespace Return.Tests;

/// <summary>The stand-in player's schedule plans (ADR-0029): what each seed runs under and what each plan's log says.</summary>
public class ScheduleRunnerTests
{
    [Fact]
    public void MixedPlansVaryApartFromTheRationPolicyAndTheZones()
    {
        Assert.Equal("default", Autoplay.DaysPlanFor("mixed", 1));       // (1 / 36) % 3 = 0
        Assert.Equal("nowatch", Autoplay.DaysPlanFor("mixed", 36));      // 1
        Assert.Equal("nightwatcher", Autoplay.DaysPlanFor("mixed", 72)); // 2
        Assert.Equal("default", Autoplay.DaysPlanFor("mixed", 108));     // 0 again
        Assert.Equal("nowatch", Autoplay.DaysPlanFor("nowatch", 1));
        var night = Autoplay.NightWatcher();
        Assert.Equal(Return.Sim.DayBlock.Sleep, night[7]);
        Assert.Equal(Return.Sim.DayBlock.Work, night[23]);
        Assert.Null(night[20]); // the camp's free time
    }

    [Fact]
    public void TheNightwatcherPlanGivesMiraNightsAndTheLogSaysSo()
    {
        var stdout = new StringWriter();
        // Seed 16: Mira must still live in the camp at the year's end for the cast line; in seeds 2 and 3 (once Zones and Schedule merged
        // with Build and the return) she leaves (ADR-0028, step 3).
        Assert.Equal(0, Cli.Run(new[] { "run", "--seed", "16", "--years", "1", "--days", "nightwatcher" }, stdout, new StringWriter()));
        string log = stdout.ToString();
        Assert.Contains("Day 1, 00:00 — You changed Mira's day: sleep 8 hours, work 13, free time 3.", log);
        Assert.Contains("Mira keeps their own hours: sleeps 07:00–15:00, works 22:00–06:00.", log);
    }

    [Fact]
    public void TheNoWatchPlanSaysSoAndThievesFindNobodyAtThePost()
    {
        var stdout = new StringWriter();
        Assert.Equal(0, Cli.Run(new[] { "run", "--seed", "2", "--years", "1", "--days", "nowatch" }, stdout, new StringWriter()));
        string log = stdout.ToString();
        Assert.Contains("Day 1, 00:00 — You decided the camp keeps no watch.", log);
        Assert.DoesNotContain("took the watch", log);
    }

    [Fact]
    public void AnUnknownDaysPlanIsABadArgument()
    {
        var stderr = new StringWriter();
        Assert.Equal(2, Cli.Run(new[] { "soak", "--worlds", "1", "--years", "1", "--out", "unused", "--days", "siesta" }, new StringWriter(), stderr));
        Assert.Contains("--days", stderr.ToString());
    }

    [Fact]
    public void TheSoakReportsADaysBlockPerPlan()
    {
        string dir = Path.Combine(Path.GetTempPath(), "return-days-soak-" + Guid.NewGuid().ToString("N"));
        var stdout = new StringWriter();
        Assert.Equal(0, Cli.Run(new[] { "soak", "--worlds", "2", "--years", "1", "--out", dir, "--days", "nowatch", "--parallel", "2" }, stdout, new StringWriter()));
        Assert.Contains("Days 'nowatch', 2 worlds:", stdout.ToString());
        string json = File.ReadAllText(Path.Combine(dir, "summary.json"));
        Assert.Contains("\"days\": \"nowatch\"", json);
        Assert.Contains("\"watchKeptPct\": 0", json);
    }
}
