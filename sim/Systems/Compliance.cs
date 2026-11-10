using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems.Culture;

namespace Return.Sim.Systems;

/// <summary>
/// Who keeps the camp's rules (zones design 5.10; the founder's answer in ADR-0025 item 2; ADR-0028). One
/// helper decides whether a person goes their own way under any rule the player set: the home area's
/// curfew (zones step 3), and a change of hours or a waking for the watch (the Schedule), so breaking
/// any rule reads alike and has one reason the Chronicle can name. How people feel about whoever set the
/// rule decides how far it carries. It never applies to the player, nor to whoever set the rule.
///
/// The odds, in permille, clamped to 0..1000, are the sum of four terms:
///   nature  — 6 per point by which their priorityRespectPct falls below 100 (the Independent, 50: 300);
///   feeling — 5 per point of opinion of whoever set the rule, either way, at most 400 either way: dislike
///             adds, liking takes away, so an Independent who likes you well enough goes along;
///   mood    — 150 while their mood is black (below SocialSystem.LowMoodBelow);
///   culture — 50 per point of their Oathbound–Free axis, which is +2 at the oath end: leaning Free adds up
///             to 100, holding oaths heavy takes up to 100 away.
/// The largest positive term names the reason, ties going nature, feeling, mood, culture. A custom that
/// settles the answer outright (culture-faith slice 2) will come before the odds.
/// </summary>
public static class Compliance
{
    public const int NaturePerPoint = 6;
    public const int FeelingPerPoint = 5;
    public const int FeelingCap = 400;
    public const int BlackMood = 150;
    public const int CulturePerPoint = 50;

    /// <summary>At this point of the Oathbound–Free axis or beyond, a person keeps rules and judges breakers.</summary>
    public const int KeepsRulesAxis = 2;

    /// <summary>Rule-keepers this close to a breach see it.</summary>
    public const int WitnessRange = 8;

    /// <summary>The reason keys event variants end in: "_own_way", "_dislike", "_mood", "_ways".</summary>
    public const string OwnWay = "own_way", Dislike = "dislike", Mood = "mood", Ways = "ways";

    /// <summary>The odds a person goes their own way (permille), each term, and the reason the largest names ("" when nothing pulls them).</summary>
    public readonly record struct Odds(int Permille, int Nature, int Feeling, int MoodTerm, int Culture, string Reason)
    {
        public static readonly Odds None = new(0, 0, 0, 0, 0, "");
    }

    /// <summary>The odds that <paramref name="c"/> goes their own way under a rule <paramref name="setterId"/> set (the player now; a delegate at kingdom scale).</summary>
    public static Odds OddsOf(World w, Character c, int setterId)
    {
        if (c.Id == w.PlayerId || c.Id == setterId) return Odds.None;
        int nature = Math.Max(0, 100 - Traits.ScalePct(w, c, t => t.PriorityRespectPct)) * NaturePerPoint;
        int feeling = Math.Clamp(-Relations.Opinion(w, c.Id, setterId) * FeelingPerPoint, -FeelingCap, FeelingCap);
        int mood = c.Mood < SocialSystem.LowMoodBelow ? BlackMood : 0;
        int culture = -Culture.Ways.Axis(w, c, EthicAxis.OathboundFree) * CulturePerPoint;
        int permille = Math.Clamp(nature + feeling + mood + culture, 0, 1000);
        string reason = "";
        int largest = 0;
        foreach ((int term, string why) in new[] { (nature, OwnWay), (feeling, Dislike), (mood, Mood), (culture, Ways) })
            if (term > largest) { largest = term; reason = why; }
        return new Odds(permille, nature, feeling, mood, culture, reason);
    }

    /// <summary>
    /// Whether they go their own way: one draw on the Work stream with the rule's own tag (zones'
    /// ZoneDefy; the Schedule's day-change and watch tags), taken only when the odds are above 0, so a
    /// camp that keeps its rules draws nothing. Returns the reason key, or null if they keep the rule.
    /// </summary>
    public static string? Defies(World w, Character c, int setterId, DiceId tag)
    {
        Odds odds = OddsOf(w, c, setterId);
        if (odds.Permille <= 0) return null;
        return w.Rng.Dice[DiceSystem.Work].Chance(tag, odds.Permille) ? odds.Reason : null;
    }

    /// <summary>Keeps rules and judges those who break them: the Oathbound (promisePriority above 0), and those whose ways hold oaths heaviest (+2).</summary>
    public static bool KeepsRules(World w, Character c) =>
        Traits.Sum(w, c, t => t.PromisePriority) > 0 || Culture.Ways.Axis(w, c, EthicAxis.OathboundFree) >= KeepsRulesAxis;

    /// <summary>
    /// Every other awake member within <see cref="WitnessRange"/> tiles of the breaker who keeps rules
    /// thinks less of them (saw_rule_broken: mood −2, opinion −4, stacking to three), caused by the breach.
    /// Only a defiant breach is witnessed: never the starving, never someone going to help. Returns the witnesses.
    /// </summary>
    public static List<Character> Witness(World w, Character breaker, SimEvent breach, params int[] notWitnesses)
    {
        var seen = new List<Character>();
        foreach (Character m in w.Characters)
        {
            if (m.Status != CharStatus.Member || m.Id == breaker.Id || m.Asleep || m.Downed || Array.IndexOf(notWitnesses, m.Id) >= 0) continue;
            if (m.Pos.ChebyshevTo(breaker.Pos) > WitnessRange || !KeepsRules(w, m)) continue;
            MoodSystem.AddThought(w, m, "saw_rule_broken", breaker.Id, breach.Id);
            seen.Add(m);
        }
        return seen;
    }
}
