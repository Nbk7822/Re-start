using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>Death and departure: the persistent endings every story system can reach.</summary>
public static class Lifecycle
{
    public static SimEvent Die(World w, Character c, SystemTag system, string key, long cause)
    {
        bool wasMember = c.Status == CharStatus.Member;
        c.Status = CharStatus.Dead;
        c.Health = 0;
        c.Path.Clear();
        c.Job = null;
        c.Downed = false;
        c.Asleep = false;
        c.Carrying = null;
        w.Reservations.RemoveAll(r => r.CharacterId == c.Id);
        Relations.DropGrudgesHeldBy(w, c);
        SimEvent e = w.Log.Emit(w, system, key, new[] { c.Id }, new[] { cause },
            new[] { new Effect { Kind = EffectKind.Death, A = c.Id } }, EventArg.Char("a", c.Id));
        if (c.Id == w.PlayerId) Fate.OnPlayerDied(w, e); // your death is answered at the end of the tick: a return, or the end of the run (ADR-0030)
        if (!wasMember) return e;
        foreach (Character m in w.Members())
        {
            MoodSystem.AddThought(w, m, "saw_death", c.Id, e.Id);
            if (Relations.Opinion(w, m.Id, c.Id) >= 30) MoodSystem.AddThought(w, m, "friend_died", c.Id, e.Id);
        }
        Culture.Omens.OnSign(w, "death", e); // a member's death is a sign the faithful may read (ADR-0023)
        return e;
    }
}
