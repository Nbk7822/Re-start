using Return.Runner;
using Return.Sim;
using Return.Sim.Systems;

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

    /// <summary>
    /// A return sends the world back mid-day and mid-turn (ADR-0030): the Days meter drops the day and the turn it was measuring
    /// in the lost world, and the part day and the turn the world gone back to is in the middle of, rather than recording a part
    /// day as a night's sleep or a turn of no length; whole days and turns after it are measured as before (merge review).
    /// </summary>
    [Fact]
    public void TheDaysMeterStartsAfreshWhenTheWorldGoesBack()
    {
        World w = WorkTests.Only(1, "mira", "oren");
        Character mira = w.Characters.First(c => c.CastKey == "mira"), oren = w.Characters.First(c => c.CastKey == "oren");
        Assert.All(new[] { mira, oren }, c => Assert.Equal("human", c.SpeciesId));
        long day = w.Content.World.TicksPerDay, hour = w.Content.World.TicksPerHour;
        var meter = new DaysMeter();
        void Post(Character c)
        {
            c.Asleep = false;
            c.Pos = w.Map.WatchPost;
            c.Job = new Job { Kind = JobKind.KeepWatch, Stage = 1, Target = w.Map.WatchPost };
        }
        void Run(long until)
        {
            for (; w.Tick < until; w.Tick += DaysMeter.EveryTicks) meter.Sample(w);
        }

        // Day 5 from 20:00: Mira keeps the watch. You die; the world goes back to day 4 at 21:00, Oren at the post.
        w.Tick = 4 * day + 20 * hour;
        Post(mira);
        Run(w.Tick + 3 * hour);
        w.Fate.Timeline++;
        w.Tick = 3 * day + 21 * hour;
        mira.Job = null;
        Post(oren);
        Run(w.Tick + hour);
        Assert.All(meter.TurnTenths, n => Assert.Equal(0, n));
        Assert.All(meter.AsleepTenths, n => Assert.Equal(0, n));
        // Oren's turn began before the knot and the day gone back to began before it too: neither is measured when it ends.
        oren.Job = null;
        Run(4 * day);
        Assert.All(meter.TurnTenths, n => Assert.Equal(0, n));
        Assert.All(meter.AsleepTenths, n => Assert.Equal(0, n));

        // Day 5 is lived whole: at its end each human's day is counted, and a turn begun and ended in view is measured.
        Run(4 * day + 2 * hour);
        Post(mira);
        Run(4 * day + 4 * hour);
        mira.Job = null;
        Run(5 * day + DaysMeter.EveryTicks);
        Assert.Equal(2, meter.AsleepTenths.Sum());
        Assert.Equal(1, meter.TurnTenths[20]); // two hours

        // A return whose knot lies after the last sample: the clock still moves on, but the timeline is new.
        Post(mira);
        Run(5 * day + 2 * hour);
        w.Fate.Timeline++;
        mira.Job = null;
        Post(oren);
        Run(5 * day + 3 * hour);
        Assert.Equal(1, meter.TurnTenths.Sum());
    }

    /// <summary>
    /// Under --days nowatch the stand-in keeps the watch for a you bound to it (the merge's loop fix): that world runs under
    /// the shipped watch, so the stand-in reports the plan it ran, "default", and the soak tallies the world there rather
    /// than under the plan it was dealt (merge review).
    /// </summary>
    [Fact]
    public void AWorldWhoseWatchTheStandInKeptIsTalliedUnderThePlanItRan()
    {
        // Reborn undead, seed 1's you is bound to the watch and seed 2's to foraging.
        var bound = Simulation.Create(TestContent.Rolled, 1, new RebirthChoice("undead"));
        Assert.Equal("keep_watch", NatureNeeds.Get(bound.World.Get(bound.World.PlayerId), Purpose.NeedId)?.BondWork);
        Assert.Equal("default", Autoplay.Begin(bound, "none", "nowatch"));
        Assert.DoesNotContain(bound.World.PendingCommands, c => c is SetWatchHours);
        var forager = Simulation.Create(TestContent.Rolled, 2, new RebirthChoice("undead"));
        Assert.Equal("nowatch", Autoplay.Begin(forager, "none", "nowatch"));
        Assert.Contains(forager.World.PendingCommands, c => c is SetWatchHours);

        string dir = Path.Combine(Path.GetTempPath(), "return-days-kept-" + Guid.NewGuid().ToString("N"));
        try
        {
            var stdout = new StringWriter();
            Assert.Equal(0, Cli.Run(new[] { "soak", "--worlds", "2", "--years", "1", "--out", dir, "--people", "undead", "--days", "nowatch", "--parallel", "2" },
                stdout, new StringWriter()));
            Assert.Contains("Days 'default', 1 worlds:", stdout.ToString());
            Assert.Contains("Days 'nowatch', 1 worlds:", stdout.ToString());
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
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
