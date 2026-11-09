using Return.Sim.Content;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// The shared upkeep of needs that come from a person's nature (ADR-0022). Every hour each need
/// drifts (its decay, plus whatever its behaviour restores), crosses its thresholds with events that
/// cite whatever last explained it, adds its low or critical thought, and, when its definition says
/// so, harms while critical or drives the person away. How a need is met, wounded or answered is the
/// behaviour named by its id, the way an incident's id names its behaviour.
/// </summary>
public static class NatureNeeds
{
    public static NatureNeed? Get(Character c, string id)
    {
        foreach (NatureNeed n in c.NatureNeeds)
            if (n.Id == id) return n;
        return null;
    }

    /// <summary>Gives a new character the needs their species adds, at their starting values, bound from Truths.</summary>
    public static void Init(World w, Character c)
    {
        c.NatureNeeds.Clear();
        foreach (string id in w.Content.SpeciesById(c.SpeciesId).Needs)
        {
            NeedDef def = w.Content.NeedById(id);
            var n = new NatureNeed { Id = id, Value = def.Start };
            Bind(w, c, n, def);
            c.NatureNeeds.Add(n);
        }
    }

    /// <summary>One hour of every nature need. Returns the health lost to needs that are critical and harm, setting the death text and health cause.</summary>
    public static int Hourly(World w, Character c, ref string deathKey)
    {
        int damage = 0;
        foreach (NatureNeed n in c.NatureNeeds)
        {
            NeedDef def = w.Content.NeedById(n.Id);
            int restore = Restore(w, c, n, def);
            n.Value = Math.Clamp(n.Value - def.DecayPerHour + restore, 0, 1000);
            // Met by its own source, and that source is open to them again: what explained the shortfall
            // is spent (as AfterFed clears LastFoodEvent). A work finished after it was forbidden does not count.
            if (restore > 0 && n.Value >= def.SatisfiedAt && SourceOpen(c, n)) n.LastEvent = 0;
            Thresholds(w, c, n, def);
            if (def.CriticalHealthPerHour > 0 && n.Value < def.CriticalBelow)
            {
                damage += def.CriticalHealthPerHour;
                deathKey = def.DeathKey;
                c.LastHealthEvent = n.LastEvent;
            }
        }
        return damage;
    }

    /// <summary>
    /// Changes a need because something happened (a wound, a kindness, a bargain). A loss makes that
    /// event the need's explanation; a gain that meets the need again clears it.
    /// </summary>
    public static void Adjust(World w, Character c, NatureNeed n, int delta, long cause)
    {
        NeedDef def = w.Content.NeedById(n.Id);
        n.Value = Math.Clamp(n.Value + delta, 0, 1000);
        if (delta < 0 && cause != 0) n.LastEvent = cause;
        else if (delta > 0 && n.Value >= def.SatisfiedAt) n.LastEvent = 0;
    }

    /// <summary>The event to cite when a need that drives people away has been critical long enough; 0 if none has.</summary>
    public static long DepartureCause(World w, Character c)
    {
        foreach (NatureNeed n in c.NatureNeeds)
        {
            NeedDef def = w.Content.NeedById(n.Id);
            if (def.LeavesAfterCriticalHours <= 0 || n.Stage < 2 || n.Value >= def.LowBelow) continue;
            if (w.Tick - n.CriticalSince >= (long)def.LeavesAfterCriticalHours * w.Content.World.TicksPerHour && n.LastEvent != 0)
                return n.LastEvent;
        }
        return 0;
    }

    /// <summary>
    /// What a person does about a nature need below its low mark (or, when <paramref name="critical"/>,
    /// its critical mark): its behaviour's job, or null. JobPicker asks for critical needs before rest
    /// and for low ones after it.
    /// </summary>
    public static Job? Job(World w, Character c, bool critical, bool sunBlocked)
    {
        foreach (NatureNeed n in c.NatureNeeds)
        {
            NeedDef def = w.Content.NeedById(n.Id);
            if (n.Value >= (critical ? def.CriticalBelow : def.LowBelow)) continue;
            Job? job = Answer(w, c, n, def, sunBlocked);
            if (job != null) return job;
        }
        return null;
    }

    private static void Thresholds(World w, Character c, NatureNeed n, NeedDef def)
    {
        if (n.Stage < 1 && n.Value < def.LowBelow)
        {
            SimEvent e = Emit(w, c, def.LowEventKey, n.LastEvent);
            n.Stage = 1;
            n.LastEvent = e.Id;
        }
        if (n.Stage < 2 && n.Value < def.CriticalBelow)
        {
            SimEvent e = Emit(w, c, def.CriticalEventKey, n.LastEvent);
            n.Stage = 2;
            n.CriticalSince = w.Tick;
            n.LastEvent = e.Id;
        }
        if (n.Value >= def.SatisfiedAt)
        {
            n.Stage = 0;
            n.ActedTick = 0;
        }
        if (n.Value < def.CriticalBelow) MoodSystem.AddThought(w, c, def.CriticalThought, 0, n.LastEvent);
        else if (n.Value < def.LowBelow) MoodSystem.AddThought(w, c, def.LowThought, 0, n.LastEvent);
    }

    private static SimEvent Emit(World w, Character c, string key, long cause) =>
        w.Log.Emit(w, SystemTag.Needs, key, new[] { c.Id }, new[] { cause }, null, EventArg.Char("a", c.Id));

    /// <summary>How much a person's nature leans them toward a kind of work (added to their traits' work bias).</summary>
    public static int WorkBias(World w, Character c, string workId) =>
        c.NatureNeeds.Count == 0 ? 0 : Purpose.WorkBias(w, c, workId);

    /// <summary>Binds a new need to what it is bound to (a tree, a work, a price), from Truths keyed by cast key.</summary>
    private static void Bind(World w, Character c, NatureNeed n, NeedDef def)
    {
        if (n.Id == Purpose.NeedId) Purpose.Bind(w, c, n, def);
        else if (n.Id == Forest.NeedId) Forest.Bind(w, c, n, def);
    }

    /// <summary>How much a need's own source restores this hour (beyond its decay).</summary>
    private static int Restore(World w, Character c, NatureNeed n, NeedDef def) => n.Id switch
    {
        Purpose.NeedId => Purpose.Restore(w, c, n, def),
        Forest.NeedId => Forest.Restore(w, c, n, def),
        _ => 0,
    };

    /// <summary>Is the need's source open to them (not, say, a work they have been told never to do)?</summary>
    private static bool SourceOpen(Character c, NatureNeed n) => n.Id switch
    {
        Purpose.NeedId => JobPicker.PriorityOf(c, n.BondWork) > 0,
        _ => true,
    };

    /// <summary>The job that answers a low need, by the need's behaviour.</summary>
    private static Job? Answer(World w, Character c, NatureNeed n, NeedDef def, bool sunBlocked) => n.Id switch
    {
        Purpose.NeedId => Purpose.Answer(w, c, n, sunBlocked),
        Forest.NeedId => Forest.Answer(w, c, n, sunBlocked),
        _ => null,
    };
}
