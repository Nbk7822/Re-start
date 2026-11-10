using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Hours as lived (ADR-0029; Schedule design 4.5). Ten times an hour, never on the hour, each person's doing is
/// tallied; when the hour ends their lived strip records what they mostly did. From the last 24 hours as lived come
/// overwork's costs, all indirect: a weary mood, no time to oneself, and being worn out, which doubles the odds of an
/// accident at risky work and becomes the cause of what follows. Those who never tire pay none of it. There is no
/// health drain and no death from exhaustion; deaths keep their story causes.
/// </summary>
public static partial class ScheduleSystem
{
    /// <summary>Samples an hour: every 30 ticks, at the 15th.</summary>
    public const int SampleEveryTicks = 30, SampleOffset = 15;

    /// <summary>Rest below this, with long duty behind them, wears someone out; rest back at RestedAt clears it.</summary>
    public const int WornOutRestBelow = 150, RestedAt = 600;

    /// <summary>An evening by the fire: half the samples of a free hour or more spent awake within the fire's comfort with someone else there.</summary>
    public const int FireSamplesForEvening = 5;

    public static void Sample(World w)
    {
        Point hearth = w.Map.Hearth;
        int byTheFire = 0;
        foreach (Character o in w.Characters)
            if (o.Status == CharStatus.Member && !o.Asleep && !o.Downed && o.Pos.ChebyshevTo(hearth) <= Zones.FireRadius) byTheFire++;
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member || !c.Named) continue;
            HourTally t = c.HourTally;
            switch (KindNow(c))
            {
                case LivedKind.Slept: t.Slept++; break;
                case LivedKind.Worked: t.Worked++; break;
                case LivedKind.Watched: t.Watched++; break;
                case LivedKind.Free: t.Free++; break;
                default: t.Needs++; break;
            }
            if (!c.Asleep && !c.Downed && c.Pos.ChebyshevTo(hearth) <= Zones.FireRadius && byTheFire >= 2) t.Fire++;
        }
    }

    /// <summary>What someone is doing now, as their lived strip counts it.</summary>
    public static LivedKind KindNow(Character c)
    {
        if (c.Asleep) return LivedKind.Slept;
        if (c.Downed) return LivedKind.Needs;
        return c.Job?.Kind switch
        {
            JobKind.KeepWatch or JobKind.WakeForWatch => LivedKind.Watched,
            JobKind.Forage or JobKind.GatherTimber or JobKind.Haul or JobKind.RepairRoof or JobKind.Care or JobKind.Rescue
                or JobKind.KeepPromise or JobKind.GiveBlood or JobKind.Fight or JobKind.Flee or JobKind.RepayDebt => LivedKind.Worked,
            null or JobKind.Idle or JobKind.Wander or JobKind.Socialize or JobKind.Feast => LivedKind.Free,
            _ => LivedKind.Needs,
        };
    }

    /// <summary>
    /// The hour just ended goes into each person's lived strip (ties: watched, worked, slept, free, needs); then the
    /// costs of the last 24 hours. Runs after needs, so rest is current, and before mood, so this hour's thoughts count.
    /// </summary>
    public static void Hourly(World w)
    {
        WorldDef def = w.Content.World;
        int hour = (Calendar.Hour(w.Tick, def) + HoursPerDay - 1) % HoursPerDay;
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member || !c.Named) continue;
            if (c.Lived.Count != HoursPerDay) ResetLived(w, c);
            HourTally t = c.HourTally;
            if (t.Total > 0) c.Lived[hour] = Mostly(t);
            bool fireside = t.Fire >= FireSamplesForEvening && c.Lived[hour] == LivedKind.Free && BlockAt(w, c, hour) == DayBlock.Free;
            t.Clear();
            if (!Places.Tires(w, c)) continue; // work is what the undead came back for: their purpose speaks for it
            Costs(w, c, fireside);
        }
    }

    private static LivedKind Mostly(HourTally t)
    {
        LivedKind best = LivedKind.Watched;
        int most = t.Watched;
        if (t.Worked > most) { best = LivedKind.Worked; most = t.Worked; }
        if (t.Slept > most) { best = LivedKind.Slept; most = t.Slept; }
        if (t.Free > most) { best = LivedKind.Free; most = t.Free; }
        if (t.Needs > most) best = LivedKind.Needs;
        return best;
    }

    /// <summary>Hours of duty (worked or watched), of free time and awake, in someone's last 24 hours as lived.</summary>
    public static (int Duty, int Free, int Awake) LastDay(Character c)
    {
        int duty = 0, free = 0, slept = 0;
        foreach (LivedKind k in c.Lived)
            if (k is LivedKind.Worked or LivedKind.Watched) duty++;
            else if (k == LivedKind.Free) free++;
            else if (k == LivedKind.Slept) slept++;
        return (duty, free, c.Lived.Count - slept);
    }

    private static void Costs(World w, Character c, bool fireside)
    {
        ScheduleDef d = w.Content.World.Schedule;
        (int duty, int free, int awake) = LastDay(c);
        long because = RecentDuty(w, c);
        if (duty >= d.OverworkHours) MoodSystem.AddThought(w, c, "overworked", 0, because);
        if (awake >= d.NoFreeTimeAwakeHours && free == 0) MoodSystem.AddThought(w, c, "no_time_to_myself", 0, because);
        if (fireside) MoodSystem.AddThought(w, c, "evening_by_the_fire", 0, 0);
        if (c.WornOutEvent != 0 && c.Rest >= RestedAt) c.WornOutEvent = 0;
        else if (c.WornOutEvent == 0 && c.Rest < WornOutRestBelow && duty >= d.WornOutDutyHours)
        {
            SimEvent e = w.Log.Emit(w, SystemTag.Needs, "event.needs.worn_out", new[] { c.Id }, new[] { because }, null, EventArg.Char("a", c.Id));
            c.WornOutEvent = e.Id;
            c.LastRestEvent = e.Id;
        }
    }

    /// <summary>
    /// Their last duty (a watch kept, work late, a longer working day), but only while it lies within the last 24 hours as
    /// lived: an older one is not what tires them now, so it is cited for nothing (ADR-0018); 0 when there is none.
    /// </summary>
    public static long RecentDuty(World w, Character c) =>
        c.LastDutyEvent != 0 && w.Log.Get(c.LastDutyEvent) is { } e && w.Tick - e.Tick <= w.Content.World.TicksPerDay ? c.LastDutyEvent : 0;

    /// <summary>A newcomer's lived strip starts as their planned day, so they never arrive overworked.</summary>
    public static void Arrived(World w, Character c) => ResetLived(w, c);

    /// <summary>
    /// An Oathbound person keeping a promise outside their working hours says so, once a day ("kept working past dark
    /// to keep their promise to {b}"), and that is the duty that will tire them.
    /// </summary>
    public static void WorkedLate(World w, Character c, Job promise)
    {
        if (BlockNow(w, c) == DayBlock.Work) return;
        int today = Calendar.Day(w.Tick, w.Content.World);
        if (w.Log.Get(c.LastDutyEvent) is { Key: "event.work.worked_late" } last && Calendar.Day(last.Tick, w.Content.World) == today) return;
        Debt? debt = promise.Kind == JobKind.KeepPromise ? w.Debts.Find(x => x.Id == promise.TargetId) : w.Debts.Find(x => x.FormedEvent == promise.CauseEvent && x.IsPromise);
        if (debt == null) return;
        SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.worked_late", new[] { c.Id, debt.CreditorId }, new[] { debt.FormedEvent }, null,
            EventArg.Char("a", c.Id), EventArg.Char("b", debt.CreditorId));
        c.LastDutyEvent = e.Id;
    }
}
