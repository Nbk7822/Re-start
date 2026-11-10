using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Simple melee with night thieves. A thief heads for the stockpile; whoever keeps watch, and any
/// Protective member awake nearby, moves to stop them. Adjacent fighters trade blows every 30 ticks.
/// A beaten thief flees; an unopposed one steals and slips away. Anxious defenders flee when hurt,
/// and those left fighting resent it.
/// </summary>
public static class CombatSystem
{
    public const int SightRange = 8;
    public const int BlowEveryTicks = 30;
    public const int HitPermille = 500;
    public const int DamageMin = 60;
    public const int DamageMax = 140;

    /// <summary>
    /// A wound bleeds this share of its damage, in health an hour, until someone tends it (the player design §8.5,
    /// ADR-0030). A code constant (ADR-0013), tuned so you die by the world's own dangers about once every three years.
    /// </summary>
    public const int BleedSharePct = 225;

    public static void Step(World w)
    {
        for (int i = 0; i < w.Characters.Count; i++)
        {
            Character t = w.Characters[i];
            if (t.Status != CharStatus.Hostile || t.Job == null) continue;
            if (t.Job.Kind == JobKind.Flee) { FleeTick(w, t); continue; }
            Spot(w, t);
            List<Character> defenders = Adjacent(w, t);
            if (defenders.Count > 0)
            {
                t.Path.Clear();
                if (w.Tick % BlowEveryTicks == 0) Exchange(w, t, defenders);
                continue;
            }
            if (t.Path.Count == 0 && t.Pos != t.Job.Target && !TravelSystem.Start(w, t, t.Job.Target)) { StartFlee(w, t, 0); continue; }
            if (t.Pos == t.Job.Target) Steal(w, t);
        }
    }

    /// <summary>
    /// Damages a member; downs them below 150 health and kills them at 0. A wound that <paramref name="bleeds"/> (an
    /// accident, a blow) goes on costing health each hour until someone tends it; it adds no draw (ADR-0030).
    /// </summary>
    public static void Hurt(World w, Character c, int damage, long cause, string deathKey, SystemTag system, bool bleeds = false)
    {
        c.Health -= damage;
        c.LastHealthEvent = cause;
        if (bleeds && c.Health > 0)
        {
            c.Bleed += Math.Max(1, damage * BleedSharePct / 100);
            c.BleedCause = cause;
            c.BleedDeath = deathKey;
        }
        MoodSystem.AddThought(w, c, "wounded", 0, cause);
        if (c.Health <= 0)
        {
            Lifecycle.Die(w, c, system, deathKey, cause);
            return;
        }
        if (!c.Downed && c.Health < NeedsSystem.DownedBelow)
        {
            c.Downed = true;
            c.Asleep = false;
            c.Path.Clear();
            SimEvent fell = w.Log.Emit(w, SystemTag.Needs, "event.needs.collapsed", new[] { c.Id }, new[] { cause }, null, EventArg.Char("a", c.Id));
            c.LastHealthEvent = fell.Id;
        }
    }

    private static void Spot(World w, Character thief)
    {
        foreach (Character m in w.Characters)
        {
            if (m.Status != CharStatus.Member || m.Asleep || m.Downed) continue;
            if (m.Job is { Kind: JobKind.Fight or JobKind.Flee }) continue;
            bool guard = m.Job is { Kind: JobKind.KeepWatch } || Traits.Any(w, m, t => t.FightsThieves);
            if (!guard || m.Pos.ChebyshevTo(thief.Pos) > SightRange) continue;
            if (Places.BurnsInDaylight(w, m) && Places.Daylight(w)) continue;
            if (thief.Job!.Stage == 0)
            {
                SimEvent seen = w.Log.Emit(w, SystemTag.Combat, "event.combat.thief_spotted", new[] { m.Id, thief.Id }, new[] { thief.Job.CauseEvent }, null,
                    EventArg.Char("a", m.Id), EventArg.Char("b", thief.Id));
                thief.Job.Stage = 1;
                thief.Job.CauseEvent = seen.Id;
            }
            Systems.WorkSystem.Drop(w, m);
            m.Job = new Job { Kind = JobKind.Fight, TargetId = thief.Id, Target = thief.Pos, StartedTick = w.Tick, CauseEvent = thief.Job.CauseEvent };
        }
    }

    private static List<Character> Adjacent(World w, Character thief)
    {
        var list = new List<Character>();
        foreach (Character m in w.Characters)
            if (m.Status == CharStatus.Member && !m.Downed && m.Job is { Kind: JobKind.Fight } j && j.TargetId == thief.Id && m.Pos.ChebyshevTo(thief.Pos) <= 1)
                list.Add(m);
        return list;
    }

    private static void Exchange(World w, Character thief, List<Character> defenders)
    {
        DiceStream dice = w.Rng.Dice[DiceSystem.Combat];
        Job tj = thief.Job!;
        if (tj.Stage < 2)
        {
            SimEvent fight = w.Log.Emit(w, SystemTag.Combat, "event.combat.fight", ActorsOf(defenders, thief), new[] { tj.CauseEvent }, null,
                EventArg.Char("a", defenders[0].Id), EventArg.Char("b", thief.Id));
            tj.Stage = 2;
            tj.CauseEvent = fight.Id;
        }
        foreach (Character d in defenders)
            if (dice.Chance(DiceId.CombatHit, HitPermille)) thief.Health -= dice.Range(DiceId.CombatDamage, DamageMin, DamageMax + 1);

        Character target = defenders[0];
        if (dice.Chance(DiceId.CombatHit, HitPermille))
        {
            int damage = dice.Range(DiceId.CombatDamage, DamageMin, DamageMax + 1);
            (Character hit, long why) = Gifts.TurnHarm(w, target, damage, tj.CauseEvent, blow: true); // Fortune's favourite (ADR-0030)
            Hurt(w, hit, damage, why, "event.combat.died_wounds", SystemTag.Combat, bleeds: true);
            if (hit.Status == CharStatus.Member && hit.Downed)
                w.Log.Emit(w, SystemTag.Combat, "event.combat.downed", new[] { hit.Id, thief.Id }, new[] { hit.LastHealthEvent }, null,
                    EventArg.Char("a", hit.Id), EventArg.Char("b", thief.Id));
        }
        foreach (Character d in defenders)
        {
            if (d.Status != CharStatus.Member || d.Downed) continue;
            int flee = Traits.Max(w, d, t => t.FleeHealthBelow);
            if (flee > 0 && d.Health < flee) Abandon(w, d, defenders, tj.CauseEvent);
        }

        IncidentDef def = w.Content.IncidentById("night_thief");
        if (thief.Health <= 0)
        {
            Lifecycle.Die(w, thief, SystemTag.Combat, "event.combat.thief_killed", tj.CauseEvent);
            DrivenOff(w, w.Log.All[^1].Id);
        }
        else if (thief.Health < def.Param("fleeHealthBelow"))
        {
            StartFlee(w, thief, tj.CauseEvent);
        }
    }

    private static void Abandon(World w, Character coward, List<Character> defenders, long fight)
    {
        SimEvent fled = w.Log.Emit(w, SystemTag.Combat, "event.combat.fled", new[] { coward.Id }, new[] { fight }, null, EventArg.Char("a", coward.Id));
        Point safe = Places.CoveredSpot(w, coward.Pos);
        coward.Job = new Job { Kind = JobKind.Flee, Target = safe == Point.None ? coward.Pos : safe, StartedTick = w.Tick, CauseEvent = fled.Id };
        TravelSystem.Start(w, coward, coward.Job.Target);
        foreach (Character d in defenders)
            if (d.Id != coward.Id && d.Status == CharStatus.Member && !(d.Job is { Kind: JobKind.Flee }))
                Relations.FormGrudge(w, d, coward, 20, fled.Id);
    }

    private static void StartFlee(World w, Character thief, long cause)
    {
        SimEvent fled = w.Log.Emit(w, SystemTag.Combat, "event.combat.thief_fled", new[] { thief.Id }, new[] { cause }, null, EventArg.Char("a", thief.Id));
        thief.Job = new Job { Kind = JobKind.Flee, Target = IncidentSystem.EdgeTile(w, DiceId.ThiefEdge), StartedTick = w.Tick, CauseEvent = fled.Id };
        TravelSystem.Start(w, thief, thief.Job.Target);
        if (cause != 0) DrivenOff(w, fled.Id);
    }

    private static void DrivenOff(World w, long cause)
    {
        foreach (Character m in w.Members()) MoodSystem.AddThought(w, m, "thief_driven_off", 0, cause);
    }

    private static void FleeTick(World w, Character thief)
    {
        if (thief.Path.Count > 0) return;
        if (thief.Pos != thief.Job!.Target && TravelSystem.Start(w, thief, thief.Job.Target) && thief.Path.Count > 0) return;
        thief.Status = CharStatus.Left;
        thief.Job = null;
    }

    private static void Steal(World w, Character thief)
    {
        int max = w.Content.IncidentById("night_thief").Param("stealMax");
        int n = Stock.Take(w, "berries", max);
        long cause = thief.Job!.CauseEvent;
        if (n > 0)
        {
            SimEvent stolen = w.Log.Emit(w, SystemTag.Incident, "event.incident.stolen", new[] { thief.Id }, new[] { cause }, null,
                EventArg.Num("n", n), EventArg.Item("item", "berries"));
            foreach (Character m in w.Members())
            {
                MoodSystem.AddThought(w, m, "robbed", 0, stolen.Id);
                if (w.Content.SpeciesById(m.SpeciesId).FoodSource == "berries") m.LastFoodEvent = stolen.Id;
            }
            cause = stolen.Id;
        }
        thief.Job = new Job { Kind = JobKind.Flee, Target = IncidentSystem.EdgeTile(w, DiceId.ThiefEdge), StartedTick = w.Tick, CauseEvent = cause };
        TravelSystem.Start(w, thief, thief.Job.Target);
    }

    private static int[] ActorsOf(List<Character> defenders, Character thief)
    {
        var ids = new int[defenders.Count + 1];
        for (int i = 0; i < defenders.Count; i++) ids[i] = defenders[i].Id;
        ids[^1] = thief.Id;
        return ids;
    }
}
