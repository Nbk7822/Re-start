using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems.Culture;

/// <summary>The four ethics axes of fixed decision 5.</summary>
public enum EthicAxis
{
    MortalImmortal,
    ArcaneMundane,
    WildCivil,
    OathboundFree,
}

/// <summary>A custom someone keeps, and whose it is (the culture or faith that holds it).</summary>
public readonly record struct KeptCustom(CustomDef Def, string HolderId, bool HolderIsFaith, string HolderNameKey);

/// <summary>
/// Reads of a person's ways: their culture and faith, the customs they keep, their ethics axes,
/// how their people regard other peoples, and what that does to blood consent. Pure queries over
/// state and content; nothing here draws randomness or changes the world, except <see cref="Assign"/>,
/// which draws only Truths (ADR-0023).
/// </summary>
public static class Ways
{
    public static SeededCulture? CultureById(World w, string id)
    {
        if (id.Length == 0) return null;
        foreach (SeededCulture s in w.Culture.Cultures)
            if (s.TemplateId == id) return s;
        return null;
    }

    public static SeededFaith? FaithById(World w, string id)
    {
        if (id.Length == 0) return null;
        foreach (SeededFaith s in w.Culture.Faiths)
            if (s.TemplateId == id) return s;
        return null;
    }

    public static SeededCulture? Culture(World w, Character c) => CultureById(w, c.CultureId);

    public static SeededFaith? Faith(World w, Character c) => FaithById(w, c.FaithId);

    /// <summary>Faith customs bind only the faithful and the devout.</summary>
    public static bool FaithBinds(Character c) => c.FaithId.Length > 0 && c.Devotion >= CultureTuning.FaithCustomDevotion;

    /// <summary>The customs c keeps: their culture's (habits, so every member keeps them), then their faith's if their devotion binds them; each custom once.</summary>
    public static List<KeptCustom> KeptCustoms(World w, Character c)
    {
        var kept = new List<KeptCustom>();
        SeededCulture? culture = Culture(w, c);
        if (culture != null)
            foreach (string id in culture.CustomIds)
                kept.Add(new KeptCustom(w.Content.CustomById(id), culture.TemplateId, false, culture.NameKey));
        SeededFaith? faith = FaithBinds(c) ? Faith(w, c) : null;
        if (faith != null)
            foreach (string id in faith.CustomIds)
            {
                bool dup = false;
                foreach (KeptCustom k in kept)
                    if (k.Def.Id == id) { dup = true; break; }
                if (!dup) kept.Add(new KeptCustom(w.Content.CustomById(id), faith.TemplateId, true, faith.NameKey));
            }
        return kept;
    }

    /// <summary>The first custom of this kind c keeps (culture before faith), or null.</summary>
    public static KeptCustom? Keeps(World w, Character c, string kind)
    {
        foreach (KeptCustom k in KeptCustoms(w, c))
            if (k.Def.Kind == kind) return k;
        return null;
    }

    /// <summary>Does c keep this festival or sacred custom as held by this culture or faith? Members of the culture do; followers of the faith do if their devotion binds them.</summary>
    public static bool IsKeeper(Character c, string holderId, bool holderIsFaith) =>
        holderIsFaith ? c.FaithId == holderId && FaithBinds(c) : c.CultureId == holderId && holderId.Length > 0;

    /// <summary>c's value on an axis: culture + faith × devotion / 100, clamped to −2..+2. The otherworlder is 0 on every axis.</summary>
    public static int Axis(World w, Character c, EthicAxis axis)
    {
        int culture = c.CultureId.Length > 0 ? AxisOf(w.Content.CultureById(c.CultureId).Ethics, axis) : 0;
        int faith = c.FaithId.Length > 0 ? AxisOf(w.Content.FaithById(c.FaithId).Ethics, axis) : 0;
        return Math.Clamp(culture + faith * c.Devotion / 100, -2, 2);
    }

    public static int AxisOf(EthicsDef e, EthicAxis axis) => axis switch
    {
        EthicAxis.MortalImmortal => e.MortalImmortal,
        EthicAxis.ArcaneMundane => e.ArcaneMundane,
        EthicAxis.WildCivil => e.WildCivil,
        _ => e.OathboundFree,
    };

    /// <summary>How heavily c weighs a promise made to them, 50..150%: the Reed Folk shrug off a broken promise, the Hearthfolk do not (O1).</summary>
    public static int OathWeightPct(World w, Character c) => 100 + CultureTuning.OathWeightPerAxisPct * Axis(w, c, EthicAxis.OathboundFree);

    /// <summary>How c's people regard other's people: revere, wary, fear, scorn, or null for no stance.</summary>
    public static string? Stance(World w, Character c, Character other)
    {
        if (c.CultureId.Length == 0) return null;
        foreach (AttitudeDef a in w.Content.CultureById(c.CultureId).Attitudes)
            if (a.Species == other.SpeciesId) return a.Stance;
        return null;
    }

    public static int StanceWarmth(World w, Character c, Character other) => CultureTuning.Stance(Stance(w, c, other)).Warmth;

    public static int StanceInsult(World w, Character c, Character other) => CultureTuning.Stance(Stance(w, c, other)).Insult;

    public static int StanceConsent(World w, Character c, Character other) => CultureTuning.Stance(Stance(w, c, other)).Consent;

    /// <summary>The summed consent bias of the blood customs c keeps (a taboo is negative, a gift positive).</summary>
    public static int BloodBias(World w, Character c)
    {
        int bias = 0;
        foreach (KeptCustom k in KeptCustoms(w, c))
            if (k.Def.Kind == "blood_stance") bias += k.Def.Arg("consentBias");
        return bias;
    }

    /// <summary>The blood custom that moves c to offer blood unasked: one that offers, when c's blood customs lean toward giving overall; else null.</summary>
    public static KeptCustom? BloodOffer(World w, Character c)
    {
        if (BloodBias(w, c) <= 0) return null;
        foreach (KeptCustom k in KeptCustoms(w, c))
            if (k.Def.Kind == "blood_stance" && k.Def.Arg("offers") == 1) return k;
        return null;
    }

    /// <summary>The first blood custom c keeps that forbids giving, or null.</summary>
    public static KeptCustom? BloodTaboo(World w, Character c)
    {
        foreach (KeptCustom k in KeptCustoms(w, c))
            if (k.Def.Kind == "blood_stance" && k.Def.Arg("consentBias") < 0) return k;
        return null;
    }

    public static string DevotionKey(int devotion) =>
        devotion < CultureTuning.LapsedBelow ? "devotion.lapsed" : devotion < CultureTuning.DevoutFrom ? "devotion.faithful" : "devotion.devout";

    /// <summary>A god's temperament as a word: stern (wrathful), kindly (gracious), fickle (both) or distant (neither).</summary>
    public static string TemperamentKey(SeededGod g) => (g.Wrath >= 50, g.Grace >= 50) switch
    {
        (true, false) => "temperament.stern",
        (false, true) => "temperament.kindly",
        (true, true) => "temperament.fickle",
        _ => "temperament.distant",
    };

    /// <summary>
    /// Gives someone their culture, faith and devotion (Truths keyed by cast key, or by id when there
    /// is none), so it survives rewinds. A people no culture names still gets one: through the "*"
    /// weights, or evenly when every weight is 0. The otherworlder belongs nowhere.
    /// </summary>
    public static void Assign(World w, Character c)
    {
        if (c.Otherworlder || w.Culture.Cultures.Count == 0)
        {
            c.CultureId = "";
            c.FaithId = "";
            c.Devotion = 0;
            return;
        }
        ContentDb db = w.Content;
        TruthRng t = w.Rng.Truths;
        ulong pk = c.CastKey.Length > 0 ? Fnv1a.Hash64(c.CastKey) : (ulong)c.Id;

        var weights = new List<int>(w.Culture.Cultures.Count);
        int total = 0;
        foreach (SeededCulture s in w.Culture.Cultures)
        {
            int weight = 0, any = 0;
            bool named = false;
            foreach (PeopleWeight p in db.CultureById(s.TemplateId).Peoples)
            {
                if (p.Species == c.SpeciesId) { weight = p.Weight; named = true; }
                else if (p.Species == "*") any = p.Weight;
            }
            int wgt = named ? weight : any;
            weights.Add(wgt);
            total += wgt;
        }
        if (total == 0)
            for (int i = 0; i < weights.Count; i++) weights[i] = 1;
        SeededCulture culture = w.Culture.Cultures[WeightedIndex(t, TruthId.PersonBelonging, pk, 0, weights)];
        CultureDef def = db.CultureById(culture.TemplateId);
        c.CultureId = culture.TemplateId;

        c.FaithId = culture.FaithId;
        if (w.Culture.Faiths.Count > 1 && t.Range(TruthId.PersonBelonging, pk, 1, 0, 1000) < def.OtherFaithPermille)
        {
            var others = new List<string>();
            foreach (SeededFaith f in w.Culture.Faiths)
                if (f.TemplateId != culture.FaithId) others.Add(f.TemplateId);
            c.FaithId = others[t.Range(TruthId.PersonBelonging, pk, 2, 0, others.Count)];
        }
        c.Devotion = t.Range(TruthId.PersonBelonging, pk, 3, def.Devotion.Min, def.Devotion.Max + 1);
    }

    /// <summary>One Truth draw over the total weight; weights must sum above 0.</summary>
    public static int WeightedIndex(TruthRng t, TruthId id, ulong key, uint index, IReadOnlyList<int> weights)
    {
        int total = 0;
        foreach (int x in weights) total += Math.Max(0, x);
        int roll = t.Range(id, key, index, 0, total);
        for (int i = 0; i < weights.Count; i++)
        {
            int x = Math.Max(0, weights[i]);
            if (roll < x) return i;
            roll -= x;
        }
        return weights.Count - 1;
    }
}
