using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
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
