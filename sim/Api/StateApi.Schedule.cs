using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Sim.Api;

/// <summary>
/// One hour of a day: its block (content id: sleep, work, free) or, in a lived strip, what it was lived as (slept,
/// worked, watched, free, needs) and its name; whose it is ("camp", "own", or "lived"); whether the sun keeps them
/// under cover then; and a line for its tooltip ("23:00 — On watch (planned: Sleep)").
/// </summary>
public sealed record HourState(int Hour, string Block, string Name, string Source, bool Sun, bool Differs, string Note);

/// <summary>A kind of day's camp day (ADR-0029): its title, its 24 hours, the members who keep it, and whether a queued or drafted change waits.</summary>
public sealed record DayState(string Rhythm, string Title, IReadOnlyList<HourState> Hours, IReadOnlyList<int> Members, bool Pending);

/// <summary>Who kept the watch for a stretch of last night (CharacterId 0: nobody), hours from inclusive, to exclusive.</summary>
public sealed record WatchSpan(int CharacterId, string Name, int From, int To);

/// <summary>
/// The camp's watch: its hours (equal: none) and turn length, who holds the post now and since when, last night as
/// lived, whether a change waits, and its lines in words. While the watch hours run, the hours already lived tonight
/// are Tonight's and LastNight holds only the hours still to come (the lived strips keep one occurrence of each hour).
/// (Named apart from the simulation's own WatchState.)
/// </summary>
public sealed record CampWatchState(int From, int To, int TurnHours, int WatcherId, string Watcher, long Since,
    IReadOnlyList<WatchSpan> LastNight, bool Pending, string Text, string Now, string LastNightText,
    IReadOnlyList<WatchSpan> Tonight, string TonightText);

/// <summary>Someone whose day differs from their kind's camp day, with what differs in words ("sleeps 07:00–15:00, works 22:00–06:00").</summary>
public sealed record PersonDaySummary(int Id, string Name, string Summary);

/// <summary>The Schedule list: every camp day, the watch, and whose day differs.</summary>
public sealed record ScheduleState(IReadOnlyList<DayState> CampDays, CampWatchState Watch, IReadOnlyList<PersonDaySummary> Different);

/// <summary>
/// One person's day for their inspector: planned (their resolved hours, own or the camp's), lived (the last 24 hours as
/// lived, by clock hour), the plain-words source of their day, the latest schedule line about them, whether they keep
/// the camp's day, whether a change waits, their hours of duty and free time in the last 24, whether they sleep at all,
/// and their kind of day.
/// </summary>
public sealed record PersonDayState(int Id, IReadOnlyList<HourState> Planned, IReadOnlyList<HourState> Lived, string Note, string Latest,
    bool SameAsCamp, bool Pending, int DutyHours, int FreeHours, bool Sleeps, string Rhythm);

public static partial class StateApi
{
    /// <summary>
    /// The Schedule as it will be once queued commands apply, and then <paramref name="drafts"/> (the view's choices made
    /// while paused). A pending change to someone's own day is shown as asked: the read cannot know whether they will go
    /// their own way, and does not try to (design 6.3).
    /// </summary>
    public static ScheduleState Schedule(World w, IEnumerable<Command>? drafts = null)
    {
        LocTable loc = w.Content.Loc;
        List<Command> wanted = Wanted(w, drafts);
        var days = new List<DayState>();
        foreach (RhythmDef def in w.Content.Rhythms)
        {
            IReadOnlyList<DayBlock> hours = CampHoursAfter(w, def.Id, wanted, out bool pending);
            var members = new List<int>();
            foreach (Character c in w.Members())
                if (ScheduleSystem.RhythmOf(w, c) == def.Id) members.Add(c.Id);
            var list = new List<HourState>(ScheduleSystem.HoursPerDay);
            for (int h = 0; h < ScheduleSystem.HoursPerDay; h++) list.Add(Planned(w, h, hours[h], "camp", sun: false));
            days.Add(new DayState(def.Id, loc.Get(def.NameKey), list, members, pending));
        }

        var different = new List<PersonDaySummary>();
        foreach (Character c in w.Members())
        {
            string summary = Summary(w, c, wanted);
            if (summary.Length > 0) different.Add(new PersonDaySummary(c.Id, EventText.Name(w, c.Id), summary));
        }
        return new ScheduleState(days, Watch(w, wanted), different);
    }

    /// <summary>One person's day, counting queued and drafted changes to it as the Schedule list does.</summary>
    public static PersonDayState Day(World w, int id, IEnumerable<Command>? drafts = null)
    {
        Character c = w.Get(id);
        LocTable loc = w.Content.Loc;
        List<Command> wanted = Wanted(w, drafts);
        DayBlock?[] own = OwnAfter(c, wanted, out bool ownPending);
        string rhythm = ScheduleSystem.RhythmOf(w, c);
        IReadOnlyList<DayBlock> camp = CampHoursAfter(w, rhythm, wanted, out bool campPending);
        bool tires = Places.Tires(w, c), burns = Places.BurnsInDaylight(w, c);
        WorldDef def = w.Content.World;

        var planned = new List<HourState>(ScheduleSystem.HoursPerDay);
        var plannedBlocks = new DayBlock[ScheduleSystem.HoursPerDay];
        for (int h = 0; h < ScheduleSystem.HoursPerDay; h++)
        {
            DayBlock b = own[h] ?? camp[h];
            if (b == DayBlock.Sleep && !tires) b = DayBlock.Free;
            plannedBlocks[h] = b;
            bool sun = burns && h >= def.DawnHour - 1 && h < def.DuskHour;
            planned.Add(Planned(w, h, b, own[h] != null ? "own" : "camp", sun));
        }

        var lived = new List<HourState>(ScheduleSystem.HoursPerDay);
        for (int h = 0; h < ScheduleSystem.HoursPerDay; h++)
        {
            LivedKind k = h < c.Lived.Count ? c.Lived[h] : LivedKind.Free;
            bool differs = !Matches(k, plannedBlocks[h]);
            string kName = loc.Get("lived." + Names.Snake(k.ToString()));
            string note = differs
                ? loc.Format("schedule.hour.lived_differs", Kv(("hour", EventText.Hour(w, h)), ("lived", kName), ("block", loc.Get("block." + ScheduleSystem.BlockId(plannedBlocks[h])))))
                : loc.Format("schedule.hour.lived", Kv(("hour", EventText.Hour(w, h)), ("lived", kName)));
            lived.Add(new HourState(h, Names.Snake(k.ToString()), kName, "lived", false, differs, note));
        }

        bool same = Array.TrueForAll(own, b => b == null);
        (int duty, int free, int _) = ScheduleSystem.LastDay(c);
        return new PersonDayState(c.Id, planned, lived, Note(w, c, same, tires, burns, rhythm), Latest(w, c), same, ownPending || campPending,
            duty, free, tires, rhythm);
    }

    /// <summary>The tooltip line for the watch post during the watch hours: who keeps it, or that nobody does.</summary>
    private static string? WatchPostLine(World w, Point p)
    {
        if (w.Map.WatchPost != p || !ScheduleSystem.InWatchHours(w, Calendar.Hour(w.Tick, w.Content.World))) return null;
        LocTable loc = w.Content.Loc;
        return w.Watch.WatcherId != 0 && w.Find(w.Watch.WatcherId) is { } watcher
            ? loc.Format("place.watch_post_kept", Kv(("a", EventText.Name(w, watcher.Id))))
            : loc.Get("place.watch_post_empty");
    }

    // ---- helpers ----

    private static List<Command> Wanted(World w, IEnumerable<Command>? drafts)
    {
        var list = new List<Command>();
        foreach (Command c in w.PendingCommands)
            if (c is SetCampDay or SetWatchHours or SetPersonDay) list.Add(c);
        if (drafts != null)
            foreach (Command c in drafts)
                if (c is SetCampDay or SetWatchHours or SetPersonDay) list.Add(c);
        return list;
    }

    private static IReadOnlyList<DayBlock> CampHoursAfter(World w, string rhythm, List<Command> wanted, out bool pending)
    {
        IReadOnlyList<DayBlock> now = ScheduleSystem.CampDayFor(w, rhythm)?.Hours ?? ScheduleSystem.DefaultDay(w.Content.RhythmById(rhythm));
        IReadOnlyList<DayBlock> shown = now;
        foreach (Command c in wanted)
            if (c is SetCampDay d && d.Rhythm == rhythm && d.Hours.Count == ScheduleSystem.HoursPerDay) shown = d.Hours;
        pending = !shown.SequenceEqual(now);
        return shown;
    }

    private static DayBlock?[] OwnAfter(Character c, List<Command> wanted, out bool pending)
    {
        DayBlock?[] now = ScheduleSystem.OwnDay(c), shown = now;
        foreach (Command cmd in wanted)
            if (cmd is SetPersonDay p && p.CharacterId == c.Id && p.Hours.Count == ScheduleSystem.HoursPerDay) shown = p.Hours.ToArray();
        pending = !shown.SequenceEqual(now);
        return shown;
    }

    private static HourState Planned(World w, int hour, DayBlock b, string source, bool sun)
    {
        LocTable loc = w.Content.Loc;
        string name = loc.Get("block." + ScheduleSystem.BlockId(b));
        string note = loc.Format(sun ? "schedule.hour.sun" : "schedule.hour.planned", Kv(("hour", EventText.Hour(w, hour)), ("block", name)));
        return new HourState(hour, ScheduleSystem.BlockId(b), name, source, sun, false, note);
    }

    /// <summary>Was an hour lived as planned? Sleep as slept; work as worked, watched or idle between jobs; free time as free; needs always bend the plan.</summary>
    private static bool Matches(LivedKind k, DayBlock b) => b switch
    {
        DayBlock.Sleep => k == LivedKind.Slept,
        DayBlock.Work => k is LivedKind.Worked or LivedKind.Watched or LivedKind.Free,
        _ => k == LivedKind.Free,
    };

    private static CampWatchState Watch(World w, List<Command> wanted)
    {
        LocTable loc = w.Content.Loc;
        int from = w.WatchFrom, to = w.WatchTo;
        foreach (Command c in wanted)
            if (c is SetWatchHours h) (from, to) = (h.From, h.To);
        bool pending = ScheduleSystem.RangeLength(from, to) == 0 ? w.WatchFrom != w.WatchTo : (from, to) != (w.WatchFrom, w.WatchTo);
        int turn = w.Content.World.Schedule.WatchTurnHours;
        string text = ScheduleSystem.RangeLength(from, to) == 0
            ? loc.Get("schedule.watch.none")
            : loc.Format(turn == 1 ? "schedule.watch.kept_one" : "schedule.watch.kept",
                Kv(("from", EventText.Hour(w, from)), ("to", EventText.Hour(w, to)), ("n", turn.ToString(Inv))));

        int watcherId = w.Watch.WatcherId;
        long since = w.Log.Get(w.Watch.TurnEvent)?.Tick ?? 0;
        string now = watcherId != 0
            ? loc.Format("schedule.watch.now", Kv(("a", EventText.Name(w, watcherId)), ("since", EventText.Clock(w, since))))
            : loc.Get("schedule.watch.nobody_now");

        // While the watch hours run, the hours already lived belong to tonight; the strips still hold last night's only for the rest.
        int hours = ScheduleSystem.RangeLength(w.WatchFrom, w.WatchTo);
        int hourNow = Calendar.Hour(w.Tick, w.Content.World);
        int tonight = ScheduleSystem.InWatchHours(w, hourNow) ? ScheduleSystem.RangeLength(w.WatchFrom, hourNow) : 0;
        List<WatchSpan> tonightSpans = Spans(w, 0, tonight), lastSpans = Spans(w, tonight, hours);
        return new CampWatchState(from, to, turn, watcherId, watcherId == 0 ? "" : EventText.Name(w, watcherId), since, lastSpans, pending, text, now,
            SpansText(w, "schedule.watch.last_night", lastSpans), tonightSpans, SpansText(w, "schedule.watch.tonight", tonightSpans));
    }

    /// <summary>
    /// The watch as lived from the strips (no state of its own), over the watch hours from the <paramref name="first"/>th to
    /// before the <paramref name="end"/>th: for each hour, who lived it as on watch (nobody, when no one did).
    /// </summary>
    private static List<WatchSpan> Spans(World w, int first, int end)
    {
        var spans = new List<WatchSpan>();
        for (int i = first; i < end; i++)
        {
            int h = (w.WatchFrom + i) % ScheduleSystem.HoursPerDay;
            int who = 0;
            foreach (Character c in w.Characters)
                if (c.Named && c.Status == CharStatus.Member && h < c.Lived.Count && c.Lived[h] == LivedKind.Watched) { who = c.Id; break; }
            if (spans.Count > 0 && spans[^1].CharacterId == who)
                spans[^1] = spans[^1] with { To = (h + 1) % ScheduleSystem.HoursPerDay };
            else
                spans.Add(new WatchSpan(who, who == 0 ? "" : EventText.Name(w, who), h, (h + 1) % ScheduleSystem.HoursPerDay));
        }
        return spans;
    }

    /// <summary>"Last night: Vale 19:00–05:00 · nobody 05:00–06:00" (or tonight's), "" with no spans.</summary>
    private static string SpansText(World w, string key, List<WatchSpan> spans)
    {
        LocTable loc = w.Content.Loc;
        var parts = new List<string>(spans.Count);
        foreach (WatchSpan s in spans)
        {
            string range = loc.Format("schedule.range", Kv(("from", EventText.Hour(w, s.From)), ("to", EventText.Hour(w, s.To))));
            parts.Add(s.CharacterId == 0
                ? loc.Format("schedule.watch.span_nobody", Kv(("range", range)))
                : loc.Format("schedule.watch.span", Kv(("a", s.Name), ("range", range))));
        }
        return parts.Count == 0 ? "" : loc.Format(key, Kv(("spans", string.Join(loc.Get("schedule.watch.span_sep"), parts))));
    }

    /// <summary>What differs in someone's day from their kind's camp day, in words; "" when nothing does.</summary>
    private static string Summary(World w, Character c, List<Command> wanted)
    {
        LocTable loc = w.Content.Loc;
        DayBlock?[] own = OwnAfter(c, wanted, out _);
        IReadOnlyList<DayBlock> camp = CampHoursAfter(w, ScheduleSystem.RhythmOf(w, c), wanted, out _);
        bool tires = Places.Tires(w, c);
        var day = new DayBlock[ScheduleSystem.HoursPerDay];
        var differs = new bool[ScheduleSystem.HoursPerDay];
        bool any = false;
        for (int h = 0; h < ScheduleSystem.HoursPerDay; h++)
        {
            DayBlock b = own[h] ?? camp[h];
            if (b == DayBlock.Sleep && !tires) b = DayBlock.Free;
            day[h] = b;
            differs[h] = b != (camp[h] == DayBlock.Sleep && !tires ? DayBlock.Free : camp[h]);
            any |= differs[h];
        }
        if (!any) return "";
        var parts = new List<string>();
        foreach (DayBlock block in Enum.GetValues<DayBlock>())
        {
            var runs = new List<(int From, int To)>();
            for (int h = 0; h < ScheduleSystem.HoursPerDay;)
            {
                if (!differs[h] || day[h] != block) { h++; continue; }
                int from = h;
                while (h < ScheduleSystem.HoursPerDay && differs[h] && day[h] == block) h++;
                runs.Add((from, h % ScheduleSystem.HoursPerDay));
            }
            // A run that reaches midnight and one that starts at it are one run across midnight.
            if (runs.Count > 1 && runs[0].From == 0 && runs[^1].To == 0)
            {
                runs[0] = (runs[^1].From, runs[0].To);
                runs.RemoveAt(runs.Count - 1);
            }
            if (runs.Count == 0) continue;
            var ranges = new List<string>(runs.Count);
            foreach ((int from, int to) in runs)
                ranges.Add(loc.Format("schedule.range", Kv(("from", EventText.Hour(w, from)), ("to", EventText.Hour(w, to)))));
            parts.Add(loc.Format("schedule.own." + ScheduleSystem.BlockId(block), Kv(("range", string.Join(loc.Get("schedule.own.and"), ranges)))));
        }
        return string.Join(loc.Get("schedule.own.sep"), parts);
    }

    /// <summary>The plain-words source of someone's day: their kind's, their own, that they never sleep, that the sun keeps them in, or that they would not change it.</summary>
    private static string Note(World w, Character c, bool same, bool tires, bool burns, string rhythm)
    {
        LocTable loc = w.Content.Loc;
        string name = EventText.Name(w, c.Id);
        string Say(string key, params (string, string)[] args) =>
            loc.Format(c.Id == w.PlayerId && loc.Has(key + "_you_a") ? key + "_you_a" : key, Kv(args));
        var lines = new List<string>();
        if (Declined(w, c)) lines.Add(Say("schedule.note.declined", ("a", name)));
        else lines.Add(same ? loc.Format("schedule.note.camp", Kv(("day", loc.Get(w.Content.RhythmById(rhythm).CampDayKey)))) : loc.Get("schedule.note.own"));
        if (!tires) lines.Add(Say("schedule.note.never_sleeps", ("a", name)));
        if (burns) lines.Add(loc.Format("schedule.note.sun", Kv(("a", name))));
        return string.Join(" ", lines);
    }

    /// <summary>Did their latest decline (of their own day, or of a change to the camp's) come after the day they were last given?</summary>
    private static bool Declined(World w, Character c)
    {
        List<SimEvent> all = w.Log.All;
        for (int i = all.Count - 1; i >= 0 && all[i].Id > c.OwnHoursEvent; i--)
        {
            SimEvent e = all[i];
            if (e.Actors.Count > 0 && e.Actors[0] == c.Id && IsDecline(e.Key)) return true;
        }
        return false;
    }

    /// <summary>The most recent schedule line about someone (their day, the watch, their wear), with when; "" if none.</summary>
    private static string Latest(World w, Character c)
    {
        List<SimEvent> all = w.Log.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            SimEvent e = all[i];
            if (!e.Actors.Contains(c.Id) || !IsScheduleLine(e.Key)) continue;
            return w.Content.Loc.Format("schedule.latest", Kv(("text", EventText.Describe(w, e)), ("when", EventText.When(w, e.Tick))));
        }
        return "";
    }

    /// <summary>A line the Schedule writes: a change of days or watch hours, a decline, a turn, a waking, a refusal, a cover, a stay, a gap, wear.</summary>
    public static bool IsScheduleLine(string key) =>
        key is "event.world.camp_day" or "event.world.watch_hours" or "event.world.person_day" or "event.world.person_day_reset"
            or "event.work.took_watch" or "event.work.went_to_wake" or "event.work.covered_watch" or "event.work.stayed_on_watch"
            or "event.work.left_watch_unrelieved" or "event.work.left_watch_hungry" or "event.work.watch_unkept" or "event.work.worked_late"
            or "event.needs.fell_asleep_on_watch" or "event.needs.worn_out" or "event.incident.work_accident_tired" or "event.incident.work_accident_tired_dark" or "event.incident.stolen_unwatched"
            or "event.social.owes_watch" or "event.social.owes_watch_sworn" or "event.social.blood_refused_woken"
            or "event.social.watch_refused_hurt" or "event.social.watch_refused_tired" or "event.social.watch_refused_hungry"
            or "event.social.watch_refused_grudge"
        || IsDecline(key) || Array.Exists(Reasons, why => key == WatchSystem.OwnWayKey(why));

    private static readonly string[] Reasons = { Compliance.OwnWay, Compliance.Dislike, Compliance.Mood, Compliance.Ways };

    /// <summary>Going one's own way over a change of day: declining one's own, or keeping the old hours of the camp's.</summary>
    private static bool IsDecline(string key) =>
        Array.Exists(Reasons, why => key == ScheduleSystem.DeclinedDayKey(why) || key == ScheduleSystem.KeptOldHoursKey(why));

    private static KeyValuePair<string, string>[] Kv(params (string Name, string Value)[] args)
    {
        var kv = new KeyValuePair<string, string>[args.Length];
        for (int i = 0; i < args.Length; i++) kv[i] = new KeyValuePair<string, string>(args[i].Name, args[i].Value);
        return kv;
    }
}
