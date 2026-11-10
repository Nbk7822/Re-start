using Return.Sim.Core;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// Each tick: interrupt jobs that have become wrong (a vampire caught by the sun, someone starving),
/// give idle members a job from <see cref="JobPicker"/>, and advance every job one tick.
/// </summary>
public static class WorkSystem
{
    public const int InterruptEveryTicks = 30;

    public static void Step(World w)
    {
        if (Calendar.IsDayStart(w.Tick, w.Content.World)) RegrowBushes(w);
        bool check = w.Tick % InterruptEveryTicks == 0;
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member) continue;
            if (c.Downed)
            {
                if (c.Job != null) Drop(w, c);
                continue;
            }
            if (c.Job != null && check && ShouldInterrupt(w, c)) Drop(w, c);
            if (c.Job == null)
            {
                Assign(w, c);
                continue;
            }
            if (JobRunner.Tick(w, c, c.Job)) Finish(w, c);
        }
    }

    public static void Assign(World w, Character c)
    {
        Job? job = JobPicker.Choose(w, c);
        if (job == null) return;
        job.StartedTick = w.Tick;
        if (job.Target == Point.None || !TravelSystem.Start(w, c, job.Target))
        {
            if (job.Kind == JobKind.Wander) return;
            job = new Job { Kind = JobKind.Wander, Target = c.Pos, TargetId = 60, StartedTick = w.Tick };
            c.Path.Clear();
        }
        c.Job = job;
    }

    /// <summary>Ends the current job early: wakes the sleeper and drops anything carried where they stand.</summary>
    public static void Drop(World w, Character c)
    {
        c.Asleep = false;
        Finish(w, c);
    }

    private static void Finish(World w, Character c)
    {
        if (c.Carrying is { } carry)
        {
            Stock.Add(w, carry.DefId, carry.Count, c.Pos);
            c.Carrying = null;
            Rations.AfterDeposit(w, carry.DefId, c.Pos);
        }
        c.Job = null;
        c.Path.Clear();
        c.MoveTicks = 0;
    }

    private static bool ShouldInterrupt(World w, Character c)
    {
        Job j = c.Job!;
        if (j.Kind is JobKind.Arrive or JobKind.Fight or JobKind.Flee or JobKind.Rescue) return false;
        if (Places.BurnsInDaylight(w, c) && Places.SunDanger(w) && !w.Map.Covered(c.Pos) && j.Kind != JobKind.SeekCover)
            return true;
        if (c.Food < NeedsSystem.StarvingBelow && j.Kind is not (JobKind.Eat or JobKind.RequestBlood or JobKind.GiveBlood or JobKind.Sleep or JobKind.Forage))
            return true;
        if (AwaitsTending(w, c) && j.Kind is not (JobKind.Wander or JobKind.Sleep or JobKind.SeekCover)) return true;
        return c.Rest < 60 && Places.Tires(w, c) && j.Kind is not (JobKind.Sleep or JobKind.Eat or JobKind.SeekCover or JobKind.RequestBlood or JobKind.GiveBlood);
    }

    /// <summary>
    /// Someone bleeding whom a carer is on their way to tend stops where they are and waits (ADR-0030): a wound cannot be
    /// tended on the move, and the undead, who never lie down, would otherwise walk away from whoever came to help.
    /// </summary>
    public static bool AwaitsTending(World w, Character c) => c.Bleed > 0 && Places.Targeted(w, JobKind.Care, c.Id, c.Id);

    private static void RegrowBushes(World w)
    {
        int grow = Places.BushRegrowPerDay;
        if (!Places.GrowingSeason(w))
            grow = Calendar.Day(w.Tick, w.Content.World) % Places.WinterRegrowEveryDays == 0 ? 1 : 0;
        if (grow == 0) return;
        foreach (Bush b in w.Map.Bushes)
            b.Berries = Math.Min(Places.BushMax, b.Berries + grow);
    }

    /// <summary>Bushes regrow fully in every season but the last, and only trickle in winter; exposed for tests.</summary>
    internal static void RegrowForTest(World w)
    {
        RegrowBushes(w);
    }
}
