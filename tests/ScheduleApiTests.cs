using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>The Schedule's text-state reads (ADR-0029; design 6.3) and every line it writes, in the third person and the second.</summary>
public class ScheduleApiTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    [Fact]
    public void TheScheduleListsEveryCampDayWithWhoKeepsItAndTheWatch()
    {
        Simulation s = ScheduleCommandTests.Camp(1, "you", "mira", "vale");
        World w = s.World;
        ScheduleState st = StateApi.Schedule(w);
        Assert.Equal(new[] { "diurnal", "nocturnal", "sleepless" }, st.CampDays.Select(d => d.Rhythm));
        Assert.Equal("The camp's day", st.CampDays[0].Title);
        Assert.Equal(new[] { Cast(w, "you").Id, Cast(w, "mira").Id }, st.CampDays[0].Members);
        Assert.Equal(new[] { Cast(w, "vale").Id }, st.CampDays[1].Members);
        Assert.Empty(st.CampDays[2].Members); // no undead in camp: the view hides that strip
        Assert.Equal("sleep", st.CampDays[0].Hours[23].Block);
        Assert.Equal("Free time", st.CampDays[0].Hours[20].Name);
        Assert.Equal("Kept from 19:00 to 06:00, in turns of 4 hours.", st.Watch.Text);
        Assert.Equal("Nobody is on watch.", st.Watch.Now);
        Assert.Empty(st.Different);
        Assert.False(st.CampDays[0].Pending);
    }

    [Fact]
    public void QueuedAndDraftedChangesShowAsPending()
    {
        Simulation s = ScheduleCommandTests.Camp(1, "you", "mira");
        World w = s.World;
        Character mira = Cast(w, "mira");
        s.Enqueue(new SetWatchHours(w.Tick + 10, 0, 0));
        var day = ScheduleSystem.CampDayFor(w, "diurnal")!.Hours.Select((b, h) => h == 21 ? DayBlock.Sleep : b).ToList();
        var drafts = new List<Command> { new SetCampDay(w.Tick, "diurnal", day), new SetPersonDay(w.Tick, mira.Id, ScheduleCommandTests.Nights()) };
        ScheduleState st = StateApi.Schedule(w, drafts);
        Assert.True(st.Watch.Pending);
        Assert.Equal("The camp keeps no watch.", st.Watch.Text);
        Assert.True(st.CampDays[0].Pending);
        Assert.Equal("sleep", st.CampDays[0].Hours[21].Block);
        PersonDaySummary miraLine = Assert.Single(st.Different);
        Assert.Equal("sleeps 07:00–15:00, works 22:00–06:00", miraLine.Summary); // what differs from the camp's day
        PersonDayState d = StateApi.Day(w, mira.Id, drafts);
        Assert.True(d.Pending);
        Assert.False(d.SameAsCamp);
        Assert.Equal("own", d.Planned[23].Source);
        Assert.Equal("camp", d.Planned[20].Source);
        Assert.Equal("Has their own hours.", d.Note);
    }

    [Fact]
    public void APersonsDayShowsPlannedAndLivedWithTheHoursThatBent()
    {
        World w = ScheduleCommandTests.Camp(1, "you", "mira").World;
        Character mira = Cast(w, "mira");
        mira.Lived[23] = LivedKind.Watched;
        mira.Lived[10] = LivedKind.Worked;
        PersonDayState d = StateApi.Day(w, mira.Id);
        Assert.True(d.SameAsCamp);
        Assert.Equal("Keeps the camp's day.", d.Note);
        Assert.Equal("watched", d.Lived[23].Block);
        Assert.True(d.Lived[23].Differs);
        Assert.Equal("23:00 — On watch (planned: Sleep)", d.Lived[23].Note);
        Assert.False(d.Lived[10].Differs);
        Assert.Equal(2, d.DutyHours);
        Assert.True(d.Sleeps);
    }

    [Fact]
    public void TheNightFolkAndTheSleeplessSayWhatTheirBodiesDo()
    {
        World w = ScheduleCommandTests.Camp(1, "vale", "ketil", "you").World;
        PersonDayState vale = StateApi.Day(w, Cast(w, "vale").Id);
        Assert.Equal("Keeps the night folk's day. The sun keeps Vale under cover by day.", vale.Note);
        Assert.True(vale.Planned[12].Sun);
        Assert.Equal("12:00 — Sleep, under cover from the sun", vale.Planned[12].Note);
        Assert.False(vale.Planned[22].Sun);
        PersonDayState ketil = StateApi.Day(w, Cast(w, "ketil").Id);
        Assert.False(ketil.Sleeps);
        Assert.Equal("Keeps the day of those who never sleep. Varrow never sleeps.", ketil.Note);
        Assert.DoesNotContain(ketil.Planned, h => h.Block == "sleep");
    }

    [Fact]
    public void SomeoneWhoWentTheirOwnWaySaysSoInTheirDay()
    {
        World w = ScheduleCommandTests.Camp(1, "you", "sera").World;
        Character sera = Cast(w, "sera");
        SimEvent order = w.Log.Emit(w, SystemTag.World, "event.world.person_day", new[] { sera.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", sera.Id), EventArg.Num("sleep_hours", 8), EventArg.Num("work_hours", 13), EventArg.Num("free_hours", 3));
        SimEvent no = w.Log.Emit(w, SystemTag.Social, "event.social.declined_day_own_way", new[] { sera.Id }, new[] { order.Id }, null, EventArg.Char("a", sera.Id));
        PersonDayState d = StateApi.Day(w, sera.Id);
        Assert.Equal("Sera would not change their day.", d.Note);
        Assert.Equal("Never one to be told, Sera would not change their day. (" + EventText.When(w, no.Tick) + ")", d.Latest);
    }

    [Fact]
    public void TheWatchPostSaysWhoKeepsTheWatchInItsHours()
    {
        World w = ScheduleCommandTests.Camp(1, "you", "vale").World;
        Character vale = Cast(w, "vale");
        int x = w.Map.WatchPost.X, y = w.Map.WatchPost.Y;
        w.Tick = 12L * w.Content.World.TicksPerHour;
        Assert.Contains("Watch post", StateApi.Location(w, x, y).Contents);
        w.Tick = 22L * w.Content.World.TicksPerHour;
        Assert.Contains("Watch post (nobody on watch)", StateApi.Location(w, x, y).Contents);
        WatchTests.OnWatch(w, vale, w.Tick + 600);
        Assert.Contains("Watch post (Vale keeps watch)", StateApi.Location(w, x, y).Contents);
        Assert.Equal("On watch now: Vale (since 22:00).", StateApi.Schedule(w).Watch.Now);
    }

    [Fact]
    public void LastNightIsReadFromTheLivedStrips()
    {
        World w = ScheduleCommandTests.Camp(1, "you", "vale").World;
        Character vale = Cast(w, "vale"), you = Cast(w, "you");
        w.Tick = w.Content.World.TicksPerDay + 12L * w.Content.World.TicksPerHour; // noon: the whole of last night's watch is in the strips
        for (int h = 19; h < 24; h++) vale.Lived[h] = LivedKind.Watched;
        for (int h = 0; h < 5; h++) you.Lived[h] = LivedKind.Watched;
        CampWatchState st = StateApi.Schedule(w).Watch;
        Assert.Equal("Last night: Vale 19:00–00:00 · You 00:00–05:00 · nobody 05:00–06:00", st.LastNightText);
        Assert.Equal("", st.TonightText);
    }

    /// <summary>
    /// In the watch hours the hours already lived tonight are tonight's, not last night's: the strips hold last night only for
    /// the hours still to come, so the list says each apart instead of running tonight's first turn into last night's.
    /// </summary>
    [Fact]
    public void DuringTheWatchTonightsTurnsAreNotCalledLastNight()
    {
        World w = ScheduleCommandTests.Camp(1, "you", "vale").World;
        Character vale = Cast(w, "vale"), you = Cast(w, "you");
        w.Tick = w.Content.World.TicksPerDay + 2L * w.Content.World.TicksPerHour; // 02:00: 19:00–02:00 lived tonight
        for (int h = 19; h < 24; h++) vale.Lived[h] = LivedKind.Watched;
        for (int h = 0; h < 5; h++) you.Lived[h] = LivedKind.Watched;
        CampWatchState st = StateApi.Schedule(w).Watch;
        Assert.Equal("Tonight so far: Vale 19:00–00:00 · You 00:00–02:00", st.TonightText);
        Assert.Equal("Last night: You 02:00–05:00 · nobody 05:00–06:00", st.LastNightText);
        Assert.Equal(new[] { 19, 0 }, st.Tonight.Select(x => x.From));
        Assert.Equal(new[] { 2, 5 }, st.LastNight.Select(x => x.From));
    }

    /// <summary>Every line the Schedule writes reads as a sentence, with the player in each place the player can fill.</summary>
    [Fact]
    public void EveryScheduleLineReadsInTheThirdPersonAndTheSecond()
    {
        World w = ScheduleCommandTests.Camp(1, "you", "mira").World;
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        string Line(string key, int a, int b, params EventArg[] extra)
        {
            var args = new List<EventArg> { EventArg.Char("a", a), EventArg.Char("b", b) };
            args.AddRange(extra);
            SimEvent e = w.Log.Emit(w, SystemTag.Work, key, new[] { a, b }, Array.Empty<long>(), null, args.ToArray());
            return EventText.Describe(w, e);
        }
        EventArg[] counts = { EventArg.Num("sleep_hours", 9), EventArg.Num("work_hours", 13), EventArg.Num("free_hours", 2) };
        var cases = new (string Key, string Mira, string You)[]
        {
            ("event.work.took_watch", "Mira took the watch.", "You took the watch."),
            ("event.work.stayed_on_watch", "Mira stayed on watch when nobody came to take it.", "You stayed on watch when nobody came to take it."),
            ("event.work.left_watch_unrelieved", "Mira left the watch; nobody came to take it.", "You left the watch; nobody came to take it."),
            ("event.work.left_watch_hungry", "Mira left the watch to look for food.", "You left the watch to look for food."),
            ("event.needs.fell_asleep_on_watch", "Mira fell asleep at the watch post.", "You fell asleep at the watch post."),
            ("event.needs.worn_out", "Mira is worn out from long hours.", "You are worn out from long hours."),
            ("event.incident.work_accident_tired", "Mira, worn out, was hurt in an accident while working.", "You, worn out, were hurt in an accident while working."),
            ("event.world.person_day_reset", "Mira now keeps the camp's day.", "You now keep the camp's day."),
        };
        foreach ((string key, string asMira, string asYou) in cases)
        {
            EventArg day = EventArg.Key("day", "rhythm.diurnal.camp_day");
            Assert.Equal(asMira, Line(key, mira.Id, you.Id, day));
            Assert.Equal(asYou, Line(key, you.Id, mira.Id, day));
        }
        Assert.Equal("You changed Mira's day: sleep 9 hours, work 13, free time 2.", Line("event.world.person_day", mira.Id, you.Id, counts));
        Assert.Equal("You changed your own day: sleep 9 hours, work 13, free time 2.", Line("event.world.person_day", you.Id, mira.Id, counts));

        var pairs = new (string Key, string MiraWakesYou, string YouWakeMira)[]
        {
            ("event.work.went_to_wake", "Mira went to wake you for your turn on watch.", "You went to wake Mira for their turn on watch."),
            ("event.social.watch_refused_hurt", "You, still hurt, would not take the watch from Mira.", "Mira, still hurt, would not take the watch from you."),
            ("event.social.watch_refused_tired", "You were too tired to take the watch from Mira.", "Mira was too tired to take the watch from you."),
            ("event.social.watch_refused_hungry", "You, hungry, would not take the watch from Mira.", "Mira, hungry, would not take the watch from you."),
            ("event.social.watch_refused_grudge", "You would not get up for Mira.", "Mira would not get up for you."),
            ("event.work.covered_watch", "Mira kept the watch in your place.", "You kept the watch in Mira's place."),
            ("event.social.owes_watch", "Mira owes you for a watch kept in their place.", "You owe Mira for a watch kept in your place."),
            ("event.social.blood_refused_woken", "You, woken from sleep, refused to give Mira blood.", "Mira, woken from sleep, refused to give you blood."),
            ("event.work.worked_late", "Mira kept working past dark to keep their promise to you.", "You kept working past dark to keep your promise to Mira."),
        };
        foreach ((string key, string miraFirst, string youFirst) in pairs)
        {
            Assert.Equal(miraFirst, Line(key, mira.Id, you.Id));
            Assert.Equal(youFirst, Line(key, you.Id, mira.Id));
        }
        // Going one's own way is never the player's: those lines need only the third person.
        foreach (string why in new[] { Compliance.OwnWay, Compliance.Dislike, Compliance.Mood, Compliance.Ways })
        {
            Assert.DoesNotContain("⟦", Line(WatchSystem.OwnWayKey(why), you.Id, mira.Id));
            Assert.DoesNotContain("⟦", Line(ScheduleSystem.DeclinedDayKey(why), mira.Id, you.Id));
            Assert.DoesNotContain("⟦", Line(ScheduleSystem.KeptOldHoursKey(why), mira.Id, you.Id));
        }
    }
}
