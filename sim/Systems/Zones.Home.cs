using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// The home area (zones step 3, ADR-0028; zones design 3.3, 5.5): the one area the camp marks as home. In slice 1 it asks
/// one thing, that after dark nobody but the night folk keep watch outside it; the curfew proper (keeping people inside it
/// after dark) waits for slice 2. It is built on the Schedule's day (ADR-0029): the Schedule says when, the home area says
/// where. These are its reads; who it binds and tonight's roll are in Zones.Curfew.cs, the watch's side in WatchSystem.
/// </summary>
public static partial class Zones
{
    /// <summary>The behaviour of the home area: after dark, only the night folk are asked to keep watch outside it.</summary>
    public const string HomeArea = "home";

    /// <summary>The camp's home area (the first zone by id whose kind has the home behaviour), or null if it has none.</summary>
    public static Zone? HomeOf(World w)
    {
        foreach (Zone z in w.Map.Zones)
            if (z.Tiles.Count > 0 && Def(w, z) is { Behaviour: HomeArea }) return z;
        return null;
    }

    public static bool IsHome(World w, Zone z) => Def(w, z)?.Behaviour == HomeArea;

    /// <summary>Does the home area take in this tile? False everywhere while the camp has none.</summary>
    public static bool InHome(World w, Point p) => w.Map.InBounds(p) && HomeCache(w)[w.Map.Index(p)] != 0;

    /// <summary>
    /// The night a tick belongs to: the day its dusk fell on, so 02:00 on day 21 is night 20 (lead's call in the design).
    /// The home area's once-a-night fields hold this number.
    /// </summary>
    public static int NightOf(World w, long tick)
    {
        WorldDef def = w.Content.World;
        int day = Calendar.Day(tick, def);
        return Calendar.Hour(tick, def) < def.DawnHour ? day - 1 : day;
    }

    /// <summary>Tonight, by <see cref="NightOf"/>.</summary>
    public static int Tonight(World w) => NightOf(w, w.Tick);
}
