using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Jobs;

/// <summary>Arriving at the camp, fighting a thief, fleeing, and carrying the fallen to safety.</summary>
public static class IncidentJobs
{
    public static bool Arrive(World w, Character c, Job j)
    {
        if (!JobRunner.At(w, c, j) && !JobRunner.Failed(j)) return false;
        IncidentSystem.Join(w, c, j.CauseEvent);
        return true;
    }

    public static bool Fight(World w, Character c, Job j)
    {
        Character? thief = w.Find(j.TargetId);
        if (thief == null || thief.Status != CharStatus.Hostile || thief.Job is { Kind: JobKind.Flee }) return true;
        if (!JobRunner.Near(w, c, j, thief)) return JobRunner.Failed(j);
        c.Facing = TravelSystem.FacingFor(c.Pos, thief.Pos);
        return false;
    }

    public static bool Flee(World w, Character c, Job j) => JobRunner.At(w, c, j) || JobRunner.Failed(j);

    public static bool Rescue(World w, Character c, Job j)
    {
        Character? patient = w.Find(j.TargetId);
        if (patient == null || patient.Status != CharStatus.Member || !patient.Downed) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.Near(w, c, j, patient)) return JobRunner.Failed(j);
            Point bed = Places.SleepSpot(w, patient);
            if (bed == Point.None || bed == patient.Pos) return true;
            JobRunner.Next(w, c, j, bed);
            j.Stage = 1;
            return false;
        }
        patient.Pos = c.Pos;
        if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
        patient.Pos = c.Pos;
        SimEvent e = JobRunner.Emit(w, SystemTag.Work, "event.work.rescued", new[] { c.Id, patient.Id }, new[] { patient.LastHealthEvent },
            EventArg.Char("a", c.Id), EventArg.Char("b", patient.Id));
        MoodSystem.AddThought(w, patient, "rescued", c.Id, e.Id);
        CareJobs.SettleOrOwe(w, helper: c, helped: patient, DebtKind.Rescue, e.Id);
        return true;
    }
}
