using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// The Schedule's commands (ADR-0029): what each decision says, who goes their own way over it by the one
/// compliance rule (never the player), and the drafts kept while paused.
/// </summary>
public class ScheduleCommandTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>A simulation with only the named cast members in camp, fed, rested, of no people or faith and of a fair mood.</summary>
    internal static Simulation Camp(ulong seed, params string[] keys)
    {
        Simulation s = Simulation.Create(TestContent.Db, seed);
        World w = s.World;
        int i = 0;
        foreach (Character c in w.Characters)
        {
            if (!keys.Contains(c.CastKey)) { c.Status = CharStatus.OffMap; continue; }
            c.Status = CharStatus.Member;
            c.Food = Math.Max(c.Food, 900); c.Rest = 900; c.Social = 900; c.Mood = 60;
            c.CultureId = ""; c.FaithId = "";
            if (c.Pos == Point.None || !w.Map.Passable(c.Pos)) c.Pos = new Point(w.Map.Hearth.X - 2 + i, w.Map.Hearth.Y + 2);
            i++;
        }
        w.Arrivals.Clear();
        return s;
    }

    private static DayBlock?[] Day(params (int From, int To, DayBlock Block)[] ranges)
    {
        var day = new DayBlock?[24];
        foreach ((int from, int to, DayBlock block) in ranges)
            for (int h = from; h != to; h = (h + 1) % 24) day[h] = block;
        return day;
    }

    /// <summary>Mira on nights: sleep 07:00-15:00, work 15:00-19:00 and 22:00-07:00 (the design's nightwatcher), the camp's free time between.</summary>
    internal static DayBlock?[] Nights() => Day((7, 15, DayBlock.Sleep), (15, 19, DayBlock.Work), (22, 7, DayBlock.Work));

    private static void Apply(Simulation s, Command c)
    {
        s.Enqueue(c with { Tick = s.World.Tick });
        s.Step();
    }

    [Fact]
    public void SettingSomeonesDaySaysSoAndTheirHoursFollow()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        Character mira = Cast(w, "mira");
        mira.Traits = new List<string> { "protective" };
        Apply(s, new SetPersonDay(0, mira.Id, Nights()));
        SimEvent e = w.Log.All.Single(x => x.Key == "event.world.person_day");
        Assert.Equal("You changed Mira's day: sleep 8 hours, work 13, free time 3.", EventText.Describe(w, e));
        Assert.Equal(e.Id, mira.OwnHoursEvent);
        Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, mira, 23));
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, mira, 10));
        Assert.Equal(DayBlock.Free, ScheduleSystem.BlockAt(w, mira, 20)); // the camp's free time, where she has no hour of her own
        Assert.Equal(0, mira.LastDutyEvent); // thirteen work hours, as before: nothing new to tire her

        DayBlock?[] longer = Nights();
        longer[19] = longer[20] = DayBlock.Work;
        Apply(s, new SetPersonDay(w.Tick, mira.Id, longer));
        SimEvent more = w.Log.All.Last(x => x.Key == "event.world.person_day");
        Assert.Equal(more.Id, mira.LastDutyEvent); // more work hours than before: the decision is why she will tire

        Apply(s, new SetPersonDay(w.Tick, mira.Id, new DayBlock?[24]));
        SimEvent reset = w.Log.All.Single(x => x.Key == "event.world.person_day_reset");
        Assert.Equal("Mira now keeps the camp's day.", EventText.Describe(w, reset));
        Assert.Empty(mira.OwnHours);
        Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, mira, 23));
    }

    [Fact]
    public void YourOwnDayReadsInTheSecondPersonAndNeverRolls()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        Character you = Cast(w, "you");
        you.Traits = new List<string> { "independent" }; // even an Independent player is never asked to comply: it is their own rule
        long draws = w.Rng.Dice[DiceSystem.Work].Draws;
        ScheduleSystem.ApplyPersonDay(w, new SetPersonDay(w.Tick, you.Id, Nights()));
        Assert.Equal(draws, w.Rng.Dice[DiceSystem.Work].Draws);
        Assert.Equal("You changed your own day: sleep 8 hours, work 13, free time 3.", EventText.Describe(w, w.Log.All[^1]));
        ScheduleSystem.ApplyPersonDay(w, new SetPersonDay(w.Tick, you.Id, new DayBlock?[24]));
        Assert.Equal("You now keep the camp's day.", EventText.Describe(w, w.Log.All[^1]));
    }

    /// <summary>Design test 11: for some seed an Independent declines, citing the command, and their day stays as it was.</summary>
    [Fact]
    public void AnIndependentMayGoTheirOwnWayOverAChangeToTheirDay()
    {
        bool declined = false, accepted = false;
        for (ulong seed = 1; seed <= 40 && !(declined && accepted); seed++)
        {
            Simulation s = Camp(seed, "you", "sera");
            World w = s.World;
            Character sera = Cast(w, "sera");
            sera.Traits = new List<string> { "independent" };
            ScheduleSystem.ApplyPersonDay(w, new SetPersonDay(w.Tick, sera.Id, Nights()));
            SimEvent order = w.Log.All.Single(x => x.Key == "event.world.person_day");
            SimEvent? no = w.Log.All.SingleOrDefault(x => x.Key.StartsWith("event.social.declined_day", StringComparison.Ordinal));
            if (no != null)
            {
                declined = true;
                Assert.Equal("event.social.declined_day_own_way", no.Key);
                Assert.Equal("Never one to be told, Sera would not change their day.", EventText.Describe(w, no));
                Assert.Contains(order.Id, no.Causes);
                Assert.Empty(sera.OwnHours);
                Assert.Equal(0, sera.OwnHoursEvent);
            }
            else
            {
                accepted = true;
                Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, sera, 23));
            }
        }
        Assert.True(declined && accepted, $"declined {declined}, accepted {accepted}");
    }

    [Fact]
    public void SomeoneWhoDislikesYouDeclinesForThatReason()
    {
        bool declined = false;
        for (ulong seed = 1; seed <= 60 && !declined; seed++)
        {
            Simulation s = Camp(seed, "you", "oren");
            World w = s.World;
            Character oren = Cast(w, "oren");
            oren.Traits = new List<string> { "practical" };
            Relations.ChangeOpinion(w, oren, w.PlayerId, -60, 0);
            ScheduleSystem.ApplyPersonDay(w, new SetPersonDay(w.Tick, oren.Id, Nights()));
            SimEvent? no = w.Log.All.SingleOrDefault(x => x.Key.StartsWith("event.social.declined_day", StringComparison.Ordinal));
            if (no == null) continue;
            declined = true;
            Assert.Equal("event.social.declined_day_dislike", no.Key);
            Assert.Equal("Caring little for what you want, Oren would not change their day.", EventText.Describe(w, no));
        }
        Assert.True(declined);
    }

    [Fact]
    public void ChangingTheCampsDaySaysSoAndAnyoneWhoGoesTheirOwnWayKeepsTheOldHours()
    {
        bool kept = false;
        for (ulong seed = 1; seed <= 40 && !kept; seed++)
        {
            Simulation s = Camp(seed, "you", "mira", "sera");
            World w = s.World;
            Character sera = Cast(w, "sera"), mira = Cast(w, "mira");
            sera.Traits = new List<string> { "independent" };
            mira.Traits = new List<string> { "protective" };
            var day = new List<DayBlock>(ScheduleSystem.CampDayFor(w, "diurnal")!.Hours) { [21] = DayBlock.Sleep };
            ScheduleSystem.ApplyCampDay(w, new SetCampDay(w.Tick, "diurnal", day));
            SimEvent order = w.Log.All.Single(x => x.Key == "event.world.camp_day");
            Assert.Equal("You changed the camp's day: sleep 9 hours, work 13, free time 2.", EventText.Describe(w, order));
            Assert.Equal(order.Id, ScheduleSystem.CampDayFor(w, "diurnal")!.SetEvent);
            Assert.Equal(DayBlock.Sleep, ScheduleSystem.BlockAt(w, mira, 21));
            SimEvent? no = w.Log.All.SingleOrDefault(x => x.Key.StartsWith("event.social.kept_old_hours", StringComparison.Ordinal));
            if (no == null) continue;
            kept = true;
            Assert.Equal("Never one to be told, Sera kept to the old hours.", EventText.Describe(w, no));
            Assert.Contains(order.Id, no.Causes);
            Assert.Equal(DayBlock.Free, ScheduleSystem.BlockAt(w, sera, 21)); // her own hour now: the old one
            Assert.Single(sera.OwnHours);
        }
        Assert.True(kept);
    }

    [Fact]
    public void TheNightFolksDayIsTheirsToChange()
    {
        Simulation s = Camp(1, "you", "vale", "mira");
        World w = s.World;
        var day = new List<DayBlock>(ScheduleSystem.CampDayFor(w, "nocturnal")!.Hours) { [18] = DayBlock.Work };
        ScheduleSystem.ApplyCampDay(w, new SetCampDay(w.Tick, "nocturnal", day));
        Assert.Equal("You changed the night folk's day: sleep 8 hours, work 12, free time 4.", EventText.Describe(w, w.Log.All.Last(x => x.Key == "event.world.camp_day")));
        Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, Cast(w, "vale"), 18));
        Assert.Equal(DayBlock.Free, ScheduleSystem.BlockAt(w, Cast(w, "vale"), 17));
        Assert.Equal(DayBlock.Work, ScheduleSystem.BlockAt(w, Cast(w, "mira"), 18)); // the day folk's day is untouched
    }

    [Fact]
    public void AChangeThatChangesNothingSaysNothing()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        int events = w.Log.All.Count;
        ScheduleSystem.ApplyCampDay(w, new SetCampDay(w.Tick, "diurnal", ScheduleSystem.CampDayFor(w, "diurnal")!.Hours.ToList()));
        ScheduleSystem.ApplyWatchHours(w, new SetWatchHours(w.Tick, 19, 6));
        ScheduleSystem.ApplyPersonDay(w, new SetPersonDay(w.Tick, Cast(w, "mira").Id, new DayBlock?[24]));
        Assert.Equal(events, w.Log.All.Count);
    }

    [Fact]
    public void TheWatchHoursSayWhenAndNoWatchSaysSo()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        ScheduleSystem.ApplyWatchHours(w, new SetWatchHours(w.Tick, 22, 6));
        Assert.Equal("You decided the camp keeps a watch from 22:00 to 06:00.", EventText.Describe(w, w.Log.All[^1]));
        Assert.Equal((22, 6, w.Log.All[^1].Id), (w.WatchFrom, w.WatchTo, w.WatchEvent));
        ScheduleSystem.ApplyWatchHours(w, new SetWatchHours(w.Tick, 0, 0));
        Assert.Equal("You decided the camp keeps no watch.", EventText.Describe(w, w.Log.All[^1]));
        Assert.False(ScheduleSystem.InWatchHours(w, 23));
        ScheduleSystem.ApplyWatchHours(w, new SetWatchHours(w.Tick, 5, 5)); // still none: nothing to say
        Assert.Equal("event.world.watch_hours", w.Log.All[^1].Key);
        Assert.Equal(2, w.Log.All.Count(x => x.Key == "event.world.watch_hours"));
    }

    [Fact]
    public void CommandsRoundTripThroughSnapshots()
    {
        Simulation s = Camp(1, "you", "mira", "vale");
        World w = s.World;
        s.Enqueue(new SetCampDay(100, "diurnal", ScheduleSystem.CampDayFor(w, "diurnal")!.Hours.Select((b, h) => h == 21 ? DayBlock.Sleep : b).ToList()));
        s.Enqueue(new SetWatchHours(100, 20, 4));
        s.Enqueue(new SetPersonDay(100, Cast(w, "mira").Id, Nights()));
        Simulation copy = Simulation.Restore(TestContent.Db, s.Snapshot());
        Assert.Equal(s.StateHash(), copy.StateHash());
        s.Advance(400);
        copy.Advance(400);
        Assert.Equal(s.StateHash(), copy.StateHash());
        Assert.Equal((20, 4), (copy.World.WatchFrom, copy.World.WatchTo));
    }

    // ---- Drafts (design test 15) ----

    [Fact]
    public void DraftsKeepOnePerSlotAndSkipWhatChangesNothing()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        int mira = Cast(w, "mira").Id, oren = Cast(w, "oren").Id;
        var drafts = new OrderDrafts();
        List<DayBlock> camp = ScheduleSystem.CampDayFor(w, "diurnal")!.Hours.ToList();
        var early = camp.Select((b, h) => h == 21 ? DayBlock.Sleep : b).ToList();
        drafts.Add(new SetCampDay(w.Tick, "diurnal", early));
        drafts.Add(new SetCampDay(w.Tick, "diurnal", camp)); // changed my mind: back to as it is
        drafts.Add(new SetWatchHours(w.Tick, 22, 6));
        drafts.Add(new SetWatchHours(w.Tick, 0, 0));
        drafts.Add(new SetPersonDay(w.Tick, mira, Nights()));
        drafts.Add(new SetPersonDay(w.Tick, oren, Nights()));
        drafts.Add(new SetPersonDay(w.Tick, oren, new DayBlock?[24]));
        Assert.Equal(camp, drafts.CampDay("diurnal"));
        Assert.Null(drafts.CampDay("nocturnal"));
        Assert.Equal((0, 0), drafts.WatchHours());
        Assert.Equal(Nights(), drafts.PersonDay(mira));
        Assert.True(drafts.HasScheduleDrafts);
        Assert.Equal(4, drafts.Count);
        drafts.Flush(sim);
        Assert.Equal(new[] { "SetWatchHours", "SetPersonDay" }, w.PendingCommands.Select(c => c.GetType().Name));
    }
}
