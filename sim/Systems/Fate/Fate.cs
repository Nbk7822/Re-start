using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Return by Death (the player design §5, ADR-0030), the parts that read and write one world: waking on the first
/// tick, noticing your death, the end of your lifetime, when the director may tie a fate knot (at a turn in the
/// story, or a quiet dawn, and only while you are safe), and the weighted pick of the knot a death goes back to.
/// The return itself swaps whole worlds, so it lives in the Simulation (Simulation.Fate.cs). Nothing here draws
/// from any stream but Fate, and only the pick draws.
/// </summary>
public static class Fate
{
    public const string WokeKey = "event.fate.woke";
    public const string ReturnedKey = "event.fate.returned";
    public const string OldAgeKey = "event.growth.died_old_age";
    public const string SoulSpentKey = "event.fate.soul_spent";
    public const string ForcedDeathKey = "event.fate.forced_death";

    /// <summary>The first tick: you died on Earth, and woke here as your people. The run's first line.</summary>
    public static void Wake(World w)
    {
        if (w.Find(w.PlayerId) is not { } you || w.Fate.PeopleId.Length == 0) return;
        RebirthPeopleDef? people = w.Content.Rebirth.People(w.Fate.PeopleId);
        w.Log.Emit(w, SystemTag.Fate, WokeKey, new[] { you.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", you.Id), EventArg.Key("people", people?.AsKey ?? "rebirth.as." + w.Fate.PeopleId));
    }

    /// <summary>
    /// You died (Lifecycle.Die tells us). Old age and the soul's end close the run; any other death waits for the
    /// end of the tick, when the world goes back to a knot. The rest of the tick runs as usual.
    /// </summary>
    public static void OnPlayerDied(World w, SimEvent death)
    {
        FateBook f = w.Fate;
        if (f.Ended) return;
        if (death.Key == OldAgeKey || death.Key == SoulSpentKey)
        {
            f.Ended = true;
            f.EndKind = death.Key == OldAgeKey ? "old_age" : "soul_spent";
            f.EndTick = w.Tick;
            f.PendingDeath = 0;
            return;
        }
        if (f.PendingDeath == 0) f.PendingDeath = death.Id;
    }

    /// <summary>At dawn, if you still live past your soul's span, its thread runs out: the end of your lifetime, whatever the body.</summary>
    public static void SoulSpan(World w)
    {
        WorldDef def = w.Content.World;
        if (w.Tick % def.TicksPerDay != (long)def.DawnHour * def.TicksPerHour) return;
        if (w.Fate.SoulEndTick <= 0 || w.Tick < w.Fate.SoulEndTick) return;
        if (w.Find(w.PlayerId) is { Status: CharStatus.Member } you)
            Lifecycle.Die(w, you, SystemTag.Fate, SoulSpentKey, 0);
    }

    /// <summary>
    /// Kills you outright, between ticks: the soak's --fates and tests use it to make a return happen on demand.
    /// It is not a command (commands are replayable intentions, and none can kill you); a forced death is recorded
    /// in the timeline so <c>Simulation.Replay</c> forces it again. The return follows at the end of the next step.
    /// </summary>
    internal static bool ForceDeath(World w)
    {
        if (w.Fate.Ended || w.Fate.PendingDeath != 0 || w.Find(w.PlayerId) is not { Status: CharStatus.Member } you) return false;
        w.Fate.ForcedDeathTick = w.Tick;
        Lifecycle.Die(w, you, SystemTag.Fate, ForcedDeathKey, 0);
        return true;
    }

    /// <summary>
    /// Is it safe to tie a knot now? Never while you are downed, bleeding, below the safe health, at a critical need,
    /// fighting, fleeing or in the sun: a knot inside a death already under way would loop (§5.1).
    /// </summary>
    public static bool Safe(World w)
    {
        if (w.Find(w.PlayerId) is not { Status: CharStatus.Member } you) return false;
        if (you.Downed || you.Bleed > 0 || you.Health < w.Content.Rebirth.Knots.SafeHealthAtLeast || you.HungerStage >= 2) return false;
        foreach (NatureNeed n in you.NatureNeeds)
            if (n.Stage >= 2) return false;
        if (you.Job is { Kind: JobKind.Fight or JobKind.Flee }) return false;
        return !(Places.BurnsInDaylight(w, you) && Places.Daylight(w) && !w.Map.Covered(you.Pos));
    }

    /// <summary>
    /// Whether the director ties a knot at the end of the tick just run (events from <paramref name="tickStart"/> on
    /// are that tick's): the arrival knot first, then a turn (an event on the trigger list), then a quiet dawn; never
    /// closer than the spacing allows, never more than the day's limit, and only while you are safe.
    /// </summary>
    public static (bool Due, KnotReason Reason, long Trigger, string Key) KnotDue(World w, int tickStart)
    {
        FateBook f = w.Fate;
        KnotRulesDef rules = w.Content.Rebirth.Knots;
        WorldDef def = w.Content.World;
        // The arrival knot is tied on the first tick whatever your state: there must always be somewhere to go back to.
        if (f.Knots.Count == 0) return w.Find(w.PlayerId) is { Status: CharStatus.Member } ? (true, KnotReason.Arrival, 0, "") : default;
        if (!Safe(w)) return default;
        long last = f.Knots[^1].Tick;
        if (w.Tick - last < (long)rules.MinHoursBetween * def.TicksPerHour) return default;
        int today = Calendar.Day(w.Tick, def), tied = 0;
        foreach (KnotRecord k in f.Knots)
            if (Calendar.Day(k.Tick, def) == today) tied++;
        if (tied >= rules.MaxPerDay) return default;
        List<SimEvent> log = w.Log.All;
        for (int i = tickStart; i < log.Count; i++)
            if (rules.Triggers.Contains(log[i].Key)) return (true, KnotReason.Turn, log[i].Id, log[i].Key);
        bool dawn = w.Tick % def.TicksPerDay == (long)def.DawnHour * def.TicksPerHour;
        if (dawn && w.Tick - last >= (long)rules.QuietAfterHours * def.TicksPerHour) return (true, KnotReason.Quiet, 0, "");
        return default;
    }

    /// <summary>The weight a knot carries in the pick before its uses: equal for old knots; by rank for recent ones, the latest heaviest.</summary>
    public const int BaseWeight = 1024;

    /// <summary>
    /// Picks the knot a death goes back to (§5.2). Eligible knots lie at least minHoursBeforeDeath before the death
    /// (the arrival knot always qualifies); those of the last recentDays are recent, the rest old. When both kinds are
    /// eligible, a <see cref="DiceId.FateKnotTail"/> chance at tailPermille reaches into the old ones (the fat tail);
    /// then <see cref="DiceId.FateKnotPick"/> picks by weight: old knots equal, recent knots by rank (the recent bias),
    /// each earlier use halving a knot's weight so a knot that leads straight back to death frays. Null only when no
    /// knot is eligible at all.
    /// </summary>
    public static KnotRecord? Pick(IReadOnlyList<KnotRecord> knots, long deathTick, ReturnRulesDef rules, WorldDef def, DiceStream dice)
    {
        var recent = new List<KnotRecord>();
        var old = new List<KnotRecord>();
        long latest = deathTick - (long)rules.MinHoursBeforeDeath * def.TicksPerHour;
        long recentFrom = deathTick - (long)rules.RecentDays * def.TicksPerDay;
        foreach (KnotRecord k in knots)
        {
            if (k.Tick > latest && k.Reason != KnotReason.Arrival) continue;
            if (k.Tick > deathTick) continue;
            (k.Tick >= recentFrom ? recent : old).Add(k);
        }
        if (recent.Count == 0 && old.Count == 0) return null;
        bool deep = recent.Count == 0 || (old.Count > 0 && dice.Chance(DiceId.FateKnotTail, rules.TailPermille));
        List<KnotRecord> pool = deep ? old : recent;
        var weights = new int[pool.Count];
        int total = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            int baseWeight = deep ? BaseWeight : (i + 1) * BaseWeight; // knots are oldest first, so the latest recent one ranks highest
            weights[i] = Math.Max(1, baseWeight >> Math.Min(pool[i].Uses, 20));
            total += weights[i];
        }
        int roll = dice.Range(DiceId.FateKnotPick, 0, total);
        for (int i = 0; i < pool.Count; i++)
        {
            if (roll < weights[i]) return pool[i];
            roll -= weights[i];
        }
        return pool[^1];
    }
}
