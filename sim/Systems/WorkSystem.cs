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
        }
        c.Job = null;
        c.Path.Clear();
        c.MoveTicks = 0;
    }

    private static bool ShouldInterrupt(World w, Character c)
    {
        Job j = c.Job!;
        if (Places.BurnsInDaylight(w, c) && Places.Daylight(w) && !w.Map.Covered(c.Pos)
            && j.Kind is not (JobKind.SeekCover or JobKind.RequestBlood or JobKind.GiveBlood))
            return true;
        if (c.Food < NeedsSystem.StarvingBelow && j.Kind is not (JobKind.Eat or JobKind.RequestBlood or JobKind.GiveBlood or JobKind.Sleep or JobKind.Forage))
            return true;
        return c.Rest < 60 && j.Kind is not (JobKind.Sleep or JobKind.Eat or JobKind.SeekCover);
    }

    private static void RegrowBushes(World w)
    {
        foreach (Bush b in w.Map.Bushes)
            if (b.Berries < Places.BushMax) b.Berries++;
    }
}
