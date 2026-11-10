using System.Diagnostics;
using System.Text.RegularExpressions;
using Return.Runner;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Snapshots;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Who you are and Return by Death (the player design §4–§6, ADR-0030): the rebirth Truths, aging and the end of a
/// lifetime, fate knots, the pick, the return and what you keep, lost futures, memories and replay.
/// </summary>
public class FateTests
{
    private const int Day = 7_200, Hour = 300;

    private static Character You(World w) => w.Get(w.PlayerId);
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>Kills you now and steps once, so the step ends in a return; the world after it.</summary>
    private static World DieAndReturn(Simulation sim)
    {
        Assert.True(sim.ForceDeath());
        sim.Step();
        return sim.World;
    }

    // ---- Rebirth ----

    [Fact]
    public void RebirthIsATruth()
    {
        ContentDb db = TestContent.Rolled;
        foreach (ulong seed in new ulong[] { 1, 2, 3, 17, 40 })
        {
            var sim = Simulation.Create(db, seed);
            World w = sim.World;
            string people = Rebirth.PeopleOf(db, seed, "you");
            Assert.Equal(people, You(w).SpeciesId);
            Assert.Equal(people, w.Fate.PeopleId);
            Assert.Equal(Rebirth.GiftOf(db, seed, "you", people), w.Fate.GiftId);
            Assert.Equal(WorldGen.NatureOf(db, seed, "you").WantType, You(w).Growth.Want!.TypeId);
            var ages = w.Characters.Select(c => (c.BirthTick, c.OldAgeTick)).ToList();
            long soul = w.Fate.SoulEndTick;

            // Reseeded Dice and re-drawn arrivals (what a return does) change none of it.
            w.Rng.Dice.Reseed(99);
            WorldGen.RedrawArrivals(w);
            w.Arrivals.Reverse();
            sim.Advance(Day);
            Assert.Equal(people, You(sim.World).SpeciesId);
            Assert.Equal(soul, sim.World.Fate.SoulEndTick);
            Assert.Equal(ages, sim.World.Characters.Take(ages.Count).Select(c => (c.BirthTick, c.OldAgeTick)).ToList());
        }

        // Over 1,000 seeds each people lands within 3 points of its weight.
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (ulong seed = 1; seed <= 1_000; seed++)
        {
            string p = Rebirth.PeopleOf(db, seed, "you");
            counts[p] = counts.GetValueOrDefault(p) + 1;
        }
        int total = db.Rebirth.Peoples.Sum(p => p.Weight);
        foreach (RebirthPeopleDef p in db.Rebirth.Peoples)
        {
            double expected = 100.0 * p.Weight / total, got = counts.GetValueOrDefault(p.Species) / 10.0;
            Assert.True(Math.Abs(expected - got) <= 3, $"{p.Species}: {got}% of 1,000 seeds, expected {expected}% ± 3");
        }
    }

    [Fact]
    public void EveryoneAgesWithinTheirPeoplesFigures()
    {
        ContentDb db = TestContent.Rolled;
        WorldDef def = db.World;
        for (ulong seed = 1; seed <= 30; seed++)
        {
            World w = WorldGen.Create(db, seed);
            foreach (Character c in w.Characters)
            {
                SpeciesDef sp = db.AgesAs(c.SpeciesId);
                int age = Rebirth.AgeYears(w, c, 0);
                Assert.InRange(age, sp.AdultAtYears[0], sp.AdultAtYears[1]);
                if (sp.Ageless) Assert.Equal(0, c.OldAgeTick);
                else Assert.InRange((c.OldAgeTick - c.BirthTick) / def.TicksPerYear, sp.LifespanYears[0], sp.LifespanYears[1]);
            }
            int soulYears = (int)(w.Fate.SoulEndTick / def.TicksPerYear);
            Assert.InRange(soulYears, db.Rebirth.SoulSpanYears[0], db.Rebirth.SoulSpanYears[1]);
        }
    }

    [Fact]
    public void ChosenForTestingIsMarkedAndHonoured()
    {
        var sim = Simulation.Create(TestContent.Rolled, 3, new RebirthChoice("fae", RebirthChoice.NoGift));
        World w = sim.World;
        Assert.Equal("fae", You(w).SpeciesId);
        Assert.Equal("", w.Fate.GiftId);
        Assert.True(w.Fate.Chosen);
        Assert.Contains(You(w).NatureNeeds, n => n.Id == "courtesy");
        Assert.False(Simulation.Create(TestContent.Rolled, 3).World.Fate.Chosen);
        Assert.Throws<ContentException>(() => Simulation.Create(TestContent.Rolled, 3, new RebirthChoice("dragon")));
    }

    [Fact]
    public void YouWakeOnTheFirstTick()
    {
        var sim = Simulation.Create(TestContent.Rolled, 4, new RebirthChoice("vampire"));
        sim.Step();
        SimEvent woke = sim.World.Log.All[0];
        Assert.Equal(Fate.WokeKey, woke.Key);
        Assert.Equal(SystemTag.Fate, woke.System);
        Assert.Equal("You died on Earth, and woke here as a vampire.", EventText.Describe(sim.World, woke));
    }

    /// <summary>
    /// The three guarantees that let rebirth sit under every world (§12.1): the rebirth Truths draw no Dice; the Fate
    /// stream leaves every other stream's sequence alone; and tying knots changes nothing but the run memory.
    /// </summary>
    [Fact]
    public void RebirthChangesNothingWhenHumanAndGiftless()
    {
        // 1. Whoever you are reborn as, creating the world draws the same Dice.
        foreach (ulong seed in new ulong[] { 2, 3, 7 })
        {
            World rolled = WorldGen.Create(TestContent.Rolled, seed);
            World human = WorldGen.Create(TestContent.Rolled, seed, new RebirthChoice("human", RebirthChoice.NoGift));
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(human.Rng.Dice, SnapshotSerializer.Options),
                System.Text.Json.JsonSerializer.Serialize(rolled.Rng.Dice, SnapshotSerializer.Options));
        }

        // 2. Draws from the Fate stream move no other stream: the worlds stay the same.
        Simulation a = Simulation.Create(TestContent.Db, 11), b = Simulation.Create(TestContent.Db, 11);
        for (int i = 0; i < 50; i++) b.World.Rng.Dice[DiceSystem.Fate].Range(DiceId.FateKnotPick, 0, 1000);
        a.Advance(2 * Day);
        b.Advance(2 * Day);
        b.World.Rng.Dice.Streams[(int)DiceSystem.Fate] = a.World.Rng.Dice.Streams[(int)DiceSystem.Fate];
        Assert.Equal(a.StateHash(), b.StateHash());

        // 3. A world that ties knots hashes the same, the run memory aside, as one that ties none.
        Simulation tying = Simulation.Create(TestContent.Db, 12), plain = Simulation.Create(TestContent.Db, 12);
        plain.TieKnots = false;
        tying.Advance(3 * Day);
        plain.Advance(3 * Day);
        Assert.NotEmpty(tying.World.Fate.Knots);
        Assert.Empty(plain.World.Fate.Knots);
        tying.World.Fate = new FateBook();
        plain.World.Fate = new FateBook();
        Assert.Equal(tying.StateHash(), plain.StateHash());
    }

    // ---- The end of a lifetime ----

    /// <summary>The shipped content (you human, giftless) with humans born the year the world begins and living a year.</summary>
    private static ContentDb ShortLived(string rebirth = "")
    {
        var files = new List<(string, string)>
        {
            ("species.json", File.ReadAllText(Path.Combine(TestPaths.Root, "content", "species.json"))
                .Replace("\"adultAtYears\": [18, 22],\n    \"lifespanYears\": [60, 78]", "\"adultAtYears\": [0, 0],\n    \"lifespanYears\": [1, 1]", StringComparison.Ordinal)),
        };
        if (rebirth.Length > 0) files.Add(("rebirth.json", rebirth));
        return ContentDb.Load(Path.Combine(TestPaths.Root, "content"), new[] { FateFixtures.HumanGiftless, CultureContentTests.Mod(files.ToArray()) });
    }

    [Fact]
    public void OldAgeEndsTheRun()
    {
        ContentDb db = ShortLived();
        var sim = Simulation.Create(db, 5);
        Character you = You(sim.World);
        Assert.InRange(you.OldAgeTick, 1, 80L * Day);
        Assert.True(sim.AdvanceTo(81L * Day, 200L * Day) || sim.World.Fate.Ended);
        World w = sim.World;
        Assert.True(w.Fate.Ended);
        Assert.Equal("old_age", w.Fate.EndKind);
        Assert.Equal(0, w.Fate.Returns); // the only death with no return
        SimEvent death = w.Log.All.Last(e => e.Effects.Any(f => f.Kind == EffectKind.Death && f.A == w.PlayerId));
        Assert.Equal(Fate.OldAgeKey, death.Key);
        Assert.Equal("You died of old age.", EventText.Describe(w, death));
        long tick = w.Tick;
        sim.Advance(1_000);
        Assert.Equal(tick, sim.World.Tick); // nothing steps after the end
    }

    [Fact]
    public void TheSoulRunsOut()
    {
        string rebirth = File.ReadAllText(Path.Combine(TestPaths.Root, "content", "rebirth.json"))
            .Replace("\"soulSpanYears\": [40, 50]", "\"soulSpanYears\": [1, 1]", StringComparison.Ordinal);
        ContentDb db = ContentDb.Load(Path.Combine(TestPaths.Root, "content"), new[] { CultureContentTests.Mod(("rebirth.json", rebirth)) });
        var sim = Simulation.Create(db, 6, new RebirthChoice("vampire", RebirthChoice.NoGift)); // ageless: only the soul's span ends it
        Assert.Equal(db.World.TicksPerYear, sim.World.Fate.SoulEndTick);
        // Keep you alive and fed so only the soul can end the run.
        for (int i = 0; i < 300 && !sim.World.Fate.Ended; i++)
        {
            Character you = You(sim.World);
            you.Food = 1000;
            you.Health = 1000;
            you.Bleed = 0;
            sim.Advance(Day / 6);
        }
        World w = sim.World;
        Assert.True(w.Fate.Ended);
        Assert.Equal("soul_spent", w.Fate.EndKind);
        SimEvent end = w.Log.All.Last(e => e.Key == Fate.SoulSpentKey);
        Assert.Equal("Your soul's thread ran out.", EventText.Describe(w, end));
        Assert.True(end.Tick >= w.Fate.SoulEndTick);
    }

    // ---- Knots and the pick ----

    [Fact]
    public void KnotsAreSafe()
    {
        var sim = Simulation.Create(TestContent.Db, 21);
        sim.Advance(Day + 9 * Hour);
        World w = sim.World;
        Assert.True(Fate.Safe(w));
        Character you = You(w);
        void Unsafe(Action make, Action undo)
        {
            make();
            Assert.False(Fate.Safe(w));
            Assert.False(Fate.KnotDue(w, w.Log.All.Count).Due);
            undo();
            Assert.True(Fate.Safe(w));
        }
        Unsafe(() => you.Downed = true, () => you.Downed = false);
        Unsafe(() => you.Bleed = 5, () => you.Bleed = 0);
        int health = you.Health;
        Unsafe(() => you.Health = 399, () => you.Health = health);
        int stage = you.HungerStage;
        Unsafe(() => you.HungerStage = 2, () => you.HungerStage = stage);
        Job? job = you.Job;
        Unsafe(() => you.Job = new Job { Kind = JobKind.Fight }, () => you.Job = job);
        Unsafe(() => you.Job = new Job { Kind = JobKind.Flee }, () => you.Job = job);

        // Every knot a year ties was tied with you safe, at most six a day, two hours apart.
        sim.Advance(20 * Day);
        w = sim.World;
        Assert.Equal(KnotReason.Arrival, w.Fate.Knots[0].Reason);
        for (int i = 0; i < w.Fate.Knots.Count; i++)
        {
            KnotRecord k = w.Fate.Knots[i];
            World knot = sim.KnotWorld(k.Id)!;
            Assert.Equal(k.Tick, knot.Tick);
            if (k.Reason != KnotReason.Arrival) // the arrival knot is tied on the first tick whatever your state
                Assert.True(Fate.Safe(knot), $"knot {k.Id} at tick {k.Tick} was tied while you were not safe");
            if (i > 0) Assert.True(k.Tick - w.Fate.Knots[i - 1].Tick >= 2 * Hour);
        }
        Assert.All(w.Fate.Knots.GroupBy(k => k.Tick / Day), g => Assert.True(g.Count() <= 6));
        Assert.Contains(w.Fate.Knots, k => k.Reason == KnotReason.Turn);
        Assert.Contains(w.Fate.Knots, k => k.Reason == KnotReason.Quiet);
    }

    [Fact]
    public void PickHonoursTheShape()
    {
        WorldDef def = TestContent.Db.World;
        ReturnRulesDef rules = TestContent.Db.Rebirth.Return;
        long death = 12L * Day;
        var knots = new List<KnotRecord> { new() { Id = 1, Tick = 1, Reason = KnotReason.Arrival } };
        for (int d = 2; d <= 6; d++) knots.Add(new KnotRecord { Id = knots.Count + 1, Tick = d * Day, Reason = KnotReason.Quiet });
        for (int h = 0; h < 5; h++) knots.Add(new KnotRecord { Id = knots.Count + 1, Tick = 10L * Day + h * 6 * Hour, Reason = KnotReason.Turn });
        knots[2].Uses = 1; // an old knot used once
        DiceStream dice = DiceRng.FromSeed(42)[DiceSystem.Fate];
        long recentFrom = death - rules.RecentDays * Day;
        var picks = new Dictionary<int, int>();
        int deep = 0;
        for (int i = 0; i < 10_000; i++)
        {
            KnotRecord k = Fate.Pick(knots, death, rules, def, dice)!;
            picks[k.Id] = picks.GetValueOrDefault(k.Id) + 1;
            if (k.Tick < recentFrom) deep++;
        }
        Assert.InRange(deep, 1_350, 1_650); // 150‰ ± 15
        // Old knots weigh the same, a used one half.
        double ratio = picks.GetValueOrDefault(knots[2].Id) / (double)picks.GetValueOrDefault(knots[3].Id);
        Assert.InRange(ratio, 0.4, 0.6);
        // Recent knots weigh by rank: the latest heaviest.
        Assert.True(picks[knots[^1].Id] > picks[knots[^2].Id] && picks[knots[^2].Id] > picks[knots[^5].Id]);
        // Nothing within two hours of the death (but the arrival knot always qualifies).
        var close = new List<KnotRecord> { new() { Id = 1, Tick = 1, Reason = KnotReason.Arrival }, new() { Id = 2, Tick = death - Hour, Reason = KnotReason.Turn } };
        for (int i = 0; i < 200; i++) Assert.Equal(1, Fate.Pick(close, death, rules, def, dice)!.Id);
    }

    // ---- The return ----

    [Fact]
    public void YourDeathReturns()
    {
        var sim = Simulation.Create(TestContent.Db, 5);
        sim.Advance(3 * Day + 2 * Hour);
        long deathTick = sim.World.Tick;
        World dying = sim.World;
        World w = DieAndReturn(sim);
        Assert.NotSame(dying, w);
        Assert.Equal(1, w.Fate.Returns);
        Assert.Equal(1, w.Fate.Timeline);
        LostFuture lost = Assert.Single(w.Fate.LostFutures);
        Assert.Equal(lost.KnotTick, w.Tick);
        KnotRecord knot = w.Fate.Knots.Single(k => k.Tick == w.Tick);
        Assert.True(knot.Reason == KnotReason.Arrival || knot.Tick <= deathTick - 2 * Hour);
        Assert.Equal(1, knot.Uses);
        Assert.Equal(Fate.ForcedDeathKey, lost.Lines[lost.DeathLine].Key);
        Assert.Equal(deathTick, lost.DeathTick);
        Assert.All(w.Fate.Knots, k => Assert.True(k.Tick <= w.Tick)); // knots tied after it are gone...
        Assert.Equal(w.Fate.Knots.Select(k => k.Id), sim.ArchivedKnots); // ...from the archive too
        Assert.NotEqual(sim.KnotWorld(knot.Id)!.Rng.Dice.DiceSeed, w.Rng.Dice.DiceSeed); // Dice reseeded
        Assert.Equal(CharStatus.Member, You(w).Status);
        SimEvent back = w.Log.All.Single(e => e.Key == Fate.ReturnedKey);
        Assert.Equal("You came back, remembering how you died.", EventText.Describe(w, back));
        if (knot.TriggerEvent != 0) Assert.Contains(knot.TriggerEvent, back.Causes);
        sim.Advance(Day); // and the world goes on
        Assert.Equal(CharStatus.Member, You(sim.World).Status);
    }

    [Fact]
    public void ArrivalsReroll()
    {
        int changed = 0, checkedWorlds = 0;
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var sim = Simulation.Create(TestContent.Db, seed);
            sim.Advance(4 * Day);
            World w = DieAndReturn(sim);
            World knot = sim.KnotWorld(w.Fate.Knots.Single(k => k.Tick == w.Tick).Id)!;
            var before = knot.Arrivals.Where(a => !a.Done).Select(a => (a.CharacterId, a.Tick)).ToList();
            var after = w.Arrivals.Where(a => !a.Done).Select(a => (a.CharacterId, a.Tick)).ToList();
            if (before.Count == 0) continue;
            checkedWorlds++;
            if (!before.SequenceEqual(after)) changed++;
            foreach (Arrival a in w.Arrivals.Where(a => !a.Done))
            {
                Assert.True(a.Tick > w.Tick, $"seed {seed}: an arrival re-drawn into the past");
                int day = Calendar.Day(a.Tick, w.Content.World);
                Assert.InRange(day, a.FirstDay, a.LastDay);
                Assert.Contains(Calendar.Hour(a.Tick, w.Content.World), WorldGen.ArrivalHours(w, w.Get(a.CharacterId)));
            }
            // Everyone still to come is still to come, once.
            Assert.Equal(before.Select(x => x.CharacterId).OrderBy(x => x), after.Select(x => x.CharacterId).OrderBy(x => x));
        }
        Assert.True(checkedWorlds >= 10);
        Assert.True(changed >= checkedWorlds - 1, $"arrivals changed in only {changed} of {checkedWorlds} worlds");
    }

    [Fact]
    public void ReturnIsDeterministic()
    {
        Simulation Live(ulong seed)
        {
            var s = Simulation.Create(TestContent.Rolled, seed);
            int mira = Cast(s.World, "mira").Id;
            bool firstDeath = false, secondDeath = false, priority = false;
            while (!(secondDeath && s.World.Fate.Timeline >= 2 && s.World.Tick >= 5 * Day))
            {
                World w = s.World;
                if (w.Tick == 1_000 && w.Fate.Timeline == 0) s.Enqueue(new SetRationPolicy(w.Tick, RationPolicy.SelfFirst));
                if (!firstDeath && w.Fate.Timeline == 0 && w.Tick >= 2 * Day) firstDeath = s.ForceDeath();
                if (!priority && w.Fate.Timeline == 1) { s.Enqueue(new SetWorkPriority(w.Tick + 30, mira, "forage", 1)); priority = true; }
                if (!secondDeath && w.Fate.Timeline == 1 && w.Tick >= 4 * Day) secondDeath = s.ForceDeath();
                s.Step();
            }
            return s;
        }
        Simulation a = Live(8), b = Live(8);
        Assert.True(a.World.Fate.Returns >= 2);
        Assert.Equal(a.StateHash(), b.StateHash());
        Simulation replay = Simulation.Replay(TestContent.Rolled, 8, a.World.Fate, a.World.Tick);
        Assert.Equal(a.World.Tick, replay.World.Tick);
        Assert.Equal(a.StateHash(), replay.StateHash());
    }

    [Fact]
    public void WhatYouKeep()
    {
        var sim = Simulation.Create(TestContent.Db, 9);
        sim.Advance(2 * Day + 4 * Hour);
        World dying = sim.World;
        Character you = You(dying);
        Character mira = Cast(dying, "mira");
        // After every knot: you learn a custom, meet someone, take a grudge, and find your gift.
        string custom = dying.Content.Customs.First(c => c.Kind == "greeting").Id;
        you.LearnedCustoms.Clear();
        you.LearnedCustoms.Add(custom);
        Relations.Meet(dying, you, Cast(dying, "oren"));
        Relations.ChangeOpinion(dying, you, mira.Id, -60, 0);
        SimEvent wrong = dying.Log.Emit(dying, SystemTag.Social, "event.social.insulted", new[] { mira.Id, you.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", mira.Id), EventArg.Char("b", you.Id));
        Relations.FormGrudge(dying, you, mira, 30, wrong.Id);
        dying.Fate.GiftAwakened = true;
        dying.Fate.GiftMastery = 1;
        int items = dying.Items.Sum(i => i.Count), futures = dying.Fate.LostFutures.Count;

        World w = DieAndReturn(sim);
        KnotRecord knot = w.Fate.Knots.Single(k => k.Tick == w.Tick);
        World atKnot = sim.KnotWorld(knot.Id)!;
        Character back = You(w);

        // You keep: the lost future, your memory of the wrong (half strength), the custom, whom you met, what you know of your gift.
        Assert.Equal(futures + 1, w.Fate.LostFutures.Count); // (seed 9 also dies once on its own, early on)
        Memory memory = w.Fate.Memories.Single(m => m.PersonId == mira.Id);
        Assert.Equal(MemoryKind.Wrong, memory.Kind);
        Assert.True(memory.Opinion <= -25, $"a memory at half of a −60 drop is about −30, got {memory.Opinion}");
        Assert.Contains(custom, back.LearnedCustoms);
        Assert.Contains(Cast(w, "oren").Id, back.Met);
        Assert.True(w.Fate.GiftAwakened);
        Assert.Equal(1, w.Fate.GiftMastery);
        Assert.Contains(back.Thoughts, t => t.DefId == "remembered_wrong" && t.AboutId == mira.Id);
        int older = atKnot.Remembered.Find(m => m.PersonId == mira.Id)?.Opinion ?? 0; // a memory of her the knot already held is replaced, not added to
        Assert.Equal(Relations.Opinion(atKnot, atKnot.PlayerId, mira.Id) + memory.Opinion - older, Relations.Opinion(w, back.Id, mira.Id));
        SimEvent remembered = w.Log.All.Last(e => e.Key == "event.fate.remembered_wrong");
        Assert.Equal("You remembered a wrong from a future that never came: Mira insulted you.", EventText.Describe(w, remembered));

        // And nothing else: the grudge, the others, the stores and the camp are the knot's.
        Assert.DoesNotContain(w.Grudges, g => g.HolderId == back.Id && g.TargetId == mira.Id && g.Active);
        string Json<T>(T x) => System.Text.Json.JsonSerializer.Serialize(x, SnapshotSerializer.Options);
        foreach (Character c in w.Characters.Where(c => c.Id != back.Id))
            Assert.Equal(Json(atKnot.Get(c.Id)), Json(c));
        Assert.Equal(Json(atKnot.Items), Json(w.Items));
        Assert.Equal(Json(atKnot.Debts), Json(w.Debts));
        Assert.Equal(Json(atKnot.Map), Json(w.Map));
        Assert.Equal(Json(atKnot.Culture), Json(w.Culture));
        Assert.Equal(atKnot.Policy, w.Policy);
        Assert.Equal((atKnot.Get(atKnot.PlayerId).Food, atKnot.Get(atKnot.PlayerId).Health, atKnot.Get(atKnot.PlayerId).Pos), (back.Food, back.Health, back.Pos));
        Assert.Equal(Json(atKnot.Log.All), Json(w.Log.All.Take(atKnot.Log.All.Count).ToList())); // the log goes back too
        Assert.True(items > 0);
    }

    [Fact]
    public void MemoriesNeverStack()
    {
        int checkedReturns = 0;
        for (ulong seed = 1; seed <= 40 && checkedReturns < 3; seed++)
        {
            var sim = Simulation.Create(TestContent.Db, seed);
            sim.Advance(2 * Day);
            World dying = sim.World;
            Character you = You(dying);
            Character mira = Cast(dying, "mira");
            Relations.ChangeOpinion(dying, you, mira.Id, -50, 0);
            Relations.FormGrudge(dying, you, mira, 30, 0);
            World first = DieAndReturn(sim);
            Assert.Contains(first.Remembered, m => m.PersonId == mira.Id);
            sim.Advance(Day + 6 * Hour); // knots tied after the memory was applied
            World before = sim.World;
            var held = before.Remembered.ToDictionary(m => m.PersonId, m => (m.Key, m.Opinion));
            World second = DieAndReturn(sim);
            KnotRecord knot = second.Fate.Knots.Single(k => k.Tick == second.Tick);
            if (knot.Timeline != 1) continue; // only a knot from after the first return already holds the memory
            checkedReturns++;
            World atKnot = sim.KnotWorld(knot.Id)!;
            Character back = You(second);
            foreach (Character x in second.Characters.Where(c => c.Id != back.Id))
            {
                AppliedMemory? had = atKnot.Remembered.Find(m => m.PersonId == x.Id), now = second.Remembered.Find(m => m.PersonId == x.Id);
                int expected = Relations.Opinion(atKnot, atKnot.PlayerId, x.Id) + (now?.Opinion ?? 0) - (had?.Opinion ?? 0);
                Assert.Equal(expected, Relations.Opinion(second, back.Id, x.Id));
            }
            // The memory the knot already held is not told again.
            AppliedMemory miraMemory = atKnot.Remembered.Single(m => m.PersonId == mira.Id);
            if (second.Remembered.Single(m => m.PersonId == mira.Id).Key == miraMemory.Key)
                Assert.DoesNotContain(second.Log.All.Skip(atKnot.Log.All.Count), e => e.Key.StartsWith("event.fate.remembered", StringComparison.Ordinal) && e.Actors.Contains(mira.Id));
        }
        Assert.True(checkedReturns >= 1, "no second return landed on a knot tied after the first");
    }

    [Fact]
    public void LostFutureLinesRender()
    {
        var bad = new Regex(@"⟦|\bYou (is|was|has|holds|owes|wants)\b|\b[Yy]ou's\b");
        for (ulong seed = 1; seed <= 8; seed++)
        {
            Simulation sim = StoryLog.Simulate(TestContent.Rolled, seed, 1, new RunSetup(null, Fates: true));
            World w = sim.World;
            Assert.NotEmpty(w.Fate.LostFutures);
            foreach (LostFuture f in w.Fate.LostFutures)
            {
                Assert.True(f.DeathLine >= 0);
                Assert.Contains("You", EventText.Describe(w, f.Lines[f.DeathLine]));
                foreach (LostLine line in f.Lines)
                {
                    string text = EventText.Describe(w, line);
                    Assert.False(bad.IsMatch(text), $"seed {seed}: '{text}' ({line.Key})");
                }
            }
            foreach (SimEvent e in w.Log.All.Where(e => e.Key.StartsWith("event.fate.remembered", StringComparison.Ordinal)))
            {
                string text = EventText.Describe(w, e);
                Assert.DoesNotMatch(new Regex(@"e:\d+:\d+"), text); // the quoted line is read out, never shown as its reference
                Assert.False(bad.IsMatch(text), text);
            }
        }
    }

    [Fact]
    public void ASnapshotAfterAReturnRestoresToTheSameFuture()
    {
        var straight = Simulation.Create(TestContent.Rolled, 13);
        straight.Advance(2 * Day);
        DieAndReturn(straight);
        straight.Advance(Day);
        Simulation restored = Simulation.Restore(TestContent.Rolled, straight.Snapshot());
        Assert.Equal(straight.StateHash(), restored.StateHash());
        Assert.Equal(straight.ArchivedKnots, restored.ArchivedKnots);
        // Both die again at the same tick: the restored archive takes them to the same knot.
        DieAndReturn(straight);
        DieAndReturn(restored);
        straight.Advance(Day);
        restored.Advance(Day);
        Assert.Equal(straight.StateHash(), restored.StateHash());
    }

    [Fact]
    public void ReturnUnderOneSecondAt200Characters()
    {
        var sim = Simulation.Create(TestContent.Db, 4);
        WorldGen.AddSyntheticCharacters(sim.World, 200);
        sim.Advance(2 * Day);
        Assert.NotEmpty(sim.World.Fate.Knots);
        Assert.True(sim.ForceDeath());
        var sw = Stopwatch.StartNew();
        sim.Step();
        sw.Stop();
        Assert.Equal(1, sim.World.Fate.Returns);
        Assert.Equal(200, sim.World.Characters.Count(c => c.CastKey.Length > 0));
        Assert.True(sw.ElapsedMilliseconds < 1000, $"the return took {sw.ElapsedMilliseconds} ms at 200 characters");
    }

    [Fact]
    public void KnotsCostWithinTheTickBudgetAtTheLargestCamp()
    {
        var sim = Simulation.Create(TestContent.Db, 3);
        sim.Advance(24 * Day); // everyone has arrived by Day 20
        int knots = sim.World.Fate.Knots.Count;
        int characters = sim.World.Characters.Count;
        var sw = Stopwatch.StartNew();
        sim.Advance(Day);
        sw.Stop();
        Assert.True(sim.World.Fate.Knots.Count > knots, "no knot was tied in the measured day");
        double nsPerCharTick = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / (Day * (double)characters);
        Assert.True(nsPerCharTick <= 2_000, $"a day with knots cost {nsPerCharTick:F0} ns per character per tick, over ADR-0005's 2,000");
    }

    [Fact]
    public void HooksSeeTheReturnAndTheWorldGoneBackTo()
    {
        var sim = Simulation.Create(TestContent.Db, 14);
        var hook = new FateHook();
        sim.AddHook(hook);
        sim.Advance(Day);
        FateReturn? raised = null;
        sim.Returned += r => raised = r;
        World w = DieAndReturn(sim);
        Assert.Equal(1, hook.Created); // re-attached, not created again
        Assert.Same(w, hook.Returned);
        Assert.NotNull(raised);
        Assert.Same(w, raised!.Restored);
        Assert.Contains(hook.Seen, e => e.Key == Fate.ReturnedKey);
        Assert.Contains(hook, w.Hooks);
        long seen = hook.Seen.Count;
        sim.Advance(10);
        Assert.True(hook.Seen.Count >= seen);
        Assert.Equal(w.Log.All.Last().Id, hook.Seen.Last().Id);
    }

    private sealed class FateHook : Return.Sim.Modding.IModHook, Return.Sim.Modding.IFateHook
    {
        public int Created;
        public World? Returned;
        public readonly List<SimEvent> Seen = new();
        public void OnWorldCreated(World w) => Created++;
        public void OnEvent(World w, SimEvent e) => Seen.Add(e);
        public void OnReturned(World w, LostFuture lost) => Returned = w;
        public void OnLifeEnded(World w) { }
    }

    [Fact]
    public void EveryRollablePeopleCanBeYou()
    {
        foreach (RebirthPeopleDef p in TestContent.Rolled.Rebirth.Peoples)
        {
            var setup = new RunSetup(new RebirthChoice(p.Species), Fates: true);
            Simulation sim = StoryLog.Simulate(TestContent.Rolled, 2, 1, setup);
            Assert.Equal(p.Species, You(sim.World).SpeciesId);
            var text = new StringWriter();
            StoryLog.Write(sim, text);
            string log = text.ToString();
            Assert.DoesNotContain("⟦", log);
            Assert.DoesNotMatch(new Regex(@"\bYou (is|was|has|burns|owes)\b"), log);
            Assert.Contains("Lost futures", log);
            Assert.Contains("chosen for testing", log);
        }
    }
}
