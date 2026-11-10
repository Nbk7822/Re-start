using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>Growth through the text-state API, the inspector's words and the story log (fixed decision 12, ADR-0021).</summary>
public class GrowthViewTests
{
    private static ContentDb Db => TestContent.Db;

    public GrowthViewTests()
    {
        Ui.Init(Db.Loc);
    }

    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static SimEvent Mark(World w, string key, params int[] actors) => w.Log.Emit(w, SystemTag.Incident, key, actors, Array.Empty<long>());
    private static string TraitName(string id) => Db.Loc.Get(Db.TraitById(id).NameKey);
    private static string NotHeld(Character c) => Db.Traits.Select(t => t.Id).First(t => !c.Traits.Contains(t));

    private static void Wants(Character c, string type, WantTargetKind kind = WantTargetKind.None, int targetId = 0, string targetRef = "")
    {
        c.Growth.Want = new Want { TypeId = type, TargetKind = kind, TargetId = targetId, TargetRef = targetRef };
        c.Growth.Arc = new Arc();
    }

    [Fact]
    public void StateApiReadsTheWantInPlainWords()
    {
        World w = WorkTests.Only(1, "you", "mira", "oren");
        Character you = Cast(w, "you"), mira = Cast(w, "mira");

        Wants(mira, "protect_person", WantTargetKind.Person, you.Id);
        GrowthState g = StateApi.Growth(w, mira.Id);
        WantState want = g.Want!;
        Assert.Equal("protect_person", want.TypeId);
        Assert.Equal("To protect you", want.Text);
        Assert.Equal(WantTargetKind.Person, want.TargetKind);
        Assert.Equal(you.Id, want.TargetId);
        Assert.Equal("You", want.Target);
        Assert.Equal(WantStatus.Active, want.Status);
        Assert.Equal("held", want.StatusLabel);
        Assert.Equal(ArcStage.Want, g.Arc!.Stage);
        Assert.Equal("not yet tested", g.Arc.StageLabel);

        Wants(you, "keep_word");
        Wants(mira, "keep_word");
        Assert.Equal("To keep your word", StateApi.Growth(w, you.Id).Want!.Text);
        Assert.Equal("To keep their word", StateApi.Growth(w, mira.Id).Want!.Text);

        Wants(mira, "own_roof", WantTargetKind.Place, targetRef: "shelter");
        WantState roof = StateApi.Growth(w, mira.Id).Want!;
        Assert.Equal(("To have a roof of their own", "shelter", "Shelter"), (roof.Text, roof.TargetRef, roof.Target));

        Wants(mira, "full_stores", WantTargetKind.Item, targetRef: "berries");
        WantState stores = StateApi.Growth(w, mira.Id).Want!;
        Assert.Equal(("To see the stores full before winter", "berries"), (stores.Text, stores.Target));
    }

    [Fact]
    public void StateApiReadsWhereEachTraitCameFrom()
    {
        World w = WorkTests.Only(1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        GrowthState born = StateApi.Growth(w, mira.Id);
        Assert.Equal(mira.Traits, born.Traits.Select(t => t.TraitId));
        Assert.Equal("Protective", born.Traits[0].Trait);
        Assert.All(born.Traits, t =>
        {
            Assert.Equal(TraitOriginKind.Generated, t.Origin);
            Assert.Equal("born with it", t.Text);
            Assert.Equal(0, t.CauseEvent);
        });

        w.Tick = 11L * Db.World.TicksPerDay + 5L * Db.World.TicksPerHour;
        SimEvent downed = w.Log.Emit(w, SystemTag.Combat, "event.combat.downed", new[] { oren.Id }, Array.Empty<long>(), null, EventArg.Char("a", oren.Id));
        string now = NotHeld(mira);
        Growth.ChangeTrait(w, mira, mira.Traits[1], now, downed.Id);
        TraitOriginState changed = StateApi.Growth(w, mira.Id).Traits[1];
        Assert.Equal(now, changed.TraitId);
        Assert.Equal(TraitName(now), changed.Trait);
        Assert.Equal(TraitOriginKind.Mutated, changed.Origin);
        Assert.Equal(downed.Id, changed.CauseEvent);
        Assert.Equal(w.Tick, changed.Tick);
        Assert.Equal("since day 12: Oren was struck down by the thief.", changed.Text);
    }

    [Fact]
    public void TheCharacterReadCarriesGrowthAndSerialisesIt()
    {
        var s = Simulation.Create(Db, 1);
        s.Step();
        World w = s.World;
        Character you = Cast(w, "you");
        CharacterState c = StateApi.Character(w, you.Id);
        Assert.Equal(StateApi.Json(StateApi.Growth(w, you.Id)), StateApi.Json(c.Growth));
        Assert.NotNull(c.Growth.Want);
        string json = StateApi.Json(c);
        Assert.Contains("\"growth\": {", json);
        Assert.Contains("\"stage\": \"Want\"", json);
        Assert.Contains("\"origin\": \"Generated\"", json);

        GrowthState wren = StateApi.Growth(w, Cast(w, "wren").Id);
        Assert.Null(wren.Want);
        Assert.Null(wren.Arc);
        Assert.Equal(2, wren.Traits.Count);
    }

    [Fact]
    public void TheInspectorShowsEachTraitsOriginAndTheWant()
    {
        World w = WorkTests.Only(1, "you", "mira", "oren");
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        Wants(mira, "protect_person", WantTargetKind.Person, you.Id);
        GrowthState g = StateApi.Growth(w, mira.Id);
        Assert.Equal($"Protective — born with it\n{TraitName(mira.Traits[1])} — born with it", ViewText.TraitsText(g));
        Assert.Equal("To protect you", ViewText.WantLine(g));

        Growth.AdvanceArc(w, mira, ArcStage.Obstacle, Mark(w, "event.test.trouble", mira.Id).Id);
        Assert.Equal("To protect you (facing an obstacle)", ViewText.WantLine(StateApi.Growth(w, mira.Id)));

        Assert.Equal("None", ViewText.WantLine(StateApi.Growth(w, Cast(w, "wren").Id)));
        var nobody = new GrowthState(Array.Empty<TraitOriginState>(), null, null);
        Assert.Equal("None", ViewText.TraitsText(nobody));
        Assert.Equal("None", ViewText.WantLine(nobody));
    }

    /// <summary>Growth beats are notable: the story log's "What happened" shows each person's want forming, but none is a story by itself.</summary>
    [Fact]
    public void TheStoryLogShowsEachWantForming()
    {
        Simulation sim = Return.Runner.StoryLog.Simulate(Db, 1, 1);
        World w = sim.World;
        string happened = WhatHappened(sim);
        foreach (Character c in w.Characters.Where(c => c.Named))
            Assert.True(Told(happened, w, w.Log.Get(c.Growth.Want!.FormedEvent)!), $"{c.CastKey}'s want is missing from What happened");
        Assert.DoesNotContain(Return.Runner.StoryLog.Analyze(w).Stories, s => w.Log.Get(s.TerminalEventId)!.System == SystemTag.Growth);
    }

    /// <summary>
    /// Starters who want the same thing form their wants in the same moment, and the log folds them like
    /// any run of identical steps ("You want to keep the camp safe." then "… and the same with Mira,
    /// Oren"), so each of them is still told.
    /// </summary>
    [Fact]
    public void StartersWhoShareAWantAreFoldedAndEachIsNamed()
    {
        var sim = Simulation.Create(Db, 1);
        World w = sim.World;
        foreach (Character c in w.Members())
            c.Growth.Want = new Want { TypeId = "keep_camp_safe", Status = WantStatus.Active };
        sim.Step();
        string happened = WhatHappened(sim);
        Character you = Cast(w, "you");
        Assert.Contains(Line(w, w.Log.Get(you.Growth.Want!.FormedEvent)!), happened);
        foreach (Character c in w.Members().Where(c => c.Id != you.Id))
        {
            SimEvent beat = w.Log.Get(c.Growth.Want!.FormedEvent)!;
            Assert.DoesNotContain(Line(w, beat), happened);
            Assert.True(Told(happened, w, beat), $"{c.CastKey}'s want is not in the fold");
        }
    }

    private static string WhatHappened(Simulation sim)
    {
        var text = new StringWriter();
        Return.Runner.StoryLog.Write(sim, text);
        string log = text.ToString();
        return log[log.IndexOf("What happened\n", StringComparison.Ordinal)..log.IndexOf("Stories\n", StringComparison.Ordinal)];
    }

    /// <summary>An event's own line under "What happened".</summary>
    private static string Line(World w, SimEvent e) => $"  {EventText.When(w, e.Tick)} — {EventText.Describe(w, e)}\n";

    /// <summary>
    /// Whether "What happened" tells this beat: on its own line, or folded into the line that follows the
    /// same beat for someone else ("… and the same with Mira, Oren, …").
    /// </summary>
    private static bool Told(string happened, World w, SimEvent beat)
    {
        if (happened.Contains(Line(w, beat), StringComparison.Ordinal)) return true;
        string fold = Db.Loc.Get("log.story.repeat");
        string lead = fold[..fold.IndexOf("{names}", StringComparison.Ordinal)];
        string name = EventText.ResolveMid(w, "c:" + beat.Actors[0].ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach (SimEvent other in w.Log.All)
        {
            if (other.Id == beat.Id || other.Key != beat.Key || other.Tick != beat.Tick) continue;
            string first = Line(w, other);
            int at = happened.IndexOf(first, StringComparison.Ordinal);
            if (at < 0) continue;
            string next = happened[(at + first.Length)..].Split('\n')[0];
            if (next.Contains(lead, StringComparison.Ordinal) && next.Contains(name, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
