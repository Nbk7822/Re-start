using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// The fae (ADR-0022). The Good Folk of Celtic and wider European folklore cannot lie, are bound by
/// their word, and are owed courtesy; slights against them are remembered long. Here a fae's need is
/// courtesy: kept words and good manners around them. Any broken promise or bargain in the camp stings
/// them like cold iron (a wound to the need, a little health, and their regard for whoever broke it);
/// kept words, talk and kindness restore it; insults cut it. Because they cannot lie, a fae never sneers:
/// where another would insult, a fae says the hard truth aloud, and anyone near hears what the listener
/// did. Their own word binds them first: whatever a fae owes (a welcome, a tending) they repay before
/// their own hunger or rest, the moment their creditor needs it.
/// </summary>
public static class Courtesy
{
    public const string NeedId = "courtesy";

    private static NeedDef Def(World w) => w.Content.NeedById(NeedId);

    /// <summary>
    /// A promise or a bargain was broken (by <paramref name="breaker"/>, if it was someone's): every fae in
    /// camp is stung. A broken promise is someone's own word, and the fae hold it against them; a bargain
    /// the camp could not pay stings as much but is held against nobody in particular.
    /// </summary>
    public static void WordBroken(World w, int breaker, SimEvent broken) => WordBroken(w, breaker, broken, oath: true);

    public static void WordBroken(World w, int breaker, SimEvent broken, bool oath)
    {
        if (!AnyFae(w)) return;
        NeedDef def = Def(w);
        foreach (Character fae in w.Characters)
        {
            if (fae.Status != CharStatus.Member || NatureNeeds.Get(fae, NeedId) is not { } n) continue;
            Character? culprit = w.Find(breaker);
            SimEvent sting = w.Log.Emit(w, SystemTag.Social, "event.social.word_wound", culprit == null ? new[] { fae.Id } : new[] { fae.Id, culprit.Id },
                new[] { broken.Id }, null, EventArg.Char("a", fae.Id));
            NatureNeeds.Adjust(w, fae, n, -def.Param("brokenWordLoss"), sting.Id);
            MoodSystem.AddThought(w, fae, "word_wound", culprit != null && culprit.Id != fae.Id ? culprit.Id : 0, sting.Id);
            // A sting, not a wound: it costs a little health but never fells them by itself.
            fae.Health = Math.Max(1, fae.Health - def.Param("brokenWordHurt"));
            fae.LastHealthEvent = sting.Id;
            // The Good Folk hold an oath-breaker to account, whoever the oath was sworn to.
            if (oath && fae.Status == CharStatus.Member && culprit != null && culprit.Id != fae.Id && culprit.Status == CharStatus.Member)
                Relations.FormGrudge(w, fae, culprit, def.Param("grudgeIntensity"), sting.Id);
        }
    }

    /// <summary>A debt was repaid or a promise or bargain kept: a kept promise heartens every fae; a fae who repaid what they owed has kept their word.</summary>
    public static void WordKept(World w, int debtor, bool promise, SimEvent kept)
    {
        if (!AnyFae(w)) return;
        NeedDef def = Def(w);
        foreach (Character fae in w.Characters)
        {
            if (fae.Status != CharStatus.Member || NatureNeeds.Get(fae, NeedId) is not { } n) continue;
            if (fae.Id == debtor) MoodSystem.AddThought(w, fae, "kept_my_word", 0, kept.Id);
            else if (promise) MoodSystem.AddThought(w, fae, "words_kept", 0, kept.Id);
            else continue;
            NatureNeeds.Adjust(w, fae, n, def.Param("keptWordGain"), kept.Id);
        }
    }

    /// <summary>Talk with a fae (a chat, or a real conversation) is courtesy shown them.</summary>
    public static void Courteous(World w, Character c, bool deep)
    {
        if (NatureNeeds.Get(c, NeedId) is not { } n) return;
        NatureNeeds.Adjust(w, c, n, Def(w).Param(deep ? "deepTalkGain" : "chatGain"), 0);
    }

    /// <summary>An insult to a fae is a slight, and cuts their courtesy.</summary>
    public static void Slighted(World w, Character fae, SimEvent insult)
    {
        if (NatureNeeds.Get(fae, NeedId) is not { } n) return;
        NatureNeeds.Adjust(w, fae, n, -Def(w).Param("slightLoss"), insult.Id);
    }

    /// <summary>
    /// What a fae says where another would insult: the hard truth. With a wrong to name (a grudge, or the
    /// worst thing they hold against the listener) they say it plainly before everyone, and whoever is near
    /// hears what the listener did; saying it eases the grudge. With none, they say plainly what they think.
    /// </summary>
    public static SimEvent HardTruth(World w, Character fae, Character target)
    {
        NeedDef def = Def(w);
        Grudge? grudge = Relations.ActiveGrudge(w, fae.Id, target.Id);
        long wrong = grudge?.FormedEvent ?? WorstWrong(w, fae, target);
        long mood = wrong == 0 && fae.Mood < SocialSystem.LowMoodBelow ? WorstThought(w, fae) : 0;
        SimEvent e = w.Log.Emit(w, SystemTag.Social, wrong != 0 ? "event.social.hard_truth" : "event.social.plain_truth",
            new[] { fae.Id, target.Id }, new[] { wrong != 0 ? wrong : mood }, null, EventArg.Char("a", fae.Id), EventArg.Char("b", target.Id));
        MoodSystem.AddThought(w, target, "told_hard_truth", fae.Id, e.Id);
        MoodSystem.AddThought(w, fae, "spoke_truth", 0, e.Id);
        if (NatureNeeds.Get(target, NeedId) is { } slighted) NatureNeeds.Adjust(w, target, slighted, -def.Param("slightLoss") / 2, e.Id);
        if (wrong == 0) return e;
        if (grudge != null) grudge.Intensity = Math.Max(1, grudge.Intensity - def.Param("truthEasesGrudge"));
        foreach (Character m in w.Characters)
        {
            if (m.Status != CharStatus.Member || m.Id == fae.Id || m.Id == target.Id || m.Asleep || m.Downed) continue;
            if (m.Pos.ChebyshevTo(fae.Pos) > SocialSystem.NeighbourRange) continue;
            MoodSystem.AddThought(w, m, "heard_hard_truth", target.Id, e.Id);
            if (Relations.Opinion(w, m.Id, target.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, m.Id, target.Id) == null)
                Relations.FormGrudge(w, m, target, def.Param("witnessGrudgeIntensity"), e.Id);
        }
        return e;
    }

    /// <summary>
    /// A fae's word comes before anything else: for the oldest debt they owe someone who needs help now,
    /// carry them to safety, tend them, or bring them a meal (from the fae's own share if need be).
    /// </summary>
    public static Job? KeepWordJob(World w, Character fae, bool sunBlocked)
    {
        foreach (Debt d in w.Debts)
        {
            if (d.Status != DebtStatus.Open || d.DebtorId != fae.Id || d.Kind == DebtKind.PromiseRest) continue;
            Character? creditor = w.Find(d.CreditorId);
            if (creditor is not { Status: CharStatus.Member } || (sunBlocked && !w.Map.Covered(creditor.Pos))) continue;
            if (!d.IsPromise && creditor.Downed && !OnBed(w, creditor.Pos) && !Places.Targeted(w, JobKind.Rescue, creditor.Id, fae.Id))
                return new Job { Kind = JobKind.Rescue, TargetId = creditor.Id, Target = creditor.Pos, CauseEvent = d.FormedEvent };
            if (!d.IsPromise && (creditor.Downed || creditor.Health < JobPicker.InjuredBelow) && fae.Health >= JobPicker.InjuredBelow
                && !Places.Targeted(w, JobKind.Care, creditor.Id, fae.Id))
                return new Job { Kind = JobKind.Care, TargetId = creditor.Id, Target = creditor.Pos, CauseEvent = d.FormedEvent };
            if (creditor.Food < NeedsSystem.HungryBelow && Places.Species(w, creditor).EatsItem && !Rations.HasReservation(w, creditor.Id)
                && (Rations.HasReservation(w, fae.Id) || Rations.Unreserved(w, "berries") > 0) && !Places.Targeted(w, JobKind.KeepPromise, d.Id, fae.Id))
            {
                Point food = Places.FoodTile(w, "berries");
                if (food != Point.None) return new Job { Kind = JobKind.KeepPromise, TargetId = d.Id, Target = food, CauseEvent = d.FormedEvent };
            }
        }
        return null;
    }

    /// <summary>
    /// A fae welcomed into the camp binds themself to repay the welcome to whoever gave it. As with a
    /// promise on joining (ADR-0014), the joining event carries the debt, so being welcomed is not yet a
    /// story; the oath line that follows is what a repayment cites.
    /// </summary>
    public static void SwearWelcome(World w, Character fae, Character host, SimEvent joined)
    {
        if (!Places.Species(w, fae).CannotLie) return;
        var d = new Debt
        {
            Id = w.NewId(), DebtorId = fae.Id, CreditorId = host.Id, Kind = DebtKind.Welcome, Weight = 1,
            CauseEvent = joined.Id, FormedTick = w.Tick, Status = DebtStatus.Open,
        };
        w.Debts.Add(d);
        joined.Effects.Add(new Effect { Kind = EffectKind.DebtFormed, A = fae.Id, B = host.Id, RefId = d.Id });
        d.FormedEvent = w.Log.Emit(w, SystemTag.Social, "event.social.swore_welcome", new[] { fae.Id, host.Id }, new[] { joined.Id }, null,
            EventArg.Char("a", fae.Id), EventArg.Char("b", host.Id)).Id;
    }

    /// <summary>Does this person's nature bind their word (the fae)? Then what they owe they have sworn to repay.</summary>
    public static bool Sworn(World w, int id) => w.Find(id) is { } c && Places.Species(w, c).CannotLie;

    private static long WorstWrong(World w, Character fae, Character target)
    {
        int worst = 0;
        long cause = 0;
        foreach (Thought t in fae.Thoughts)
        {
            if (t.AboutId != target.Id || t.CauseEvent == 0) continue;
            int opinion = w.Content.ThoughtById(t.DefId).Opinion;
            if (opinion < worst) { worst = opinion; cause = t.CauseEvent; }
        }
        return cause;
    }

    private static long WorstThought(World w, Character c)
    {
        int worst = 0;
        long cause = 0;
        foreach (Thought t in c.Thoughts)
        {
            int mood = w.Content.ThoughtById(t.DefId).Mood;
            if (mood < worst && t.CauseEvent != 0) { worst = mood; cause = t.CauseEvent; }
        }
        return cause;
    }

    private static bool OnBed(World w, Point p)
    {
        foreach (Bed b in w.Map.Beds)
            if (b.Pos == p) return true;
        return false;
    }

    private static bool AnyFae(World w)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member && c.NatureNeeds.Count > 0 && NatureNeeds.Get(c, NeedId) != null) return true;
        return false;
    }
}
