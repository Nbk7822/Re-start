using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// The Schedule's commands as they apply (ADR-0029). Each says what the player decided in one line, then lets
/// each person it touches decide by the one compliance rule (ADR-0025 item 2; the player never rolls): someone who
/// goes their own way over a change to the camp's day keeps the old hours as their own; over a change to their own
/// day, keeps the day they had. Neither is seen by anyone, so neither has witnesses.
/// </summary>
public static partial class ScheduleSystem
{
    public static void ApplyCampDay(World w, SetCampDay cmd)
    {
        CampDay? day = CampDayFor(w, cmd.Rhythm);
        if (day == null || cmd.Hours is not { Count: HoursPerDay } || day.Hours.SequenceEqual(cmd.Hours)) return;
        var keepers = new List<(Character C, List<DayBlock> Before)>();
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member && RhythmOf(w, c) == cmd.Rhythm) keepers.Add((c, PlannedDay(w, c)));
        var old = new List<DayBlock>(day.Hours);
        day.Hours = new List<DayBlock>(cmd.Hours);
        (int sleep, int work, int free) = Counts(day.Hours);
        SimEvent e = w.Log.Emit(w, SystemTag.World, "event.world.camp_day", PlayerActors(w), Array.Empty<long>(), null,
            EventArg.Key("day", w.Content.RhythmById(cmd.Rhythm).CampDayKey),
            EventArg.Num("sleep_hours", sleep), EventArg.Num("work_hours", work), EventArg.Num("free_hours", free));
        day.SetEvent = e.Id;
        foreach ((Character c, List<DayBlock> before) in keepers)
        {
            var changed = new List<int>();
            for (int h = 0; h < HoursPerDay; h++)
                if (OwnBlock(c, h) == null && old[h] != day.Hours[h]) changed.Add(h);
            if (changed.Count == 0) continue;
            string? why = Compliance.Defies(w, c, w.PlayerId, DiceId.DayChangeDefy);
            if (why != null)
            {
                foreach (int h in changed) SetOwn(c, h, old[h]);
                w.Log.Emit(w, SystemTag.Social, KeptOldHoursKey(why), new[] { c.Id },
                    new[] { e.Id, Compliance.ReasonCause(w, c, w.PlayerId, why) }, null, EventArg.Char("a", c.Id));
                continue;
            }
            if (WorkHours(PlannedDay(w, c)) > WorkHours(before)) c.LastDutyEvent = e.Id;
        }
    }

    public static void ApplyWatchHours(World w, SetWatchHours cmd)
    {
        int from = Math.Clamp(cmd.From, 0, HoursPerDay - 1), to = Math.Clamp(cmd.To, 0, HoursPerDay - 1);
        int hours = RangeLength(from, to);
        bool same = hours == 0 ? w.WatchFrom == w.WatchTo : w.WatchFrom == from && w.WatchTo == to;
        if (same) return;
        w.WatchFrom = from;
        w.WatchTo = to;
        SimEvent e = w.Log.Emit(w, SystemTag.World, "event.world.watch_hours", PlayerActors(w), Array.Empty<long>(), null,
            EventArg.Hour("from", from), EventArg.Hour("to", to), EventArg.Num("n", hours));
        w.WatchEvent = e.Id;
        // With no watch left, those who came back to keep it lose their purpose, and this decision is why (design 4.3).
        if (hours == 0)
            foreach (Character c in w.Members()) Purpose.OnPriority(w, c, "keep_watch", 0, e.Id);
    }

    public static void ApplyPersonDay(World w, SetPersonDay cmd)
    {
        Character? c = w.Find(cmd.CharacterId);
        if (c is not { Status: CharStatus.Member } || cmd.Hours is not { Count: HoursPerDay }) return;
        var own = new List<HourBlock>();
        for (int h = 0; h < HoursPerDay; h++)
            if (cmd.Hours[h] is { } b) own.Add(new HourBlock { Hour = h, Block = b });
        if (SameHours(c.OwnHours, own)) return;
        List<DayBlock> before = PlannedDay(w, c);
        var after = new List<DayBlock>(HoursPerDay);
        string rhythm = RhythmOf(w, c);
        for (int h = 0; h < HoursPerDay; h++)
        {
            DayBlock b = cmd.Hours[h] ?? CampBlock(w, rhythm, h);
            after.Add(b == DayBlock.Sleep && !Places.Tires(w, c) ? DayBlock.Free : b);
        }
        (int sleep, int work, int free) = Counts(after);
        int[] actors = c.Id == w.PlayerId || w.PlayerId == 0 ? new[] { c.Id } : new[] { w.PlayerId, c.Id };
        SimEvent e = own.Count == 0
            ? w.Log.Emit(w, SystemTag.World, "event.world.person_day_reset", actors, Array.Empty<long>(), null,
                EventArg.Char("a", c.Id), EventArg.Key("day", w.Content.RhythmById(rhythm).CampDayKey))
            : w.Log.Emit(w, SystemTag.World, "event.world.person_day", actors, Array.Empty<long>(), null,
                EventArg.Char("a", c.Id), EventArg.Num("sleep_hours", sleep), EventArg.Num("work_hours", work), EventArg.Num("free_hours", free));
        string? why = Compliance.Defies(w, c, w.PlayerId, DiceId.DayChangeDefy);
        if (why != null)
        {
            w.Log.Emit(w, SystemTag.Social, DeclinedDayKey(why), new[] { c.Id },
                new[] { e.Id, Compliance.ReasonCause(w, c, w.PlayerId, why) }, null, EventArg.Char("a", c.Id));
            return;
        }
        c.OwnHours = own;
        c.OwnHoursEvent = e.Id;
        if (WorkHours(after) > WorkHours(before)) c.LastDutyEvent = e.Id;
    }

    /// <summary>Would a schedule command change anything now? (A draft that changes nothing is never sent.)</summary>
    public static bool Changes(World w, Command cmd) => cmd switch
    {
        SetCampDay d => CampDayFor(w, d.Rhythm) is { } day && d.Hours is { Count: HoursPerDay } && !day.Hours.SequenceEqual(d.Hours),
        SetWatchHours h => RangeLength(h.From, h.To) == 0 ? w.WatchFrom != w.WatchTo : (w.WatchFrom, w.WatchTo) != (h.From, h.To),
        SetPersonDay p => w.Find(p.CharacterId) is { Status: CharStatus.Member } c && p.Hours is { Count: HoursPerDay } && !OwnDay(c).SequenceEqual(p.Hours),
        _ => false,
    };

    /// <summary>The line for going one's own way over a change to one's own day, by Compliance's reason.</summary>
    public static string DeclinedDayKey(string why) => why switch
    {
        Compliance.Dislike => "event.social.declined_day_dislike",
        Compliance.Mood => "event.social.declined_day_mood",
        Compliance.Ways => "event.social.declined_day_ways",
        _ => "event.social.declined_day_own_way",
    };

    /// <summary>The line for keeping the old hours when the camp's day changed, by Compliance's reason.</summary>
    public static string KeptOldHoursKey(string why) => why switch
    {
        Compliance.Dislike => "event.social.kept_old_hours_dislike",
        Compliance.Mood => "event.social.kept_old_hours_mood",
        Compliance.Ways => "event.social.kept_old_hours_ways",
        _ => "event.social.kept_old_hours_own_way",
    };

    /// <summary>Hours of sleep, work and free time in a day.</summary>
    public static (int Sleep, int Work, int Free) Counts(IReadOnlyList<DayBlock> day)
    {
        int sleep = 0, work = 0, free = 0;
        foreach (DayBlock b in day)
            if (b == DayBlock.Sleep) sleep++;
            else if (b == DayBlock.Work) work++;
            else free++;
        return (sleep, work, free);
    }

    private static int WorkHours(IReadOnlyList<DayBlock> day) => Counts(day).Work;

    private static int[] PlayerActors(World w) => w.Find(w.PlayerId) is { } p ? new[] { p.Id } : Array.Empty<int>();

    /// <summary>Sets one of someone's own hours, keeping the list in hour order.</summary>
    internal static void SetOwn(Character c, int hour, DayBlock block)
    {
        int i = 0;
        while (i < c.OwnHours.Count && c.OwnHours[i].Hour < hour) i++;
        if (i < c.OwnHours.Count && c.OwnHours[i].Hour == hour) c.OwnHours[i].Block = block;
        else c.OwnHours.Insert(i, new HourBlock { Hour = hour, Block = block });
    }

    /// <summary>Do two lists of own hours say the same thing (both in hour order)?</summary>
    public static bool SameHours(IReadOnlyList<HourBlock> a, IReadOnlyList<HourBlock> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i].Hour != b[i].Hour || a[i].Block != b[i].Block) return false;
        return true;
    }

    /// <summary>Someone's own hours as a 24-entry list (null where they keep the camp's day), as SetPersonDay carries them.</summary>
    public static DayBlock?[] OwnDay(Character c)
    {
        var day = new DayBlock?[HoursPerDay];
        foreach (HourBlock h in c.OwnHours)
            if (h.Hour is >= 0 and < HoursPerDay) day[h.Hour] = h.Block;
        return day;
    }
}
