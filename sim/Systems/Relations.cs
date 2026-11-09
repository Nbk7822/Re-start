using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// The relationship graph: directed opinions plus first-class grudges and debts. Forming, repaying
/// or breaking one emits a Social event carrying the matching persistent effect, which is what lets
/// the story analyzer end a chain there.
/// </summary>
public static class Relations
{
    /// <summary>
    /// How well two people get on by nature, from -2 (they grate on each other) to +2 (they click).
    /// A Truth keyed by the pair's cast keys, so it is symmetric, order-independent and survives rewinds.
    /// Weights: -2 10%, -1 20%, 0 40%, +1 20%, +2 10%.
    /// </summary>
    public static int Chemistry(World w, Character a, Character b)
    {
        ulong ka = NatureKey(a), kb = NatureKey(b);
        ulong pair = ka < kb ? Mix64.Combine(ka, kb) : Mix64.Combine(kb, ka);
        int roll = w.Rng.Truths.Range(TruthId.PairChemistry, pair, 0, 0, 100);
        return roll < 10 ? -2 : roll < 30 ? -1 : roll < 70 ? 0 : roll < 90 ? 1 : 2;
    }

    private static ulong NatureKey(Character c) => c.CastKey.Length > 0 ? Fnv1a.Hash64(c.CastKey) : (ulong)c.Id;

    public static int Opinion(World w, int fromId, int toId)
    {
        Character? from = w.Find(fromId);
        if (from == null) return 0;
        foreach (Opinion o in from.Opinions)
            if (o.TargetId == toId) return o.Value;
        return 0;
    }

    /// <summary>Changes from's opinion of toId by delta (clamped to −100..100), keeping the list ordered by target id.</summary>
    public static void ChangeOpinion(World w, Character from, int toId, int delta, long cause)
    {
        if (toId == from.Id || delta == 0) return;
        List<Opinion> list = from.Opinions;
        int i = 0;
        while (i < list.Count && list[i].TargetId < toId) i++;
        if (i < list.Count && list[i].TargetId == toId)
            list[i].Value = Math.Clamp(list[i].Value + delta, -100, 100);
        else
            list.Insert(i, new Opinion { TargetId = toId, Value = Math.Clamp(delta, -100, 100) });
    }

    public static Grudge? ActiveGrudge(World w, int holderId, int targetId)
    {
        foreach (Grudge g in w.Grudges)
            if (g.Active && g.HolderId == holderId && g.TargetId == targetId) return g;
        return null;
    }

    /// <summary>Forms a grudge, or deepens an existing one without a new persistent effect.</summary>
    public static Grudge FormGrudge(World w, Character holder, Character target, int intensity, long cause)
    {
        Grudge? existing = ActiveGrudge(w, holder.Id, target.Id);
        if (existing != null)
        {
            existing.Intensity = Math.Min(100, existing.Intensity + intensity);
            return existing;
        }
        var g = new Grudge { Id = w.NewId(), HolderId = holder.Id, TargetId = target.Id, Intensity = intensity, CauseEvent = cause, FormedTick = w.Tick };
        w.Grudges.Add(g);
        SimEvent e = w.Log.Emit(w, SystemTag.Social, "event.social.grudge_formed", new[] { holder.Id, target.Id }, new[] { cause },
            new[] { new Effect { Kind = EffectKind.GrudgeFormed, A = holder.Id, B = target.Id, RefId = g.Id } },
            EventArg.Char("a", holder.Id), EventArg.Char("b", target.Id));
        g.FormedEvent = e.Id;
        return g;
    }

    /// <summary>Someone who leaves or dies takes their grudges with them; nobody sees them let go, so no event.</summary>
    public static void DropGrudgesHeldBy(World w, Character c)
    {
        foreach (Grudge g in w.Grudges)
            if (g.HolderId == c.Id) g.Active = false;
    }

    public static void EndGrudge(World w, Grudge g)
    {
        if (!g.Active) return;
        g.Active = false;
        w.Log.Emit(w, SystemTag.Social, "event.social.grudge_ended", new[] { g.HolderId, g.TargetId }, new[] { g.FormedEvent },
            new[] { new Effect { Kind = EffectKind.GrudgeEnded, A = g.HolderId, B = g.TargetId, RefId = g.Id } },
            EventArg.Char("a", g.HolderId), EventArg.Char("b", g.TargetId));
    }

    /// <summary>Forms a debt. Promises are debts with a due tick: the debtor promised the creditor something.</summary>
    public static Debt FormDebt(World w, Character debtor, Character creditor, DebtKind kind, int weight, long cause, long dueTick = 0)
    {
        var d = new Debt
        {
            Id = w.NewId(), DebtorId = debtor.Id, CreditorId = creditor.Id, Kind = kind, Weight = weight,
            CauseEvent = cause, FormedTick = w.Tick, DueTick = dueTick, Status = DebtStatus.Open,
        };
        w.Debts.Add(d);
        string key = kind switch
        {
            DebtKind.PromiseMeal => "event.social.promised_meal",
            DebtKind.PromiseRest => "event.social.promised_rest",
            DebtKind.Blood => "event.social.owes_blood",
            DebtKind.Rescue => "event.social.owes_rescue",
            DebtKind.Meal => "event.social.owes_meal",
            _ => "event.social.owes_care",
        };
        SimEvent e = w.Log.Emit(w, SystemTag.Social, key, new[] { debtor.Id, creditor.Id }, new[] { cause },
            new[] { new Effect { Kind = EffectKind.DebtFormed, A = debtor.Id, B = creditor.Id, RefId = d.Id } },
            EventArg.Char("a", debtor.Id), EventArg.Char("b", creditor.Id));
        d.FormedEvent = e.Id;
        return d;
    }

    public static SimEvent Repay(World w, Debt d, long cause)
    {
        d.Status = DebtStatus.Repaid;
        string key = d.IsPromise ? "event.social.promise_kept" : "event.social.debt_repaid";
        SimEvent e = w.Log.Emit(w, SystemTag.Social, key, new[] { d.DebtorId, d.CreditorId }, new[] { cause, d.FormedEvent },
            new[] { new Effect { Kind = EffectKind.DebtRepaid, A = d.DebtorId, B = d.CreditorId, RefId = d.Id } },
            EventArg.Char("a", d.DebtorId), EventArg.Char("b", d.CreditorId));
        d.ClosedEvent = e.Id;
        Character? creditor = w.Find(d.CreditorId);
        if (d.IsPromise && creditor != null && creditor.Status == CharStatus.Member)
            MoodSystem.AddThought(w, creditor, "promise_kept", d.DebtorId, e.Id);
        Culture.Omens.RecordDeed(w, d.IsPromise ? DeedKind.OathKept : DeedKind.DebtRepaid, e.Id, d.DebtorId);
        return e;
    }

    public static SimEvent Break(World w, Debt d, long cause)
    {
        d.Status = DebtStatus.Broken;
        SimEvent e = w.Log.Emit(w, SystemTag.Social, "event.social.promise_broken", new[] { d.DebtorId, d.CreditorId }, new[] { d.FormedEvent, cause },
            new[] { new Effect { Kind = EffectKind.DebtBroken, A = d.DebtorId, B = d.CreditorId, RefId = d.Id } },
            EventArg.Char("a", d.DebtorId), EventArg.Char("b", d.CreditorId));
        d.ClosedEvent = e.Id;
        Character? creditor = w.Find(d.CreditorId), debtor = w.Find(d.DebtorId);
        if (creditor != null && debtor != null && creditor.Status == CharStatus.Member)
        {
            MoodSystem.AddThought(w, creditor, "promise_broken", debtor.Id, e.Id);
            FormGrudge(w, creditor, debtor, 40, e.Id);
        }
        Culture.Omens.RecordDeed(w, DeedKind.OathBroken, e.Id, d.DebtorId);
        return e;
    }

    /// <summary>
    /// Records that a and b have spoken. True the first time (each now lists the other, in ascending
    /// order), false if they had met before. Opinions cannot stand in for this: a chat warm enough to
    /// count can still move opinion by 0, which leaves no opinion entry (ADR-0023).
    /// </summary>
    public static bool Meet(World w, Character a, Character b)
    {
        int at = a.Met.BinarySearch(b.Id);
        if (at >= 0) return false;
        a.Met.Insert(~at, b.Id);
        int bt = b.Met.BinarySearch(a.Id);
        if (bt < 0) b.Met.Insert(~bt, a.Id);
        return true;
    }

    /// <summary>The oldest open debt the debtor owes the creditor (optionally of one kind), or null.</summary>
    public static Debt? OpenDebt(World w, int debtorId, int creditorId, DebtKind? kind = null)
    {
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && d.DebtorId == debtorId && d.CreditorId == creditorId && (kind == null || d.Kind == kind))
                return d;
        return null;
    }
}
