using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>The Schedule (ADR-0029): kinds of day, resolving a person's hours, and the content checks made at load.</summary>
public class ScheduleTests
{
    private static string ContentDir => Path.Combine(TestPaths.Root, "content");

    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    // ---- Design test 1: the days load from content and resolve ----

    [Fact]
    public void TheDefaultDaysLoadFromContent()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Assert.Equal(new[] { "diurnal", "nocturnal", "sleepless" }, w.CampDays.Select(d => d.Rhythm));
        Assert.All(w.CampDays, d => Assert.Equal(24, d.Hours.Count));
        Assert.All(w.CampDays, d => Assert.Equal(0, d.SetEvent));
        List<DayBlock> camp = ScheduleSystem.CampDayFor(w, "diurnal")!.Hours;
        for (int h = 0; h < 24; h++)
            Assert.Equal(h >= 22 || h < 6 ? DayBlock.Sleep : h < 19 ? DayBlock.Work : DayBlock.Free, camp[h]);
        Assert.Equal(19, w.WatchFrom);
        Assert.Equal(6, w.WatchTo);
        Assert.Equal(0, w.WatchEvent);
    }

    [Fact]
    public void VampiresKeepTheNightFolksDayAndTheUndeadTheSleeplessOne()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Assert.Equal("diurnal", ScheduleSystem.RhythmOf(w, Cast(w, "mira")));
        Assert.Equal("nocturnal", ScheduleSystem.RhythmOf(w, Cast(w, "vale")));
        Assert.Equal("sleepless", ScheduleSystem.RhythmOf(w, Cast(w, "ketil")));
        Assert.Equal("diurnal", ScheduleSystem.RhythmOf(w, Cast(w, "dryope"))); // dryads tire and keep the camp's day
        Character vale = Cast(w, "vale");
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, vale, 6));
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, vale, 13));
        Assert.Equal(DayBlock.Free, ScheduleSystem.BlockAt(w, vale, 14));
        Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, vale, 23));
        Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, vale, 2)); // the night folk's work wraps past midnight
        Assert.DoesNotContain(DayBlock.Sleep, ScheduleSystem.PlannedDay(w, Cast(w, "ketil")));
    }

    [Fact]
    public void ResolvingWrapsPastMidnight()
    {
        Assert.True(ScheduleSystem.InRange(22, 6, 23));
        Assert.True(ScheduleSystem.InRange(22, 6, 0));
        Assert.True(ScheduleSystem.InRange(22, 6, 5));
        Assert.False(ScheduleSystem.InRange(22, 6, 6));
        Assert.False(ScheduleSystem.InRange(22, 6, 21));
        Assert.False(ScheduleSystem.InRange(7, 7, 7)); // equal ends: nothing
        Assert.Equal(8, ScheduleSystem.RangeLength(22, 6));
        Assert.Equal(11, ScheduleSystem.RangeLength(19, 6));
        Assert.Equal(0, ScheduleSystem.RangeLength(4, 4));
        World w = WorldGen.Create(TestContent.Db, 1);
        Character mira = Cast(w, "mira");
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, mira, 23));
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, mira, 0));
        Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, mira, 6));
    }

    [Fact]
    public void OwnHoursOverrideTheCampsDay()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Character mira = Cast(w, "mira");
        mira.OwnHours.Add(new HourBlock { Hour = 23, Block = DayBlock.Work });
        Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, mira, 23));
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, mira, 0));
        mira.OwnHours.Clear();
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, mira, 23));
    }

    [Fact]
    public void AStraySleepHourReadsAsFreeTimeForThoseWhoNeverTire()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Character ketil = Cast(w, "ketil");
        ketil.OwnHours.Add(new HourBlock { Hour = 3, Block = DayBlock.Sleep });
        Assert.Equal(DayBlock.Free, ScheduleSystem.BlockAt(w, ketil, 3));
        w.Tick = 3L * w.Content.World.TicksPerHour;
        Assert.False(ScheduleSystem.IsSleepTime(w, ketil));
    }

    [Fact]
    public void EveryoneStartsWithALivedStripOfTheirPlannedDayAndNoDuty()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        foreach (Character c in w.Characters.Where(c => c.Named))
        {
            Assert.Equal(24, c.Lived.Count);
            for (int h = 0; h < 24; h++)
                Assert.Equal(ScheduleSystem.BlockAt(w, c, h) == DayBlock.Sleep ? LivedKind.Slept : LivedKind.Free, c.Lived[h]);
        }
    }

    /// <summary>Filling defaults is idempotent, so a restored world hashes like the one it came from; an old snapshot without days gets them from content.</summary>
    [Fact]
    public void DefaultsFillAnOldSnapshotAndChangeNothingElse()
    {
        var sim = Simulation.Create(TestContent.Db, 2);
        string before = sim.StateHash();
        ScheduleSystem.EnsureDefaults(sim.World);
        Assert.Equal(before, sim.StateHash());

        World w = sim.World;
        w.CampDays.Clear();
        w.WatchFrom = w.WatchTo = 0;
        foreach (Character c in w.Characters) c.Lived.Clear();
        World restored = Simulation.Restore(TestContent.Db, sim.Snapshot()).World;
        Assert.Equal(3, restored.CampDays.Count);
        Assert.Equal((19, 6), (restored.WatchFrom, restored.WatchTo));
        Assert.All(restored.Characters.Where(c => c.Named), c => Assert.Equal(24, c.Lived.Count));
    }

    // ---- Hours as lived and overwork (design 4.5, test 9) ----

    private static long Hours(World w, double h) => (long)(h * w.Content.World.TicksPerHour);

    private static void SampleHour(World w, int samples = 10)
    {
        for (int i = 0; i < samples; i++) ScheduleSystem.Sample(w);
    }

    [Fact]
    public void TheLivedStripRecordsWhatSomeoneMostlyDidEachHour()
    {
        World w = WorkTests.Only(1, "mira", "oren");
        Character mira = Cast(w, "mira");
        w.Tick = Hours(w, 10);
        mira.Job = new Job { Kind = JobKind.Forage, Stage = 1 };
        SampleHour(w, 6);
        mira.Job = new Job { Kind = JobKind.KeepWatch, Stage = 1 };
        SampleHour(w, 4);
        w.Tick = Hours(w, 11);
        ScheduleSystem.Hourly(w);
        Assert.Equal(LivedKind.Worked, mira.Lived[10]);
        Assert.Equal(0, mira.HourTally.Total);

        mira.Job = new Job { Kind = JobKind.Forage, Stage = 1 };
        SampleHour(w, 5);
        mira.Job = new Job { Kind = JobKind.KeepWatch, Stage = 1 };
        SampleHour(w, 5);
        w.Tick = Hours(w, 12);
        ScheduleSystem.Hourly(w);
        Assert.Equal(LivedKind.Watched, mira.Lived[11]); // ties: watched before worked

        mira.Job = null;
        mira.Asleep = true;
        SampleHour(w);
        w.Tick = Hours(w, 13);
        ScheduleSystem.Hourly(w);
        Assert.Equal(LivedKind.Slept, mira.Lived[12]);
        Assert.Equal(LivedKind.Needs, ScheduleSystem.KindNow(new Character { Job = new Job { Kind = JobKind.Eat } }));
        Assert.Equal(LivedKind.Free, ScheduleSystem.KindNow(new Character { Job = new Job { Kind = JobKind.Feast } }));
    }

    [Fact]
    public void LongDutyWearsSomeoneOutAndTheWearCitesTheDuty()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        w.Tick = Hours(w, 18);
        for (int h = 6; h < 18; h++) oren.Lived[h] = LivedKind.Worked; // twelve hours of duty
        SimEvent duty = w.Log.Emit(w, SystemTag.Work, "event.test.duty", new[] { oren.Id }, Array.Empty<long>());
        oren.LastDutyEvent = duty.Id;
        oren.Rest = 140;
        ScheduleSystem.Hourly(w);
        SimEvent worn = w.Log.All.Single(e => e.Key == "event.needs.worn_out");
        Assert.Equal(new[] { duty.Id }, worn.Causes);
        Assert.Equal("Oren is worn out from long hours.", Return.Sim.Api.EventText.Describe(w, worn));
        Assert.Equal(worn.Id, oren.WornOutEvent);
        Assert.Contains(oren.Thoughts, t => t.DefId == "overworked");
        w.Tick += Hours(w, 1);
        ScheduleSystem.Hourly(w);
        Assert.Single(w.Log.All, e => e.Key == "event.needs.worn_out"); // once per stretch
        oren.Rest = 650;
        w.Tick += Hours(w, 1);
        ScheduleSystem.Hourly(w);
        Assert.Equal(0, oren.WornOutEvent); // rested again
    }

    [Fact]
    public void AwakeLongWithNoTimeToThemselvesWeighsOnSomeone()
    {
        World w = WorkTests.Only(1, "mira");
        Character mira = Cast(w, "mira");
        w.Tick = Hours(w, 20);
        for (int h = 0; h < 24; h++) mira.Lived[h] = h < 8 ? LivedKind.Slept : h < 15 ? LivedKind.Worked : LivedKind.Needs;
        ScheduleSystem.Hourly(w);
        Assert.Contains(mira.Thoughts, t => t.DefId == "no_time_to_myself");
        Assert.DoesNotContain(mira.Thoughts, t => t.DefId == "overworked"); // seven hours of duty is not overwork
    }

    [Fact]
    public void ThoseWhoNeverTirePayNoneOfOverworksCosts()
    {
        World w = WorkTests.Only(1, "ketil");
        Character ketil = Cast(w, "ketil");
        w.Tick = Hours(w, 20);
        for (int h = 0; h < 24; h++) ketil.Lived[h] = LivedKind.Watched;
        ketil.Rest = 100;
        ScheduleSystem.Hourly(w);
        Assert.DoesNotContain(ketil.Thoughts, t => t.DefId is "overworked" or "no_time_to_myself");
        Assert.Equal(0, ketil.WornOutEvent);
    }

    [Fact]
    public void AnEveningByTheFireWithOthersWarmsSomeone()
    {
        World w = WorkTests.Only(1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        w.Tick = Hours(w, 20);
        mira.Pos = Places.Adjacent(w, w.Map.Hearth, w.Map.Hearth);
        oren.Pos = Places.Adjacent(w, w.Map.Hearth, mira.Pos);
        mira.Job = new Job { Kind = JobKind.Wander, Stage = 1 };
        oren.Job = new Job { Kind = JobKind.Wander, Stage = 1 };
        SampleHour(w);
        w.Tick = Hours(w, 21);
        ScheduleSystem.Hourly(w);
        Assert.Contains(mira.Thoughts, t => t.DefId == "evening_by_the_fire");

        // Alone at the fire, or by it in working hours, is not an evening by the fire.
        mira.Thoughts.Clear();
        oren.Pos = w.Map.StartStockpile.Center;
        w.Tick = Hours(w, 21);
        SampleHour(w);
        w.Tick = Hours(w, 22);
        ScheduleSystem.Hourly(w);
        Assert.DoesNotContain(mira.Thoughts, t => t.DefId == "evening_by_the_fire");
    }

    /// <summary>Design test 9: worn out, the same accident draw lands twice as often, and only those accidents say so.</summary>
    [Fact]
    public void WornOutAccidentsComeTwiceAsOftenAndSaySo()
    {
        var risky = new WorkDef { Id = "test_risky", AccidentPermille = 300 };
        int Accidents(bool worn, out int tired)
        {
            World w = WorkTests.Only(9, "oren");
            Character oren = Cast(w, "oren");
            if (worn) oren.WornOutEvent = w.Log.Emit(w, SystemTag.Needs, "event.needs.worn_out", new[] { oren.Id }, Array.Empty<long>()).Id;
            for (int i = 0; i < 400; i++)
            {
                Return.Sim.Systems.Jobs.JobRunner.RollAccident(w, oren, risky, 0);
                oren.Health = 1000;
                oren.Downed = false;
            }
            tired = w.Log.All.Count(e => e.Key == "event.incident.work_accident_tired");
            return w.Log.All.Count(e => e.Key is "event.incident.work_accident" or "event.incident.work_accident_tired");
        }
        int rested = Accidents(false, out int restedTired), worn = Accidents(true, out int wornTired);
        Assert.Equal(0, restedTired);
        Assert.Equal(worn, wornTired);
        Assert.InRange(worn * 100 / rested, 170, 230);
    }

    [Fact]
    public void AnOathboundKeepingAPromiseAfterHoursSaysSoOnceADay()
    {
        World w = WorkTests.Only(1, "oren", "nia");
        Character oren = Cast(w, "oren"), nia = Cast(w, "nia");
        oren.Traits = new List<string> { "oathbound" };
        w.Tick = Hours(w, 20);
        Stock.Add(w, "berries", 3, w.Map.StartStockpile.Center);
        Debt promise = Relations.FormDebt(w, oren, nia, DebtKind.PromiseMeal, 1, 0, w.Tick + Hours(w, 48));
        nia.Food = 300;
        Job? job = JobPicker.Choose(w, oren);
        Assert.Equal(JobKind.KeepPromise, job?.Kind);
        SimEvent late = w.Log.All.Single(e => e.Key == "event.work.worked_late");
        Assert.Equal(new[] { promise.FormedEvent }, late.Causes);
        Assert.Equal("Oren kept working past dark to keep their promise to Nia.", Return.Sim.Api.EventText.Describe(w, late));
        Assert.Equal(late.Id, oren.LastDutyEvent);
        JobPicker.Choose(w, oren);
        Assert.Single(w.Log.All, e => e.Key == "event.work.worked_late");
        w.Tick = Hours(w, 10);
        JobPicker.Choose(w, oren); // in working hours, nothing to say
        Assert.Single(w.Log.All, e => e.Key == "event.work.worked_late");
    }

    // ---- Design test 2 and 3: the blocks in job choice ----

    [Fact]
    public void TheSleepBlockKeepsPeopleInBed()
    {
        World w = WorkTests.Only(1, "mira", "nia");
        Character mira = Cast(w, "mira"), nia = Cast(w, "nia");
        mira.Traits = new List<string> { "protective" };
        nia.Traits = new List<string> { "practical" };
        w.Tick = Hours(w, 23);
        foreach (Character c in new[] { mira, nia })
        {
            c.Rest = 1000; // fully rested: nobody gets up at 950 in their sleeping hours
            c.Job = JobPicker.Choose(w, c);
            Assert.Equal(JobKind.Sleep, c.Job?.Kind);
            c.Pos = c.Job!.Target;
        }
        nia.Food = 140; // starving: hunger gets her up
        for (int t = 0; t < Hours(w, 7.5); t++)
        {
            TravelSystem.Step(w);
            WorkSystem.Step(w);
            w.Tick++;
            if (w.Tick == Hours(w, 29)) Assert.True(mira.Asleep, "rested Mira still asleep at 05:00");
        }
        Assert.False(mira.Asleep); // up after 06:00
        Assert.NotEqual(JobKind.Sleep, nia.Job?.Kind);
    }

    [Fact]
    public void FreeTimeSkipsOrdinaryWorkButNotUrgentHelp()
    {
        World w = WorkTests.Only(1, "bram", "oren");
        Character bram = Cast(w, "bram"), oren = Cast(w, "oren");
        Stock.Take(w, "berries", 99); // berries wanted: forage is there to do
        w.Tick = Hours(w, 20);
        bram.Rest = 900;
        Assert.NotEqual(JobKind.Forage, JobPicker.Choose(w, bram)?.Kind);
        w.Tick = Hours(w, 10);
        Assert.Equal(JobKind.Forage, JobPicker.Choose(w, bram)?.Kind); // the same need in working hours
        w.Tick = Hours(w, 20);
        oren.Health = 120;
        oren.Downed = true;
        Assert.Contains(JobPicker.Choose(w, bram)?.Kind, new JobKind?[] { JobKind.Rescue, JobKind.Care }); // a carer still helps the fallen in free time
        bram.Rest = 300;
        oren.Health = 1000;
        oren.Downed = false;
        Assert.Equal(JobKind.Sleep, JobPicker.Choose(w, bram)?.Kind); // a nap below 400 in free time
    }

    // ---- Design test 13: content checks ----

    [Fact]
    public void AnHourCoveredTwiceOrNotAtAllIsRejected()
    {
        string twice = CopyContentWith("rhythms.json", "{ \"from\": 19, \"to\": 22, \"block\": \"free\" } ] },", "{ \"from\": 18, \"to\": 22, \"block\": \"free\" } ] },");
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(twice));
        Assert.Contains("rhythms.json", ex.Message);
        Assert.Contains("diurnal", ex.Message);
        Assert.Contains("hour 18", ex.Message);

        string none = CopyContentWith("rhythms.json", "{ \"from\": 19, \"to\": 22, \"block\": \"free\" } ] },", "{ \"from\": 20, \"to\": 22, \"block\": \"free\" } ] },");
        ex = Assert.Throws<ContentException>(() => ContentDb.Load(none));
        Assert.Contains("hour 19", ex.Message);
    }

    [Fact]
    public void ASpeciesNamingAnUnknownKindOfDayIsRejected()
    {
        string dir = CopyContentWith("species.json", "\"rhythm\": \"nocturnal\"", "\"rhythm\": \"crepuscular\"");
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(dir));
        Assert.Contains("species.json", ex.Message);
        Assert.Contains("crepuscular", ex.Message);
    }

    [Fact]
    public void ASpeciesWhoseTiringDisagreesWithItsKindOfDayIsRejected()
    {
        // The vampire tires, so a day with no sleep in it cannot be theirs.
        string tires = CopyContentWith("species.json", "\"rhythm\": \"nocturnal\"", "\"rhythm\": \"sleepless\"");
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(tires));
        Assert.Contains("vampire", ex.Message);
        Assert.Contains("tires", ex.Message);

        // The undead never tire, so a day with sleep in it cannot be theirs.
        string never = CopyContentWith("species.json", "\"tires\": false,", "\"tires\": false,\n    \"rhythm\": \"diurnal\",");
        ex = Assert.Throws<ContentException>(() => ContentDb.Load(never));
        Assert.Contains("undead", ex.Message);
        Assert.Contains("never tires", ex.Message);
    }

    [Fact]
    public void TheWatchAndItsTuningComeFromWorldJson()
    {
        ScheduleDef d = TestContent.Db.World.Schedule;
        Assert.Equal((19, 6), (d.Watch.From, d.Watch.To));
        Assert.Equal(4, d.WatchTurnHours);
        Assert.Equal(2, d.WakeTries);
        Assert.Equal(12, d.OverworkHours);
        Assert.Equal(8, d.WornOutDutyHours);
        Assert.Equal(14, d.NoFreeTimeAwakeHours);
        Assert.Equal(200, d.TiredAccidentPct);
        Assert.Equal(60, d.AsleepOnWatchRestBelow);
        Assert.Equal(400, d.FreeTimeNapBelow);
        Assert.Equal(300, TestContent.Db.TraitById("anxious").RiskyWorkRestBelow);
    }

    private static string CopyContentWith(string file, string find, string replace)
    {
        string dir = Path.Combine(Path.GetTempPath(), "return-content-" + Guid.NewGuid().ToString("N"));
        foreach (string f in Directory.GetFiles(ContentDir, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(dir, Path.GetRelativePath(ContentDir, f));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(f, target);
        }
        string path = Path.Combine(dir, file);
        string text = File.ReadAllText(path);
        Assert.Contains(find, text);
        File.WriteAllText(path, text.Replace(find, replace, StringComparison.Ordinal));
        return dir;
    }
}
