using System.Text.Json;
using System.Text.RegularExpressions;
using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Events;

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

    // ---- Art pass (ADR-0009 update): poses, light, gore and looks are derived from sim state ----

    [Theory]
    [InlineData(CharStatus.Member, false, false, null, 0, true, false, Pose.Walk)]
    [InlineData(CharStatus.Member, false, false, JobKind.Forage, 0, true, false, Pose.Walk)]
    [InlineData(CharStatus.Member, false, false, JobKind.Forage, 1, false, false, Pose.Crouch)]
    [InlineData(CharStatus.Member, false, false, JobKind.Care, 1, false, false, Pose.Crouch)]
    [InlineData(CharStatus.Member, false, false, JobKind.RequestBlood, 1, false, false, Pose.Crouch)]
    [InlineData(CharStatus.Member, false, false, JobKind.GatherTimber, 1, false, false, Pose.Swing)]
    [InlineData(CharStatus.Member, false, false, JobKind.RepairRoof, 1, false, false, Pose.Stand)]
    [InlineData(CharStatus.Member, false, false, JobKind.RepairRoof, 2, false, false, Pose.Swing)]
    [InlineData(CharStatus.Member, false, false, JobKind.Fight, 0, false, false, Pose.Swing)]
    [InlineData(CharStatus.Member, false, false, JobKind.Fight, 0, true, false, Pose.Walk)]
    [InlineData(CharStatus.Member, false, false, JobKind.Eat, 0, false, false, Pose.Sit)]
    [InlineData(CharStatus.Member, false, false, JobKind.GiveBlood, 1, false, false, Pose.Sit)]
    [InlineData(CharStatus.Member, false, false, JobKind.Socialize, 1, false, false, Pose.Talk)]
    [InlineData(CharStatus.Member, false, false, JobKind.Wander, 0, false, true, Pose.Sit)]
    [InlineData(CharStatus.Member, false, false, JobKind.Wander, 0, false, false, Pose.Stand)]
    [InlineData(CharStatus.Member, false, false, JobKind.KeepWatch, 1, false, false, Pose.Stand)]
    [InlineData(CharStatus.Member, false, true, JobKind.Sleep, 1, false, false, Pose.Sleep)]
    [InlineData(CharStatus.Member, true, false, JobKind.Fight, 0, true, false, Pose.Downed)]
    [InlineData(CharStatus.Dead, false, false, null, 0, false, false, Pose.Dead)]
    [InlineData(CharStatus.Hostile, false, false, JobKind.Raid, 0, false, true, Pose.Stand)]
    public void PoseComesFromSimState(CharStatus status, bool downed, bool asleep, JobKind? job, int stage, bool moving, bool nearHearth, Pose expected)
    {
        Assert.Equal(expected, Poses.For(new PoseInput(status, downed, asleep, job, stage, moving, nearHearth)));
    }

    [Fact]
    public void PeopleFaceWhomTheyTalkTo()
    {
        Simulation sim = Simulation.Create(Content, 1);
        World w = sim.World;
        Character you = w.Characters.First(c => c.CastKey == "you");
        Character mira = w.Characters.First(c => c.CastKey == "mira");
        mira.Pos = new Return.Sim.Core.Point(you.Pos.X + 2, you.Pos.Y);
        you.Facing = Facing.W;

        you.Job = new Job { Kind = JobKind.Socialize, TargetId = mira.Id, Stage = 1 };
        Assert.Equal(Facing.E, Poses.FacingOf(w, you, moving: false));
        Assert.Equal(Facing.W, Poses.FacingOf(w, you, moving: true));

        // A chat a moment ago turns them toward the other; long after, they face where they last stepped.
        you.Job = null;
        int tph = Content.World.TicksPerHour;
        you.Thoughts.Add(new Thought { DefId = "chatted", AboutId = mira.Id, ExpiresTick = w.Tick + 4 * tph });
        Assert.Equal(Facing.E, Poses.FacingOf(w, you, moving: false));
        sim.Advance(Poses.TalkTurnTicks + 1);
        you.Pos = new Return.Sim.Core.Point(mira.Pos.X - 2, mira.Pos.Y);
        you.Job = null;
        you.Facing = Facing.W;
        Assert.Equal(Facing.W, Poses.FacingOf(w, you, moving: false));
    }

    [Fact]
    public void PeopleSittingByTheFireFaceIt()
    {
        Simulation sim = Simulation.Create(Content, 1);
        World w = sim.World;
        Character oren = w.Characters.First(c => c.CastKey == "oren");
        Return.Sim.Core.Point hearth = w.Map.Hearth;
        oren.Job = null;
        oren.Thoughts.Clear();
        oren.Facing = Facing.W;

        oren.Pos = new Return.Sim.Core.Point(hearth.X, hearth.Y + 1);
        Assert.Equal(Pose.Sit, Poses.For(w, oren, moving: false));
        Assert.Equal(Facing.N, Poses.FacingOf(w, oren, moving: false));
        oren.Pos = new Return.Sim.Core.Point(hearth.X - Poses.HearthSitRange, hearth.Y);
        Assert.Equal(Facing.E, Poses.FacingOf(w, oren, moving: false));
        Assert.Equal(Facing.W, Poses.FacingOf(w, oren, moving: true));

        // Out of the fire's reach they stand, facing the way they last stepped.
        oren.Pos = new Return.Sim.Core.Point(hearth.X - Poses.HearthSitRange - 2, hearth.Y);
        Assert.Equal(Pose.Stand, Poses.For(w, oren, moving: false));
        Assert.Equal(Facing.W, Poses.FacingOf(w, oren, moving: false));
    }

    [Fact]
    public void WorkersHoldTheirTools()
    {
        var c = new Character { Status = CharStatus.Member, Job = new Job { Kind = JobKind.GatherTimber, Stage = 1 } };
        Assert.Equal(HeldTool.Axe, Poses.ToolFor(c, Pose.Swing));
        c.Job = new Job { Kind = JobKind.RepairRoof, Stage = 2 };
        Assert.Equal(HeldTool.Hammer, Poses.ToolFor(c, Pose.Swing));
        c.Job = new Job { Kind = JobKind.Fight };
        Assert.Equal(HeldTool.Club, Poses.ToolFor(c, Pose.Swing));
        Assert.Equal(HeldTool.None, Poses.ToolFor(c, Pose.Walk));
        var thief = new Character { Status = CharStatus.Hostile };
        Assert.Equal(HeldTool.Knife, Poses.ToolFor(thief, Pose.Walk));
        Assert.Equal(HeldTool.None, Poses.ToolFor(thief, Pose.Downed));
    }

    [Theory]
    [InlineData(Facing.E)]
    [InlineData(Facing.W)]
    public void BlowsAndReachesLandInFrontOfThePerson(Facing facing)
    {
        // Seen from the side, a swinging arm (felling, mending, fighting) keeps the hand in front of
        // the body all through the blow, as do a crouch, sitting, talking and carrying.
        float d = facing == Facing.E ? 1f : -1f;
        const float arm = 11.5f;
        for (int i = 0; i < 400; i++)
        {
            float time = i * Poses.SwingSeconds / 100f;
            foreach (float seed in new[] { 0f, 1.37f, 9.59f })
            {
                foreach (Pose pose in new[] { Pose.Swing, Pose.Crouch, Pose.Sit, Pose.Talk })
                {
                    float hand = Poses.HandX(Poses.SideArmAngle(Poses.SideArmReach(pose, 0f, time, seed, carrying: false), facing), arm);
                    Assert.True(hand * d > 0f, $"{pose} facing {facing} at {time:F2}s: hand x {hand:F2}");
                }
                float carry = Poses.HandX(Poses.SideArmAngle(Poses.SideArmReach(Pose.Walk, time, time, seed, carrying: true), facing), arm);
                Assert.True(carry * d > 0f, "carrying: hand behind the body");
            }
        }

        // A blow raises the tool above the shoulder (more than a right angle) and strikes down low.
        var reaches = Enumerable.Range(0, 200).Select(i => Poses.SideArmReach(Pose.Swing, 0f, i * Poses.SwingSeconds / 200f, 0f, false)).ToList();
        Assert.True(reaches.Max() > MathF.PI / 2f + 0.3f && reaches.Min() < 0.6f);
    }

    [Fact]
    public void DaylightFollowsTheClock()
    {
        WorldDef def = Content.World;
        DaylightState noon = Daylight.At(12.5f, def, raining: false, winter: false);
        DaylightState dusk = Daylight.At(def.DuskHour, def, raining: false, winter: false);
        DaylightState midnight = Daylight.At(0f, def, raining: false, winter: false);
        Assert.True(noon.Ambient.Luma > dusk.Ambient.Luma && dusk.Ambient.Luma > midnight.Ambient.Luma);
        Assert.True(dusk.Ambient.R > dusk.Ambient.B, "dusk is warm");
        Assert.True(midnight.Ambient.B > midnight.Ambient.R, "night is blue");
        Assert.Equal(0f, noon.Night, 3);
        Assert.Equal(1f, midnight.Night, 3);
        Assert.InRange(dusk.Night, 0.2f, 0.8f);

        // Smooth: no visible jump from one in-game minute to the next, all day round.
        Rgb last = Daylight.At(0, def, false).Ambient;
        int minutes = def.HoursPerDay * 60;
        for (int i = 1; i <= minutes; i++)
        {
            Rgb now = Daylight.At((long)i * def.TicksPerHour / 60, def, false).Ambient;
            Assert.True(MathF.Abs(now.R - last.R) < 0.02f && MathF.Abs(now.G - last.G) < 0.02f && MathF.Abs(now.B - last.B) < 0.02f,
                $"jump at minute {i}: {last} -> {now}");
            last = now;
        }

        Assert.True(Daylight.At(12.5f, def, raining: true, winter: false).Ambient.Luma < noon.Ambient.Luma, "rain darkens");
        Assert.True(noon.SunX == 0 || noon.SunHeight > 0.9f);
        Assert.True(Daylight.At(def.DawnHour + 1f, def, false, false).SunX > 0.5f, "morning sun in the east");
        Assert.True(Daylight.At(def.DuskHour - 1f, def, false, false).SunX < -0.5f, "evening sun in the west");
        Assert.Equal(0f, midnight.SunHeight);
    }

    [Fact]
    public void RainNeverMakesTheEveningDarkerThanNight()
    {
        WorldDef def = Content.World;
        float dryMidnight = Daylight.At(0f, def, raining: false, winter: false).Ambient.Luma;
        float rainyDusk = Daylight.At(def.DuskHour + 0.9f, def, raining: true, winter: false).Ambient.Luma;
        float rainyMidnight = Daylight.At(0f, def, raining: true, winter: false).Ambient.Luma;
        Assert.True(rainyDusk >= dryMidnight, $"a rainy dusk ({rainyDusk:F3}) is darker than a dry midnight ({dryMidnight:F3})");
        Assert.True(rainyMidnight >= 0.9f * dryMidnight, "rain should grey the night only a little");
        Assert.True(rainyMidnight < dryMidnight, "but it still greys it");
    }

    [Fact]
    public void DawnAndDuskComeFromWorldJson()
    {
        WorldDef def = Content.World;
        var lateDusk = new WorldDef
        {
            TicksPerHour = def.TicksPerHour, HoursPerDay = def.HoursPerDay, DaysPerSeason = def.DaysPerSeason,
            SeasonsPerYear = def.SeasonsPerYear, DawnHour = def.DawnHour, DuskHour = def.DuskHour + 2,
        };
        float hour = def.DuskHour + 0.5f;
        Assert.True(Daylight.At(hour, lateDusk, false, false).Ambient.Luma > Daylight.At(hour, def, false, false).Ambient.Luma + 0.2f);
        Assert.True(Daylight.IsWinter((long)def.TicksPerSeason * (def.SeasonsPerYear - 1), def));
        Assert.False(Daylight.IsWinter(0, def));
    }

    [Fact]
    public void GoreFollowsTheThiefFightWithoutTouchingTheSim()
    {
        // Seed 34: the first night-thief fight in which a defender is struck, found from the sim itself
        // so the test follows the history when the cast changes (it was day 27, near (16, 15), before ADR-0022).
        (long start, float fx, float fy) = FirstBloodyFight(34);
        Simulation watched = Simulation.Create(Content, 34), plain = Simulation.Create(Content, 34);
        watched.Advance(start);
        plain.Advance(start);
        var gore = new GoreLog();
        for (int t = 0; t < 500; t++)
        {
            watched.Step();
            plain.Step();
            gore.Observe(watched.World);
        }
        Assert.Equal(plain.StateHash(), watched.StateHash());

        var blood = gore.Decals.Where(d => d.Kind is DecalKind.Splatter or DecalKind.Drops).ToList();
        Assert.NotEmpty(blood);
        Assert.Contains(blood, d => MathF.Abs(d.X - fx) <= 2.5f && MathF.Abs(d.Y - fy) <= 2.5f);

        // The same history always leaves the same marks.
        var again = new GoreLog();
        Simulation replay = Simulation.Create(Content, 34);
        replay.Advance(start);
        for (int t = 0; t < 500; t++)
        {
            replay.Step();
            again.Observe(replay.World);
        }
        Assert.Equal(gore.Decals.Select(Describe), again.Decals.Select(Describe));
    }

    /// <summary>A tick 200 before the first thief fight in which a defender loses health, and where that defender stood (tile centre).</summary>
    private static (long Start, float X, float Y) FirstBloodyFight(ulong seed)
    {
        Simulation sim = Simulation.Create(Content, seed);
        World w = sim.World;
        var health = new Dictionary<int, int>();
        while (w.Tick < w.Content.World.TicksPerYear)
        {
            sim.Step();
            foreach (Character c in w.Characters)
            {
                bool fighting = c.Status == CharStatus.Member && c.Job is { Kind: JobKind.Fight };
                if (fighting && health.TryGetValue(c.Id, out int before) && before - c.Health >= 40)
                {
                    SimEvent fight = w.Log.All.Last(e => e.Key == "event.combat.fight");
                    return (Math.Max(0, fight.Tick - 200), c.Pos.X + 0.5f, c.Pos.Y + 0.5f);
                }
                health[c.Id] = c.Health;
            }
        }
        throw new InvalidOperationException($"seed {seed} has no fight in which a defender is struck");
    }

    [Fact]
    public void GoreDoesNotDependOnHowManyTicksAFrameRuns()
    {
        // Seed 34's thief fight: blows are health drops between looks, so a gore log that looked only
        // once a frame would merge blows, thin the drip trail and misplace marks on a long frame.
        long Start = FirstBloodyFight(34).Start;
        const long Span = 1_500;
        Simulation perTick = Simulation.Create(Content, 34), perFrame = Simulation.Create(Content, 34);
        perTick.Advance(Start);
        perFrame.Advance(Start);
        var everyTick = new GoreLog();
        var framed = new GoreLog();
        everyTick.Observe(perTick.World);
        framed.Observe(perFrame.World);
        for (long t = 0; t < Span; t++)
        {
            perTick.Step();
            everyTick.Observe(perTick.World);
        }

        // Frames of very different lengths (up to TickClock.MaxTicksPerFrame), each followed by the
        // map's own look in MapView._Process, which must then add nothing.
        int[] frames = { 1, 2, 3, TickClock.MaxTicksPerFrame, 37, 0, 120, 7 };
        long done = 0;
        for (int i = 0; done < Span; i++)
        {
            long n = Math.Min(frames[i % frames.Length], Span - done);
            ViewStepper.Run(perFrame, n, framed.Observe);
            framed.Observe(perFrame.World);
            done += n;
        }

        Assert.Equal(perTick.StateHash(), perFrame.StateHash());
        Assert.Contains(everyTick.Decals, d => d.Kind is DecalKind.Splatter or DecalKind.Drops);
        Assert.Equal(everyTick.Decals.Select(Describe), framed.Decals.Select(Describe));
    }

    [Fact]
    public void TheViewStepsTheSimOnlyThroughTheWitnessedLoop()
    {
        string host = File.ReadAllText(Path.Combine(TestPaths.Root, "game", "scripts", "SimHost.cs"));
        Assert.DoesNotContain("Sim.Step(", host, StringComparison.Ordinal);
        Assert.DoesNotContain(".Advance(", host.Replace("Clock.Advance(", "", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(host, @"ViewStepper\.Run\(Sim, [^;]+, _witness\)").Count);
    }

    [Fact]
    public void ADeathByWoundsLeavesABodyInItsBlood()
    {
        Simulation sim = Simulation.Create(Content, 1);
        World w = sim.World;
        var gore = new GoreLog();
        gore.Observe(w);
        Character mira = w.Characters.First(c => c.CastKey == "mira");
        Character oren = w.Characters.First(c => c.CastKey == "oren");
        Return.Sim.Core.Point miraAt = mira.Pos;

        Return.Sim.Systems.CombatSystem.Hurt(w, mira, 2000, 0, "event.combat.died_wounds", Return.Sim.Events.SystemTag.Combat);
        Return.Sim.Systems.Lifecycle.Die(w, oren, Return.Sim.Events.SystemTag.Needs, "event.needs.died_starving", 0);
        sim.Step();
        gore.Observe(w);

        Assert.Equal(CharStatus.Dead, mira.Status);
        Assert.Equal(DeathKind.Wounds, gore.RemainsOf(w, mira)!.Value.Kind);
        Assert.Equal(DeathKind.Wasted, gore.RemainsOf(w, oren)!.Value.Kind);
        Assert.Null(gore.RemainsOf(w, w.Characters.First(c => c.CastKey == "you")));
        Decal pool = gore.Decals.Single(d => d.Kind == DecalKind.Pool);
        Assert.True(pool.Lasting);
        Assert.Equal(miraAt.X + 0.5f, pool.X, 0.2f);
        Assert.Equal(miraAt.Y + 0.5f, pool.Y, 0.2f);
        Assert.Contains(gore.Decals, d => d.Kind == DecalKind.Splatter);
        Assert.DoesNotContain(gore.Decals, d => MathF.Abs(d.X - (oren.Pos.X + 0.5f)) < 0.4f && MathF.Abs(d.Y - (oren.Pos.Y + 0.5f)) < 0.4f);

        // Days later the splatter has worn away; the pool under the body has dried but stays.
        for (int h = 0; h < (GoreLog.FadeDays + 1) * Content.World.HoursPerDay; h++)
        {
            sim.Advance(Content.World.TicksPerHour);
            gore.Observe(w);
        }
        Assert.DoesNotContain(gore.Decals, d => d.Kind == DecalKind.Splatter && d.Key == pool.Key);
        Decal dried = gore.Decals.Single(d => d.Kind == DecalKind.Pool && d.Key == pool.Key);
        Assert.Equal(0.5f, dried.Wear, 3);
    }

    [Fact]
    public void HungerAndSunDoNotBleed()
    {
        Simulation sim = Simulation.Create(Content, 1);
        World w = sim.World;
        var gore = new GoreLog();
        gore.Observe(w);
        Character you = w.Characters.First(c => c.CastKey == "you");
        var hungry = w.Log.Emit(w, Return.Sim.Events.SystemTag.Needs, "event.needs.starving", new[] { you.Id }, Array.Empty<long>());
        w.Log.Emit(w, Return.Sim.Events.SystemTag.Needs, "event.needs.collapsed", new[] { you.Id }, new[] { hungry.Id });
        gore.Observe(w);
        Assert.Empty(gore.Decals);

        var burn = w.Log.Emit(w, Return.Sim.Events.SystemTag.Needs, "event.needs.sun_burn", new[] { you.Id }, Array.Empty<long>());
        w.Log.Emit(w, Return.Sim.Events.SystemTag.Needs, "event.needs.collapsed", new[] { you.Id }, new[] { burn.Id });
        gore.Observe(w);
        Assert.NotEmpty(gore.Decals);
        Assert.All(gore.Decals, d => Assert.Equal(DecalKind.Scorch, d.Kind));
    }

    [Fact]
    public void WoundsStainClothesOnlyWhenTheyBleed()
    {
        Simulation sim = Simulation.Create(Content, 1);
        World w = sim.World;
        Character you = w.Characters.First(c => c.CastKey == "you");
        you.Health = 350;
        Assert.Equal(0, GoreLog.WoundLevel(w, you)); // weak from hunger: no blood
        you.Thoughts.Add(new Thought { DefId = "wounded", ExpiresTick = w.Tick + 100 });
        Assert.Equal(2, GoreLog.WoundLevel(w, you));
        you.Health = 200;
        Assert.Equal(3, GoreLog.WoundLevel(w, you));
    }

    [Fact]
    public void LooksCoverTheWholeCast()
    {
        LookBook looks = LookBook.Load(Path.Combine(TestPaths.Root, "game", "art", "looks.json"));
        foreach (CastDef cast in Content.Cast)
        {
            Assert.True(looks.Has(cast.Key), "game/art/looks.json has no look for " + cast.Key);
            Look look = looks.For(cast.Key);
            Rgb cloth = look.Cloth ?? Rgb.Parse(cast.Color);
            Assert.True(cloth.Muted().Luma < cloth.Luma, "muted cloth is darker for " + cast.Key);
        }
        Assert.True(looks.Hostile.Hood);
        Assert.Throws<FormatException>(() => LookBook.Parse("{\"people\":{\"x\":{\"hair\":\"red\"}}}"));
        Assert.Throws<FormatException>(() => LookBook.Parse("{\"people\":{\"x\":{\"build\":\"huge\"}}}"));

        // A value of the wrong JSON type is a FormatException too, so the view falls back to the default look.
        foreach (string wrong in new[]
        {
            "{\"people\":{\"x\":{\"beard\":\"yes\"}}}",
            "{\"people\":{\"x\":{\"hair\":123}}}",
            "{\"people\":{\"x\":{\"hairStyle\":false}}}",
            "{\"people\":{\"x\":[]}}",
            "{\"people\":[]}",
            "{\"default\":\"plain\"}",
            "[]",
        })
            Assert.Throws<FormatException>(() => LookBook.Parse(wrong));
    }

    [Fact]
    public void GroundIsWornAroundTheCamp()
    {
        World w = Simulation.Create(Content, 1).World;
        Map m = w.Map;
        float[] wear = GroundWear.Compute(m);
        Assert.Equal(wear, GroundWear.Compute(m));
        var nextToHearth = new Return.Sim.Core.Point(m.Hearth.X + 1, m.Hearth.Y);
        Assert.True(wear[m.Index(nextToHearth)] > 0.5f);
        for (int i = 0; i < wear.Length; i++)
        {
            Assert.InRange(wear[i], 0f, 1f);
            if ((TerrainKind)m.Terrain[i] is TerrainKind.Wall or TerrainKind.Floor or TerrainKind.Water or TerrainKind.Rock) Assert.Equal(0f, wear[i]);
        }
        Assert.Equal(TerrainKind.Door, m.At(GroundWear.DoorOf(m)));
    }

    [Fact]
    public void ArtHashIsStableAndSpread()
    {
        Assert.Equal(ArtHash.Of(1, 2, 3), ArtHash.Of(1, 2, 3));
        Assert.NotEqual(ArtHash.Of(1, 2, 3), ArtHash.Of(3, 2, 1));
        var units = Enumerable.Range(0, 2000).Select(i => ArtHash.Unit(i, 7)).ToList();
        Assert.All(units, u => Assert.InRange(u, 0f, 0.99999994f));
        Assert.InRange(units.Average(), 0.45, 0.55);
        Assert.InRange(ArtHash.Pick(4, 99), 0, 3);
    }

    private static string Describe(Decal d) =>
        $"{d.Kind}:{d.X:F3},{d.Y:F3}:{d.Rotation:F3}:{d.Variant}:{d.Scale:F3}:{d.Tick}:{d.Wear:F4}:{d.Lasting}";
}
