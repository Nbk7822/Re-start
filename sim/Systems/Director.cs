using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// The storyteller's pacing for M0: pressure rises every hour; major incidents (spoilage, night
/// thieves) roll with odds scaled by pressure, cost pressure when they fire, and never come within
/// minHoursBetweenMajors of each other. Rain and windfalls roll on their own small odds.
/// </summary>
public static class Director
{
    public static void Hourly(World w)
    {
        WorldDef def = w.Content.World;
        DirectorState st = w.Director;
        DiceStream dice = w.Rng.Dice[DiceSystem.Incident];
        int hour = Calendar.Hour(w.Tick, def);
        int day = Calendar.Day(w.Tick, def);
        st.Pressure = Math.Min(100, st.Pressure + def.Director.PressurePerHour);

        if (!w.Weather.Raining && dice.Chance(DiceId.RainStart, w.Content.IncidentById("rain").Param("startPermillePerHour")))
            IncidentSystem.Fire(w, "rain");

        if (hour == 10 && dice.Chance(DiceId.WindfallRoll, w.Content.IncidentById("windfall").Param("dailyPermille")))
            IncidentSystem.Fire(w, "windfall");

        if (w.Tick - st.LastMajorTick < (long)def.Director.MinHoursBetweenMajors * def.TicksPerHour) return;

        IncidentDef spoils = w.Content.IncidentById("food_spoils");
        if (hour == 6 && Stock.Count(w, "berries") >= spoils.Param("minBerries")
            && dice.Chance(DiceId.SpoilRoll, spoils.Param("dailyPermille") * st.Pressure / 50))
        {
            Major(w, "food_spoils");
            return;
        }

        IncidentDef thief = w.Content.IncidentById("night_thief");
        if (!Calendar.IsDaylight(w.Tick, def) && day >= thief.Param("earliestDay") && !HostilePresent(w)
            && dice.Chance(DiceId.ThiefRoll, thief.Param("nightPermillePerHour") * st.Pressure / 50))
            Major(w, "night_thief");
    }

    private static void Major(World w, string id)
    {
        if (IncidentSystem.Fire(w, id) == null) return;
        w.Director.Pressure = Math.Max(0, w.Director.Pressure - w.Content.World.Director.MajorCost);
        w.Director.LastMajorTick = w.Tick;
    }

    private static bool HostilePresent(World w)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Hostile) return true;
        return false;
    }
}
