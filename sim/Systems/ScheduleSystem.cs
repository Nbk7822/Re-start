using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// The camp's day (ADR-0029). Each kind of day (rhythms.json) has one camp day the player paints; a person
/// keeps their kind's camp day except at hours they have their own. The body sits above the schedule: who
/// sleeps at all is their species' tires, so a stray sleep hour reads as free time for those who never tire,
/// and hunger, injury, exhaustion and the sun always beat the block (JobPicker).
/// </summary>
public static partial class ScheduleSystem
{
    public const int HoursPerDay = 24;

    /// <summary>The kinds of day the code falls back to (RequiredContent.Rhythms): for a species that tires, and for one that does not.</summary>
    public const string Diurnal = "diurnal", Sleepless = "sleepless";

    /// <summary>The kind of day someone keeps: their species' own, else diurnal if they tire and sleepless if not.</summary>
    public static string RhythmOf(World w, Character c)
    {
        SpeciesDef sp = w.Content.SpeciesById(c.SpeciesId);
        return sp.Rhythm.Length > 0 ? sp.Rhythm : sp.Tires ? Diurnal : Sleepless;
    }

    /// <summary>The camp day of a kind of day, or null if the world has none for it.</summary>
    public static CampDay? CampDayFor(World w, string rhythm)
    {
        foreach (CampDay d in w.CampDays)
            if (d.Rhythm == rhythm) return d;
        return null;
    }

    /// <summary>The block a kind of day's camp day keeps at an hour (content's default day if the world has none for it).</summary>
    public static DayBlock CampBlock(World w, string rhythm, int hour)
    {
        CampDay? d = CampDayFor(w, rhythm);
        if (d != null && d.Hours.Count == HoursPerDay) return d.Hours[hour];
        RhythmDef? def = w.Content.TryRhythm(rhythm);
        return def == null ? DayBlock.Work : DefaultDay(def)[hour];
    }

    /// <summary>Their own block at an hour, or null where they keep the camp's day.</summary>
    public static DayBlock? OwnBlock(Character c, int hour)
    {
        foreach (HourBlock h in c.OwnHours)
            if (h.Hour == hour) return h.Block;
        return null;
    }

    /// <summary>
    /// What someone's day says at an hour: their own hour if set, else their kind's camp day. A sleep hour reads
    /// as free time for anyone who never tires (the body sits above the schedule). Observances (culture-faith
    /// slice 2) will come first.
    /// </summary>
    public static DayBlock BlockAt(World w, Character c, int hour)
    {
        DayBlock b = OwnBlock(c, hour) ?? CampBlock(w, RhythmOf(w, c), hour);
        return b == DayBlock.Sleep && !Places.Tires(w, c) ? DayBlock.Free : b;
    }

    public static DayBlock BlockNow(World w, Character c) => BlockAt(w, c, Calendar.Hour(w.Tick, w.Content.World));

    /// <summary>Is it their time to sleep? Never for those who do not tire.</summary>
    public static bool IsSleepTime(World w, Character c) => BlockNow(w, c) == DayBlock.Sleep;

    /// <summary>A block's id as content names it: sleep, work or free.</summary>
    public static string BlockId(DayBlock b) => Names.Snake(b.ToString());

    /// <summary>A block from its content id; null if unknown.</summary>
    public static DayBlock? ParseBlock(string id)
    {
        foreach (DayBlock b in Enum.GetValues<DayBlock>())
            if (BlockId(b) == id) return b;
        return null;
    }

    /// <summary>A kind of day's default camp day, hour by hour.</summary>
    public static List<DayBlock> DefaultDay(RhythmDef def)
    {
        var day = new List<DayBlock>(HoursPerDay);
        for (int h = 0; h < HoursPerDay; h++) day.Add(DayBlock.Work);
        foreach (RhythmRange r in def.Day)
        {
            DayBlock b = ParseBlock(r.Block) ?? DayBlock.Work;
            for (int h = r.From; h != r.To; h = (h + 1) % HoursPerDay) day[h] = b;
        }
        return day;
    }

    /// <summary>Someone's whole day as planned: their resolved block at each hour.</summary>
    public static List<DayBlock> PlannedDay(World w, Character c)
    {
        var day = new List<DayBlock>(HoursPerDay);
        for (int h = 0; h < HoursPerDay; h++) day.Add(BlockAt(w, c, h));
        return day;
    }

    /// <summary>
    /// Fills what a world lacks from content: a camp day for every kind of day, the default watch hours (only for a
    /// world with no camp days at all, so a choice of "no watch" survives), and a lived strip for every named person. A
    /// lived strip starts as their planned day with no duty in it, so nobody arrives overworked. Idempotent: a
    /// world that has all of it is left exactly as it is, so a restored snapshot hashes like the world it came from.
    /// </summary>
    public static void EnsureDefaults(World w)
    {
        if (w.CampDays.Count == 0)
        {
            WatchRangeDef watch = w.Content.World.Schedule.Watch;
            w.WatchFrom = watch.From;
            w.WatchTo = watch.To;
        }
        foreach (RhythmDef def in w.Content.Rhythms)
            if (CampDayFor(w, def.Id) == null) w.CampDays.Add(new CampDay { Rhythm = def.Id, Hours = DefaultDay(def) });
        foreach (Character c in w.Characters)
            if (c.Named && c.Lived.Count != HoursPerDay) ResetLived(w, c); // thieves keep no hours
    }

    /// <summary>Their lived strip as their planned day, without duty: sleep hours slept, the rest free.</summary>
    public static void ResetLived(World w, Character c)
    {
        c.Lived.Clear();
        for (int h = 0; h < HoursPerDay; h++) c.Lived.Add(BlockAt(w, c, h) == DayBlock.Sleep ? LivedKind.Slept : LivedKind.Free);
        c.HourTally.Clear();
    }

    /// <summary>Is an hour inside the camp's watch hours?</summary>
    public static bool InWatchHours(World w, int hour) => InRange(w.WatchFrom, w.WatchTo, hour);

    /// <summary>Is an hour in [from, to), wrapping past midnight; never when from equals to.</summary>
    public static bool InRange(int from, int to, int hour) =>
        from != to && (from < to ? hour >= from && hour < to : hour >= from || hour < to);

    /// <summary>How many hours [from, to) covers, wrapping; 0 when from equals to.</summary>
    public static int RangeLength(int from, int to) => from == to ? 0 : (to - from + HoursPerDay) % HoursPerDay;
}
