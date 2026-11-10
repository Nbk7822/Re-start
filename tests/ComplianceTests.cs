using System.Text.RegularExpressions;
using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;

namespace Return.Tests;

/// <summary>
/// The one rule for who keeps the camp's rules (zones design 5.10, ADR-0025 item 2, ADR-0028): the odds
/// and their reason, the one draw, who judges breakers, and that it never touches the player.
/// </summary>
public class ComplianceTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>A camp member with exactly these traits, no people or faith (axis 0), a fair mood, and this opinion of the player.</summary>
    private static Character Person(World w, string key, int opinionOfYou, params string[] traits)
    {
        Character c = Cast(w, key);
        c.Status = CharStatus.Member;
        c.Traits = traits.ToList();
        c.CultureId = "";
        c.FaithId = "";
        c.Mood = 60;
        c.Opinions.Clear();
        if (opinionOfYou != 0) Relations.ChangeOpinion(w, c, w.PlayerId, opinionOfYou, 0);
        return c;
    }

    [Fact]
    public void AnIndependentGoesTheirOwnWayThreeTimesInTen()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Compliance.Odds odds = Compliance.OddsOf(w, Person(w, "sera", 0, "independent"), w.PlayerId);
        Assert.Equal(300, odds.Permille);
        Assert.Equal(Compliance.OwnWay, odds.Reason);
        Assert.Equal(0, Compliance.OddsOf(w, Person(w, "oren", 0, "oathbound"), w.PlayerId).Permille);
    }

    [Fact]
    public void FeelingsAboutWhoeverSetTheRuleCarryEitherWay()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Compliance.Odds sour = Compliance.OddsOf(w, Person(w, "oren", -60, "practical"), w.PlayerId);
        Assert.Equal(300, sour.Permille);
        Assert.Equal(Compliance.Dislike, sour.Reason);
        Assert.Equal(400, Compliance.OddsOf(w, Person(w, "oren", -100, "practical"), w.PlayerId).Feeling); // capped
        Assert.Equal(0, Compliance.OddsOf(w, Person(w, "sera", 100, "independent"), w.PlayerId).Permille); // won over
        Assert.Equal(100, Compliance.OddsOf(w, Person(w, "sera", 40, "independent"), w.PlayerId).Permille);
    }

    [Fact]
    public void ABlackMoodAndFreeWaysAddTheirOwnReasons()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Character low = Person(w, "nia", 0, "practical");
        low.Mood = SocialSystem.LowMoodBelow - 1;
        Compliance.Odds mood = Compliance.OddsOf(w, low, w.PlayerId);
        Assert.Equal(150, mood.Permille);
        Assert.Equal(Compliance.Mood, mood.Reason);

        // Find a people and faith whose ways lean Free (−2 on the Oathbound–Free axis), and one that holds oaths heavy (+2).
        Character free = Person(w, "wren", 0, "practical"), oath = Person(w, "bram", 0, "practical");
        (string Culture, string Faith)? FirstWith(int axis)
        {
            foreach (var culture in w.Content.Cultures)
                foreach (var faith in w.Content.Faiths)
                {
                    free.CultureId = culture.Id;
                    free.FaithId = faith.Id;
                    free.Devotion = 100;
                    if (Ways.Axis(w, free, EthicAxis.OathboundFree) == axis) return (culture.Id, faith.Id);
                }
            return null;
        }
        (string, string)? oathWays = FirstWith(2);
        (string c, string f)? freeWays = FirstWith(-2);
        Assert.NotNull(freeWays);
        free.CultureId = freeWays!.Value.c;
        free.FaithId = freeWays.Value.f;
        Compliance.Odds ways = Compliance.OddsOf(w, free, w.PlayerId);
        Assert.Equal(100, ways.Permille);
        Assert.Equal(Compliance.Ways, ways.Reason);
        Assert.False(Compliance.KeepsRules(w, free));
        Assert.NotNull(oathWays);
        oath.CultureId = oathWays!.Value.Item1;
        oath.FaithId = oathWays.Value.Item2;
        oath.Devotion = 100;
        Assert.Equal(-100, Compliance.OddsOf(w, oath, w.PlayerId).Culture);
        Assert.True(Compliance.KeepsRules(w, oath));
    }

    [Fact]
    public void TiesGoNatureThenFeelingThenMood()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Assert.Equal(Compliance.OwnWay, Compliance.OddsOf(w, Person(w, "sera", -60, "independent"), w.PlayerId).Reason); // 300 and 300
        Character c = Person(w, "nia", -30, "practical");
        c.Mood = 10;
        Assert.Equal(Compliance.Dislike, Compliance.OddsOf(w, c, w.PlayerId).Reason); // 150 and 150
    }

    [Fact]
    public void ThePlayerAndWhoeverSetTheRuleNeverRoll()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Character you = Cast(w, "you");
        you.Traits = new List<string> { "independent" };
        you.Mood = 0;
        long draws = w.Rng.Dice[DiceSystem.Work].Draws;
        Assert.Equal(0, Compliance.OddsOf(w, you, w.PlayerId).Permille);
        Assert.Null(Compliance.Defies(w, you, w.PlayerId, DiceId.ZoneDefy));
        Character sera = Person(w, "sera", 0, "independent");
        Assert.Equal(0, Compliance.OddsOf(w, sera, sera.Id).Permille); // a delegate's own rule
        Assert.Equal(draws, w.Rng.Dice[DiceSystem.Work].Draws);
    }

    [Fact]
    public void ADefianceIsOneTaggedDrawOnTheWorkStreamAndOnlyWhenItCouldHappen()
    {
        World a = WorldGen.Create(TestContent.Db, 7), b = WorldGen.Create(TestContent.Db, 7);
        DiceStream work = a.Rng.Dice[DiceSystem.Work];
        long before = work.Draws;
        Assert.Null(Compliance.Defies(a, Person(a, "oren", 0, "oathbound"), a.PlayerId, DiceId.ZoneDefy));
        Assert.Equal(before, work.Draws);

        var outcomes = new List<string?>();
        for (int i = 0; i < 40; i++) outcomes.Add(Compliance.Defies(a, Person(a, "sera", 0, "independent"), a.PlayerId, DiceId.ZoneDefy));
        Assert.Equal(before + 40, work.Draws);
        Assert.Equal(DiceId.ZoneDefy, work.Recent[^1]);
        Assert.Contains(Compliance.OwnWay, outcomes);
        Assert.Contains(null, outcomes);
        for (int i = 0; i < 40; i++) Assert.Equal(outcomes[i], Compliance.Defies(b, Person(b, "sera", 0, "independent"), b.PlayerId, DiceId.ZoneDefy));
    }

    [Fact]
    public void RuleKeepersNearbySeeADefiantBreachAndThinkLessOfTheBreaker()
    {
        World w = WorldGen.Create(TestContent.Db, 2);
        Character sera = Person(w, "sera", 0, "independent"), oren = Person(w, "oren", 0, "oathbound"),
            mira = Person(w, "mira", 0, "oathbound"), bram = Person(w, "bram", 0, "oathbound"), nia = Person(w, "nia", 0, "practical");
        Point at = w.Map.Hearth;
        sera.Pos = new Point(at.X + 1, at.Y);
        oren.Pos = new Point(sera.Pos.X + Compliance.WitnessRange, sera.Pos.Y);
        mira.Pos = new Point(sera.Pos.X + Compliance.WitnessRange + 1, sera.Pos.Y);
        bram.Pos = new Point(sera.Pos.X, sera.Pos.Y + 1);
        bram.Asleep = true;
        nia.Pos = new Point(sera.Pos.X, sera.Pos.Y - 1);
        SimEvent breach = w.Log.Emit(w, SystemTag.Work, "event.world.zone_marked", new[] { sera.Id }, Array.Empty<long>());
        List<Character> seen = Compliance.Witness(w, sera, breach);
        Assert.Equal(new[] { oren.Id }, seen.Select(c => c.Id));
        Thought t = Assert.Single(oren.Thoughts, x => x.DefId == "saw_rule_broken");
        Assert.Equal(sera.Id, t.AboutId);
        Assert.Equal(breach.Id, t.CauseEvent);
        Assert.Equal(-4, Relations.Opinion(w, oren.Id, sera.Id));
        Assert.Empty(Compliance.Witness(w, sera, breach, oren.Id));
    }

    /// <summary>Every compliance roll comes from one stream through one door: Compliance.Defies is the only place a rule's defiance is drawn.</summary>
    [Fact]
    public void ComplianceDrawsOnlyFromTheWorkStreamAndOnlyThroughDefies()
    {
        string sim = Path.Combine(TestPaths.Root, "sim");
        string compliance = File.ReadAllText(Path.Combine(sim, "Systems", "Compliance.cs"));
        Assert.Equal(new[] { "DiceSystem.Work" }, Regex.Matches(compliance, @"DiceSystem\.\w+").Select(m => m.Value).Distinct());
        foreach (string file in Directory.EnumerateFiles(sim, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)) continue;
            string text = File.ReadAllText(file);
            string name = Path.GetFileName(file);
            if (name is "DrawIds.cs" or "Compliance.cs") continue;
            foreach (Match m in Regex.Matches(text, @"DiceId\.ZoneDefy"))
            {
                int line = text.LastIndexOf('\n', m.Index);
                string context = text.Substring(Math.Max(0, line), m.Index - Math.Max(0, line));
                Assert.True(context.Contains("Compliance.Defies("), $"{name} draws DiceId.ZoneDefy outside Compliance.Defies");
            }
        }
    }
}
