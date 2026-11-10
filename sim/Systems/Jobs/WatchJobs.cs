namespace Return.Sim.Systems.Jobs;

/// <summary>
/// The watch's jobs (ADR-0029). Keeping watch is standing at the post until the WatchSystem ends the turn; it never
/// ends itself, so every way off the post has a line. Waking someone is walking to a sleeper and asking.
/// </summary>
public static class WatchJobs
{
    public static bool KeepWatch(World w, Character c, Job j)
    {
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            j.Stage = 1;
            c.Facing = Facing.S;
            WatchSystem.ReachedPost(w, c);
            return false;
        }
        j.Progress++;
        return false;
    }

    /// <summary>Walks to the sleeper; beside them, the WatchSystem wakes them and hears their answer, and gives the waker what comes next.</summary>
    public static bool WakeForWatch(World w, Character c, Job j)
    {
        Character? sleeper = w.Find(j.TargetId);
        if (sleeper is not { Status: CharStatus.Member } || sleeper.Downed) return true;
        if (!JobRunner.Near(w, c, j, sleeper)) return JobRunner.Failed(j);
        c.Facing = TravelSystem.FacingFor(c.Pos, sleeper.Pos);
        WatchSystem.Ask(w, c, sleeper);
        return false; // the WatchSystem has given them their next job (another waking, the post again) or none
    }
}
