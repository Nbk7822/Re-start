using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Beds and who owns them (build design 4.6). Lying down in an unclaimed bed claims it, if you have none;
/// nobody sleeps in someone else's bed; a bed restores rest faster; waking in your own bed, or with none,
/// is felt; and waking sheltered by someone else's work warms you to them, at most once a day.
/// </summary>
public static partial class Building
{
    /// <summary>
    /// The bed this person should sleep in, if any: their own; an unclaimed one they built; an unclaimed one
    /// under a roof; any unclaimed one. Those who burn in daylight take only a covered bed. Beds are taken in
    /// list (id) order; an unclaimed bed someone else is in or heading for is skipped.
    /// </summary>
    public static Bed? SleepBed(World w, Character c, bool coveredOnly)
    {
        Bed? own = OwnedBed(w, c.Id);
        if (own != null) return !coveredOnly || w.Map.Covered(own.Pos) ? own : null;
        Bed? built = null, roofed = null, any = null;
        foreach (Bed b in w.Map.Beds)
        {
            if (b.OwnerId != 0 || Places.TakenBed(w, b.Pos, c.Id)) continue;
            bool covered = w.Map.Covered(b.Pos);
            if (coveredOnly && !covered) continue;
            if (built == null && b.BuiltBy.Contains(c.Id)) built = b;
            if (roofed == null && covered) roofed = b;
            any ??= b;
        }
        return built ?? roofed ?? any;
    }

    /// <summary>Does someone other than this person own a bed here? Nobody sleeps in someone else's bed.</summary>
    public static bool OthersBed(World w, Point p, int characterId) => BedAt(w, p) is { OwnerId: not 0 } b && b.OwnerId != characterId;

    /// <summary>
    /// Someone lies down to sleep here: an unclaimed bed becomes theirs if they have none. False if the bed is
    /// someone else's (it was claimed while they walked to it), so they look for another spot.
    /// </summary>
    internal static bool LieDown(World w, Character c)
    {
        Bed? bed = BedAt(w, c.Pos);
        if (bed == null) return true;
        if (bed.OwnerId == c.Id) return true;
        if (bed.OwnerId != 0) return false;
        if (OwnedBed(w, c.Id) == null) Claim(w, c, bed);
        return true;
    }

    /// <summary>A claim, in the words that fit: the bed they built, the bed that was someone's, the bed someone built, or a bed. It cites the bed's building.</summary>
    private static void Claim(World w, Character c, Bed bed)
    {
        int builder = Builder(bed.BuiltBy);
        SimEvent e;
        if (bed.BuiltBy.Contains(c.Id))
            e = w.Log.Emit(w, SystemTag.Social, "event.social.claimed_own_bed", new[] { c.Id }, new[] { bed.BuiltEvent }, null, EventArg.Char("a", c.Id));
        else if (bed.PrevOwnerId != 0)
            e = w.Log.Emit(w, SystemTag.Social, "event.social.claimed_bed_of", new[] { c.Id, bed.PrevOwnerId }, new[] { bed.BuiltEvent }, null,
                EventArg.Char("a", c.Id), EventArg.Char("b", bed.PrevOwnerId));
        else if (builder != 0)
            e = w.Log.Emit(w, SystemTag.Social, "event.social.claimed_bed_built", new[] { c.Id, builder }, new[] { bed.BuiltEvent }, null,
                EventArg.Char("a", c.Id), EventArg.Char("b", builder));
        else
            e = w.Log.Emit(w, SystemTag.Social, "event.social.claimed_bed", new[] { c.Id }, Array.Empty<long>(), null, EventArg.Char("a", c.Id));
        bed.OwnerId = c.Id;
        bed.ClaimEvent = e.Id;
    }

    /// <summary>Someone who leaves or dies leaves their bed unclaimed, without an event; the bed remembers whose it was.</summary>
    public static void LetGo(World w, Character c)
    {
        foreach (Bed b in w.Map.Beds)
        {
            if (b.OwnerId != c.Id) continue;
            b.PrevOwnerId = c.Id;
            b.OwnerId = 0;
            b.ClaimEvent = 0;
        }
    }

    /// <summary>The extra rest an hour of sleep gives in a bed (its building's restBonusPct of the usual).</summary>
    public static int RestBonus(World w, Character c, int restore)
    {
        Bed? bed = BedAt(w, c.Pos);
        return bed == null ? 0 : restore * Def(w, bed).RestBonusPct / 100;
    }

    /// <summary>
    /// On waking: their own bed is a comfort, and having no bed at all is felt (not by an owner who slept away from
    /// theirs, such as a vampire kept in the ruins by the sun); waking under a roof someone else raised or mended, or
    /// in a bed someone else built, warms them to that person (at most once a day for each).
    /// </summary>
    internal static void Woke(World w, Character c)
    {
        Bed? bed = BedAt(w, c.Pos);
        if (bed == null && OwnedBed(w, c.Id) == null) MoodSystem.AddThought(w, c, "no_bed", 0, 0);
        else if (bed != null && bed.OwnerId == c.Id) MoodSystem.AddThought(w, c, "slept_in_own_bed", 0, bed.ClaimEvent);
        foreach (RoofSection r in w.Map.Roof)
        {
            if (!r.Sound || r.Removed || !r.Area.Contains(c.Pos)) continue;
            int by = r.MendedBy != 0 ? r.MendedBy : Builder(r.BuiltBy);
            Thank(w, c, by, r.BuiltEvent);
            break;
        }
        if (bed != null) Thank(w, c, Builder(bed.BuiltBy), bed.BuiltEvent);
    }

    private static void Thank(World w, Character c, int by, long cause)
    {
        if (by == 0 || by == c.Id || w.Find(by) == null) return;
        foreach (Thought t in c.Thoughts)
            if (t.DefId == "sheltered_by" && t.AboutId == by) return; // at most once a day: the thought lasts a day
        MoodSystem.AddThought(w, c, "sheltered_by", by, cause);
    }
}
