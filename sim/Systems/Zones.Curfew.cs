using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// Who the home area binds, and tonight's roll (zones step 3, ADR-0028; zones design 5.5), on the Schedule's day
/// (ADR-0029). The home area binds everyone whose kind of day sleeps in the dark (all but the night folk). After dark
/// nobody it binds is asked to keep a watch post outside it, unless they go their own way tonight by the one
/// compliance rule (ADR-0025 item 2; never the player); the watch's side of it is in WatchSystem. The curfew proper
/// (keeping people from work, food, company and wandering outside it after dark, with its breach, hunger and kept-in
/// lines) moved to slice 2 with keep-out under the design's cut rule (section 9.2; ADR-0028's merge update): it made a
/// story in 3 of 300 home-area worlds against a floor of 15%.
/// </summary>
public static partial class Zones
{
    /// <summary>Is someone bound by the home area: the camp has one, and they are not of the night folk? (Dark or not.)</summary>
    public static bool Bound(World w, Character c) => HomeOf(w) != null && !NightFolk(w, c);

    /// <summary>The reason they go their own way tonight, or null: nobody counts as defiant before tonight's roll.</summary>
    public static string? DefiantTonight(World w, Character c) =>
        c.NightRollDay == Tonight(w) && c.DefiesTonight.Length > 0 ? c.DefiesTonight : null;

    /// <summary>Does the home area bind someone right now: after dark, bound by it, and not going their own way tonight?</summary>
    public static bool CurfewBinds(World w, Character c) => IsDark(w) && Bound(w, c) && DefiantTonight(w, c) == null;

    /// <summary>
    /// Runs at the start of every hour, before the watch and work (lead's call: the design rolled after mood, which left the
    /// watch's first turn at dusk deciding before anyone could be defiant; the mood read is the last hour's). At the dusk
    /// hour, when the camp has a home area, everyone it binds but the player rolls once whether they go their own way
    /// tonight (the home area's own tag, drawn only when the odds are above 0).
    /// </summary>
    public static void Hourly(World w)
    {
        if (Calendar.Hour(w.Tick, w.Content.World) != w.Content.World.DuskHour) return;
        Zone? home = HomeOf(w);
        if (home == null) return;
        int night = Tonight(w);
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member || !c.Named || NightFolk(w, c)) continue;
            c.NightRollDay = night;
            c.DefiesTonight = Compliance.Defies(w, c, home.MadeBy, DiceId.ZoneDefy) ?? "";
        }
    }
}
