using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Your one gift (the player design §4.2, ADR-0030), the rules every gift shares. One per run, a Truth. It wakes at its
/// trigger, or by its day in a dream, and once found it is known in every timeline. It grows by its own events, never
/// grind: stages come at counts, the count is world state (a return sets it back) but a stage you reached before needs
/// half the count. Every event it makes cites its cause, and nobody's choice is taken away. Each gift's behaviour is
/// named by its id (Satchel, Luck, FamiliarFaces), as an incident's or a need's is. A gift draws only from the Fate
/// stream, and only when it acts.
/// </summary>
public static class Gifts
{
    public const string SatchelId = "satchel";
    public const string LuckId = "fortunes_favourite";
    public const string FamiliarId = "familiar_faces";

    /// <summary>Your gift's definition, or null if you have none.</summary>
    public static GiftDef? Held(World w) => w.Fate.GiftId.Length == 0 ? null : w.Content.TryGift(w.Fate.GiftId);

    public static bool Holds(World w, string id) => w.Fate.GiftId == id;

    /// <summary>Hourly, before food is shared out: a gift that has not woken by its day arrives in a dream at dawn; then each gift's own hour.</summary>
    public static void Hourly(World w)
    {
        if (Held(w) is not { } g || w.Find(w.PlayerId) is not { Status: CharStatus.Member } you) return;
        WorldDef def = w.Content.World;
        if (!w.Fate.GiftAwakened && w.Tick % def.TicksPerDay == (long)def.DawnHour * def.TicksPerHour
            && Calendar.Day(w.Tick, def) + 1 >= g.Awakening.ByDay)
            Awaken(w, 0, dream: true);
        switch (g.Id)
        {
            case SatchelId:
                Satchel.Hourly(w, you, g);
                break;
            case FamiliarId:
                FamiliarFaces.Hourly(w, you, g);
                break;
        }
    }

    /// <summary>
    /// Your gift wakes: its trigger's line ("You found your gift: harm meant for you finds someone near you."), citing
    /// the trigger; or, by its day, a dream. Once found it is known in every timeline (run memory). Null if already awake.
    /// </summary>
    public static SimEvent? Awaken(World w, long trigger, bool dream)
    {
        if (w.Fate.GiftAwakened || Held(w) is not { } g || w.Find(w.PlayerId) is not { } you) return null;
        w.Fate.GiftAwakened = true;
        return w.Log.Emit(w, SystemTag.Gift, dream ? g.Awakening.DreamKey : g.Awakening.TextKey, new[] { you.Id }, new[] { trigger }, null,
            EventArg.Char("a", you.Id));
    }

    /// <summary>How many of its events a stage needs: its count, or half (rounded up) for a stage reached in any timeline before.</summary>
    public static int Needed(World w, GiftDef g, int tier)
    {
        int at = g.Tiers[tier].At;
        return tier <= w.Fate.GiftMastery ? (at + 1) / 2 : at;
    }

    /// <summary>
    /// One of the events that make your gift grow happened (<paramref name="cause"/>): count it, and when the count reaches
    /// the next stage, the gift grows, a line that cites the event and ends a story (<see cref="EffectKind.GiftGrew"/>).
    /// </summary>
    public static void Count(World w, long cause)
    {
        if (Held(w) is not { } g || w.Find(w.PlayerId) is not { } you) return;
        GiftState s = w.Gift;
        s.Progress++;
        while (s.Tier + 1 < g.Tiers.Count && s.Progress >= Needed(w, g, s.Tier + 1))
        {
            s.Tier++;
            w.Fate.GiftMastery = Math.Max(w.Fate.GiftMastery, s.Tier);
            w.Log.Emit(w, SystemTag.Gift, "event.gift.grew", new[] { you.Id }, new[] { cause },
                new[] { new Effect { Kind = EffectKind.GiftGrew, A = you.Id, RefId = s.Tier } },
                EventArg.Char("a", you.Id), EventArg.Key("gift", g.NameKey), EventArg.Key("stage", g.Tiers[s.Tier].StageKey));
        }
    }

    /// <summary>You foraged: the satchel fills (and the first forage wakes it).</summary>
    public static void Foraged(World w, Character c, SimEvent forage)
    {
        if (c.Id == w.PlayerId && Holds(w, SatchelId)) Satchel.Foraged(w, c, forage);
    }

    /// <summary>
    /// Harm meant for <paramref name="target"/> is about to land: under Fortune's favourite, harm meant for you lands on
    /// someone near you instead. Returns who is hurt and the event to cite (the target and <paramref name="cause"/> when nothing turns).
    /// </summary>
    public static (Character Hurt, long Cause) TurnHarm(World w, Character target, int damage, long cause, bool blow) =>
        target.Id == w.PlayerId && Holds(w, LuckId) ? Luck.Turn(w, target, damage, cause, blow) : (target, cause);

    /// <summary>What you do about your gift before ordinary work (the satchel's share, under its order), or null.</summary>
    public static Job? Job(World w, Character c, bool sunBlocked) =>
        c.Id == w.PlayerId && Holds(w, SatchelId) ? Satchel.ShareJob(w, c, sunBlocked) : null;

    /// <summary>Someone walked into the world: under Familiar faces, someone you were close to in a lost future knows you.</summary>
    public static void OnArrival(World w, Character c, long arrival)
    {
        if (Holds(w, FamiliarId)) FamiliarFaces.Arrived(w, c, arrival);
    }

    /// <summary>At a return, before the dying world is let go: what your gift carries back (Familiar faces' echoes).</summary>
    public static void KeepEchoes(World dying, FateBook fate)
    {
        if (fate.GiftId == FamiliarId) FamiliarFaces.KeepEchoes(dying, fate);
    }

    /// <summary>After a return, once the world gone back to is in place and the return is logged: each gift's return hook.</summary>
    public static void OnReturned(World w, SimEvent back)
    {
        if (Holds(w, FamiliarId)) FamiliarFaces.Returned(w, back);
    }

    /// <summary>Your standing order for a gift's order id, or its default.</summary>
    public static string Order(World w, GiftOrderDef order)
    {
        foreach (StandingOrder o in w.StandingOrders)
            if (o.OrderId == order.Id) return o.ChoiceId;
        return order.Default;
    }
}
