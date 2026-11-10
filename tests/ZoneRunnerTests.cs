using Return.Runner;

namespace Return.Tests;

/// <summary>The stand-in player's zone plans (ADR-0028): what each seed runs under and what a dry world's log says.</summary>
public class ZoneRunnerTests
{
    [Fact]
    public void MixedPlansVaryApartFromTheRationPolicyAndKeepTheirStockpileWhenTheHomeAreaComes()
    {
        Assert.Equal("none", Autoplay.ZonePlanFor("mixed", 1));   // (1 / 9) % 5 = 0: none
        Assert.Equal("dry", Autoplay.ZonePlanFor("mixed", 9));    // 1: dry
        Assert.Equal("none", Autoplay.ZonePlanFor("mixed", 18));  // 2: home, which waits for step 3
        Assert.Equal("none", Autoplay.ZonePlanFor("mixed", 27));  // 3: short, likewise
        Assert.Equal("dry", Autoplay.ZonePlanFor("mixed", 36));   // 4: both, whose stockpile half runs now
        Assert.Equal("dry", Autoplay.ZonePlanFor("dry", 1));
        Assert.Equal("none", Autoplay.ZonePlanFor("none", 9));
    }

    [Fact]
    public void ADryWorldsLogTellsTheStockpileMoveAndListsTheZones()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Assert.Equal(0, Cli.Run(new[] { "run", "--seed", "4", "--years", "1", "--zones", "dry" }, stdout, stderr));
        string log = stdout.ToString();
        Assert.Contains("Day 1, 00:00 — You marked a stockpile in the covered ruins (9 tiles).", log);
        Assert.Contains("Day 1, 00:00 — You took away the stockpile by the hearth.", log);
        Assert.Contains("Zones: a meeting place by the hearth (24 tiles); a stockpile in the covered ruins (9 tiles).", log);
        Assert.DoesNotContain("spoiled in the damp", log);
    }

    [Fact]
    public void AZonePlanNotBuiltYetIsABadArgument()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Assert.Equal(2, Cli.Run(new[] { "soak", "--worlds", "1", "--years", "1", "--out", "unused", "--zones", "home" }, stdout, stderr));
        Assert.Contains("--zones", stderr.ToString());
    }
}
