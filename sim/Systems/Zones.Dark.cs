using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// The dark (zones design 5.5; ADR-0028, step 3), zone or no zone: from dusk to dawn, beyond the firelight, work goes
/// wrong twice as often for anyone whose kind of day sleeps in the dark. The night folk (whose shipped day has no
/// sleep in the dark: the vampires and the undead) see in it, and the home area never binds them. Pure reads, no draws.
/// </summary>
public static partial class Zones
{
    /// <summary>
    /// A tile within this straight-line distance of the hearth lies in the firelight (dx² + dy² ≤ 64, in integers). The
    /// view's firelight (a 9.5-tile falloff, ADR-0019) fades just beyond it; keep the two in step.
    /// </summary>
    public const int LitRange = 8;

    /// <summary>Accidents at work in the dark beyond the firelight come this many times as often, in percent (design question 2); they multiply with being worn out.</summary>
    public const int DarkAccidentPct = 200;

    /// <summary>From dusk to dawn (not daylight).</summary>
    public static bool IsDark(World w) => !Calendar.IsDaylight(w.Tick, w.Content.World);

    /// <summary>Does a tile lie in the hearth's light?</summary>
    public static bool Lit(World w, Point p)
    {
        Point h = w.Map.Hearth;
        int dx = p.X - h.X, dy = p.Y - h.Y;
        return dx * dx + dy * dy <= LitRange * LitRange;
    }

    /// <summary>
    /// The night folk: their species keeps a kind of day (rhythms.json) whose shipped day has no sleep hours in the
    /// dark, so the dark is their day (today the vampires, nocturnal, and the undead, sleepless). Read from content, not
    /// from the camp day the player paints: putting a human on nights does not make them see in the dark (lead's call:
    /// no species field and no species id in code).
    /// </summary>
    public static bool NightFolk(World w, Character c)
    {
        RhythmDef? def = w.Content.TryRhythm(ScheduleSystem.RhythmOf(w, c));
        if (def == null) return false;
        WorldDef wd = w.Content.World;
        foreach (RhythmRange r in def.Day)
        {
            if (ScheduleSystem.ParseBlock(r.Block) != DayBlock.Sleep) continue;
            for (int h = r.From; h != r.To; h = (h + 1) % ScheduleSystem.HoursPerDay)
                if (h >= wd.DuskHour || h < wd.DawnHour) return false;
        }
        return true;
    }

    /// <summary>Someone working where they stand now would be working in the dark: after dusk, beyond the firelight, and not of the night folk.</summary>
    public static bool InTheDark(World w, Character c) => IsDark(w) && !Lit(w, c.Pos) && !NightFolk(w, c);
}
