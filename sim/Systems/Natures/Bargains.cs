using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// The djinn's need (ADR-0022). In Arabian folklore, as the Nights tell it, the djinn are spirits of
/// living flame with free will and their own ethics; a djinn keeps its word and expects it kept.
/// Here a djinn without a bargain grows restless and goes to offer one: a service (a Truth of its
/// nature) in return for a price each dawn (also a Truth) until the season turns. Accepting is the
/// offeree's own call, never the player's: the djinn asks whoever it thinks best of, and they agree
/// if they think well enough of it (their consent bias, as for blood) and, if they refuse unsafe
/// bargains (Proud), only when the camp could pay. Paid to the end, a bargain is kept; a dawn with
/// nothing to pay breaks it and the djinn holds the patron to account. Left unbound with a wrong
/// unanswered, it lets the fire loose in the stockpile; unbound long enough, it leaves.
/// </summary>
public static class Bargains
{
    public const string NeedId = "contract";

    public static void Bind(World w, Character c, NatureNeed n, NeedDef def)
    {
        ulong key = Fnv1a.Hash64(c.CastKey);
        if (def.BondWorks.Count > 0) n.BondWork = def.BondWorks[w.Rng.Truths.Range(TruthId.BargainService, key, 0, 0, def.BondWorks.Count)];
        if (def.BondItems.Count > 0) n.BondItem = def.BondItems[w.Rng.Truths.Range(TruthId.BargainPrice, key, 0, 0, def.BondItems.Count)];
    }

    /// <summary>The open bargain this person is bound by, if any.</summary>
    public static Contract? Binding(World w, int boundId)
    {
        foreach (Contract k in w.Contracts)
            if (k.Status == ContractStatus.Open && k.BoundId == boundId) return k;
        return null;
    }

    /// <summary>Bound by a bargain, the need fills each hour.</summary>
    public static int Restore(World w, Character c, NatureNeed n, NeedDef def)
    {
        if (Binding(w, c.Id) == null) return 0;
        MoodSystem.AddThought(w, c, "bound_by_bargain", 0, 0);
        return def.Param("boundRestorePerHour");
    }

    /// <summary>Restless and unbound, the djinn goes to offer a bargain, unless it was just turned down.</summary>
    public static Job? Answer(World w, Character c, NatureNeed n, bool sunBlocked)
    {
        if (Binding(w, c.Id) != null || n.BondWork.Length == 0 || n.BondItem.Length == 0 || RecentlyRefused(w, c)) return null;
        Character? to = Offeree(w, c);
        if (to == null) return null;
        Point spot = Places.Adjacent(w, to.Pos, c.Pos);
        return spot == Point.None ? null : new Job { Kind = JobKind.OfferBargain, TargetId = to.Id, Target = spot, CauseEvent = n.LastEvent };
    }

    /// <summary>The OfferBargain job: reach them, make the offer, and take their answer.</summary>
    public static bool Offer(World w, Character djinn, Job j)
    {
        Character? to = w.Find(j.TargetId);
        NatureNeed? n = NatureNeeds.Get(djinn, NeedId);
        if (to == null || to.Status != CharStatus.Member || to.Asleep || to.Downed || n == null || Binding(w, djinn.Id) != null) return true;
        if (!JobRunner.Near(w, djinn, j, to)) return JobRunner.Failed(j);
        NeedDef def = w.Content.NeedById(NeedId);
        int amount = def.Param("priceAmount");
        // The offer cites what makes the djinn restless now (it may have changed on the walk over).
        long why = n.LastEvent != 0 ? n.LastEvent : j.CauseEvent;
        SimEvent offer = w.Log.Emit(w, SystemTag.Social, "event.social.bargain_offered", new[] { djinn.Id, to.Id }, new[] { why }, null,
            EventArg.Char("a", djinn.Id), EventArg.Char("b", to.Id),
            EventArg.Key("service", ServiceKey(n.BondWork)), EventArg.Key("price", PriceKey(n.BondItem)));
        string? refusal = RefusalKey(w, to, djinn, n.BondItem, amount);
        if (refusal == null) Strike(w, djinn, to, n, def, amount, offer.Id);
        else Refuse(w, djinn, to, n, refusal, offer.Id);
        return true;
    }

    /// <summary>Null if they agree; otherwise why not: an unsafe bargain (Proud, and the camp could not pay the dawns of its reserve), or too little regard for the djinn.</summary>
    public static string? RefusalKey(World w, Character offeree, Character djinn, string item, int amount)
    {
        int reserve = w.Content.NeedById(NeedId).Param("reserveDays") * amount;
        if (Traits.Any(w, offeree, t => t.RefusesUnsafeBargain) && Stock.Count(w, item) < reserve) return "event.social.bargain_refused_unsafe";
        int required = -Traits.Sum(w, offeree, t => t.ConsentBias);
        return Relations.Opinion(w, offeree.Id, djinn.Id) >= required ? null : "event.social.bargain_refused";
    }

    /// <summary>
    /// Each hour, before food is shared out: a bargain whose party is gone lapses; at dawn the price is
    /// paid from the stockpile or the bargain breaks; at the term's end it is kept. A wronged djinn left
    /// unbound and burning lets the fire loose, once.
    /// </summary>
    public static void Hourly(World w)
    {
        if (w.Contracts.Count == 0 && !AnyDjinn(w)) return;
        NeedDef def = w.Content.NeedById(NeedId);
        foreach (Contract k in w.Contracts)
        {
            if (k.Status != ContractStatus.Open) continue;
            Character? djinn = w.Find(k.BoundId), patron = w.Find(k.PatronId);
            if (djinn is not { Status: CharStatus.Member } || patron is not { Status: CharStatus.Member })
            {
                k.Status = ContractStatus.Void;
                continue;
            }
            if (w.Tick >= k.NextDueTick && !Pay(w, k, djinn, patron, def)) continue;
            if (w.Tick >= k.EndsTick) Keep(w, k, djinn, patron);
        }
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member && NatureNeeds.Get(c, NeedId) is { } n) Trouble(w, c, n, def);
    }

    /// <summary>How much of an item open bargains want kept in store (a reserve of days), so the camp gathers it.</summary>
    public static int Demand(World w, string item)
    {
        int days = 0, n = 0;
        foreach (Contract k in w.Contracts)
            if (k.Status == ContractStatus.Open && k.PriceItem == item) n += k.PriceAmount;
        if (n > 0) days = w.Content.NeedById(NeedId).Param("reserveDays");
        return n * days;
    }

    /// <summary>
    /// How much of an item this person's own open bargains want kept in store. The bound party serves and
    /// the camp pays: a djinn does not gather its own price (it used to fell the trees for its own timber,
    /// and was the one who most often brought down a dryad's tree).
    /// </summary>
    public static int OwnDemand(World w, Character c, string item)
    {
        int n = 0;
        foreach (Contract k in w.Contracts)
            if (k.Status == ContractStatus.Open && k.BoundId == c.Id && k.PriceItem == item) n += k.PriceAmount;
        return n == 0 ? 0 : n * w.Content.NeedById(NeedId).Param("reserveDays");
    }

    /// <summary>The loc key of a service a bargain offers ("keep the night watch"); ContentDb checks one exists for every service a need can offer.</summary>
    public static string ServiceKey(string work) => "bargain.service." + work;

    /// <summary>The loc key of a bargain's price ("timber for the fire").</summary>
    public static string PriceKey(string item) => "bargain.price." + item;

    /// <summary>The bargain that wants this item gathered (the oldest open one), as a cause for the gathering; 0 if none.</summary>
    public static long DemandCause(World w, string item)
    {
        foreach (Contract k in w.Contracts)
            if (k.Status == ContractStatus.Open && k.PriceItem == item) return k.FormedEvent;
        return 0;
    }

    /// <summary>A djinn leans toward the work its bargain binds it to.</summary>
    public static int ServiceBias(World w, Character c, string workId) =>
        Binding(w, c.Id) is { } k && k.ServiceWork == workId ? w.Content.NeedById(NeedId).Param("serviceBias") : 0;

    private static void Strike(World w, Character djinn, Character patron, NatureNeed n, NeedDef def, int amount, long offerId)
    {
        var k = new Contract
        {
            Id = w.NewId(), BoundId = djinn.Id, PatronId = patron.Id, ServiceWork = n.BondWork, PriceItem = n.BondItem, PriceAmount = amount,
            FormedTick = w.Tick, NextDueTick = NextDawn(w), EndsTick = TermEnd(w, def), CauseEvent = offerId, Status = ContractStatus.Open,
        };
        w.Contracts.Add(k);
        SimEvent struck = w.Log.Emit(w, SystemTag.Social, "event.social.bargain_struck", new[] { djinn.Id, patron.Id }, new[] { offerId },
            new[] { new Effect { Kind = EffectKind.ContractFormed, A = djinn.Id, B = patron.Id, RefId = k.Id } },
            EventArg.Char("a", djinn.Id), EventArg.Char("b", patron.Id));
        k.FormedEvent = struck.Id;
    }

    private static void Refuse(World w, Character djinn, Character offeree, NatureNeed n, string key, long offerId)
    {
        SimEvent no = w.Log.Emit(w, SystemTag.Social, key, new[] { djinn.Id, offeree.Id }, new[] { offerId }, null,
            EventArg.Char("a", djinn.Id), EventArg.Char("b", offeree.Id));
        MoodSystem.AddThought(w, djinn, "bargain_refused", offeree.Id, no.Id);
        n.LastEvent = no.Id;
        if (Relations.Opinion(w, djinn.Id, offeree.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, djinn.Id, offeree.Id) == null)
            Relations.FormGrudge(w, djinn, offeree, w.Content.NeedById(NeedId).Param("refusedGrudgeIntensity"), no.Id);
    }

    private static bool Pay(World w, Contract k, Character djinn, Character patron, NeedDef def)
    {
        if (Stock.Count(w, k.PriceItem) >= k.PriceAmount)
        {
            Stock.Take(w, k.PriceItem, k.PriceAmount);
            w.Log.Emit(w, SystemTag.Social, "event.social.bargain_paid", new[] { djinn.Id, patron.Id }, new[] { k.FormedEvent }, null,
                EventArg.Char("a", djinn.Id), EventArg.Num("n", k.PriceAmount), EventArg.Item("item", k.PriceItem));
            k.NextDueTick += w.Content.World.TicksPerDay;
            return true;
        }
        // Nothing to pay at dawn. Winter is a cause only when it is food the frost took from the bushes.
        long winter = w.Content.ItemById(k.PriceItem).Nutrition > 0 ? w.WinterEvent : 0;
        k.Status = ContractStatus.Broken;
        SimEvent broken = w.Log.Emit(w, SystemTag.Social, "event.social.bargain_broken", new[] { djinn.Id, patron.Id }, new[] { k.FormedEvent, winter },
            new[] { new Effect { Kind = EffectKind.ContractBroken, A = djinn.Id, B = patron.Id, RefId = k.Id } },
            EventArg.Char("a", djinn.Id), EventArg.Char("b", patron.Id), EventArg.Item("item", k.PriceItem));
        k.ClosedEvent = broken.Id;
        MoodSystem.AddThought(w, djinn, "bargain_betrayed", patron.Id, broken.Id);
        if (NatureNeeds.Get(djinn, NeedId) is { } n) NatureNeeds.Adjust(w, djinn, n, -def.Param("betrayalLoss"), broken.Id);
        Relations.FormGrudge(w, djinn, patron, def.Param("grudgeIntensity"), broken.Id);
        Courtesy.WordBroken(w, patron.Id, broken, oath: false);
        return false;
    }

    private static void Keep(World w, Contract k, Character djinn, Character patron)
    {
        k.Status = ContractStatus.Kept;
        SimEvent kept = w.Log.Emit(w, SystemTag.Social, "event.social.bargain_kept", new[] { djinn.Id, patron.Id }, new[] { k.FormedEvent },
            new[] { new Effect { Kind = EffectKind.ContractKept, A = djinn.Id, B = patron.Id, RefId = k.Id } },
            EventArg.Char("a", djinn.Id), EventArg.Char("b", patron.Id));
        k.ClosedEvent = kept.Id;
        MoodSystem.AddThought(w, djinn, "bargain_honoured", patron.Id, kept.Id);
        MoodSystem.AddThought(w, patron, "bargain_honoured", djinn.Id, kept.Id);
        // Unbound again from here: its next restlessness follows from this bargain's end.
        if (NatureNeeds.Get(djinn, NeedId) is { } n) n.LastEvent = kept.Id;
        Courtesy.WordKept(w, 0, true, kept);
    }

    /// <summary>Burning, unbound and wronged (it holds a grudge), the djinn lets the fire loose once, burning timber in the stockpile; the camp holds it against them.</summary>
    private static void Trouble(World w, Character djinn, NatureNeed n, NeedDef def)
    {
        if (n.Value >= def.CriticalBelow || n.ActedTick != 0 || Binding(w, djinn.Id) != null) return;
        Grudge? wrong = null;
        foreach (Grudge g in w.Grudges)
            if (g.Active && g.HolderId == djinn.Id) { wrong = g; break; }
        if (wrong == null) return;
        n.ActedTick = w.Tick;
        int burned = Stock.Take(w, "timber", def.Param("troubleTimber"));
        SimEvent fire = w.Log.Emit(w, SystemTag.Social, "event.social.djinn_trouble", new[] { djinn.Id }, new[] { wrong.FormedEvent, n.LastEvent }, null,
            EventArg.Char("a", djinn.Id), EventArg.Num("n", burned), EventArg.Item("item", "timber"));
        n.LastEvent = fire.Id;
        foreach (Character m in w.Characters)
        {
            if (m.Status != CharStatus.Member || m.Id == djinn.Id) continue;
            MoodSystem.AddThought(w, m, "fire_loosed", djinn.Id, fire.Id);
            if (Relations.Opinion(w, m.Id, djinn.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, m.Id, djinn.Id) == null)
                Relations.FormGrudge(w, m, djinn, def.Param("fireGrudgeIntensity"), fire.Id);
        }
    }

    private static bool RecentlyRefused(World w, Character djinn)
    {
        ThoughtDef def = w.Content.ThoughtById("bargain_refused");
        long window = (long)(def.DurationHours - w.Content.NeedById(NeedId).Param("offerCooldownHours")) * w.Content.World.TicksPerHour;
        foreach (Thought t in djinn.Thoughts)
            if (t.DefId == "bargain_refused" && t.ExpiresTick - w.Tick > window) return true;
        return false;
    }

    /// <summary>The one the djinn thinks best of, among those awake who have not just turned it down and whom it holds no grudge against; the player breaks a tie.</summary>
    private static Character? Offeree(World w, Character djinn)
    {
        Character? best = null;
        int bestOpinion = int.MinValue;
        foreach (Character m in w.Characters)
        {
            if (m.Status != CharStatus.Member || m.Id == djinn.Id || m.Downed || m.Asleep || NatureNeeds.Get(m, NeedId) != null) continue;
            if (Refused(djinn, m.Id) || Relations.ActiveGrudge(w, djinn.Id, m.Id) != null) continue;
            int op = Relations.Opinion(w, djinn.Id, m.Id);
            if (op > bestOpinion || (op == bestOpinion && m.Id == w.PlayerId))
            {
                best = m;
                bestOpinion = op;
            }
        }
        return best;
    }

    private static bool Refused(Character djinn, int otherId)
    {
        foreach (Thought t in djinn.Thoughts)
            if (t.DefId == "bargain_refused" && t.AboutId == otherId) return true;
        return false;
    }

    private static bool AnyDjinn(World w)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member && c.NatureNeeds.Count > 0 && NatureNeeds.Get(c, NeedId) != null) return true;
        return false;
    }

    private static long NextDawn(World w)
    {
        WorldDef def = w.Content.World;
        long dawn = w.Tick / def.TicksPerDay * def.TicksPerDay + (long)def.DawnHour * def.TicksPerHour;
        return dawn <= w.Tick ? dawn + def.TicksPerDay : dawn;
    }

    /// <summary>The bargain runs until the season turns, or the turn after if that is too soon.</summary>
    private static long TermEnd(World w, NeedDef def)
    {
        WorldDef wd = w.Content.World;
        long end = (w.Tick / wd.TicksPerSeason + 1) * wd.TicksPerSeason;
        if (end - w.Tick < (long)def.Param("minTermDays") * wd.TicksPerDay) end += wd.TicksPerSeason;
        return end;
    }
}
