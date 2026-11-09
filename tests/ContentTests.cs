using System.Reflection;
using System.Text.Json;
using Json.Schema;
using Return.Sim.Content;

namespace Return.Tests;

public class ContentTests
{
    private static string ContentDir => Path.Combine(TestPaths.Root, "content");

    [Fact]
    public void AllContentValidatesAgainstSchemas()
    {
        var files = Directory.GetFiles(ContentDir, "*.json").OrderBy(f => f, StringComparer.Ordinal).ToList();
        files.Add(Path.Combine(ContentDir, "loc", "en.json"));
        Assert.Equal(10, files.Count);
        foreach (string file in files)
        {
            string name = file.EndsWith("en.json", StringComparison.Ordinal) ? "loc" : Path.GetFileNameWithoutExtension(file);
            string schemaPath = Path.Combine(ContentDir, "schemas", name + ".schema.json");
            Assert.True(File.Exists(schemaPath), "missing schema " + schemaPath);
            JsonSchema schema = JsonSchema.FromText(File.ReadAllText(schemaPath));
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file));
            EvaluationResults result = schema.Evaluate(doc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            Assert.True(result.IsValid, $"{Path.GetFileName(file)} fails {name}.schema.json: {Describe(result)}");
        }
    }

    [Fact]
    public void M0ContentShape()
    {
        ContentDb db = ContentDb.Load(ContentDir);
        Assert.Equal(10, db.Cast.Count);
        Assert.Equal(new[] { "human", "vampire" }, db.Cast.Select(c => c.Species).Distinct().OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(8, db.Traits.Count);
        Assert.Equal(6, db.Incidents.Count);
        Assert.Single(db.Cast, c => c.Otherworlder);
    }

    [Fact]
    public void EveryLocKeyExists()
    {
        ContentDb db = ContentDb.Load(ContentDir);
        var defs = new List<object> { db.World };
        defs.AddRange(db.Species); defs.AddRange(db.Traits); defs.AddRange(db.Items); defs.AddRange(db.Work);
        defs.AddRange(db.Roles); defs.AddRange(db.Thoughts); defs.AddRange(db.Incidents); defs.AddRange(db.Cast);
        foreach (object def in defs)
            foreach (PropertyInfo p in def.GetType().GetProperties())
                if (p.PropertyType == typeof(string) && p.Name.EndsWith("Key", StringComparison.Ordinal) && p.Name != "Key")
                {
                    string key = (string)p.GetValue(def)!;
                    Assert.True(db.Loc.Has(key), $"{def.GetType().Name}.{p.Name} = '{key}' is missing from loc/en.json");
                }
        foreach (string key in db.World.SeasonKeys) Assert.True(db.Loc.Has(key), key);
    }

    [Fact]
    public void BadTraitIdNamesFileAndId()
    {
        string dir = CopyContent();
        string cast = Path.Combine(dir, "cast.json");
        File.WriteAllText(cast, File.ReadAllText(cast).Replace("\"signatureTrait\": \"desperate\"", "\"signatureTrait\": \"nope\"", StringComparison.Ordinal));
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(dir));
        Assert.Contains("cast.json", ex.Message);
        Assert.Contains("nope", ex.Message);
    }

    [Fact]
    public void UnknownFieldIsRejected()
    {
        string dir = CopyContent();
        string items = Path.Combine(dir, "items.json");
        File.WriteAllText(items, File.ReadAllText(items).Replace("\"nutrition\": 450", "\"nutriton\": 450", StringComparison.Ordinal));
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(dir));
        Assert.Contains("items.json", ex.Message);
    }

    [Fact]
    public void ModOverridesById()
    {
        string mod = Path.Combine(Path.GetTempPath(), "return-mod-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(mod);
        File.WriteAllText(Path.Combine(mod, "traits.json"),
            "[{ \"id\": \"proud\", \"nameKey\": \"trait.proud.name\", \"descKey\": \"trait.proud.desc\", \"insultOpinionScalePct\": 999 }]");
        ContentDb db = ContentDb.Load(ContentDir, new[] { mod });
        Assert.Equal(999, db.TraitById("proud").InsultOpinionScalePct);
        Assert.Equal(8, db.Traits.Count);
    }

    [Fact]
    public void MissingLocKeyIsVisible()
    {
        ContentDb db = ContentDb.Load(ContentDir);
        Assert.Equal("⟦no.such.key⟧", db.Loc.Format("no.such.key", Array.Empty<KeyValuePair<string, string>>()));
    }

    [Fact]
    public void LocFormatsPlaceholders()
    {
        var loc = new LocTable(new Dictionary<string, string> { ["test.met"] = "{a} met {b} by the {place}." });
        string s = loc.Format("test.met", new[] { KV("a", "Mira"), KV("b", "Oren"), KV("place", "hearth") });
        Assert.Equal("Mira met Oren by the hearth.", s);
    }

    private static KeyValuePair<string, string> KV(string k, string v) => new(k, v);

    private static string CopyContent()
    {
        string dir = Path.Combine(Path.GetTempPath(), "return-content-" + Guid.NewGuid().ToString("N"));
        foreach (string file in Directory.GetFiles(ContentDir, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(dir, Path.GetRelativePath(ContentDir, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        return dir;
    }

    private static string Describe(EvaluationResults r)
    {
        var lines = new List<string>();
        foreach (EvaluationResults d in r.Details ?? Enumerable.Empty<EvaluationResults>())
            if (d.Errors != null)
                foreach (var e in d.Errors) lines.Add($"{d.InstanceLocation}: {e.Value}");
        return string.Join("; ", lines);
    }
}
