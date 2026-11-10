using Return.Sim.Content;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Familiar faces ("have we met?"; the player design §4.4): each return keeps every cast member's last opinion of you
/// as an echo (the newest winning). After a return, anyone in camp, or arriving, whose echo is warm enough feels they
/// have known you for years: their regard for you rises by a share of it (a quarter, two fifths, three fifths by stage)
/// and they are cheered; a cold enough echo makes them uneasy instead, and cools them. Nobody else notices. It wakes at
/// the first warm recognition (by Day 10 in a dream) and grows each time a familiar face becomes your friend, or does
/// you a kindness, within days of a return.
/// </summary>
public static class FamiliarFaces
{
    /// <summary>At a return: every cast member's last opinion of you becomes their echo.</summary>
    public static void KeepEchoes(World dying, FateBook fate)
    {
        foreach (Character x in dying.Characters)
        {
            if (x.Id == dying.PlayerId || !LostFutures.IsCast(dying, x.Id)) continue;
            Opinion? held = x.Opinions.Find(o => o.TargetId == dying.PlayerId);
            if (held == null) continue;
            fate.Echoes.RemoveAll(e => e.PersonId == x.Id);
            fate.Echoes.Add(new Echo { PersonId = x.Id, Opinion = held.Value });
        }
    }

    /// <summary>After a return: everyone in camp with an echo feels it now.</summary>
    public static void Returned(World w, SimEvent back)
    {
        for (int i = 0; i < w.Characters.Count; i++)
        {
            Character x = w.Characters[i];
            if (x.Status == CharStatus.Member && x.Id != w.PlayerId) Feel(w, x, back.Id, 0);
        }
    }

    /// <summary>Someone arriving after a return feels their echo on arrival.</summary>
    public static void Arrived(World w, Character x, long arrival)
    {
        if (w.Fate.Returns > 0 && x.Id != w.PlayerId) Feel(w, x, w.Gift.ReturnedEvent, arrival);
    }

    private static void Feel(World w, Character x, long back, long arrival)
    {
        if (w.Gift.Familiar.Contains(x.Id) || w.Find(w.PlayerId) is not { } you) return;
        Echo? echo = w.Fate.Echoes.Find(e => e.PersonId == x.Id);
        GiftDef g = Gifts.Held(w)!;
        if (echo == null || Math.Abs(echo.Opinion) < g.Num("echoAtLeast")) return;
        bool warm = echo.Opinion > 0;
        SimEvent e = w.Log.Emit(w, SystemTag.Social, warm ? "event.gift.familiar.knew_you" : "event.gift.familiar.uneasy",
            new[] { x.Id, you.Id }, new[] { back, arrival }, null, EventArg.Char("a", x.Id), EventArg.Char("b", you.Id));
        w.Gift.Familiar.Add(x.Id);
        w.Gift.FamiliarEvents.Add(e.Id);
        MoodSystem.AddThought(w, x, warm ? "familiar" : "uneasy_familiar", you.Id, e.Id);
        Relations.ChangeOpinion(w, x, you.Id, echo.Opinion * g.Num("echoPct", w.Gift.Tier) / 100, e.Id);
        if (warm) Gifts.Awaken(w, e.Id, dream: false);
    }

    /// <summary>
    /// Within days of a return, each familiar face who becomes your friend (their regard for you at the friend line) or
    /// does you a kindness (you come to owe them) counts once toward the gift's growth, citing the kindness or the recognition.
    /// </summary>
    public static void Hourly(World w, Character you, GiftDef g)
    {
        GiftState s = w.Gift;
        if (s.ReturnedTick < 0 || w.Tick - s.ReturnedTick > (long)g.Num("withinDays") * w.Content.World.TicksPerDay) return;
        for (int i = 0; i < s.Familiar.Count; i++)
        {
            int id = s.Familiar[i];
            if (s.Counted.BinarySearch(id) >= 0 || w.Find(id) is not { Status: CharStatus.Member } x) continue;
            if (w.Log.Get(s.FamiliarEvents[i])?.Key != "event.gift.familiar.knew_you") continue; // only a warm recognition is a familiar face
            long cause = 0;
            foreach (Debt d in w.Debts)
                if (d.DebtorId == you.Id && d.CreditorId == id && d.FormedTick >= s.ReturnedTick) { cause = d.FormedEvent; break; }
            if (cause == 0 && Relations.Opinion(w, x.Id, you.Id) >= Memories.FriendAt && Relations.Opinion(w, you.Id, x.Id) > 0) cause = s.FamiliarEvents[i];
            if (cause == 0) continue;
            s.Counted.Insert(~s.Counted.BinarySearch(id), id);
            Gifts.Count(w, cause);
        }
    }
}
