using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// First meetings and the warmth of talk (ADR-0023). Two of the same people greet each other as
/// kin (G2); someone who does not know the listener's greeting may breach it (G4), which slights the
/// listener, ends the conversation, and teaches the speaker the custom. The otherworlder, who knows
/// no ways at all, stumbles three times as often. Chat warmth leans toward kin and away from
/// strangers and the peoples a culture is wary of (G1, G3, A1).
/// </summary>
public static class Greetings
{
    /// <summary>
    /// Called when c starts talking to p. On their first meeting, kin greet each other; otherwise c
    /// may breach p's greeting. True when a breach ended the conversation.
    /// </summary>
    public static bool FirstMeeting(World w, Character c, Character p)
    {
        if (!Relations.Meet(w, c, p)) return false;
        if (c.CultureId.Length > 0 && c.CultureId == p.CultureId)
        {
            MoodSystem.AddThought(w, c, "kin_greeting", p.Id, 0);
            MoodSystem.AddThought(w, p, "kin_greeting", c.Id, 0);
            return false;
        }
        if (Ways.Keeps(w, p, "greeting") is not { } greeting || greeting.HolderIsFaith) return false;
        if (c.LearnedCustoms.Contains(greeting.Def.Id) || KeepsCustom(w, c, greeting.Def.Id)) return false;
        int odds = greeting.Def.Arg("breachPermille") * (c.CultureId.Length == 0 ? CultureTuning.OutsiderBreachMultiplier : 1);
        if (!w.Rng.Dice[DiceSystem.Culture].Chance(DiceId.FauxPas, Math.Min(1000, odds))) return false;
        SimEvent e = w.Log.Emit(w, SystemTag.Culture, greeting.Def.EventKey("breach"), new[] { c.Id, p.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", c.Id), EventArg.Char("b", p.Id), EventArg.Key("ways", greeting.HolderNameKey));
        MoodSystem.AddThought(w, p, "faux_pas", c.Id, e.Id);
        int at = c.LearnedCustoms.BinarySearch(greeting.Def.Id, StringComparer.Ordinal);
        if (at < 0) c.LearnedCustoms.Insert(~at, greeting.Def.Id);
        return true;
    }

    /// <summary>
    /// How warmly c takes a chat with p, in percent of a plain chat: chemistry, plus kin (same
    /// culture), minus strangers (different cultures while either is new to the camp), plus or minus
    /// c's people's stance toward p's people, plus festival-goers together. Never below 0.
    /// </summary>
    public static int Warmth(World w, Character c, Character p, int chemistry)
    {
        int warmth = 100 + 50 * chemistry;
        bool sameCulture = c.CultureId.Length > 0 && c.CultureId == p.CultureId;
        if (sameCulture) warmth += CultureTuning.KinWarmth;
        else if (Newcomer(w, c) || Newcomer(w, p)) warmth += CultureTuning.StrangerWarmth;
        warmth += Ways.StanceWarmth(w, c, p);
        if (Festivals.BothAttend(w, c.Id, p.Id)) warmth += CultureTuning.FestivalWarmth;
        return Math.Max(0, warmth);
    }

    public static bool Newcomer(World w, Character c) =>
        c.ArrivedTick > 0 && w.Tick - c.ArrivedTick < (long)SocialSystem.NewcomerDays * w.Content.World.TicksPerDay;

    private static bool KeepsCustom(World w, Character c, string customId)
    {
        foreach (KeptCustom k in Ways.KeptCustoms(w, c))
            if (k.Def.Id == customId) return true;
        return false;
    }
}
