using Return.Sim.Content;

namespace Return.Tests;

/// <summary>Culture and faith content (ADR-0023): its shape, and the load-time checks that name the file and the id.</summary>
public class CultureContentTests
{
    private static string ContentDir => Path.Combine(TestPaths.Root, "content");

    /// <summary>A mod folder holding the given files, merged over the real content by id.</summary>
    internal static string Mod(params (string File, string Json)[] files)
    {
        string mod = Path.Combine(Path.GetTempPath(), "return-mod-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(mod);
        foreach ((string file, string json) in files)
        {
            string path = Path.Combine(mod, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }
        return mod;
    }

    private static ContentException LoadFails(params (string File, string Json)[] files) =>
        Assert.Throws<ContentException>(() => ContentDb.Load(ContentDir, new[] { Mod(files) }));

    [Fact]
    public void CultureContentShape()
    {
        ContentDb db = TestContent.Db;
        Assert.Equal(new[] { "hearth", "reed", "ashen", "night" }, db.Cultures.Select(c => c.Id));
        Assert.Equal(new[] { "sworn", "roots", "ember" }, db.Faiths.Select(f => f.Id));
        Assert.Equal(12, db.Customs.Count);
        Assert.Equal(4, db.Domains.Count);
        Assert.Equal(4, db.Faiths.Sum(f => f.Gods.Count));
        var kinds = RequiredContent.CustomKinds.Select(k => k.Kind).ToHashSet();
        Assert.All(db.Customs, c => Assert.Contains(c.Kind, kinds));
        Assert.Equal(7, db.Customs.Select(c => c.Kind).Distinct().Count());
    }

    [Fact]
    public void UnknownCustomKindFailsAtLoad()
    {
        var ex = LoadFails(("customs.json", "[{ \"id\": \"guest_bread\", \"kind\": \"juggling\", \"params\": {} }]"));
        Assert.Contains("customs.json", ex.Message);
        Assert.Contains("guest_bread", ex.Message);
        Assert.Contains("juggling", ex.Message);
    }

    [Fact]
    public void MissingCustomParamFailsAtLoad()
    {
        var ex = LoadFails(("customs.json", "[{ \"id\": \"guest_bread\", \"kind\": \"guest_right\", \"params\": { \"offerPermille\": 700 } }]"));
        Assert.Contains("customs.json", ex.Message);
        Assert.Contains("guest_bread", ex.Message);
        Assert.Contains("wrongedGrudgePct", ex.Message);
    }

    [Fact]
    public void UnknownDeedFailsAtLoad()
    {
        var ex = LoadFails(("domains.json",
            "[{ \"id\": \"oaths\", \"epithetKeys\": [\"god.epithet.oaths.0\"], \"caresKey\": \"god.cares.oaths\", \"signs\": [], \"pleasedBy\": [\"oath_kept\"], \"angeredBy\": [\"pie_eaten\"] }]"));
        Assert.Contains("domains.json", ex.Message);
        Assert.Contains("oaths", ex.Message);
        Assert.Contains("pie_eaten", ex.Message);
    }

    [Fact]
    public void MissingCustomEventTextFailsAtLoad()
    {
        var ex = LoadFails(("customs.json", "[{ \"id\": \"quiet_greeting\", \"kind\": \"greeting\", \"params\": { \"breachPermille\": 100 } }]"),
            ("loc/en.mod.json", "{ \"custom.quiet_greeting.name\": \"the quiet nod\", \"custom.quiet_greeting.rule\": \"Nods.\" }"));
        Assert.Contains("customs.json", ex.Message);
        Assert.Contains("quiet_greeting", ex.Message);
        Assert.Contains("event.culture.quiet_greeting.breach", ex.Message);
    }

    [Fact]
    public void TwoGreetingsInOneCultureFailAtLoad()
    {
        string reed = File.ReadAllText(Path.Combine(ContentDir, "cultures.json"));
        string twoGreetings = reed.Replace("{ \"custom\": \"no_touch_greeting\", \"always\": true },",
            "{ \"custom\": \"no_touch_greeting\", \"always\": true }, { \"custom\": \"bow_greeting\", \"always\": true },", StringComparison.Ordinal);
        Assert.NotEqual(reed, twoGreetings);
        var ex = LoadFails(("cultures.json", twoGreetings));
        Assert.Contains("cultures.json", ex.Message);
        Assert.Contains("reed", ex.Message);
        Assert.Contains("greeting", ex.Message);
    }

    [Fact]
    public void AGreetingInAFaithFailsAtLoad()
    {
        string faiths = File.ReadAllText(Path.Combine(ContentDir, "faiths.json"));
        string withGreeting = faiths.Replace("{ \"custom\": \"sacred_grove\", \"always\": true },",
            "{ \"custom\": \"sacred_grove\", \"always\": true }, { \"custom\": \"bow_greeting\", \"always\": true },", StringComparison.Ordinal);
        Assert.NotEqual(faiths, withGreeting);
        var ex = LoadFails(("faiths.json", withGreeting));
        Assert.Contains("faiths.json", ex.Message);
        Assert.Contains("roots", ex.Message);
        Assert.Contains("bow_greeting", ex.Message);
    }

    [Fact]
    public void PicksBeyondThePoolFailAtLoad()
    {
        string cultures = File.ReadAllText(Path.Combine(ContentDir, "cultures.json"));
        int at = cultures.IndexOf("\"customPicks\": 2", StringComparison.Ordinal);
        string tooMany = cultures[..at] + "\"customPicks\": 5" + cultures[(at + "\"customPicks\": 2".Length)..];
        var ex = LoadFails(("cultures.json", tooMany));
        Assert.Contains("cultures.json", ex.Message);
        Assert.Contains("hearth", ex.Message);
    }

    [Fact]
    public void AnUnknownFaithOrSpeciesFailsAtLoad()
    {
        string cultures = File.ReadAllText(Path.Combine(ContentDir, "cultures.json"));
        var ex = LoadFails(("cultures.json", cultures.Replace("{ \"faith\": \"sworn\", \"weight\": 3 }", "{ \"faith\": \"nope\", \"weight\": 3 }", StringComparison.Ordinal)));
        Assert.Contains("cultures.json", ex.Message);
        Assert.Contains("nope", ex.Message);
        ex = LoadFails(("cultures.json", cultures.Replace("{ \"species\": \"vampire\", \"stance\": \"fear\" }", "{ \"species\": \"gryphon\", \"stance\": \"fear\" }", StringComparison.Ordinal)));
        Assert.Contains("reed", ex.Message);
        Assert.Contains("gryphon", ex.Message);
    }

    [Fact]
    public void EveryCultureLocKeyExists()
    {
        ContentDb db = TestContent.Db;
        var keys = new List<string>();
        foreach (CultureDef c in db.Cultures)
        {
            keys.AddRange(c.NameKeys);
            foreach (AttitudeDef a in c.Attitudes)
                keys.AddRange(new[] { "species." + a.Species + ".plural", "stance." + a.Stance, "stance." + a.Stance + ".many" });
        }
        foreach (FaithDef f in db.Faiths)
        {
            keys.AddRange(f.NameKeys);
            foreach (GodSlotDef g in f.Gods) keys.AddRange(g.NameKeys);
        }
        foreach (CustomDef c in db.Customs)
        {
            keys.Add(c.NameKey);
            keys.Add(c.RuleKey);
            foreach (string m in RequiredContent.CustomMoments(c)) keys.Add(c.EventKey(m));
        }
        foreach (DomainDef d in db.Domains)
        {
            keys.AddRange(d.EpithetKeys);
            keys.Add(d.CaresKey);
        }
        foreach (string s in RequiredContent.Signs) keys.Add("sign." + s);
        foreach (string d in RequiredContent.Deeds) keys.Add("deed." + d);
        keys.AddRange(new[]
        {
            "event.culture.omen_anger", "event.culture.omen_blessing", "event.culture.word_spread",
            "devotion.lapsed", "devotion.faithful", "devotion.devout",
            "temperament.stern", "temperament.kindly", "temperament.fickle", "temperament.distant",
            "ethic.oath_heavy", "ethic.oath_light", "ethic.signs_many", "ethic.signs_few",
            "job.feast", "job.repay_debt",
        });
        foreach (string s in new[] { "revere", "wary", "fear", "scorn" }) keys.AddRange(new[] { "stance." + s, "stance." + s + ".many" });
        var missing = keys.Where(k => !db.Loc.Has(k)).Distinct().ToList();
        Assert.True(missing.Count == 0, "Missing loc keys: " + string.Join(", ", missing));
    }
}
