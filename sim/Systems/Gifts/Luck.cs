using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Fortune's favourite (the lucky hero of isekai; the player design §4.4): harm meant for you (an accident, or a
/// hostile's blow) lands on the nearest person within reach instead, for the same damage. It always turns when someone
/// is in reach; the Fate stream (<see cref="DiceId.LuckVictim"/>) only breaks a tie between people equally near, so
/// keeping people near you is your choice, and so is its price. The one hurt holds it against you (ill luck), and those
/// who see it think a little less of you. At its last stage, once a season, harm that would fell you with nobody in
/// reach finds the nearest person anywhere: death passes you by. It wakes at the first turned harm and grows with each.
/// A bleed you let run stays yours: only the blow turns.
/// </summary>
public static class Luck
{
    /// <param name="wornOut">The wear that made an accident likelier, cited beside the cause (merge review); 0 when none.</param>
    public static (Character Hurt, long Cause) Turn(World w, Character you, int damage, long cause, bool blow, long wornOut = 0)
    {
        GiftDef g = Gifts.Held(w)!;
        int tier = w.Gift.Tier, reach = g.Num("reach", tier);
        var near = new List<Character>();
        foreach (Character m in w.Characters)
            if (m.Id != you.Id && m.Status == CharStatus.Member && !m.Downed && m.Pos.ChebyshevTo(you.Pos) <= reach) near.Add(m);
        bool passes = false;
        if (near.Count == 0)
        {
            long season = w.Tick / w.Content.World.TicksPerSeason;
            bool fells = you.Health - damage < NeedsSystem.DownedBelow;
            if (g.Num("deathPasses", tier) == 0 || !fells || w.Gift.LastDeathPassedSeason == season) return (you, cause);
            foreach (Character m in w.Characters)
                if (m.Id != you.Id && m.Status == CharStatus.Member && !m.Downed) near.Add(m);
            if (near.Count == 0) return (you, cause);
            w.Gift.LastDeathPassedSeason = season;
            passes = true;
        }
        int closest = int.MaxValue;
        foreach (Character m in near) closest = Math.Min(closest, m.Pos.ChebyshevTo(you.Pos));
        var tied = near.FindAll(m => m.Pos.ChebyshevTo(you.Pos) == closest); // in id order
        Character victim = tied.Count == 1 ? tied[0] : tied[w.Rng.Dice[DiceSystem.Fate].Range(DiceId.LuckVictim, 0, tied.Count)];

        string key = passes ? "event.gift.luck.death_passed" : blow ? "event.gift.luck.turned_blow" : "event.gift.luck.turned_accident";
        SimEvent turned = w.Log.Emit(w, SystemTag.Gift, key, new[] { you.Id, victim.Id }, new[] { cause, wornOut }, null,
            EventArg.Char("a", you.Id), EventArg.Char("b", victim.Id));
        Gifts.Awaken(w, turned.Id, dream: false);
        MoodSystem.AddThought(w, victim, "ill_luck", you.Id, turned.Id);
        int witnessed = w.Content.ThoughtById("ill_luck").Opinion * g.Num("witnessPct") / 100, sight = g.Num("sight");
        foreach (Character m in w.Characters)
            if (m.Id != you.Id && m.Id != victim.Id && m.Status == CharStatus.Member && !m.Asleep && !m.Downed && m.Pos.ChebyshevTo(you.Pos) <= sight)
                Relations.ChangeOpinion(w, m, you.Id, witnessed, turned.Id);
        Gifts.Count(w, turned.Id);
        return (victim, turned.Id);
    }
}
