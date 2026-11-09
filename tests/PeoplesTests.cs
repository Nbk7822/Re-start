using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// The four peoples beyond human and vampire (ADR-0022): each need's mechanics, and for each people
/// a seeded scenario proving one of its story chains can happen.
/// </summary>
public class PeoplesTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static long Hours(World w, int h) => (long)h * w.Content.World.TicksPerHour;

    /// <summary>A simulation where only the named cast members are in camp, fed, rested and near the hearth.</summary>
    private static Simulation Camp(ulong seed, params string[] keys)
    {
        Simulation s = Simulation.Create(TestContent.Db, seed);
        World w = s.World;
        int i = 0;
        foreach (Character c in w.Characters)
        {
            if (!keys.Contains(c.CastKey)) { c.Status = CharStatus.OffMap; continue; }
            c.Status = CharStatus.Member;
            c.Food = Math.Max(c.Food, 900); c.Rest = 900; c.Social = 900; c.Mood = 60;
            if (!w.Map.Passable(c.Pos) || c.Pos == Point.None) c.Pos = new Point(w.Map.Hearth.X - 2 + i, w.Map.Hearth.Y + 2);
            i++;
        }
        w.Arrivals.Clear();
        return s;
    }

    private static Story StoryEndingAt(World w, long terminal) =>
        StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true).Single(s => s.TerminalEventId == terminal);

    // ---- Undead: purpose ----

    [Fact]
    public void TheUndeadNeitherEatNorSleep()
    {
        Simulation s = Camp(1, "ketil", "you");
        World w = s.World;
        Character ketil = Cast(w, "ketil");
        Assert.Equal(1000, ketil.Food);
        s.Advance(3L * w.Content.World.TicksPerDay);
        Assert.Equal(1000, ketil.Food);
        Assert.Equal(1000, ketil.Rest);
        Assert.False(ketil.Asleep);
        Assert.DoesNotContain(w.Log.All, e => e.Actors.Contains(ketil.Id) && e.Key is "event.needs.hungry" or "event.needs.went_without");
        Assert.DoesNotContain(w.Reservations, r => r.CharacterId == ketil.Id);
        Assert.Equal(0, Places.Eaters(w, "berries") - w.Members().Count(c => c.SpeciesId == "human"));

        CharacterState state = StateApi.Character(w, ketil.Id);
        Assert.Equal(new[] { "purpose", "social", "health" }, state.Needs.Select(n => n.Need));
        Assert.Equal("Purpose", state.Needs[0].Label);
    }

    [Fact]
    public void EachUndeadIsBoundToOneWorkByTheirNature()
    {
        var seen = new HashSet<string>();
        for (ulong seed = 1; seed <= 12; seed++)
        {
            NatureNeed a = NatureNeeds.Get(Cast(WorldGen.Create(TestContent.Db, seed), "ketil"), Purpose.NeedId)!;
            NatureNeed b = NatureNeeds.Get(Cast(WorldGen.Create(TestContent.Db, seed), "ketil"), Purpose.NeedId)!;
            Assert.Equal(a.BondWork, b.BondWork);
            Assert.Contains(a.BondWork, TestContent.Db.NeedById(Purpose.NeedId).BondWorks);
            seen.Add(a.BondWork);
        }
        Assert.Equal(TestContent.Db.NeedById(Purpose.NeedId).BondWorks.Count, seen.Count);
    }

    [Fact]
    public void DoingTheBoundWorkRestoresPurposeAndWithoutItTheyFade()
    {
        Simulation s = Camp(1, "ketil");
        World w = s.World;
        Character ketil = Cast(w, "ketil");
        NatureNeed purpose = NatureNeeds.Get(ketil, Purpose.NeedId)!;
        var def = w.Content.NeedById(Purpose.NeedId);
        purpose.Value = 400;
        ketil.Job = new Job { Kind = Enum.Parse<JobKind>(purpose.BondWork.Replace("_", ""), ignoreCase: true), Stage = 1 };
        w.Tick = Hours(w, 1);
        NeedsSystem.Hourly(w);
        Assert.Equal(400 - def.DecayPerHour + def.Param("restorePerHour"), purpose.Value);
        Assert.Contains(ketil.Thoughts, t => t.DefId == "at_purpose");

        ketil.Job = null;
        purpose.Value = def.CriticalBelow - 1;
        int health = ketil.Health;
        w.Tick += Hours(w, 1);
        NeedsSystem.Hourly(w);
        Assert.Equal(health - def.CriticalHealthPerHour, ketil.Health);
        Assert.Contains(ketil.Thoughts, t => t.DefId == def.CriticalThought);
    }

    /// <summary>The story the player causes: setting the work an undead came back for to "never" fades them until they lie down at rest.</summary>
    [Fact]
    public void TakingAnUndeadsWorkAwayFadesThemToRest()
    {
        Simulation s = Camp(3, "ketil");
        World w = s.World;
        Character ketil = Cast(w, "ketil");
        NatureNeed purpose = NatureNeeds.Get(ketil, Purpose.NeedId)!;
        // An hour in, mid-watch (seed 3 binds Ketil to keep_watch): the watch already begun still counts, but the order stands as the cause.
        s.Enqueue(new SetWorkPriority(Hours(w, 1), ketil.Id, purpose.BondWork, 0));
        s.Advance(9L * w.Content.World.TicksPerDay);

        Assert.Equal(CharStatus.Dead, ketil.Status);
        SimEvent death = w.Log.All.Single(e => e.Effects.Any(f => f.Kind == EffectKind.Death && f.A == ketil.Id));
        Assert.Equal("event.needs.died_purpose", death.Key);
        SimEvent order = w.Log.All.Single(e => e.Key == "event.world.work_priority");
        Story story = StoryEndingAt(w, death.Id);
        Assert.Equal(order.Id, story.Chain[0]);
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.purpose_low" && story.Chain.Contains(e.Id));
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.purpose_critical" && story.Chain.Contains(e.Id));
        Assert.Equal(new[] { SystemTag.World, SystemTag.Needs }, story.Systems);
        Assert.Equal("Ketil lay down, their purpose gone, and was at rest at last.", EventText.Describe(w, death));
    }

    [Fact]
    public void LettingThemWorkAgainBringsThemBack()
    {
        Simulation s = Camp(3, "ketil");
        World w = s.World;
        Character ketil = Cast(w, "ketil");
        NatureNeed purpose = NatureNeeds.Get(ketil, Purpose.NeedId)!;
        s.Enqueue(new SetWorkPriority(0, ketil.Id, purpose.BondWork, 0));
        s.Advance(3L * w.Content.World.TicksPerDay);
        Assert.True(purpose.Value < w.Content.NeedById(Purpose.NeedId).LowBelow, $"purpose {purpose.Value}");
        s.Enqueue(new SetWorkPriority(w.Tick, ketil.Id, purpose.BondWork, 3));
        s.Advance(3L * w.Content.World.TicksPerDay);
        Assert.Equal(CharStatus.Member, ketil.Status);
        Assert.True(purpose.Value >= w.Content.NeedById(Purpose.NeedId).LowBelow, $"purpose {purpose.Value}");
    }

    [Fact]
    public void TheLivingAreUneasyAroundTheNewlyDeadAndWarmWithTime()
    {
        World w = WorkTests.Only(1, "ketil", "wren");
        Character ketil = Cast(w, "ketil"), wren = Cast(w, "wren");
        ketil.ArrivedTick = w.Tick;
        Assert.Equal(0, Unease.WarmthPct(w, wren, ketil));
        Assert.Equal(100, Unease.WarmthPct(w, ketil, wren));
        w.Tick += 6L * w.Content.World.TicksPerDay;
        Assert.Equal(50, Unease.WarmthPct(w, wren, ketil));
        w.Tick += 6L * w.Content.World.TicksPerDay;
        Assert.Equal(100, Unease.WarmthPct(w, wren, ketil));

        // A chat at first: no warmth for the living, and unease.
        w.Tick = ketil.ArrivedTick;
        ketil.Pos = wren.Pos with { X = wren.Pos.X + 1 };
        for (int t = 0; t < 4000 && !wren.Thoughts.Any(x => x.DefId == "chatted"); t++)
        {
            w.Tick = ketil.ArrivedTick + t * w.Content.World.SocialEveryTicks;
            SocialSystem.Step(w);
            if (wren.Thoughts.Any(x => x.DefId == "insulted") || ketil.Thoughts.Any(x => x.DefId == "insulted")) { wren.Thoughts.Clear(); ketil.Thoughts.Clear(); wren.Opinions.Clear(); }
        }
        Assert.Contains(wren.Thoughts, t => t.DefId == "uneasy_near_dead" && t.AboutId == ketil.Id);
        Assert.True(Relations.Opinion(w, wren.Id, ketil.Id) < 0);
    }
}
