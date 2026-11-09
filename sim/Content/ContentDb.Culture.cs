namespace Return.Sim.Content;

/// <summary>Culture and faith content (ADR-0023): loading, lookup and the checks made at load.</summary>
public sealed partial class ContentDb
{
    private static readonly string[] Stances = { "revere", "wary", "fear", "scorn" };
    private static readonly string[] Readings = { "anger", "blessing" };

    private Dictionary<string, CultureDef> _cultures = new();
    private Dictionary<string, FaithDef> _faiths = new();
    private Dictionary<string, CustomDef> _customs = new();
    private Dictionary<string, DomainDef> _domains = new();

    public IReadOnlyList<CultureDef> Cultures { get; private set; } = Array.Empty<CultureDef>();
    public IReadOnlyList<FaithDef> Faiths { get; private set; } = Array.Empty<FaithDef>();
    public IReadOnlyList<CustomDef> Customs { get; private set; } = Array.Empty<CustomDef>();
    public IReadOnlyList<DomainDef> Domains { get; private set; } = Array.Empty<DomainDef>();

    public CultureDef CultureById(string id) => Find(_cultures, id, "cultures.json");
    public FaithDef FaithById(string id) => Find(_faiths, id, "faiths.json");
    public CustomDef CustomById(string id) => Find(_customs, id, "customs.json");
    public DomainDef DomainById(string id) => Find(_domains, id, "domains.json");

    private void LoadCulture(List<string> dirs)
    {
        Cultures = ReadMerged<CultureDef>(dirs, "cultures.json", d => d.Id);
        Faiths = ReadMerged<FaithDef>(dirs, "faiths.json", d => d.Id);
        Customs = ReadMerged<CustomDef>(dirs, "customs.json", d => d.Id);
        Domains = ReadMerged<DomainDef>(dirs, "domains.json", d => d.Id);
        _cultures = Index(Cultures, d => d.Id);
        _faiths = Index(Faiths, d => d.Id);
        _customs = Index(Customs, d => d.Id);
        _domains = Index(Domains, d => d.Id);
    }

    private void ValidateCulture()
    {
        foreach (string id in RequiredContent.CultureThoughts)
            if (!_thoughts.ContainsKey(id)) Fail("thoughts.json", $"the simulation needs a thought '{id}'");

        foreach (CustomDef c in Customs)
        {
            string[]? needs = null;
            foreach ((string kind, string[] ps, string[] _) in RequiredContent.CustomKinds)
                if (kind == c.Kind) needs = ps;
            if (needs == null) { Fail("customs.json", $"'{c.Id}' has kind '{c.Kind}', which has no behaviour in this version of the simulation"); continue; }
            foreach (string param in needs)
                if (!c.Params.ContainsKey(param)) Fail("customs.json", $"'{c.Id}' needs the param '{param}'");
            if (c.Kind == "festival" && (c.Arg("season") < 0 || c.Arg("season") >= World.SeasonsPerYear))
                Fail("customs.json", $"'{c.Id}' falls in season {c.Arg("season")}, but the year has seasons 0 to {World.SeasonsPerYear - 1}");
            if (c.Kind == "festival" && c.Arg("hours") < 1) Fail("customs.json", $"'{c.Id}' must last at least an hour");
            if (c.Kind == "sacred_tree" && c.Arg("nearestTrees") < 1) Fail("customs.json", $"'{c.Id}' must choose among at least one tree");
            RequireLoc("customs.json", c.Id, c.NameKey, c.RuleKey);
            foreach (string moment in RequiredContent.CustomMoments(c))
                RequireLoc("customs.json", c.Id, c.EventKey(moment));
        }

        foreach (DomainDef d in Domains)
        {
            if (d.EpithetKeys.Count == 0) Fail("domains.json", $"'{d.Id}' has no epithets");
            RequireLoc("domains.json", d.Id, d.EpithetKeys.ToArray());
            RequireLoc("domains.json", d.Id, d.CaresKey);
            foreach (SignDef s in d.Signs)
            {
                if (!RequiredContent.Signs.Contains(s.Sign)) Fail("domains.json", $"'{d.Id}' reads unknown sign '{s.Sign}'");
                if (!Readings.Contains(s.Reads)) Fail("domains.json", $"'{d.Id}' reads '{s.Sign}' as '{s.Reads}', which is neither anger nor blessing");
                RequireLoc("domains.json", d.Id, "sign." + s.Sign);
            }
            foreach (string deed in d.PleasedBy.Concat(d.AngeredBy))
            {
                if (!RequiredContent.Deeds.Contains(deed)) Fail("domains.json", $"'{d.Id}' cares about unknown deed '{deed}'");
                RequireLoc("domains.json", d.Id, "deed." + deed);
            }
        }

        foreach (FaithDef f in Faiths)
        {
            RequireLoc("faiths.json", f.Id, f.NameKeys.ToArray());
            if (f.NameKeys.Count == 0) Fail("faiths.json", $"'{f.Id}' has no names");
            if (f.Gods.Count == 0) Fail("faiths.json", $"'{f.Id}' has no gods");
            for (int s = 0; s < f.Gods.Count; s++)
            {
                GodSlotDef g = f.Gods[s];
                if (g.NameKeys.Count == 0) Fail("faiths.json", $"'{f.Id}' god {s} has no names");
                RequireLoc("faiths.json", f.Id, g.NameKeys.ToArray());
                CheckRange("faiths.json", f.Id, "wrath", g.Wrath);
                CheckRange("faiths.json", f.Id, "grace", g.Grace);
                int total = 0;
                foreach (DomainWeight dw in g.Domains)
                {
                    if (!_domains.ContainsKey(dw.Domain)) Fail("faiths.json", $"'{f.Id}' god {s} references unknown domain '{dw.Domain}'");
                    total += dw.Weight;
                }
                if (total <= 0) Fail("faiths.json", $"'{f.Id}' god {s} has no domain with a weight above 0");
            }
            CheckCustoms("faiths.json", f.Id, f.Customs, f.CustomPicks, allowGreeting: false);
        }

        foreach (CultureDef c in Cultures)
        {
            RequireLoc("cultures.json", c.Id, c.NameKeys.ToArray());
            if (c.NameKeys.Count == 0) Fail("cultures.json", $"'{c.Id}' has no names");
            foreach (PeopleWeight p in c.Peoples)
                if (p.Species != "*" && !_species.ContainsKey(p.Species)) Fail("cultures.json", $"'{c.Id}' references unknown species '{p.Species}'");
            int faithWeight = 0;
            foreach (FaithWeight fw in c.Faiths)
            {
                if (!_faiths.ContainsKey(fw.Faith)) Fail("cultures.json", $"'{c.Id}' references unknown faith '{fw.Faith}'");
                faithWeight += fw.Weight;
            }
            if (faithWeight <= 0) Fail("cultures.json", $"'{c.Id}' has no faith with a weight above 0");
            CheckRange("cultures.json", c.Id, "devotion", c.Devotion);
            CheckCustoms("cultures.json", c.Id, c.Customs, c.CustomPicks, allowGreeting: true);
            foreach (AttitudeDef a in c.Attitudes)
            {
                if (!_species.ContainsKey(a.Species)) Fail("cultures.json", $"'{c.Id}' has an attitude toward unknown species '{a.Species}'");
                if (!Stances.Contains(a.Stance)) Fail("cultures.json", $"'{c.Id}' has unknown stance '{a.Stance}'");
                RequireLoc("cultures.json", c.Id, "species." + a.Species + ".plural", "stance." + a.Stance, "stance." + a.Stance + ".many");
            }
        }
    }

    /// <summary>
    /// A holder's customs resolve, hold at most one custom of each kind always, keep greetings to
    /// cultures, and can always fill <paramref name="picks"/> from the pool: draws skip kinds already
    /// held, so picks may not exceed the distinct kinds in the pool that no always-held custom has.
    /// </summary>
    private void CheckCustoms(string file, string id, List<CustomRef> customs, int picks, bool allowGreeting)
    {
        var always = new List<string>();
        var poolKinds = new List<string>();
        foreach (CustomRef r in customs)
        {
            if (!_customs.TryGetValue(r.Custom, out CustomDef? def)) { Fail(file, $"'{id}' references unknown custom '{r.Custom}'"); continue; }
            if (def.Kind == "greeting" && !allowGreeting) Fail(file, $"'{id}' holds the greeting '{r.Custom}', but greetings belong to cultures");
            if (!r.Always) continue;
            if (always.Contains(def.Kind)) Fail(file, $"'{id}' always holds two customs of kind '{def.Kind}'");
            always.Add(def.Kind);
        }
        foreach (CustomRef r in customs)
        {
            if (r.Always || !_customs.TryGetValue(r.Custom, out CustomDef? def)) continue;
            if (!always.Contains(def.Kind) && !poolKinds.Contains(def.Kind) && r.Weight > 0) poolKinds.Add(def.Kind);
        }
        if (picks > poolKinds.Count) Fail(file, $"'{id}' picks {picks} customs, but its pool offers only {poolKinds.Count} kinds it does not already hold");
    }

    private static void CheckRange(string file, string id, string name, RangeDef r)
    {
        if (r.Min > r.Max) Fail(file, $"'{id}' has {name} min {r.Min} above max {r.Max}");
    }
}
