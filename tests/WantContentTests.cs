using System.Text.RegularExpressions;
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
