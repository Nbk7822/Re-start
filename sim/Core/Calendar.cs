using Return.Sim.Content;

namespace Return.Sim.Core;

/// <summary>Tick to calendar arithmetic. Day 0 is the first day; all values are 0-based.</summary>
public static class Calendar
{
    public static int Hour(long tick, WorldDef w) => (int)(tick / w.TicksPerHour % w.HoursPerDay);

    public static int Minute(long tick, WorldDef w) => (int)(tick % w.TicksPerHour * 60 / w.TicksPerHour);

    public static int Day(long tick, WorldDef w) => (int)(tick / w.TicksPerDay);

    public static int DayOfSeason(long tick, WorldDef w) => Day(tick, w) % w.DaysPerSeason;

    public static int Season(long tick, WorldDef w) => Day(tick, w) / w.DaysPerSeason % w.SeasonsPerYear;

    public static int Year(long tick, WorldDef w) => (int)(tick / w.TicksPerYear);

    public static bool IsDaylight(long tick, WorldDef w)
    {
        int h = Hour(tick, w);
        return h >= w.DawnHour && h < w.DuskHour;
    }

    public static bool IsHourStart(long tick, WorldDef w) => tick % w.TicksPerHour == 0;

    public static bool IsDayStart(long tick, WorldDef w) => tick % w.TicksPerDay == 0;
}
