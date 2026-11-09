using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Jobs;

/// <summary>Dispatches one tick of a job and holds the shared walking and working helpers.</summary>
public static class JobRunner
{
    /// <summary>Advances the job one tick; true when it is finished (done or failed).</summary>
    public static bool Tick(World w, Character c, Job j) => j.Kind switch
    {
        JobKind.Eat => NeedsJobs.Eat(w, c, j),
        JobKind.Sleep => NeedsJobs.Sleep(w, c, j),
        JobKind.SeekCover => At(w, c, j) || Failed(j),
        JobKind.Wander => NeedsJobs.Wander(w, c, j),
        JobKind.Socialize => NeedsJobs.Socialize(w, c, j),
        JobKind.Forage => CampJobs.Forage(w, c, j),
        JobKind.GatherTimber => CampJobs.GatherTimber(w, c, j),
        JobKind.Haul => CampJobs.Haul(w, c, j),
        JobKind.RepairRoof => CampJobs.RepairRoof(w, c, j),
        JobKind.KeepWatch => CampJobs.KeepWatch(w, c, j),
        JobKind.Care => CareJobs.Care(w, c, j),
        JobKind.KeepPromise => CareJobs.KeepPromise(w, c, j),
        JobKind.RequestBlood => BloodJobs.Request(w, c, j),
        JobKind.GiveBlood => BloodJobs.Give(w, c, j),
        JobKind.Arrive => IncidentJobs.Arrive(w, c, j),
        JobKind.Fight => IncidentJobs.Fight(w, c, j),
        JobKind.Flee => IncidentJobs.Flee(w, c, j),
        JobKind.Rescue => IncidentJobs.Rescue(w, c, j),
        JobKind.TendTree => Forest.Tend(w, c, j),
        _ => true,
    };

    public static bool Failed(Job j) => j.Stage < 0;

    /// <summary>True once the character stands on the job's target tile; re-plans if the path ran out; marks failure if unreachable.</summary>
    public static bool At(World w, Character c, Job j)
    {
        if (c.Path.Count > 0) return false;
        if (c.Pos == j.Target) return true;
        if (!TravelSystem.Start(w, c, j.Target))
        {
            j.Stage = -1;
            return false;
        }
        return c.Path.Count == 0;
    }

    /// <summary>Walks toward another (possibly moving) person; true once adjacent.</summary>
    public static bool Near(World w, Character c, Job j, Character other)
    {
        if (c.Pos.ChebyshevTo(other.Pos) <= 1)
        {
            c.Path.Clear();
            c.MoveTicks = 0;
            return true;
        }
        bool stale = c.Path.Count == 0 || (w.Tick % 20 == 0 && c.Path[^1].ChebyshevTo(other.Pos) > 1);
        if (!stale) return false;
        Point spot = Places.Adjacent(w, other.Pos, c.Pos);
        if (spot == Point.None || !TravelSystem.Start(w, c, spot)) j.Stage = -1;
        else j.Target = spot;
        return false;
    }

    /// <summary>Counts working ticks; true when the duration is reached.</summary>
    public static bool Work(Job j, int duration)
    {
        j.Progress++;
        return j.Progress >= duration;
    }

    /// <summary>Moves on to the next stage of a job, heading for a new target.</summary>
    public static void Next(World w, Character c, Job j, Point target)
    {
        j.Stage++;
        j.Progress = 0;
        j.Target = target;
        if (!TravelSystem.Start(w, c, target)) j.Stage = -1;
    }

    public static void Deliver(World w, Character c)
    {
        if (c.Carrying is not { } carry) return;
        Stock.Add(w, carry.DefId, carry.Count, c.Pos);
        c.Carrying = null;
        Rations.AfterDeposit(w, carry.DefId, c.Pos);
    }

    public static void RollAccident(World w, Character c, WorkDef def, long cause)
    {
        if (def.AccidentPermille > 0 && w.Rng.Dice[DiceSystem.Work].Chance(DiceId.AccidentRoll, def.AccidentPermille))
            IncidentSystem.Fire(w, "work_accident", cause, c.Id);
    }

    public static SimEvent Emit(World w, SystemTag sys, string key, int[] actors, long[] causes, params EventArg[] args) =>
        w.Log.Emit(w, sys, key, actors, causes, null, args);
}
