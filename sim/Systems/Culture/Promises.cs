using Return.Sim.Events;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// How a people weighs a promise (ADR-0023). A broken or kept promise weighs 50% to 150% by the
/// creditor's Oathbound/Free axis (O1). A guest whose people keep guest-right, wronged in their
/// first days, holds it heavier still (H2). And among a people who hold a promise heavy, word of a
/// broken one spreads to their kin (O2).
/// </summary>
public static class Promises
{
    /// <summary>The creditor's thought and grudge after <paramref name="broken"/>; then word spreads. Called by Relations.Break.</summary>
    public static void Broken(World w, Debt d, SimEvent broken, Character creditor, Character debtor)
    {
        int pct = Ways.OathWeightPct(w, creditor);
        if (Ways.Keeps(w, creditor, "guest_right") is { } guest && Greetings.Newcomer(w, creditor))
        {
            SimEvent g = w.Log.Emit(w, SystemTag.Culture, guest.Def.EventKey("wronged"), new[] { debtor.Id, creditor.Id }, new[] { broken.Id }, null,
                EventArg.Char("a", debtor.Id), EventArg.Char("b", creditor.Id), EventArg.Key("ways", guest.HolderNameKey));
            MoodSystem.AddThought(w, creditor, "guest_right_broken", debtor.Id, g.Id, pct);
            Relations.FormGrudge(w, creditor, debtor, BrokenGrudge * guest.Def.Arg("wrongedGrudgePct") / 100 * pct / 100, g.Id);
            Omens.RecordDeed(w, DeedKind.GuestWronged, g.Id, debtor.Id);
        }
        else
        {
            MoodSystem.AddThought(w, creditor, "promise_broken", debtor.Id, broken.Id, pct);
            Relations.FormGrudge(w, creditor, debtor, BrokenGrudge * pct / 100, broken.Id);
        }
        WordSpreads(w, creditor, debtor, broken);
    }

    /// <summary>The grudge a broken promise leaves at full weight (as before culture).</summary>
    public const int BrokenGrudge = 40;

    /// <summary>
    /// Among a people who hold a promise heavy, everyone else of the creditor's culture in camp hears
    /// of it; anyone it pushes past the grudge line now holds one (O2).
    /// </summary>
    private static void WordSpreads(World w, Character creditor, Character debtor, SimEvent broken)
    {
        if (creditor.CultureId.Length == 0) return;
        if (w.Content.CultureById(creditor.CultureId).Ethics.OathboundFree < CultureTuning.WordSpreadsFromAxis) return;
        var listeners = new List<Character>();
        foreach (Character m in w.Members())
            if (m.Id != creditor.Id && m.Id != debtor.Id && m.CultureId == creditor.CultureId) listeners.Add(m);
        if (listeners.Count == 0) return;
        var actors = new List<int> { debtor.Id };
        foreach (Character l in listeners) actors.Add(l.Id);
        SeededCulture culture = Ways.Culture(w, creditor)!;
        SimEvent e = w.Log.Emit(w, SystemTag.Culture, "event.culture.word_spread", actors, new[] { broken.Id }, null,
            EventArg.Char("a", debtor.Id), EventArg.Key("ways", culture.NameKey));
        foreach (Character l in listeners)
        {
            MoodSystem.AddThought(w, l, "heard_oath_broken", debtor.Id, e.Id);
            if (Relations.Opinion(w, l.Id, debtor.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, l.Id, debtor.Id) == null)
                Relations.FormGrudge(w, l, debtor, CultureTuning.HearsayGrudge, e.Id);
        }
    }
}
