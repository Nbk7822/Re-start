using Return.Runner;

namespace Return.Tests;

/// <summary>The stand-in player's zone plans (ADR-0028): what each seed runs under and what a dry world's log says.</summary>
public class ZoneRunnerTests
{
    [Fact]
    public void MixedPlansVaryApartFromTheRationPolicy()
    {
        Assert.Equal("none", Autoplay.ZonePlanFor("mixed", 1));   // (1 / 9) % 5 = 0: none
        Assert.Equal("dry", Autoplay.ZonePlanFor("mixed", 9));    // 1: dry
        Assert.Equal("home", Autoplay.ZonePlanFor("mixed", 18));  // 2: home
        Assert.Equal("short", Autoplay.ZonePlanFor("mixed", 27)); // 3: short of the watch post
        Assert.Equal("both", Autoplay.ZonePlanFor("mixed", 36));  // 4: dry stores and a home area
        Assert.Equal("dry", Autoplay.ZonePlanFor("dry", 1));
        Assert.Equal("none", Autoplay.ZonePlanFor("none", 9));
    }

    [Fact]
    public void TheHomePlansMarkTheClearingAndTheShortOneLeavesThePostOutside()
    {
        foreach ((string plan, bool postInside, int stockpiles) in new[] { ("home", true, 1), ("short", false, 1), ("both", true, 1) })
        {
            var sim = Return.Sim.Simulation.Create(TestContent.Db, 4);
            Autoplay.Begin(sim, plan);
            sim.Step();
            Return.Sim.World w = sim.World;
            Return.Sim.Zone home = Return.Sim.Systems.Zones.HomeOf(w)!;
            Assert.Equal(postInside, Return.Sim.Systems.Zones.InHome(w, w.Map.WatchPost));
            Assert.True(Return.Sim.Systems.Zones.InHome(w, w.Map.Hearth), plan);
            Assert.Equal(stockpiles, w.Map.Zones.Count(z => z.Kind == "stockpile"));
            Assert.Equal(plan == "short" ? 22 * (w.Map.WatchPost.Y - Return.Sim.WorldGen.ClearingOf(w.Map.Shelter).Y) : 418, Return.Sim.Systems.Zones.Count(home.Tiles));
        }
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
    public void AnUnknownZonePlanIsABadArgument()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Assert.Equal(2, Cli.Run(new[] { "soak", "--worlds", "1", "--years", "1", "--out", "unused", "--zones", "moat" }, stdout, stderr));
        Assert.Contains("--zones", stderr.ToString());
    }
}
