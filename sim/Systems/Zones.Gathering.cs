using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Meeting places (zones step 2, ADR-0028): where idle people gather, who they gather with, and who keeps
/// away. It puts the relationship graph on the map: people go where the people they like are sitting,
/// and someone who holds a grudge against a person sitting there keeps away and sits apart.
/// </summary>
public static partial class Zones
{
    /// <summary>The behaviour of a meeting place: idle people gather there.</summary>
    public const string Gather = "gather";

    /// <summary>A meeting place is "by the fire" when one of its tiles lies this close to the hearth (the fire's comfort radius).</summary>
    public const int FireRadius = 3;

    /// <summary>
    /// Where someone idle would go: the meeting place they would rather sit in (<see cref="Place"/>), or,
    /// when every meeting place has someone in it they hold a grudge against, the first such place, the
    /// person who keeps them away and that grudge (<see cref="Shunned"/>, <see cref="Shunning"/>, <see cref="Grudge"/>).
    /// </summary>
    public readonly record struct Gathering(Zone? Place, Zone? Shunned, Character? Shunning, Grudge? Grudge);

    public static bool IsGathering(World w, Zone z) => Def(w, z)?.Behaviour == Gather;

    /// <summary>
    /// The meeting place someone idle goes to (Schedule's free time calls this before its hearth default).
    /// Each place scores the sum of their opinion of every awake member standing in it; a place where
    /// anyone they hold an active grudge against is standing is skipped; the highest score wins, ties to
    /// the lower zone id. A read: no draws, no events.
    /// </summary>
    public static Gathering MeetingPlaceFor(World w, Character c)
    {
        Zone? best = null, shunned = null;
        Character? shunning = null;
        Grudge? grudge = null;
        int bestScore = int.MinValue;
        bool keptIn = CurfewBinds(w, c); // after dark, those the home area binds sit only where it reaches (zones step 3)
        foreach (Zone z in w.Map.Zones)
        {
            if (z.Tiles.Count == 0 || !IsGathering(w, z) || (keptIn && !Reaches(w, z))) continue;
            int score = 0;
            Character? enemy = null;
            Grudge? held = null;
            foreach (Character o in w.Characters)
            {
                if (o.Id == c.Id || o.Status != CharStatus.Member || o.Asleep || UseZoneAt(w, o.Pos)?.Id != z.Id) continue;
                if (Relations.ActiveGrudge(w, c.Id, o.Id) is { } g)
                {
                    if (enemy == null) { enemy = o; held = g; }
                    continue;
                }
                score += Relations.Opinion(w, c.Id, o.Id);
            }
            if (enemy != null)
            {
                if (shunned == null) { shunned = z; shunning = enemy; grudge = held; }
                continue;
            }
            if (score > bestScore) { best = z; bestScore = score; }
        }
        return best != null ? new Gathering(best, null, null, null) : new Gathering(null, shunned, shunning, grudge);
    }

    /// <summary>
    /// A tile of a zone to stand on, drawn as wandering draws (DiceId.WanderTarget on the Work stream), inside the home
    /// area when <paramref name="insideHome"/>; Point.None if none is free to stand on.
    /// </summary>
    public static Point TileIn(World w, Zone z, bool insideHome = false)
    {
        int n = Count(z.Tiles);
        if (n == 0) return Point.None;
        DiceStream dice = w.Rng.Dice[DiceSystem.Work];
        for (int attempt = 0; attempt < 6; attempt++)
        {
            int k = dice.Range(DiceId.WanderTarget, 0, n);
            foreach (TileRun r in z.Tiles)
            {
                if (k >= r.Length) { k -= r.Length; continue; }
                var p = new Point(r.X + k, r.Y);
                if (w.Map.Passable(p) && (!insideHome || InHome(w, p))) return p;
                break;
            }
        }
        return Point.None;
    }

    /// <summary>Does the home area reach a zone (any of its tiles a passable home tile)?</summary>
    public static bool Reaches(World w, Zone z)
    {
        foreach (Point p in Each(z.Tiles))
            if (InHome(w, p) && w.Map.Passable(p)) return true;
        return false;
    }

    /// <summary>
    /// Someone kept from every meeting place by a grudge sits apart: at most once a day they say so
    /// ("Sera kept away from the fire while Oren sat there."), citing the grudge, and feel it (sat_apart).
    /// </summary>
    public static void KeepAway(World w, Character c, Gathering g)
    {
        if (g.Shunned == null || g.Shunning == null || g.Grudge == null) return;
        int day = Calendar.Day(w.Tick, w.Content.World);
        if (c.LastKeptAwayDay == day) return;
        c.LastKeptAwayDay = day;
        string key = ByTheFire(w, g.Shunned) ? "event.mood.kept_away_fire" : "event.mood.kept_away";
        SimEvent e = w.Log.Emit(w, SystemTag.Mood, key, new[] { c.Id, g.Shunning.Id }, new[] { g.Grudge.FormedEvent }, null,
            EventArg.Char("a", c.Id), EventArg.Char("b", g.Shunning.Id));
        MoodSystem.AddThought(w, c, "sat_apart", g.Shunning.Id, e.Id);
    }

    /// <summary>Does a meeting place lie by the fire (a tile within <see cref="FireRadius"/> of the hearth)?</summary>
    public static bool ByTheFire(World w, Zone z)
    {
        foreach (Point p in Each(z.Tiles))
            if (p.ChebyshevTo(w.Map.Hearth) <= FireRadius) return true;
        return false;
    }

    /// <summary>Where a newcomer walks to be received: the nearest meeting-place tile, ties to the lower tile index; else beside the hearth.</summary>
    public static Point ArrivalSpot(World w, Point from)
    {
        Map m = w.Map;
        Point best = Point.None;
        int bestKey = int.MaxValue;
        foreach (Zone z in m.Zones)
        {
            if (!IsGathering(w, z)) continue;
            foreach (Point p in Each(z.Tiles))
            {
                if (!m.Passable(p)) continue;
                int key = p.ChebyshevTo(from) * m.Width * m.Height + m.Index(p);
                if (key < bestKey) { best = p; bestKey = key; }
            }
        }
        return best != Point.None ? best : Places.Adjacent(w, m.Hearth, from);
    }

    /// <summary>The camp's first meeting place: the open tiles round the hearth, two deep (the 24 the demo's camp gathered on).</summary>
    internal static List<TileRun> HearthRing(World w)
    {
        var tiles = new List<Point>();
        Content.ZoneDef def = w.Content.ZoneById("meeting_place");
        Point h = w.Map.Hearth;
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
            {
                var p = new Point(h.X + dx, h.Y + dy);
                if (Check(w, def, p) == null) tiles.Add(p);
            }
        return Canonical(tiles);
    }
}
