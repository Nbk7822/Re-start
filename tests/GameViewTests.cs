using System.Text.Json;
using System.Text.RegularExpressions;
using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;

namespace Return.Tests;

/// <summary>
/// Tests for the Godot-free half of the view (game/scripts/Ui.cs and game/scripts/Logic/*.cs, linked
/// into this project). Compiling them here also proves they do not depend on Godot.
/// </summary>
public class GameViewTests
{
    private static readonly string ContentDir = Path.Combine(TestPaths.Root, "content");
    private static readonly ContentDb Content = ContentDb.Load(ContentDir);

    public GameViewTests()
    {
        Ui.Init(Content.Loc);
    }

    [Fact]
    public void SpeedsComeFromContent()
    {
        var clock = new TickClock(Content.World.TicksPerSecond, Content.World.SpeedMultipliers);
        Assert.Equal(4, clock.SpeedCount);
        var rates = new List<int>();
        for (int s = 0; s < clock.SpeedCount; s++)
        {
            clock.SetSpeed(s);
            rates.Add(clock.TicksPerSecond);
        }
        Assert.Equal(new[] { 0, 20, 60, 120 }, rates);
    }

    [Fact]
    public void StartsRunningAtFirstSpeed()
    {
        var clock = new TickClock(20, new[] { 1, 3, 6 });
        Assert.False(clock.Paused);
        Assert.Equal(1, clock.Speed);
        Assert.Equal(5, clock.Advance(0.25));
    }

    [Fact]
    public void PausedClockRunsNoTicks()
    {
        var clock = new TickClock(20, new[] { 1, 3, 6 });
        clock.SetSpeed(0);
        Assert.True(clock.Paused);
        Assert.Equal(0, clock.Advance(1.0));
    }

    [Fact]
    public void FractionalTicksCarryOverBetweenFrames()
    {
        var clock = new TickClock(20, new[] { 1, 3, 6 });
        var perFrame = new List<int>();
        for (int i = 0; i < 8; i++) perFrame.Add(clock.Advance(1.0 / 32));
        Assert.Equal(new[] { 0, 1, 0, 1, 1, 0, 1, 1 }, perFrame);
        Assert.Equal(5, perFrame.Sum());
    }

    [Fact]
    public void FrameIsCappedAndExcessIsDropped()
    {
        var clock = new TickClock(20, new[] { 1, 3, 6 });
        clock.SetSpeed(3);
        Assert.Equal(TickClock.MaxTicksPerFrame, clock.Advance(10.0));
        Assert.Equal(400, TickClock.MaxTicksPerFrame);
        Assert.Equal(0, clock.Advance(0.0));
        Assert.Equal(3, clock.Advance(0.025));
    }

    [Fact]
    public void TogglePauseResumesLastSpeed()
    {
        var clock = new TickClock(20, new[] { 1, 3, 6 });
        clock.SetSpeed(3);
        clock.TogglePause();
        Assert.True(clock.Paused);
        clock.TogglePause();
        Assert.Equal(3, clock.Speed);
        Assert.Equal(120, clock.TicksPerSecond);
    }

    [Fact]
    public void InvalidSpeedOrDeltaIsIgnored()
    {
        var clock = new TickClock(20, new[] { 1, 3, 6 });
        clock.SetSpeed(9);
        clock.SetSpeed(-1);
        Assert.Equal(1, clock.Speed);
        Assert.Equal(0, clock.Advance(double.NaN));
        Assert.Equal(0, clock.Advance(-1.0));
        Assert.Equal(0, clock.Advance(double.PositiveInfinity));
        Assert.Equal(20, clock.Advance(1.0));
    }

    [Fact]
    public void EveryEmoteHasAGlyph()
    {
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(Path.Combine(ContentDir, "schemas", "thoughts.schema.json")));
        JsonElement emotes = schema.RootElement.GetProperty("items").GetProperty("properties").GetProperty("emote").GetProperty("enum");
        int checkedCount = 0;
        foreach (JsonElement e in emotes.EnumerateArray())
        {
            string emote = e.GetString()!;
            if (emote == "none")
            {
                Assert.Null(ViewText.EmoteGlyph(emote));
                continue;
            }
            Assert.True(Content.Loc.Has("ui.emote." + emote), "missing loc key ui.emote." + emote);
            string glyph = ViewText.EmoteGlyph(emote)!;
            Assert.Equal(1, glyph.Length);
            checkedCount++;
        }
        Assert.Equal(9, checkedCount);
        Assert.Equal("♥", ViewText.EmoteGlyph("hearts"));
        Assert.Equal("…", ViewText.EmoteGlyph("gloom"));
    }

    [Fact]
    public void MoodMarkerFollowsMostRecentThought()
    {
        Simulation sim = Simulation.Create(Content, 1);
        World w = sim.World;
        Character you = w.Characters.First(c => c.CastKey == "you");
        int tph = Content.World.TicksPerHour;
        Assert.Null(ViewText.MoodMarker(w, you));

        // insulted (48 h, anger) began at tick 100; chatted (4 h, music) began at tick 200.
        you.Thoughts.Add(new Thought { DefId = "chatted", ExpiresTick = 200 + 4 * tph });
        you.Thoughts.Add(new Thought { DefId = "insulted", ExpiresTick = 100 + 48 * tph });
        Assert.Equal("♪", ViewText.MoodMarker(w, you));

        // ate (no emote) is now the most recent thought: no marker.
        you.Thoughts.Add(new Thought { DefId = "ate", ExpiresTick = 300 + 6 * tph });
        Assert.Null(ViewText.MoodMarker(w, you));
    }

    [Fact]
    public void ClockShowsDayTimeAndSeason()
    {
        Simulation sim = Simulation.Create(Content, 1);
        Assert.Equal("Day 1, 00:00 · Spring", ViewText.Clock(sim.World));
        sim.Advance(Content.World.TicksPerSeason + Content.World.TicksPerHour * 14 + 150);
        Assert.Equal("Day 11, 14:30 · Summer", ViewText.Clock(sim.World));
    }

    [Fact]
    public void SpeedLabelsUseLoc()
    {
        Assert.Equal("Pause", ViewText.SpeedLabel(0));
        Assert.Equal("×3", ViewText.SpeedLabel(3));
    }

    [Fact]
    public void PeopleListsMembersWithTheirJob()
    {
        Simulation sim = Simulation.Create(Content, 1);
        IReadOnlyList<ViewText.PersonLine> people = ViewText.People(sim.World);
        Assert.Equal(sim.World.Members().Select(c => c.Id), people.Select(p => p.Id));
        Assert.Equal(new[] { "You — Idle", "Mira — Idle", "Oren — Idle" }, people.Select(p => p.Text));
    }

    [Fact]
    public void ChronicleIsNewestFirstAndCapped()
    {
        Simulation sim = Simulation.Create(Content, 1);
        Assert.Empty(ViewText.Chronicle(sim.World));
        int mira = sim.World.Characters.First(c => c.CastKey == "mira").Id;
        sim.Enqueue(new SetRationPolicy(0, RationPolicy.PromisesFirst));
        for (int i = 1; i <= 34; i++) sim.Enqueue(new SetWorkPriority(i, mira, "forage", 1 + i % 4));
        sim.Enqueue(new SetRationPolicy(40, RationPolicy.SelfFirst));
        sim.Advance(41);

        IReadOnlyList<string> lines = ViewText.Chronicle(sim.World);
        Assert.Equal(ViewText.ChronicleLength, lines.Count);
        Assert.Equal("Day 1, 00:08 — You decided that when food runs short, you eat first.", lines[0]);
        Assert.Equal("Day 1, 00:06 — Mira's priority for Forage is now 3.", lines[1]);
        Assert.DoesNotContain(lines, l => l.Contains("promised meals come first", StringComparison.Ordinal));
    }

    [Fact]
    public void TileLinesShowTerrainContentsAndPeople()
    {
        Simulation sim = Simulation.Create(Content, 1);
        World w = sim.World;
        LocationState hearth = StateApi.Location(w, w.Map.Hearth.X, w.Map.Hearth.Y);
        IReadOnlyList<string> lines = ViewText.TileLines(w, hearth);
        Assert.Contains(hearth.Terrain, new[] { "Grass", "Soil" });
        Assert.Equal($"{hearth.Terrain} ({w.Map.Hearth.X}, {w.Map.Hearth.Y})", lines[0]);
        Assert.Contains("Hearth", lines);

        Character you = w.Characters.First(c => c.CastKey == "you");
        IReadOnlyList<string> here = ViewText.TileLines(w, StateApi.Location(w, you.Pos.X, you.Pos.Y));
        Assert.Contains("You", here);
    }

    [Fact]
    public void RelationLinesReadAsSentences()
    {
        Assert.Equal("Mira: +12", ViewText.OpinionLine(new OpinionState(2, "Mira", 12)));
        Assert.Equal("Nia: -40", ViewText.OpinionLine(new OpinionState(6, "Nia", -40)));
        Assert.Equal("Against Nia (intensity 40): Nia took the meal.",
            ViewText.GrudgeLine(new GrudgeState(1, 6, "Nia", 40, 9, "Nia took the meal.")));
        Assert.Equal("Against Nia (intensity 40)", ViewText.GrudgeLine(new GrudgeState(1, 6, "Nia", 40, 0, "")));

        Simulation sim = Simulation.Create(Content, 1);
        long due = Content.World.TicksPerDay * 2L + Content.World.TicksPerHour * 8L;
        Assert.Equal("Owes Mira a promised meal by Day 3, 08:00 (open)",
            ViewText.DebtLine(sim.World, new DebtState(1, true, 2, "Mira", "a promised meal", 10, "open", due, 0)));
        Assert.Equal("Mira owes care (repaid)",
            ViewText.DebtLine(sim.World, new DebtState(1, false, 2, "Mira", "care", 10, "repaid", 0, 0)));
    }

    [Fact]
    public void UiStringsLiveInTheirOwnLocFile()
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(ContentDir, "loc", "en.ui.json")));
        int count = 0;
        foreach (JsonProperty p in doc.RootElement.EnumerateObject())
        {
            Assert.StartsWith("ui.", p.Name, StringComparison.Ordinal);
            count++;
        }
        Assert.True(count > 0);
    }

    [Fact]
    public void PriorityPickerOffersOneToFourThenNever()
    {
        Assert.Equal(new[] { 1, 2, 3, 4, 0 }, ViewText.PriorityValues);
        Assert.Equal(new[] { "1", "2", "3", "4", "–" }, ViewText.PriorityValues.Select(ViewText.PriorityLabel));
    }

    [Fact]
    public void ChangingYourMindWhilePausedLeavesNoTrace()
    {
        var sim = Simulation.Create(Content, 1);
        World w = sim.World;
        int mira = w.Characters.First(c => c.CastKey == "mira").Id;
        var drafts = new OrderDrafts();
        drafts.Add(new SetRationPolicy(w.Tick, RationPolicy.PromisesFirst));
        drafts.Add(new SetRationPolicy(w.Tick, RationPolicy.Hungriest));
        drafts.Add(new SetWorkPriority(w.Tick, mira, "forage", 3));
        drafts.Add(new SetWorkPriority(w.Tick, mira, "forage", 1));
        Assert.Equal(RationPolicy.Hungriest, drafts.Policy);
        Assert.Equal(1, drafts.Priority(mira, "forage"));
        drafts.Flush(sim);
        Assert.Equal(0, drafts.Count);
        Assert.Empty(w.PendingCommands);
    }

    [Fact]
    public void DraftsGoOutOncePerChangeWhenTimeMoves()
    {
        var sim = Simulation.Create(Content, 1);
        World w = sim.World;
        int mira = w.Characters.First(c => c.CastKey == "mira").Id;
        var drafts = new OrderDrafts();
        drafts.Add(new SetWorkPriority(w.Tick, mira, "forage", 2));
        drafts.Add(new SetWorkPriority(w.Tick, mira, "forage", 0));
        drafts.Add(new SetRationPolicy(w.Tick, RationPolicy.SelfFirst));
        drafts.Flush(sim);
        sim.Step();
        Assert.Equal(new[] { "event.world.work_priority", "event.world.ration_policy" },
            w.Log.All.Where(e => e.Key is "event.world.work_priority" or "event.world.ration_policy").Select(e => e.Key));
        Assert.Equal("Mira will no longer do Forage.", Return.Sim.Api.EventText.Describe(w, w.Log.All.First(e => e.Key == "event.world.work_priority")));
        Assert.Equal(RationPolicy.SelfFirst, w.Policy);
    }

    [Fact]
    public void DraftsForSomeoneWhoLeftAreDropped()
    {
        var sim = Simulation.Create(Content, 1);
        World w = sim.World;
        Character mira = w.Characters.First(c => c.CastKey == "mira");
        var drafts = new OrderDrafts();
        drafts.Add(new SetWorkPriority(w.Tick, mira.Id, "forage", 4));
        mira.Status = CharStatus.Left;
        drafts.Flush(sim);
        Assert.Empty(w.PendingCommands);
    }

    [Fact]
    public void EveryRationPolicyHasAChoiceAndAHint()
    {
        foreach (RationPolicy p in Enum.GetValues<RationPolicy>())
        {
            Assert.DoesNotContain("⟦", ViewText.PolicyOption(p));
            Assert.DoesNotContain("⟦", ViewText.PolicyHint(p));
        }
        Assert.Equal("You eat first", ViewText.PolicyOption(RationPolicy.SelfFirst));
    }

    [Fact]
    public void WorkListShowsWhatEachPersonDoesFirst()
    {
        var sim = Simulation.Create(Content, 1);
        World w = sim.World;
        int mira = w.Characters.First(c => c.CastKey == "mira").Id;
        ViewText.PersonLine line = ViewText.WorkLines(w).Single(l => l.Id == mira);
        Assert.Equal("Mira — first: Forage", line.Text);
        foreach (WorkPriorityState s in StateApi.WorkPriorities(w, mira))
            if (s.Priority == 1) sim.Enqueue(new SetWorkPriority(w.Tick, mira, s.WorkId, 2));
        Assert.Equal("Mira — nothing marked first", ViewText.WorkLines(w).Single(l => l.Id == mira).Text);
    }

    [Fact]
    public void EveryUiKeyTheViewUsesExists()
    {
        var keyLiteral = new Regex("\"(ui\\.[a-z0-9_.]+)\"", RegexOptions.CultureInvariant);
        string scripts = Path.Combine(TestPaths.Root, "game", "scripts");
        var used = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(scripts, "*.cs", SearchOption.AllDirectories))
            foreach (Match m in keyLiteral.Matches(File.ReadAllText(file)))
                if (!m.Groups[1].Value.EndsWith('.')) used.Add(m.Groups[1].Value);
        Assert.NotEmpty(used);
        var missing = used.Where(k => !Content.Loc.Has(k)).ToList();
        Assert.True(missing.Count == 0, "Loc keys used by game/scripts but missing from content/loc: " + string.Join(", ", missing));
    }
}
