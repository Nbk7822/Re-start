using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// The home area's curfew (zones step 3, ADR-0028; zones design 5.5), built on the Schedule's day (ADR-0029): after dark,
/// everyone bound by the home area (everyone whose kind of day sleeps in the dark) starts nothing outside it, unless they
/// are starving (food), going to someone hurt (care, rescue) or going their own way tonight by the one compliance rule
/// (ADR-0025 item 2; never the player). It shapes their own choices; nothing orders anyone, and marking a home area
/// never pulls anyone off the job in hand. Rule-keepers who see a defiant breach think less of the breaker; an
/// Independent kept in resents you a little; someone hungry who stays in as asked has the home area on their hunger's
/// chain. The watch's side of it (who is not asked to keep a post outside) is in WatchSystem.
/// </summary>
public static partial class Zones
{
    /// <summary>Is someone bound by the home area: the camp has one, and they are not of the night folk? (Dark or not.)</summary>
    public static bool Bound(World w, Character c) => HomeOf(w) != null && !NightFolk(w, c);

    /// <summary>The reason they go their own way tonight, or null: nobody counts as defiant before tonight's roll.</summary>
    public static string? DefiantTonight(World w, Character c) =>
        c.NightRollDay == Tonight(w) && c.DefiesTonight.Length > 0 ? c.DefiesTonight : null;

    /// <summary>
    /// Starving by the test the Schedule uses to get someone out of bed for food (food below 150, or below their
    /// stealFoodBelow: the Desperate, 220), so the Schedule never wakes anyone to find food the curfew then forbids. Never
    /// for those who eat nothing.
    /// </summary>
    public static bool StarvingNow(World w, Character c) =>
        !Places.Species(w, c).EatsNothing && (c.Food < NeedsSystem.StarvingBelow || c.Food < Traits.Max(w, c, t => t.StealFoodBelow));

    /// <summary>
    /// Does the home area keep someone in from a job with this target? After dark, bound, the target outside it, and no
    /// excuse: hunger that has become starving (only for food: <paramref name="forFood"/>), or going their own way tonight.
    /// A read: JobPicker skips the candidate and chooses again.
    /// </summary>
    public static bool KeepsIn(World w, Character c, Point target, bool forFood = false)
    {
        if (target == Point.None || InHome(w, target) || !CurfewBinds(w, c)) return false;
        return !(forFood && StarvingNow(w, c));
    }

    /// <summary>Does the curfew bind someone right now: after dark, bound by the home area, and not going their own way tonight?</summary>
    public static bool CurfewBinds(World w, Character c) => IsDark(w) && Bound(w, c) && DefiantTonight(w, c) == null;

    /// <summary>The jobs the curfew asks people to leave at dusk and not start outside after dark: work, wandering, company, eating and foraging.</summary>
    public static bool Curfewed(JobKind kind) =>
        kind is JobKind.Forage or JobKind.GatherTimber or JobKind.Haul or JobKind.RepairRoof or JobKind.Wander or JobKind.Socialize or JobKind.Eat;

    /// <summary>
    /// Runs every hour right after the mood system. At the dusk hour, when the camp has a home area: everyone bound by it
    /// but the player rolls once whether they go their own way tonight (the home area's own tag, drawn only when the odds are
    /// above 0), and then each bound person out at work or idling outside the home area chooses: excused or going their
    /// own way, they stay out (and say so); otherwise they leave the job and their next choice takes them home.
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
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member || c.Downed || c.Job is not { } job || job.Target == Point.None || InHome(w, job.Target) || NightFolk(w, c)) continue;
            if (job.Kind is JobKind.Care or JobKind.Rescue) { Breach(w, c, job); continue; }
            if (!Curfewed(job.Kind)) continue; // sleep, cover, promises, blood, feasts, needs of their nature, fights and the watch finish as they are
            if ((job.Kind is JobKind.Forage or JobKind.Eat && StarvingNow(w, c)) || DefiantTonight(w, c) != null)
            {
                Breach(w, c, job);
                continue;
            }
            // Their own choice at dusk, made after their roll: they leave the job and walk home (design 5.5).
            WorkSystem.Drop(w, c);
            KeptIn(w, c);
        }
    }

    /// <summary>A job has just started (WorkSystem): if it takes a bound person outside the home area after dark, they went out, and say so once a night.</summary>
    public static void Started(World w, Character c, Job job)
    {
        if (job.Target == Point.None || !IsDark(w) || InHome(w, job.Target) || !Bound(w, c)) return;
        Breach(w, c, job);
    }

    /// <summary>
    /// The breach line, at most once a night, by the excuse that let them out: starving (a food job; cites their hunger),
    /// help (care or rescue; cites the hurt), or their own way tonight (cites what Compliance's reason names, and
    /// rule-keepers within sight think less of them). Jobs the curfew never covers say nothing. Every line cites the home
    /// area's MadeEvent: it is a transgression of the rule, though never the cause of what the job then brings.
    /// </summary>
    internal static void Breach(World w, Character c, Job job)
    {
        int night = Tonight(w);
        Zone? home = HomeOf(w);
        if (home == null || c.LastBreachDay == night) return;
        SimEvent e;
        if (job.Kind is JobKind.Care or JobKind.Rescue && w.Find(job.TargetId) is { } hurt)
        {
            e = w.Log.Emit(w, SystemTag.Work, "event.work.out_after_dark_help", new[] { c.Id, hurt.Id }, new[] { home.MadeEvent, hurt.LastHealthEvent }, null,
                EventArg.Char("a", c.Id), EventArg.Char("b", hurt.Id));
        }
        else if (job.Kind is JobKind.Forage or JobKind.Eat && StarvingNow(w, c))
        {
            e = w.Log.Emit(w, SystemTag.Work, "event.work.out_after_dark_starving", new[] { c.Id }, new[] { home.MadeEvent, c.LastFoodEvent }, null,
                EventArg.Char("a", c.Id));
        }
        else if ((Curfewed(job.Kind) || job.Kind == JobKind.KeepWatch) && DefiantTonight(w, c) is { } why)
        {
            e = w.Log.Emit(w, SystemTag.Work, OutAfterDarkKey(why), new[] { c.Id }, new[] { home.MadeEvent, Compliance.ReasonCause(w, c, home.MadeBy, why) }, null,
                EventArg.Char("a", c.Id));
            c.LastBreachDay = night;
            Compliance.Witness(w, c, e);
            return;
        }
        else return;
        c.LastBreachDay = night;
    }

    /// <summary>The line for going out after dark by one's own way, by Compliance's reason (each spelled out, so the loc check covers it).</summary>
    public static string OutAfterDarkKey(string why) => why switch
    {
        Compliance.Dislike => "event.work.out_after_dark_dislike",
        Compliance.Mood => "event.work.out_after_dark_mood",
        Compliance.Ways => "event.work.out_after_dark_ways",
        _ => "event.work.out_after_dark_own_way",
    };

    /// <summary>
    /// An Independent (a nature term above 0 in the compliance odds) held back by the curfew resents you a little, at most once
    /// a night (kept_in: a thought about whoever set the rule, caused by the home area's marking). Never the player.
    /// </summary>
    internal static void KeptIn(World w, Character c)
    {
        Zone? home = HomeOf(w);
        int night = Tonight(w);
        if (home == null || c.Id == w.PlayerId || c.LastKeptInDay == night || home.MadeBy == 0) return;
        if (Compliance.OddsOf(w, c, home.MadeBy).Nature <= 0) return;
        c.LastKeptInDay = night;
        MoodSystem.AddThought(w, c, "kept_in", home.MadeBy, home.MadeEvent);
    }

    /// <summary>
    /// Someone hungry who skipped a food job outside stays in as asked, and says so once a night ("Mira, hungry, stayed in camp
    /// after dark as you asked."): the line cites the home area and their hunger, and becomes the cause of the hunger that follows.
    /// </summary>
    internal static void StayedInHungry(World w, Character c)
    {
        Zone? home = HomeOf(w);
        int night = Tonight(w);
        if (home == null || c.Food >= NeedsSystem.HungryBelow || c.LastStayedInDay == night) return;
        c.LastStayedInDay = night;
        SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.stayed_in_hungry", new[] { c.Id }, new[] { home.MadeEvent, c.LastFoodEvent }, null,
            EventArg.Char("a", c.Id));
        c.LastFoodEvent = e.Id;
    }

    /// <summary>
    /// Where someone kept in goes when idle: a tile near the home tile nearest them, drawn as wandering draws (up to six
    /// tries of DiceId.WanderTarget within two tiles, inside the home area), else that nearest tile; Point.None with no home area.
    /// </summary>
    public static Point HomeSpot(World w, Character c)
    {
        Zone? home = HomeOf(w);
        if (home == null) return Point.None;
        Map m = w.Map;
        Point nearest = Point.None;
        int best = int.MaxValue;
        foreach (Point p in Each(home.Tiles))
        {
            if (!m.Passable(p)) continue;
            int key = p.ChebyshevTo(c.Pos) * m.Width * m.Height + m.Index(p);
            if (key < best) { best = key; nearest = p; }
        }
        if (nearest == Point.None) return Point.None;
        DiceStream dice = w.Rng.Dice[DiceSystem.Work];
        for (int attempt = 0; attempt < 6; attempt++)
        {
            var p = new Point(nearest.X + dice.Range(DiceId.WanderTarget, -2, 3), nearest.Y + dice.Range(DiceId.WanderTarget, -2, 3));
            if (m.Passable(p) && InHome(w, p)) return p;
        }
        return nearest;
    }
}
