using System.Text.Json;
using System.Text.RegularExpressions;
using Return.Game;
using Return.Game.Logic;
using Return.Runner;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Snapshots;
using Return.Sim.Systems.Culture;

namespace Return.Tests;

/// <summary>Seeding a world's peoples, faiths and gods (Truths only), and reading them back in plain words (ADR-0023).</summary>
public class CultureSeedTests
{
    private static ContentDb Db => TestContent.Db;
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, SnapshotSerializer.Options);
    private static string People(World w) => string.Join(";", w.Characters.Select(c => $"{c.CastKey}:{c.CultureId}/{c.FaithId}/{c.Devotion}"));

    [Fact]
    public void CultureSeedingIsATruth()
    {
        World a = WorldGen.Create(Db, 21), b = WorldGen.Create(Db, 21);
        Assert.Equal(Json(a.Culture), Json(b.Culture));
        Assert.Equal(People(a), People(b));

        // Seeding again draws no Dice and gives the same book, even after the Dice are reseeded (a rewind).
        string book = Json(a.Culture), people = People(a);
        long[] draws = a.Rng.Dice.Streams.Select(s => s.Draws).ToArray();
        a.Culture = new CultureBook();
        CultureGen.Seed(a);
        Assert.Equal(draws, a.Rng.Dice.Streams.Select(s => s.Draws).ToArray());
        Assert.Equal(book, Json(a.Culture));
        a.Rng.Dice.Reseed(0xBEEF);
        a.Culture = new CultureBook();
        CultureGen.Seed(a);
        Assert.Equal(book, Json(a.Culture));
        Assert.Equal(people, People(a));
    }

    [Fact]
    public void CultureSeedsVary()
    {
        var membersOf = new HashSet<string>();
        var godNames = new Dictionary<string, HashSet<string>>();
        var combos = new HashSet<string>();
        for (ulong seed = 1; seed <= 50; seed++)
        {
            World w = WorldGen.Create(Db, seed);
            foreach (Character c in w.Characters) if (c.CultureId.Length > 0) membersOf.Add(c.CultureId);
            foreach (SeededFaith f in w.Culture.Faiths)
                foreach (SeededGod g in f.Gods)
                {
                    string slot = f.TemplateId + "/" + g.Slot;
                    if (!godNames.TryGetValue(slot, out var names)) godNames[slot] = names = new HashSet<string>();
                    names.Add(g.NameKey);
                }
            foreach (SeededCulture s in w.Culture.Cultures) combos.Add(s.TemplateId + ":" + s.NameKey + ":" + string.Join(",", s.CustomIds));
        }
        Assert.Equal(Db.Cultures.Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal), membersOf.OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(4, godNames.Count);
        Assert.All(godNames.Values, names => Assert.True(names.Count >= 2));
        Assert.True(combos.Count >= 3);
    }

    [Fact]
    public void EveryoneButTheOtherworlderBelongs()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            World w = WorldGen.Create(Db, seed);
            foreach (Character c in w.Characters)
            {
                if (c.Otherworlder)
                {
                    Assert.Equal("", c.CultureId);
                    Assert.Equal("", c.FaithId);
                    Assert.Equal(0, c.Devotion);
                    continue;
                }
                CultureDef culture = Db.CultureById(c.CultureId);
                Assert.InRange(c.Devotion, culture.Devotion.Min, culture.Devotion.Max);
                Assert.NotNull(Ways.FaithById(w, c.FaithId));
            }
        }
    }

    /// <summary>The post-merge path: a new people that no culture names gets a culture through the "*" weights, or evenly if there are none.</summary>
    [Fact]
    public void APeopleNoCultureNamesStillGetsOne()
    {
        string human = File.ReadAllText(Path.Combine(TestPaths.Root, "content", "species.json"));
        string dryad = "[" + human.Substring(human.IndexOf('{'), human.IndexOf('}') - human.IndexOf('{') + 1)
            .Replace("\"id\": \"human\"", "\"id\": \"dryad\"", StringComparison.Ordinal)
            .Replace("species.human.name", "species.dryad.name", StringComparison.Ordinal) + "]";
        (string, string)[] files =
        {
            ("species.json", dryad),
            ("cast.json", "[{ \"key\": \"elm\", \"nameKey\": \"cast.elm.name\", \"species\": \"dryad\", \"signatureTrait\": \"generous\", \"role\": \"forager\" }]"),
            ("loc/en.mod.json", "{ \"species.dryad.name\": \"Dryad\", \"cast.elm.name\": \"Elm\" }"),
        };
        ContentDb db = ContentDb.Load(Path.Combine(TestPaths.Root, "content"), new[] { CultureContentTests.Mod(files) });
        for (ulong seed = 1; seed <= 5; seed++)
        {
            Character elm = Cast(WorldGen.Create(db, seed), "elm");
            Assert.NotEqual("", elm.CultureId);
            Assert.NotEqual("", elm.FaithId);
        }

        string cultures = Regex.Replace(File.ReadAllText(Path.Combine(TestPaths.Root, "content", "cultures.json")),
            "\\{ \"species\": \"\\*\", \"weight\": \\d+ \\}", "{ \"species\": \"*\", \"weight\": 0 }");
        ContentDb noStar = ContentDb.Load(Path.Combine(TestPaths.Root, "content"), new[] { CultureContentTests.Mod(files.Append(("cultures.json", cultures)).ToArray()) });
        Assert.NotEqual("", Cast(WorldGen.Create(noStar, 3), "elm").CultureId);
    }

    [Fact]
    public void SyntheticCharactersCarryTheirCulture()
    {
        World w = WorldGen.Create(Db, 4);
        WorldGen.AddSyntheticCharacters(w, 30);
        foreach (Character c in w.Characters.Skip(10))
        {
            Character template = Cast(w, c.CastKey[..c.CastKey.LastIndexOf('_')]);
            Assert.Equal(template.CultureId, c.CultureId);
            Assert.Equal(template.FaithId, c.FaithId);
            Assert.Equal(template.Devotion, c.Devotion);
        }
    }

    [Fact]
    public void SeedingEmitsNoEvents()
    {
        World w = WorldGen.Create(Db, 8);
        Assert.Empty(w.Log.All);
        Assert.Equal(4, w.Culture.Cultures.Count);
        Assert.Equal(3, w.Culture.Faiths.Count);
    }

    [Fact]
    public void BelongingReadsInPlainWords()
    {
        var digits = new Regex("[0-9]");
        for (ulong seed = 1; seed <= 5; seed++)
        {
            World w = WorldGen.Create(Db, seed);
            BelongingState you = StateApi.Character(w, w.PlayerId).Belonging;
            Assert.Equal("From another world", you.Line);
            Assert.Equal(new[] { "Knows none of the ways of this world yet." }, you.Ways);
            Assert.Empty(you.Gods);

            Character mira = Cast(w, "mira");
            BelongingState b = StateApi.Character(w, mira.Id).Belonging;
            Assert.StartsWith("Of the ", b.Line, StringComparison.Ordinal);
            Assert.Contains(" · follows the ", b.Line, StringComparison.Ordinal);
            Assert.Contains(b.Devotion, new[] { "lapsed", "faithful", "devout" });
            Assert.EndsWith("(" + b.Devotion + ")", b.Line, StringComparison.Ordinal);
            Assert.NotEmpty(b.Ways);
            Assert.NotEmpty(b.Gods);
            foreach (string s in b.Ways.Concat(b.Gods).Append(b.Line))
            {
                Assert.DoesNotMatch(digits, s);
                Assert.DoesNotContain("⟦", s);
            }
            // Culture customs come first, as rules.
            SeededCulture culture = Ways.Culture(w, mira)!;
            Assert.Equal(Db.Loc.Get(Db.CustomById(culture.CustomIds[0]).RuleKey), b.Ways[0]);
        }
    }

    [Fact]
    public void LearnedWaysAreNamedByTheirPeople()
    {
        World w = WorldGen.Create(Db, 2);
        Character you = Cast(w, "you");
        SeededCulture reed = Ways.CultureById(w, "reed")!;
        you.LearnedCustoms.Add(reed.CustomIds[0]);
        BelongingState b = StateApi.Character(w, you.Id).Belonging;
        Assert.Empty(b.Ways);
        Assert.Equal(new[] { "The way of " + Db.Loc.Get(reed.NameKey) + ": no touch at meeting." }, b.Learned);
    }

    [Fact]
    public void CultureAndFaithReads()
    {
        World w = WorldGen.Create(Db, 3);
        IReadOnlyList<CultureState> cultures = StateApi.Cultures(w);
        Assert.Equal(new[] { "hearth", "reed", "ashen", "night" }, cultures.Select(c => c.Id));
        Assert.Equal(w.Characters.Count(c => c.Named && !c.Otherworlder), cultures.Sum(c => c.Members.Count));
        CultureState reed = StateApi.Culture(w, "reed");
        Assert.Equal(new[] { "fear vampires" }, reed.Stances);
        Assert.StartsWith("the ", reed.Name, StringComparison.Ordinal);
        Assert.Equal(3, reed.Ways.Count);
        // A festival's date sits mid-sentence, so the season is in lower case ("the Longest Night (day 8 of winter)").
        var festivals = cultures.SelectMany(c => c.Festivals).Concat(StateApi.Faiths(w).SelectMany(f => f.Ways.Where(x => x.Contains("(day ", StringComparison.Ordinal)))).ToList();
        Assert.NotEmpty(festivals);
        foreach (string f in festivals) Assert.Matches("\\(day [1-9] of (winter|autumn)\\)$", f);

        IReadOnlyList<FaithState> faiths = StateApi.Faiths(w);
        Assert.Equal(new[] { "sworn", "roots", "ember" }, faiths.Select(f => f.Id));
        Assert.Equal(2, faiths[0].Gods.Count);
        foreach (GodState g in faiths.SelectMany(f => f.Gods))
        {
            Assert.Contains(g.Temperament, new[] { "stern", "kindly", "fickle", "distant" });
            Assert.DoesNotContain("⟦", g.Name + g.Epithet + g.Cares);
        }
        Assert.Contains("\"belonging\"", StateApi.Json(StateApi.Character(w, Cast(w, "mira").Id)));
        Assert.Throws<KeyNotFoundException>(() => StateApi.Culture(w, "nope"));
    }

    [Fact]
    public void SacredTreeShowsInTheTooltip()
    {
        World w = WorldGen.Create(Db, 5);
        SacredPlace place = Assert.Single(w.Culture.SacredPlaces);
        Tree tree = w.Map.Trees.Single(t => t.Id == place.TreeId);
        string faith = Db.Loc.Get(Ways.FaithById(w, place.HolderId)!.NameKey);
        Assert.Contains("Tree (sacred to " + faith + ")", StateApi.Location(w, tree.Pos.X, tree.Pos.Y).Contents);
        Assert.DoesNotContain("Tree", StateApi.Location(w, tree.Pos.X, tree.Pos.Y).Contents);
        tree.Felled = true;
        Assert.Contains("Stump (once sacred to " + faith + ")", StateApi.Location(w, tree.Pos.X, tree.Pos.Y).Contents);
    }

    [Fact]
    public void TheSacredTreeIsOneOfTheNearestToTheHearth()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            World w = WorldGen.Create(Db, seed);
            SacredPlace place = Assert.Single(w.Culture.SacredPlaces);
            int n = Db.CustomById(place.CustomId).Arg("nearestTrees");
            List<int> nearest = w.Map.Trees.OrderBy(t => t.Pos.ChebyshevTo(w.Map.Hearth)).ThenBy(t => t.Id).Take(n).Select(t => t.Id).ToList();
            Assert.Contains(place.TreeId, nearest);
        }
    }

    [Fact]
    public void StoryLogListsPeoplesAndFaiths()
    {
        var sim = Simulation.Create(Db, 1);
        var text = new StringWriter();
        StoryLog.Write(sim, text);
        string log = text.ToString();
        Assert.Contains("Peoples and faiths\n------------------\n  Among the ", log);
        Assert.Matches("\n  Followers of the [A-Za-z ]+ honour [A-Z][a-z]+, [^\n]+ \\((stern|kindly|fickle|distant)\\)", log);
        Assert.Contains("  You (Human, from another world): ", log);
        Assert.DoesNotContain("⟦", log);
    }
}

/// <summary>The view's belonging line and tooltip (game/scripts/Logic/ViewText.cs, Godot-free).</summary>
public class CultureViewTests
{
    public CultureViewTests() => Ui.Init(TestContent.Db.Loc);

    [Fact]
    public void InspectorBelongingLine()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        BelongingState b = StateApi.Character(w, w.Characters.First(c => c.CastKey == "mira").Id).Belonging;
        string tip = ViewText.BelongingTooltip(b);
        string[] lines = tip.Split('\n');
        Assert.Equal("Their ways", lines[0]);
        Assert.Equal(b.Ways[0], lines[1]);
        Assert.Contains("Their gods", lines);
        Assert.DoesNotContain("Ways learned here", lines);

        BelongingState you = StateApi.Character(w, w.PlayerId).Belonging;
        Assert.Equal("Their ways\nKnows none of the ways of this world yet.", ViewText.BelongingTooltip(you));
    }

    [Fact]
    public void ChronicleShowsCultureEvents()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Character mira = w.Characters.First(c => c.CastKey == "mira");
        mira.CultureId = "reed";
        Ways.CultureById(w, "reed")!.CustomIds = new List<string> { "no_touch_greeting" };
        mira.Status = CharStatus.Member;
        Character you = w.Characters.First(c => c.Otherworlder);
        w.Rng = Rng.FromSeed(3);
        for (int i = 0; i < 50 && !Greetings.FirstMeeting(w, you, mira); i++)
        {
            you.Met.Clear();
            mira.Met.Clear();
        }
        string line = ViewText.Chronicle(w)[0];
        Assert.Contains("You took Mira by the hand at your first meeting;", line);
        Assert.StartsWith("Day 1, 00:00 — ", line);
    }
}
