using System.Text.Json;
using System.Text.RegularExpressions;
using Json.Schema;
using Return.Sim.Content;

namespace Return.Tests;

/// <summary>wants.json (fixed decision 12, ADR-0021): every want has words, in both persons, and load-time checks catch bad references.</summary>
public class WantContentTests
{
    private static string ContentDir => Path.Combine(TestPaths.Root, "content");
    private static readonly Regex ThirdPerson = new(@"\b(they|them|their|theirs|themselves)\b", RegexOptions.CultureInvariant);

    [Fact]
    public void TenToSixteenWantsSuitTheCamp()
    {
        ContentDb db = ContentDb.Load(ContentDir);
        Assert.InRange(db.Wants.Count, 10, 16);
        Assert.Contains(db.Wants, d => d.Target == "none" && d.Weights.Base > 0);
        foreach (string kind in new[] { "none", "person", "place", "item" })
            Assert.Contains(db.Wants, d => d.Target == kind);
    }

    /// <summary>
    /// Every want has its plain words and its beat, the beat has a second-person form for when the
    /// player wants it, and plain words that say "their" or "they" have a "_you" form that does not.
    /// </summary>
    [Fact]
    public void EveryWantHasLocTextInBothPersons()
    {
        ContentDb db = ContentDb.Load(ContentDir);
        foreach (WantDef d in db.Wants)
        {
            Assert.True(db.Loc.Has(d.TextKey), $"{d.Id}: missing {d.TextKey}");
            Assert.True(db.Loc.Has(d.BeatKey), $"{d.Id}: missing {d.BeatKey}");
            Assert.True(db.Loc.Has(d.BeatKey + "_you_a"), $"{d.Id}: missing {d.BeatKey}_you_a, used when the player wants it");
            Assert.StartsWith("{a} ", db.Loc.Get(d.BeatKey), StringComparison.Ordinal);
            Assert.DoesNotMatch(ThirdPerson, db.Loc.Get(d.BeatKey + "_you_a"));
            if (ThirdPerson.IsMatch(db.Loc.Get(d.TextKey)))
            {
                Assert.True(db.Loc.Has(d.TextKey + "_you"), $"{d.Id}: '{db.Loc.Get(d.TextKey)}' needs a {d.TextKey}_you form for the player");
                Assert.DoesNotMatch(ThirdPerson, db.Loc.Get(d.TextKey + "_you"));
            }
            bool aboutSomeone = d.Target == "person";
            Assert.Equal(aboutSomeone, db.Loc.Get(d.TextKey).Contains("{target}", StringComparison.Ordinal));
            Assert.Equal(aboutSomeone, db.Loc.Get(d.BeatKey).Contains("{target}", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Weights add, so a want meant for one kind of person must take all its weight from that axis
    /// (ADR-0021): whatever a cast member's secondary trait, only the otherworlder can want to go home
    /// to Earth, and only those the sun burns can want to walk in daylight again.
    /// </summary>
    [Fact]
    public void WantsMeantForOneKindOfPersonStayWithThem()
    {
        ContentDb db = ContentDb.Load(ContentDir);
        WantDef home = db.WantById("find_home"), daylight = db.WantById("daylight");
        foreach (CastDef c in db.Cast)
        {
            bool burns = db.SpeciesById(c.Species).DaylightBurnPerHour > 0;
            foreach (TraitDef secondary in db.Traits.Where(t => t.Id != c.SignatureTrait))
            {
                string[] traits = { c.SignatureTrait, secondary.Id };
                Assert.Equal(c.Otherworlder, Return.Sim.Systems.Growth.Weight(home, traits, c.Species, c.Role, c.Otherworlder) > 0);
                Assert.Equal(burns, Return.Sim.Systems.Growth.Weight(daylight, traits, c.Species, c.Role, c.Otherworlder) > 0);
            }
        }
    }

    /// <summary>No dead content: across a couple of hundred worlds, every kind of want is somebody's nature.</summary>
    [Fact]
    public void EveryWantIsSomeonesNatureInSomeWorld()
    {
        ContentDb db = ContentDb.Load(ContentDir);
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        for (ulong seed = 1; seed <= 200; seed++)
            foreach (CastDef c in db.Cast) drawn.Add(Return.Sim.WorldGen.NatureOf(db, seed, c.Key).WantType);
        Assert.Equal(db.Wants.Select(d => d.Id).OrderBy(i => i, StringComparer.Ordinal), drawn.OrderBy(i => i, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("\"place\": \"shelter\"", "\"place\": \"moon\"", "moon")]
    [InlineData("\"protective\": 40", "\"brave\": 40", "brave")]
    [InlineData("\"item\": \"berries\"", "\"item\": \"bread\"", "bread")]
    [InlineData("\"vampire\": 36", "\"dragon\": 36", "dragon")]
    [InlineData("\"pick\": \"worst_chemistry\"", "\"pick\": \"loudest\"", "loudest")]
    [InlineData("\"beatKey\": \"event.growth.want_trust\"", "\"beatKey\": \"event.growth.want_nobody\"", "event.growth.want_nobody")]
    public void ABadWantFailsAtLoadNamingFileAndValue(string from, string to, string named)
    {
        string dir = CopyContent();
        string wants = Path.Combine(dir, "wants.json");
        string text = File.ReadAllText(wants);
        Assert.Contains(from, text);
        File.WriteAllText(wants, text.Replace(from, to, StringComparison.Ordinal));
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(dir));
        Assert.Contains("wants.json", ex.Message);
        Assert.Contains(named, ex.Message);
    }

    private const string Keys = "\"id\": \"x\", \"textKey\": \"want.x.text\", \"beatKey\": \"event.growth.want_x\", ";

    /// <summary>The schema's target rules hold: a pick only for a person want, a place only for a place want, an item only for an item want.</summary>
    [Theory]
    [InlineData(true, "\"target\": \"person\", \"pick\": \"best_chemistry\", \"weights\": { \"traits\": { \"anxious\": 3 } }")]
    [InlineData(true, "\"target\": \"place\", \"place\": \"hearth\", \"weights\": { \"base\": 1, \"otherworlder\": -5 }")]
    [InlineData(false, "\"target\": \"none\", \"pick\": \"best_chemistry\", \"weights\": {}")]
    [InlineData(false, "\"target\": \"person\", \"weights\": {}")]
    [InlineData(false, "\"target\": \"place\", \"weights\": {}")]
    [InlineData(false, "\"target\": \"item\", \"item\": \"berries\", \"place\": \"shelter\", \"weights\": {}")]
    [InlineData(false, "\"target\": \"somewhere\", \"weights\": {}")]
    [InlineData(false, "\"target\": \"none\", \"weights\": { \"base\": -1 }")]
    [InlineData(false, "\"target\": \"none\", \"weights\": { \"luck\": 5 }")]
    public void TheSchemaHoldsTheTargetRules(bool valid, string rest)
    {
        JsonSchema schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(ContentDir, "schemas", "wants.schema.json")));
        using JsonDocument doc = JsonDocument.Parse("[{ " + Keys + rest + " }]");
        Assert.Equal(valid, schema.Evaluate(doc.RootElement).IsValid);
    }

    [Fact]
    public void SomeoneAloneMustStillBeAbleToWantSomething()
    {
        string dir = CopyContent();
        string wants = Path.Combine(dir, "wants.json");
        File.WriteAllText(wants, Regex.Replace(File.ReadAllText(wants), "\"base\": \\d+", "\"base\": 0"));
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(dir));
        Assert.Contains("wants.json", ex.Message);
    }

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
}
