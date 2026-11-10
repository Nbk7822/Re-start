using System.Text.Json;
using Return.Runner;
using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using Return.Sim.Systems.Jobs;

namespace Return.Tests;

/// <summary>The culture engine (ADR-0023): its own Dice stream, the deed log, and counting culture stories in a soak.</summary>
public class CultureEngineTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static SimEvent Mark(World w, string key, params int[] actors) => w.Log.Emit(w, SystemTag.Incident, key, actors, Array.Empty<long>());

    /// <summary>
    /// Culture rolls draw only from the Culture stream, so they never shift another system's sequence
    /// (ADR-0004), and culture code never calls a helper that draws from another stream.
    /// </summary>
    [Fact]
    public void CultureCodeDrawsOnlyFromTheCultureStream()
    {
        string sim = Path.Combine(TestPaths.Root, "sim");
        var cultureFiles = Directory.GetFiles(Path.Combine(sim, "Systems", "Culture"), "*.cs").ToList();
        string jobs = Path.Combine(sim, "Systems", "Jobs", "CultureJobs.cs");
        if (File.Exists(jobs)) cultureFiles.Add(jobs);
        Assert.NotEmpty(cultureFiles);
        var problems = new List<string>();
        var system = new System.Text.RegularExpressions.Regex("DiceSystem\\.([A-Za-z0-9]+)");
        foreach (string file in cultureFiles)
        {
            string text = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match m in system.Matches(text))
                if (m.Groups[1].Value != "Culture") problems.Add($"{Path.GetFileName(file)} draws from DiceSystem.{m.Groups[1].Value}");
            foreach (string helper in new[] { "Places.Near(", "JobRunner.RollAccident(", "IncidentSystem.EdgeTile(" })
                if (text.Contains(helper, StringComparison.Ordinal)) problems.Add($"{Path.GetFileName(file)} calls {helper}, which draws from another stream");
        }
        foreach (string file in Directory.EnumerateFiles(sim, "*.cs", SearchOption.AllDirectories))
        {
            if (cultureFiles.Contains(file) || file.EndsWith("DrawIds.cs", StringComparison.Ordinal)) continue;
            string text = File.ReadAllText(file);
            foreach (string id in new[] { "DiceId.FauxPas", "DiceId.GuestOffer", "DiceId.OmenRead", "DiceSystem.Culture" })
                if (text.Contains(id, StringComparison.Ordinal)) problems.Add($"{Path.GetFileName(file)} uses {id} outside the culture code");
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void TheCultureStreamStandsApart()
    {
        Rng a = Rng.FromSeed(3), b = Rng.FromSeed(3);
        for (int i = 0; i < 20; i++) a.Dice[DiceSystem.Culture].Range(DiceId.OmenRead, 0, 1000);
        Assert.Equal(b.Dice[DiceSystem.Social].Range(DiceId.SocialInsult, 0, 1000), a.Dice[DiceSystem.Social].Range(DiceId.SocialInsult, 0, 1000));
        Assert.Equal(0, a.Dice[DiceSystem.Growth].Draws);
    }

    [Fact]
    public void DeedsAreRecordedWhereTheyHappen()
    {
        World w = WorkTests.Only(1, "you", "mira", "nia");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), nia = Cast(w, "nia");
        SimEvent why = Mark(w, "event.test.why", mira.Id);
        Debt promise = Relations.FormDebt(w, you, mira, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100);
        SimEvent broken = Relations.Break(w, promise, why.Id);
        Debt care = Relations.FormDebt(w, mira, you, DebtKind.Care, 1, why.Id);
        SimEvent repaid = Relations.Repay(w, care, why.Id);
        Debt promise2 = Relations.FormDebt(w, you, nia, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100);
        SimEvent kept = Relations.Repay(w, promise2, why.Id);
        Stock.Add(w, "berries", 1, w.Map.StartStockpile.Center);
        w.Reservations.Add(new Reservation { CharacterId = mira.Id, Count = 1 });
        SimEvent taken = NeedsJobs.TakeReservedMeal(w, nia, mira, "berries")!;

        Assert.Equal(new[]
        {
            (DeedKind.OathBroken, broken.Id, you.Id),
            (DeedKind.DebtRepaid, repaid.Id, mira.Id),
            (DeedKind.OathKept, kept.Id, you.Id),
            (DeedKind.MealTaken, taken.Id, nia.Id),
        }, w.Culture.Deeds.Select(d => (d.Kind, d.EventId, d.DoerId)));

        w.Tick += (CultureTuning.DeedMemoryHours + 1) * w.Content.World.TicksPerHour;
        Omens.Prune(w);
        Assert.Empty(w.Culture.Deeds);
    }

    [Fact]
    public void CultureLinksNameTheFamilyOfTheFirstCultureEvent()
    {
        Assert.Equal("guest_bread", Soak.CultureLink("event.culture.guest_bread.shared"));
        Assert.Equal("omen_anger", Soak.CultureLink("event.culture.omen_anger"));
        Assert.Equal("word_spread", Soak.CultureLink("event.culture.word_spread"));
        Assert.Equal("", Soak.CultureLink("event.social.insulted"));
    }

    [Fact]
    public void SoakCountsCultureStories()
    {
        string dir = Path.Combine(Path.GetTempPath(), "return-soak-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var results = new[]
            {
                new Soak.WorldResult(1, 40, new[] { "Culture+Social", "Needs+Social" }, new[] { "event.social.grudge_formed", "event.social.left_camp" }, null,
                    new[] { "omen_anger", "" }, new[] { "vampire", "human" }),
                new Soak.WorldResult(2, 30, new[] { "Culture+Needs+Social" }, new[] { "event.social.debt_repaid" }, null, new[] { "debt_of_honour" }, Array.Empty<string>()),
                new Soak.WorldResult(3, 30, new[] { "Needs+Social" }, new[] { "event.social.left_camp" }, null, new[] { "" }, new[] { "vampire" }),
            };
            var options = new SoakOptions { Worlds = 3, Years = 1, OutDir = dir, Parallel = 1 };
            SoakSummary s = Soak.Summarize(options, dir, results, TimeSpan.FromSeconds(1));
            Assert.Equal(2, s.CultureStories);
            Assert.Equal(50.0, s.CultureStoryShare);
            Assert.Equal(2, s.CultureWorlds);
            Assert.Equal(new[] { new Tally("debt_of_honour", 1), new Tally("omen_anger", 1) }, s.ByCultureLink);
            Assert.Equal(new[] { new Tally("vampire", 2), new Tally("human", 1) }, s.DeparturesAndDeathsBySpecies);

            string path = Path.Combine(dir, "summary.json");
            Soak.WriteSummaryJson(s, path);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = doc.RootElement;
            Assert.Equal(2, root.GetProperty("cultureStories").GetInt32());
            Assert.Equal(50.0, root.GetProperty("cultureStoryShare").GetDouble());
            Assert.Equal(2, root.GetProperty("cultureWorlds").GetInt32());
            Assert.Equal(1, root.GetProperty("byCultureLink").GetProperty("omen_anger").GetInt32());
            Assert.Equal(2, root.GetProperty("departuresAndDeathsBySpecies").GetProperty("vampire").GetInt32());

            var printed = new StringWriter();
            Soak.Report(s, TestContent.Db.Loc, printed);
            Assert.Contains("Stories with culture or faith in them: 2 (50.0% of all), in 2 of 3 worlds.", printed.ToString());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
