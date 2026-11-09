using System.Text.Json;
using System.Text.Json.Serialization;

namespace Return.Sim.Content;

/// <summary>
/// All definitions from /content plus any mod folders (later folders override earlier ones by id).
/// Lists keep file order; dictionaries here are for lookup only and never drive simulation order.
/// </summary>
public sealed class ContentDb
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private Dictionary<string, SpeciesDef> _species = new();
    private Dictionary<string, TraitDef> _traits = new();
    private Dictionary<string, ItemDef> _items = new();
    private Dictionary<string, WorkDef> _work = new();
    private Dictionary<string, RoleDef> _roles = new();
    private Dictionary<string, ThoughtDef> _thoughts = new();
    private Dictionary<string, IncidentDef> _incidents = new();
    private Dictionary<string, CastDef> _cast = new();
    private Dictionary<string, WantDef> _wants = new();

    public WorldDef World { get; private set; } = new();
    public IReadOnlyList<SpeciesDef> Species { get; private set; } = Array.Empty<SpeciesDef>();
    public IReadOnlyList<TraitDef> Traits { get; private set; } = Array.Empty<TraitDef>();
    public IReadOnlyList<ItemDef> Items { get; private set; } = Array.Empty<ItemDef>();
    public IReadOnlyList<WorkDef> Work { get; private set; } = Array.Empty<WorkDef>();
    public IReadOnlyList<RoleDef> Roles { get; private set; } = Array.Empty<RoleDef>();
    public IReadOnlyList<ThoughtDef> Thoughts { get; private set; } = Array.Empty<ThoughtDef>();
    public IReadOnlyList<IncidentDef> Incidents { get; private set; } = Array.Empty<IncidentDef>();
    public IReadOnlyList<CastDef> Cast { get; private set; } = Array.Empty<CastDef>();
    public IReadOnlyList<WantDef> Wants { get; private set; } = Array.Empty<WantDef>();
    public LocTable Loc { get; private set; } = new(new Dictionary<string, string>());

    public SpeciesDef SpeciesById(string id) => Find(_species, id, "species.json");
    public TraitDef TraitById(string id) => Find(_traits, id, "traits.json");
    public ItemDef ItemById(string id) => Find(_items, id, "items.json");
    public WorkDef WorkById(string id) => Find(_work, id, "work.json");
    public WorkDef? TryWork(string id) => _work.TryGetValue(id, out WorkDef? def) ? def : null;
    public ItemDef? TryItem(string id) => _items.TryGetValue(id, out ItemDef? def) ? def : null;
    public RoleDef RoleById(string id) => Find(_roles, id, "roles.json");
    public ThoughtDef ThoughtById(string id) => Find(_thoughts, id, "thoughts.json");
    public IncidentDef IncidentById(string id) => Find(_incidents, id, "incidents.json");
    public CastDef CastByKey(string key) => Find(_cast, key, "cast.json");
    public WantDef WantById(string id) => Find(_wants, id, "wants.json");

    public static ContentDb Load(string contentDir, IReadOnlyList<string>? modDirs = null)
    {
        var dirs = new List<string> { contentDir };
        if (modDirs != null) dirs.AddRange(modDirs);

        var db = new ContentDb
        {
            World = ReadWorld(dirs),
            Species = ReadMerged<SpeciesDef>(dirs, "species.json", d => d.Id),
            Traits = ReadMerged<TraitDef>(dirs, "traits.json", d => d.Id),
            Items = ReadMerged<ItemDef>(dirs, "items.json", d => d.Id),
            Work = ReadMerged<WorkDef>(dirs, "work.json", d => d.Id),
            Roles = ReadMerged<RoleDef>(dirs, "roles.json", d => d.Id),
            Thoughts = ReadMerged<ThoughtDef>(dirs, "thoughts.json", d => d.Id),
            Incidents = ReadMerged<IncidentDef>(dirs, "incidents.json", d => d.Id),
            Cast = ReadMerged<CastDef>(dirs, "cast.json", d => d.Key),
            Wants = ReadMerged<WantDef>(dirs, "wants.json", d => d.Id),
            Loc = new LocTable(ReadLoc(dirs)),
        };
        db._species = Index(db.Species, d => d.Id);
        db._traits = Index(db.Traits, d => d.Id);
        db._items = Index(db.Items, d => d.Id);
        db._work = Index(db.Work, d => d.Id);
        db._roles = Index(db.Roles, d => d.Id);
        db._thoughts = Index(db.Thoughts, d => d.Id);
        db._incidents = Index(db.Incidents, d => d.Id);
        db._cast = Index(db.Cast, d => d.Key);
        db._wants = Index(db.Wants, d => d.Id);
        db.Validate();
        return db;
    }

    /// <summary>Walks up from the app's folder to the first directory holding content/world.json.</summary>
    public static string FindContentDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "world.json"))) return candidate;
            dir = dir.Parent;
        }
        throw new ContentException("Could not find a content/ folder above " + AppContext.BaseDirectory);
    }

    private void Validate()
    {
        WorldDef w = World;
        if (w.SeasonKeys.Length != w.SeasonsPerYear)
            Fail("world.json", $"has {w.SeasonKeys.Length} seasonKeys but seasonsPerYear is {w.SeasonsPerYear}");
        if (w.DawnHour >= w.DuskHour || w.DuskHour > w.HoursPerDay)
            Fail("world.json", $"dawnHour {w.DawnHour} must be before duskHour {w.DuskHour} within the day");
        RequireLoc("world.json", "world", w.SeasonKeys);

        foreach (SpeciesDef s in Species)
        {
            RequireLoc("species.json", s.Id, s.NameKey);
            if (!s.DrinksBlood && !_items.ContainsKey(s.FoodSource))
                Fail("species.json", $"'{s.Id}' has foodSource '{s.FoodSource}', which is neither \"blood\" nor an item id");
            if (!s.DrinksBlood && s.FoodSource != "berries")
                Fail("species.json", $"'{s.Id}' eats '{s.FoodSource}', but M0 only grows, rations, steals and spoils berries (ADR-0018)");
        }
        foreach (TraitDef t in Traits)
        {
            RequireLoc("traits.json", t.Id, t.NameKey, t.DescKey);
            foreach (WorkBias b in t.WorkBias)
                if (!_work.ContainsKey(b.Work)) Fail("traits.json", $"'{t.Id}' biases unknown work '{b.Work}'");
        }
        foreach (ItemDef i in Items) RequireLoc("items.json", i.Id, i.NameKey);
        foreach (WorkDef wd in Work) RequireLoc("work.json", wd.Id, wd.NameKey);
        foreach (RoleDef r in Roles)
        {
            RequireLoc("roles.json", r.Id, r.NameKey);
            foreach (RolePriority p in r.Priorities)
                if (!_work.ContainsKey(p.Work)) Fail("roles.json", $"'{r.Id}' prioritises unknown work '{p.Work}'");
        }
        foreach (ThoughtDef th in Thoughts) RequireLoc("thoughts.json", th.Id, th.TextKey);
        foreach (IncidentDef inc in Incidents) RequireLoc("incidents.json", inc.Id, inc.TextKey);

        foreach (string id in RequiredContent.Items)
            if (!_items.ContainsKey(id)) Fail("items.json", $"the simulation needs an item '{id}'");
        foreach (string id in RequiredContent.Species)
            if (!_species.ContainsKey(id)) Fail("species.json", $"the simulation needs a species '{id}'");
        foreach (string id in RequiredContent.Work)
            if (!_work.ContainsKey(id)) Fail("work.json", $"the simulation needs work '{id}'");
        foreach (string id in RequiredContent.Thoughts)
            if (!_thoughts.ContainsKey(id)) Fail("thoughts.json", $"the simulation needs a thought '{id}'");
        foreach ((string id, string[] ps) in RequiredContent.Incidents)
        {
            if (!_incidents.TryGetValue(id, out IncidentDef? inc)) { Fail("incidents.json", $"the simulation needs an incident '{id}'"); continue; }
            foreach (string param in ps)
                if (!inc.Params.ContainsKey(param)) Fail("incidents.json", $"'{id}' needs the param '{param}'");
        }
        foreach (IncidentDef inc in Incidents)
        {
            bool known = false;
            foreach ((string id, string[] _) in RequiredContent.Incidents) if (id == inc.Id) known = true;
            if (!known) Fail("incidents.json", $"'{inc.Id}' has no behaviour in this version of the simulation");
        }

        int otherworlders = 0;
        foreach (CastDef c in Cast)
        {
            RequireLoc("cast.json", c.Key, c.NameKey);
            if (!_species.ContainsKey(c.Species)) Fail("cast.json", $"'{c.Key}' references unknown species '{c.Species}'");
            if (!_traits.ContainsKey(c.SignatureTrait)) Fail("cast.json", $"'{c.Key}' references unknown trait '{c.SignatureTrait}'");
            if (!_roles.ContainsKey(c.Role)) Fail("cast.json", $"'{c.Key}' references unknown role '{c.Role}'");
            if (c.Otherworlder) otherworlders++;
        }
        if (otherworlders != 1) Fail("cast.json", $"needs exactly one otherworlder, found {otherworlders}");

        ValidateWants();
    }

    /// <summary>Wants name real traits, species, roles, items and camp places, and someone alone in the world can still want something.</summary>
    private void ValidateWants()
    {
        bool fallback = false;
        foreach (WantDef d in Wants)
        {
            RequireLoc("wants.json", d.Id, d.TextKey, d.BeatKey);
            switch (d.Target)
            {
                case "none":
                    if (d.Weights.Base > 0) fallback = true;
                    break;
                case "person":
                    if (d.Pick is not ("best_chemistry" or "worst_chemistry"))
                        Fail("wants.json", $"'{d.Id}' is about a person, so its pick must be best_chemistry or worst_chemistry, not '{d.Pick}'");
                    break;
                case "place":
                    if (!WantDef.Places.Contains(d.Place))
                        Fail("wants.json", $"'{d.Id}' is about the place '{d.Place}', which is not one of the camp's places ({string.Join(", ", WantDef.Places)})");
                    RequireLoc("wants.json", d.Id, "place." + d.Place);
                    break;
                case "item":
                    if (!_items.ContainsKey(d.Item)) Fail("wants.json", $"'{d.Id}' is about unknown item '{d.Item}'");
                    break;
                default:
                    Fail("wants.json", $"'{d.Id}' has target '{d.Target}'; use none, person, place or item");
                    break;
            }
            foreach (string t in d.Weights.Traits.Keys)
                if (!_traits.ContainsKey(t)) Fail("wants.json", $"'{d.Id}' weighs unknown trait '{t}'");
            foreach (string s in d.Weights.Species.Keys)
                if (!_species.ContainsKey(s)) Fail("wants.json", $"'{d.Id}' weighs unknown species '{s}'");
            foreach (string r in d.Weights.Roles.Keys)
                if (!_roles.ContainsKey(r)) Fail("wants.json", $"'{d.Id}' weighs unknown role '{r}'");
        }
        if (!fallback) Fail("wants.json", "needs a want about nothing in particular with a base weight, so someone alone in the world can still want something");
    }

    private void RequireLoc(string file, string id, params string[] keys)
    {
        foreach (string key in keys)
            if (!Loc.Has(key)) Fail(file, $"'{id}' uses missing loc key '{key}'");
    }

    private static void Fail(string file, string message) => throw new ContentException($"{file}: {message}.");

    private static T Find<T>(Dictionary<string, T> map, string id, string file) =>
        map.TryGetValue(id, out T? def) ? def : throw new ContentException($"{file}: no entry with id '{id}'.");

    private static Dictionary<string, T> Index<T>(IReadOnlyList<T> defs, Func<T, string> id)
    {
        var map = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (T d in defs) map[id(d)] = d;
        return map;
    }

    private static WorldDef ReadWorld(List<string> dirs)
    {
        WorldDef? world = null;
        foreach (string dir in dirs)
        {
            string path = Path.Combine(dir, "world.json");
            if (File.Exists(path)) world = Deserialize<WorldDef>(path);
        }
        return world ?? throw new ContentException("world.json: not found in " + dirs[0]);
    }

    private static IReadOnlyList<T> ReadMerged<T>(List<string> dirs, string file, Func<T, string> id)
    {
        var merged = new List<T>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int d = 0; d < dirs.Count; d++)
        {
            string path = Path.Combine(dirs[d], file);
            if (!File.Exists(path))
            {
                if (d == 0) throw new ContentException($"{file}: not found in {dirs[0]}");
                continue;
            }
            var inFile = new HashSet<string>(StringComparer.Ordinal);
            foreach (T def in Deserialize<List<T>>(path))
            {
                string key = id(def);
                if (!inFile.Add(key)) throw new ContentException($"{file}: duplicate id '{key}' in {path}.");
                if (seen.TryGetValue(key, out int at)) merged[at] = def;
                else
                {
                    seen[key] = merged.Count;
                    merged.Add(def);
                }
            }
        }
        return merged;
    }

    /// <summary>Reads loc/en.json, then any loc/en.*.json (e.g. en.ui.json) in ordinal name order; later keys win.</summary>
    private static Dictionary<string, string> ReadLoc(List<string> dirs)
    {
        var loc = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string dir in dirs)
        {
            string locDir = Path.Combine(dir, "loc");
            if (!Directory.Exists(locDir)) continue;
            var files = new List<string>();
            string main = Path.Combine(locDir, "en.json");
            if (File.Exists(main)) files.Add(main);
            var extra = new List<string>(Directory.GetFiles(locDir, "en.*.json"));
            extra.Sort(StringComparer.Ordinal);
            files.AddRange(extra);
            foreach (string path in files)
                foreach (KeyValuePair<string, string> kv in Deserialize<Dictionary<string, string>>(path))
                    loc[kv.Key] = kv.Value;
        }
        return loc;
    }

    private static T Deserialize<T>(string path)
    {
        string name = Path.GetFileName(path);
        string file = name.StartsWith("en.", StringComparison.Ordinal) ? "loc/" + name : name;
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)
                ?? throw new ContentException($"{file}: empty document.");
        }
        catch (JsonException ex)
        {
            throw new ContentException($"{file}: {ex.Message}", ex);
        }
    }
}
