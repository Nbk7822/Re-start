using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// The home area's soak measures that no line records (zones design 9.2), sampled every 30 ticks between steps, reading
/// only, so a world's log is the same whether it is measured or not: the walks home at dusk (someone bound out at work or
/// idling beyond the home area just before dusk, off that job just after), and the rule-keepers who saw a defiant breach,
/// by why they keep rules (the Oathbound trait, or their people's ways at the oath end).
/// </summary>
internal sealed class HomeMeter
{
    public int WalksHome, WitnessTrait, WitnessWays;

    private readonly List<(Character C, Job Job)> _outAtDusk = new();
    private readonly HashSet<(int Holder, long Cause)> _seen = new(); // membership only; never iterated

    public void Sample(World w)
    {
        var def = w.Content.World;
        long ofDay = w.Tick % def.TicksPerDay, dusk = (long)def.DuskHour * def.TicksPerHour;
        if (ofDay == dusk + DaysMeter.EveryTicks)
        {
            foreach ((Character c, Job job) in _outAtDusk)
                if (!ReferenceEquals(c.Job, job) && (c.Job == null || Zones.InHome(w, c.Job.Target))) WalksHome++;
            _outAtDusk.Clear();
        }
        if (ofDay == dusk && Zones.HomeOf(w) != null)
            foreach (Character c in w.Characters)
                if (c.Status == CharStatus.Member && !c.Downed && c.Job is { } j && Zones.Curfewed(j.Kind) && !Zones.InHome(w, j.Target) && Zones.Bound(w, c))
                    _outAtDusk.Add((c, j));
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member) continue;
            foreach (Thought t in c.Thoughts)
            {
                if (t.DefId != "saw_rule_broken" || w.Log.Get(t.CauseEvent) is not { } cause
                    || !cause.Key.StartsWith("event.work.out_after_dark", StringComparison.Ordinal) || !_seen.Add((c.Id, t.CauseEvent))) continue;
                if (Traits.Sum(w, c, x => x.PromisePriority) > 0) WitnessTrait++;
                else WitnessWays++;
            }
        }
    }
}
