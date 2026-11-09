using System.Text.RegularExpressions;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
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
    public void OneArcIsOneStory()
    {
        // arrival -> went without -> promise broken -> grudge -> left: three persistent changes, one story
        var events = new[]
        {
            E(1, SystemTag.Incident, new[] { 1 }, Array.Empty<long>()),
            E(2, SystemTag.Needs, new[] { 1 }, new long[] { 1 }),
            E(3, SystemTag.Social, new[] { 2, 1 }, new long[] { 2 }, persistent: true),
            E(4, SystemTag.Social, new[] { 1, 2 }, new long[] { 3 }, persistent: true),
            E(5, SystemTag.Social, new[] { 1 }, new long[] { 4 }, persistent: true),
            // a separate arc that branches from the same arrival is still its own story
            E(6, SystemTag.Needs, new[] { 1 }, new long[] { 1 }),
            E(7, SystemTag.Work, new[] { 3, 1 }, new long[] { 6 }, persistent: true),
        };
        IReadOnlyList<Story> stories = StoryAnalyzer.Find(events, Named);
        Assert.Equal(new long[] { 5, 7 }, stories.Select(s => s.TerminalEventId));
        Assert.Equal(new long[] { 1, 2, 3, 4, 5 }, stories[0].Chain);
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
    public void WorkPrioritiesShowOrdersBeforeTheyApply()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        int mira = w.Characters.First(c => c.CastKey == "mira").Id;
        WorkPriorityState forage = StateApi.WorkPriorities(w, mira).Single(p => p.WorkId == "forage");
        Assert.Equal(1, forage.Priority);
        Assert.False(forage.Pending);
        Assert.Equal(Return.Sim.Systems.JobPicker.PrioritizedWork.Count, StateApi.WorkPriorities(w, mira).Count);

        s.Enqueue(new SetWorkPriority(w.Tick, mira, "forage", 0));
        forage = StateApi.WorkPriorities(w, mira).Single(p => p.WorkId == "forage");
        Assert.Equal(0, forage.Priority);
        Assert.True(forage.Pending);

        s.Step();
        forage = StateApi.WorkPriorities(w, mira).Single(p => p.WorkId == "forage");
        Assert.Equal(0, forage.Priority);
        Assert.False(forage.Pending);
    }

    [Fact]
    public void PolicyShowsAnOrderBeforeItApplies()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        Assert.Equal(RationPolicy.Hungriest, StateApi.Policy(w).Effective);
        s.Enqueue(new SetRationPolicy(w.Tick, RationPolicy.PromisesFirst));
        PolicyState p = StateApi.Policy(w);
        Assert.Equal(RationPolicy.Hungriest, p.Current);
        Assert.Equal(RationPolicy.PromisesFirst, p.Effective);
        Assert.True(p.Pending);
        Assert.Equal("promised meals come first", p.Text);
        s.Step();
        Assert.False(StateApi.Policy(w).Pending);
        Assert.Equal(RationPolicy.PromisesFirst, w.Policy);
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
    public void EventTextSpeaksToThePlayerInSecondPerson()
    {
        World w = WorldGen.Create(TestContent.Db, 2);
        int you = w.PlayerId, mira = w.Characters.First(c => c.CastKey == "mira").Id, ilex = w.Characters.First(c => c.CastKey == "ilex").Id;
        Assert.Equal("you", w.Get(you).CastKey);
        string Say(string key, params EventArg[] args) => EventText.Describe(w, new SimEvent { Key = key, Args = args.ToList() });

        Assert.Equal("You were hurt in an accident while working.", Say("event.incident.work_accident", EventArg.Char("a", you)));
        Assert.Equal("Mira was hurt in an accident while working.", Say("event.incident.work_accident", EventArg.Char("a", mira)));
        Assert.Equal("You kept your promise to Ilex.", Say("event.social.promise_kept", EventArg.Char("a", you), EventArg.Char("b", ilex)));
        Assert.Equal("Mira tended your wounds.", Say("event.work.cared_for", EventArg.Char("a", mira), EventArg.Char("b", you)));
        Assert.Equal("You tended Mira's wounds.", Say("event.work.cared_for", EventArg.Char("a", you), EventArg.Char("b", mira)));
        Assert.Equal("You, hungry yourself, refused to give Ilex blood.", Say("event.social.blood_refused_hungry", EventArg.Char("a", ilex), EventArg.Char("b", you)));
        Assert.Equal("Ilex asked you for blood.", Say("event.social.blood_request", EventArg.Char("a", ilex), EventArg.Char("b", you)));
        Assert.Equal("Your priority for Forage is now 2.", Say("event.world.work_priority", EventArg.Char("a", you), EventArg.Key("work", "work.forage.name"), EventArg.Num("n", 2)));
    }

    [Fact]
    public void EventTextUsesTheSingularForOne()
    {
        World w = WorldGen.Create(TestContent.Db, 2);
        string Say(int n) => EventText.Describe(w, new SimEvent { Key = "event.incident.stolen", Args = { EventArg.Num("n", n), EventArg.Item("item", "berries") } });
        Assert.Equal("A thief made off with 1 berry.", Say(1));
        Assert.Equal("A thief made off with 3 berries.", Say(3));
    }

    /// <summary>Every second-person variant belongs to a real key and only names placeholders that key's events carry.</summary>
    [Fact]
    public void SecondPersonVariantsMatchTheirBaseKeys()
    {
        LocTable loc = TestContent.Db.Loc;
        var placeholder = new Regex("\\{([a-z_]+)\\}");
        int variants = 0;
        foreach (string key in loc.Keys.Where(k => k.Contains("_you_", StringComparison.Ordinal)))
        {
            int at = key.LastIndexOf("_you_", StringComparison.Ordinal);
            string baseKey = key[..at], role = key[(at + 5)..];
            Assert.True(loc.Has(baseKey), $"{key} has no base key {baseKey}");
            var baseArgs = placeholder.Matches(loc.Get(baseKey)).Select(m => m.Groups[1].Value).ToHashSet();
            Assert.Contains(role, baseArgs);
            foreach (Match m in placeholder.Matches(loc.Get(key)))
                Assert.True(baseArgs.Contains(m.Groups[1].Value), $"{key} uses {{{m.Groups[1].Value}}}, which {baseKey} does not");
            variants++;
        }
        Assert.True(variants >= 20);
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
