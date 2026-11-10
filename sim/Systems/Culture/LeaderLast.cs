using Return.Sim.Events;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// "A leader eats last" (F1, F2; ADR-0023). The player's ration policy becomes a cultural statement:
/// a keeper who goes without while the leader is fed first under "you eat first" takes offence, and
/// every keeper who is fed while the leader goes without sees how a leader is known.
/// </summary>
public static class LeaderLast
{
    /// <summary>After a share-out: <paramref name="wentWithout"/> holds each person this share-out newly left out, with the event that said so.</summary>
    public static void AfterShareOut(World w, List<(Character Who, long Event)> wentWithout)
    {
        if (wentWithout.Count == 0 || w.Find(w.PlayerId) is not { Status: CharStatus.Member } leader) return;

        if (w.Policy == RationPolicy.SelfFirst && Rations.HasReservation(w, leader.Id))
            foreach ((Character c, long went) in wentWithout)
            {
                if (c.Id == leader.Id || Ways.Keeps(w, c, "leader_last") is not { } custom) continue;
                SimEvent e = w.Log.Emit(w, SystemTag.Culture, custom.Def.EventKey("offended"), new[] { c.Id, leader.Id }, new[] { went, w.PolicyEvent }, null,
                    EventArg.Char("a", c.Id), EventArg.Char("b", leader.Id), EventArg.Key("ways", custom.HolderNameKey));
                MoodSystem.AddThought(w, c, "leader_ate_first", leader.Id, e.Id);
                if (Relations.Opinion(w, c.Id, leader.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, c.Id, leader.Id) == null)
                    Relations.FormGrudge(w, c, leader, CultureTuning.LeaderLastGrudge, e.Id);
            }

        long leaderWent = 0;
        foreach ((Character c, long went) in wentWithout)
            if (c.Id == leader.Id) leaderWent = went;
        if (leaderWent == 0) return;
        foreach (Character m in w.Members())
        {
            if (m.Id == leader.Id || !Rations.HasReservation(w, m.Id) || m.Thoughts.Exists(t => t.DefId == "leader_ate_last")) continue;
            if (Ways.Keeps(w, m, "leader_last") is not { } custom) continue;
            SimEvent e = w.Log.Emit(w, SystemTag.Culture, custom.Def.EventKey("honoured"), new[] { m.Id, leader.Id }, new[] { leaderWent }, null,
                EventArg.Char("a", m.Id), EventArg.Char("b", leader.Id), EventArg.Key("ways", custom.HolderNameKey));
            MoodSystem.AddThought(w, m, "leader_ate_last", leader.Id, e.Id);
        }
    }
}
