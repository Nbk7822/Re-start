using System.Text.RegularExpressions;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Events;
using Return.Sim.Stories;

namespace Return.Tests;

public class StoryAnalyzerTests
{
    private static SimEvent E(long id, SystemTag sys, int[] actors, long[] causes, bool persistent = false) => new()
    {
        Id = id,
        Tick = id * 10,
        System = sys,
        Key = "event.test",
        Actors = actors.ToList(),
        Causes = causes.ToList(),
        Effects = persistent ? new List<Effect> { new() { Kind = EffectKind.GrudgeFormed, A = actors[0] } } : new List<Effect>(),
    };

    private static bool Named(int id) => id < 100;

    [Fact]
    public void FindsThreeEventTwoSystemChain()
    {
        var events = new[]
        {
            E(1, SystemTag.Incident, new[] { 1 }, Array.Empty<long>()),
            E(2, SystemTag.Needs, new[] { 1 }, new long[] { 1 }),
            E(3, SystemTag.Social, new[] { 2, 1 }, new long[] { 2 }, persistent: true),
        };
        Story s = Assert.Single(StoryAnalyzer.Find(events, Named));
        Assert.Equal(3, s.TerminalEventId);
        Assert.Equal(new long[] { 1, 2, 3 }, s.Chain);
        Assert.Equal(new[] { SystemTag.Incident, SystemTag.Needs, SystemTag.Social }, s.Systems);
        Assert.Equal(new[] { 1, 2 }, s.Characters);
    }

    [Fact]
    public void RejectsSingleSystemChain()
    {
        var events = new[]
        {
            E(1, SystemTag.Social, new[] { 1 }, Array.Empty<long>()),
            E(2, SystemTag.Social, new[] { 1 }, new long[] { 1 }),
            E(3, SystemTag.Social, new[] { 1 }, new long[] { 2 }, persistent: true),
        };
        Assert.Empty(StoryAnalyzer.Find(events, Named));
    }

    [Fact]
    public void RejectsChainWithoutNamedCharacter()
    {
        var events = new[]
        {
            E(1, SystemTag.Incident, new[] { 500 }, Array.Empty<long>()),
            E(2, SystemTag.Needs, new[] { 500 }, new long[] { 1 }),
            E(3, SystemTag.Social, new[] { 501 }, new long[] { 2 }, persistent: true),
        };
        Assert.Empty(StoryAnalyzer.Find(events, Named));
    }

    [Fact]
    public void RejectsTwoEventChain()
    {
        var events = new[]
        {
            E(1, SystemTag.Incident, new[] { 1 }, Array.Empty<long>()),
            E(2, SystemTag.Social, new[] { 1 }, new long[] { 1 }, persistent: true),
        };
        Assert.Empty(StoryAnalyzer.Find(events, Named));
    }

    [Fact]
    public void OneStoryPerTerminalUsingTheLongerPath()
    {
        var events = new[]
        {
            E(1, SystemTag.Incident, new[] { 1 }, Array.Empty<long>()),
            E(2, SystemTag.Needs, new[] { 1 }, new long[] { 1 }),
            E(3, SystemTag.Work, new[] { 1 }, new long[] { 2 }),
            E(4, SystemTag.Social, new[] { 1 }, new long[] { 1, 3 }, persistent: true),
        };
        Story s = Assert.Single(StoryAnalyzer.Find(events, Named));
        Assert.Equal(new long[] { 1, 2, 3, 4 }, s.Chain);
    }

    [Fact]
    public void PrefersAMultiSystemChainOverALongerSingleSystemOne()
    {
        var events = new[]
        {
            E(1, SystemTag.Social, new[] { 1 }, Array.Empty<long>()),
            E(2, SystemTag.Social, new[] { 1 }, new long[] { 1 }),
            E(3, SystemTag.Social, new[] { 1 }, new long[] { 2 }),
            E(4, SystemTag.Incident, new[] { 1 }, Array.Empty<long>()),
            E(5, SystemTag.Needs, new[] { 1 }, new long[] { 4 }),
            E(6, SystemTag.Social, new[] { 1 }, new long[] { 3, 5 }, persistent: true),
        };
        Story s = Assert.Single(StoryAnalyzer.Find(events, Named));
        Assert.Equal(new long[] { 4, 5, 6 }, s.Chain);
    }
}

public class StateApiTests
{
    [Fact]
    public void CharacterStateJsonHasNeedsAndRelations()
    {
        World w = WorldGen.Create(TestContent.Db, 2);
        Character you = w.Characters.First(c => c.Otherworlder);
        string json = StateApi.Json(StateApi.Character(w, you.Id));
        Assert.Contains("\"needs\"", json);
        Assert.Contains("\"opinions\"", json);
        Assert.Contains("\"debts\"", json);
        Assert.Contains("\"name\": \"You\"", json);
        Assert.DoesNotContain("⟦", json);
    }

    [Fact]
    public void FactionLocationAndIncidentReads()
    {
        var s = Simulation.Create(TestContent.Db, 2);
        s.Enqueue(new SetRationPolicy(5, RationPolicy.SelfFirst));
        s.Advance(10);
        World w = s.World;
        FactionState camp = StateApi.Faction(w);
        Assert.Equal(3, camp.Members.Count);
        Assert.Equal(TestContent.Db.World.StartBerries, camp.Berries);
        Assert.Equal(3, camp.RoofTotal);
        LocationState hearth = StateApi.Location(w, w.Map.Hearth.X, w.Map.Hearth.Y);
        Assert.Contains(hearth.Contents, t => t.Contains("hearth", StringComparison.OrdinalIgnoreCase));
        IncidentState inc = StateApi.Incident(w, w.PolicyEvent);
        Assert.Contains("you eat first", inc.Text);
        Assert.DoesNotContain("⟦", StateApi.Json(camp) + StateApi.Json(hearth) + StateApi.Json(inc));
    }

    [Fact]
    public void EventTextResolvesNames()
    {
        World w = WorldGen.Create(TestContent.Db, 2);
        int mira = w.Characters.First(c => c.CastKey == "mira").Id;
        var e = new SimEvent { Key = "event.world.work_priority", Args = { EventArg.Char("a", mira), EventArg.Key("work", "work.forage.name"), EventArg.Num("n", 1) } };
        Assert.Equal("Mira's priority for Forage is now 1.", EventText.Describe(w, e));
    }

    [Fact]
    public void LocCoversKeysUsedInCode()
    {
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        var literal = new Regex("\"((?:event|job|need|status|terrain|debt|debt_status|place|time|name|policy|faction|log)\\.[a-z0-9_.]+)\"");
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestPaths.Root, "sim"), "*.cs", SearchOption.AllDirectories))
            foreach (Match m in literal.Matches(File.ReadAllText(file)))
                if (!m.Groups[1].Value.EndsWith(".", StringComparison.Ordinal)) keys.Add(m.Groups[1].Value);
        foreach (JobKind k in Enum.GetValues<JobKind>()) keys.Add("job." + Names.Snake(k.ToString()));
        foreach (CharStatus s in Enum.GetValues<CharStatus>()) keys.Add("status." + Names.Snake(s.ToString()));
        foreach (TerrainKind t in Enum.GetValues<TerrainKind>()) keys.Add("terrain." + Names.Snake(t.ToString()));
        foreach (DebtKind d in Enum.GetValues<DebtKind>()) keys.Add("debt." + Names.Snake(d.ToString()));
        foreach (DebtStatus d in Enum.GetValues<DebtStatus>()) keys.Add("debt_status." + Names.Snake(d.ToString()));
        var missing = keys.Where(k => !TestContent.Db.Loc.Has(k)).ToList();
        Assert.True(missing.Count == 0, "Missing from loc/en.json: " + string.Join(", ", missing));
    }
}
