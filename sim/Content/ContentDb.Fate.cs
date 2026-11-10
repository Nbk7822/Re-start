namespace Return.Sim.Content;

/// <summary>Who you are and how a run works (ADR-0030): loading rebirth.json and gifts.json, and the checks made at load.</summary>
public sealed partial class ContentDb
{
    private static readonly string[] SeenWords = { "hidden", "seen", "loud" };

    private Dictionary<string, GiftDef> _gifts = new();

    public RebirthDef Rebirth { get; private set; } = new();
    public IReadOnlyList<GiftDef> Gifts { get; private set; } = Array.Empty<GiftDef>();

    public GiftDef GiftById(string id) => Find(_gifts, id, "gifts.json");
    public GiftDef? TryGift(string id) => _gifts.TryGetValue(id, out GiftDef? def) ? def : null;
    public CastDef? TryCast(string key) => _cast.TryGetValue(key, out CastDef? def) ? def : null;

    /// <summary>The species someone ages as: their own, if it sets ages, else the human's (the player design §6.1).</summary>
    public SpeciesDef AgesAs(string speciesId)
    {
        SpeciesDef sp = SpeciesById(speciesId);
        return sp.HasAges ? sp : SpeciesById("human");
    }

    private sealed class RebirthFile
    {
        public List<RebirthPeopleDef>? Peoples { get; init; }
        public int[]? SoulSpanYears { get; init; }
        public KnotRulesDef? Knots { get; init; }
        public ReturnRulesDef? Return { get; init; }
        public MemoryRulesDef? Memory { get; init; }
    }

    private void LoadFate(List<string> dirs)
    {
        Rebirth = ReadRebirth(dirs);
        Gifts = ReadMerged<GiftDef>(dirs, "gifts.json", d => d.Id);
        _gifts = Index(Gifts, d => d.Id);
    }

    /// <summary>The base rebirth.json, with each mod's peoples merged by species id and any block a mod gives replacing the base's.</summary>
    private static RebirthDef ReadRebirth(List<string> dirs)
    {
        var peoples = new List<RebirthPeopleDef>();
        int[]? span = null;
        KnotRulesDef? knots = null;
        ReturnRulesDef? ret = null;
        MemoryRulesDef? memory = null;
        for (int d = 0; d < dirs.Count; d++)
        {
            string path = Path.Combine(dirs[d], "rebirth.json");
            if (!File.Exists(path))
            {
                if (d == 0) throw new ContentException($"rebirth.json: not found in {dirs[0]}");
                continue;
            }
            RebirthFile file = Deserialize<RebirthFile>(path);
            var inFile = new HashSet<string>(StringComparer.Ordinal);
            foreach (RebirthPeopleDef p in file.Peoples ?? new List<RebirthPeopleDef>())
            {
                if (!inFile.Add(p.Species)) throw new ContentException($"rebirth.json: duplicate people '{p.Species}' in {path}.");
                int at = peoples.FindIndex(x => x.Species == p.Species);
                if (at >= 0) peoples[at] = p;
                else peoples.Add(p);
            }
            span = file.SoulSpanYears ?? span;
            knots = file.Knots ?? knots;
            ret = file.Return ?? ret;
            memory = file.Memory ?? memory;
        }
        return new RebirthDef
        {
            Peoples = peoples,
            SoulSpanYears = span ?? Array.Empty<int>(),
            Knots = knots ?? new KnotRulesDef(),
            Return = ret ?? new ReturnRulesDef(),
            Memory = memory ?? new MemoryRulesDef(),
        };
    }

    private void ValidateFate()
    {
        foreach (string id in RequiredContent.FateThoughts)
            if (!_thoughts.ContainsKey(id)) Fail("thoughts.json", $"the simulation needs a thought '{id}'");
        ValidateAges();
        ValidateRebirth();
        ValidateGifts();

        // A need that drives others away harms you instead, once its clock has run, and can kill you (the player design §4.1).
        foreach (NeedDef n in Needs)
            if (n.LeavesAfterCriticalHours > 0)
            {
                if (n.DeathKey.Length == 0) Fail("needs.json", $"'{n.Id}' drives people away, so it needs a deathKey: it is how you would die of it");
                else RequireLoc("needs.json", n.Id, n.DeathKey + "_you_a");
            }
    }

    /// <summary>Age fields are pairs with min ≤ max, a body outlives its adulthood, the ageless set no lifespan, and the human (whom others age as) sets its ages.</summary>
    private void ValidateAges()
    {
        foreach (SpeciesDef s in Species)
        {
            bool any = s.AdultAtYears.Length > 0 || s.LifespanYears.Length > 0 || s.Ageless;
            if (!any) continue;
            if (!Pair(s.AdultAtYears)) Fail("species.json", $"'{s.Id}' needs adultAtYears [min, max] with min <= max");
            if (s.Ageless && s.LifespanYears.Length > 0) Fail("species.json", $"'{s.Id}' is ageless, so it cannot also have lifespanYears");
            if (!s.Ageless)
            {
                if (!Pair(s.LifespanYears)) Fail("species.json", $"'{s.Id}' needs lifespanYears [min, max] with min <= max, or ageless: true");
                else if (Pair(s.AdultAtYears) && s.LifespanYears[0] <= s.AdultAtYears[1])
                    Fail("species.json", $"'{s.Id}' could be born past its lifespan: lifespanYears min {s.LifespanYears[0]} must be above adultAtYears max {s.AdultAtYears[1]}");
            }
        }
        if (_species.TryGetValue("human", out SpeciesDef? human) && !human.HasAges)
            Fail("species.json", "'human' needs adultAtYears and lifespanYears: peoples without their own age as humans");
    }

    private static bool Pair(int[] p) => p.Length == 2 && p[0] >= 0 && p[0] <= p[1];

    /// <summary>
    /// Rollable peoples exist, with opening words, and every need and sun line of theirs has the second-person
    /// form the log uses when you are the one starving, burning or restless; the soul's span and knot triggers are well formed.
    /// </summary>
    private void ValidateRebirth()
    {
        RebirthDef r = Rebirth;
        int rollable = 0;
        foreach (RebirthPeopleDef p in r.Peoples)
        {
            if (!_species.TryGetValue(p.Species, out SpeciesDef? sp)) { Fail("rebirth.json", $"people '{p.Species}' is not a species in species.json"); continue; }
            if (p.Weight <= 0) continue;
            rollable++;
            RequireLoc("rebirth.json", p.Species, p.OpeningKey, p.AsKey);
            foreach (string line in YouLines(sp)) RequireLoc("rebirth.json", p.Species, line + "_you_a");
        }
        if (rollable == 0) Fail("rebirth.json", "needs at least one people with a weight above 0");
        if (!(r.SoulSpanYears.Length == 2 && r.SoulSpanYears[0] >= 1 && r.SoulSpanYears[0] <= r.SoulSpanYears[1]))
            Fail("rebirth.json", "soulSpanYears must be [min, max] with 1 <= min <= max");
        foreach (string key in r.Knots.Triggers)
            if (!Loc.Has(key)) Fail("rebirth.json", $"knot trigger '{key}' is not a known event key");
        if (r.Knots.MaxPerDay < 1) Fail("rebirth.json", "knots.maxPerDay must be at least 1");
        if (r.Return.RecentDays < 1) Fail("rebirth.json", "return.recentDays must be at least 1");
    }

    /// <summary>The lines that name someone of this people when their needs or the sun bite: food or blood, the sun, and each need of their nature.</summary>
    public static IReadOnlyList<string> NeedLines(SpeciesDef sp, Func<string, NeedDef> need)
    {
        var lines = new List<string>();
        if (sp.DrinksBlood) lines.AddRange(new[] { "event.needs.thirsting", "event.needs.blood_starved", "event.needs.died_blood" });
        else if (sp.EatsItem) lines.AddRange(new[] { "event.needs.hungry", "event.needs.starving", "event.needs.died_starving" });
        if (sp.DaylightBurnPerHour > 0) lines.AddRange(new[] { "event.needs.sun_burn", "event.needs.died_sun" });
        foreach (string id in sp.Needs)
        {
            NeedDef n = need(id);
            lines.Add(n.LowEventKey);
            lines.Add(n.CriticalEventKey);
            if (n.DeathKey.Length > 0) lines.Add(n.DeathKey);
        }
        return lines;
    }

    private IReadOnlyList<string> YouLines(SpeciesDef sp) =>
        NeedLines(sp, id => _needs.TryGetValue(id, out NeedDef? n) ? n : new NeedDef());

    /// <summary>
    /// Every gift has behaviour in code (RequiredContent.Gifts) and the numbers that behaviour reads, its words
    /// (with your forms of its awakening lines), stages ascending from 0, an order whose choices the code knows,
    /// and an awakening by a day within the first season.
    /// </summary>
    private void ValidateGifts()
    {
        foreach (GiftDef g in Gifts)
        {
            (string Id, string[] Params, string[] Choices)? known = null;
            foreach (var k in RequiredContent.Gifts) if (k.Id == g.Id) known = k;
            if (known == null) { Fail("gifts.json", $"'{g.Id}' has no behaviour in this version of the simulation"); continue; }
            RequireLoc("gifts.json", g.Id, g.NameKey, g.SummaryKey, g.GrowsKey, "gift_seen." + g.Seen);
            if (!SeenWords.Contains(g.Seen)) Fail("gifts.json", $"'{g.Id}' is seen '{g.Seen}'; use hidden, seen or loud");
            RequireLoc("gifts.json", g.Id, g.Awakening.TextKey, g.Awakening.TextKey + "_you_a", g.Awakening.DreamKey, g.Awakening.DreamKey + "_you_a");
            if (g.Awakening.ByDay < 1 || g.Awakening.ByDay > World.DaysPerSeason)
                Fail("gifts.json", $"'{g.Id}' must awaken by a day within the first season (1 to {World.DaysPerSeason}), not {g.Awakening.ByDay}");
            if (g.Tiers.Count == 0 || g.Tiers[0].At != 0) Fail("gifts.json", $"'{g.Id}' needs tiers starting at 0");
            for (int t = 0; t < g.Tiers.Count; t++)
            {
                RequireLoc("gifts.json", g.Id, g.Tiers[t].StageKey);
                if (t > 0 && g.Tiers[t].At <= g.Tiers[t - 1].At) Fail("gifts.json", $"'{g.Id}' tiers must ascend: tier {t + 1} at {g.Tiers[t].At} is not above {g.Tiers[t - 1].At}");
            }
            foreach (string param in known.Value.Params)
                if (g.Tiers.Count > 0 && !g.Has(param, 0)) Fail("gifts.json", $"'{g.Id}' needs the param '{param}'");
            foreach (string species in g.Peoples.Keys)
                if (!_species.ContainsKey(species)) Fail("gifts.json", $"'{g.Id}' weighs unknown people '{species}'");
            if (known.Value.Choices.Length > 0 && g.Order == null) Fail("gifts.json", $"'{g.Id}' needs an order with the choices {string.Join(", ", known.Value.Choices)}");
            if (g.Order is { } o)
            {
                foreach (string choice in known.Value.Choices)
                    if (!o.Choices.Contains(choice)) Fail("gifts.json", $"'{g.Id}' order needs the choice '{choice}'");
                foreach (string choice in o.Choices)
                {
                    if (!known.Value.Choices.Contains(choice)) Fail("gifts.json", $"'{g.Id}' order choice '{choice}' has no behaviour");
                    RequireLoc("gifts.json", g.Id, o.ChoiceKey(choice), o.HintKey(choice), o.DecidedKey(choice));
                }
                if (!o.Choices.Contains(o.Default)) Fail("gifts.json", $"'{g.Id}' order default '{o.Default}' is not one of its choices");
                RequireLoc("gifts.json", g.Id, o.TitleKey);
            }
        }
    }
}
