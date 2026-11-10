using System.Text.Json;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Snapshots;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Fixed decision 12, M0 scope (BIBLE.md 5.3, ADR-0021): trait origins, wants and arcs as data, every
/// change a logged beat that names its cause, and no system that advances arcs or mutates traits yet.
/// </summary>
public class GrowthTests
{
    private static ContentDb Db => TestContent.Db;
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static SimEvent Mark(World w, string key, params int[] actors) => w.Log.Emit(w, SystemTag.Incident, key, actors, Array.Empty<long>());
    private static string Json(object o) => JsonSerializer.Serialize(o, SnapshotSerializer.Options);
    private static string TraitName(string id) => Db.Loc.Get(Db.TraitById(id).NameKey);
    private static string NotHeld(Character c) => Db.Traits.Select(t => t.Id).First(t => !c.Traits.Contains(t));

    /// <summary>A world run past its last arrival, so every cast member has entered it.</summary>
    private static Simulation PastArrivals(ulong seed)
    {
        var s = Simulation.Create(Db, seed);
        s.Advance(s.World.Arrivals.Max(a => a.Tick) + Db.World.TicksPerHour);
        return s;
    }

    // ---- Trait origins

    [Fact]
    public void EveryoneIsBornWithGeneratedTraitsWhoseOriginsRunInStep()
    {
        World w = WorldGen.Create(Db, 11);
        foreach (Character c in w.Characters)
        {
            Assert.True(Growth.InStep(c), c.CastKey);
            Assert.Equal(2, c.Growth.TraitOrigins.Count);
            Assert.All(c.Growth.TraitOrigins, o =>
            {
                Assert.Equal(TraitOriginKind.Generated, o.Kind);
                Assert.Equal(0, o.CauseEvent);
                Assert.Equal(0, o.Tick);
            });
        }
    }

    // ---- Wants form when people enter the world

    [Fact]
    public void CampStartersWantSomethingFromCreationLoggedOnTheFirstTick()
    {
        var s = Simulation.Create(Db, 11);
        World w = s.World;
        foreach (Character c in w.Characters)
        {
            bool starter = c.Status == CharStatus.Member;
            Assert.Equal(starter, c.Growth.Want != null);
            Assert.Equal(starter, c.Growth.Arc != null);
            if (!starter) continue;
            Assert.Equal(WorldGen.NatureOf(Db, 11, c.CastKey).WantType, c.Growth.Want!.TypeId);
            Assert.Equal(WantStatus.Active, c.Growth.Want.Status);
            Assert.Equal(0, c.Growth.Want.FormedEvent);
            Assert.Equal(ArcStage.Want, c.Growth.Arc!.Stage);
            Assert.Equal(0, c.Growth.Arc.StalledSinceTick);
        }
        // Nothing is logged before time moves, so mod hooks and the Chronicle see every beat.
        Assert.Empty(w.Log.All);

        s.Step();
        List<SimEvent> beats = w.Log.All.Where(e => e.System == SystemTag.Growth).ToList();
        Assert.Equal(3, beats.Count);
        foreach (SimEvent beat in beats)
        {
            Character c = w.Get(beat.Actors[0]);
            Assert.Equal(0, beat.Tick);
            Assert.Equal(Db.WantById(c.Growth.Want!.TypeId).BeatKey, beat.Key);
            Assert.Equal(beat.Id, c.Growth.Want.FormedEvent);
            Assert.Equal(beat.Id, c.Growth.Arc!.LastEvent);
            Assert.Empty(beat.Causes);
            Assert.Empty(beat.Effects);
        }
        s.Advance(Db.World.TicksPerHour);
        Assert.Equal(3, w.Log.All.Count(e => e.System == SystemTag.Growth));
    }

    [Fact]
    public void ANewcomersWantFormsOnArrivalAndCitesIt()
    {
        World w = WorkTests.Only(1, "you", "mira");
        Character wren = Cast(w, "wren");
        Assert.Null(wren.Growth.Want);
        w.Tick = 3L * Db.World.TicksPerDay;

        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: wren.Id)!;
        Assert.Equal("event.incident.wanderer_arrives", arrived.Key);
        Assert.NotNull(wren.Growth.Want);
        Want want = wren.Growth.Want!;
        Assert.Equal(WorldGen.NatureOf(Db, 1, "wren").WantType, want.TypeId);
        Assert.Equal(w.Tick, want.FormedTick);
        SimEvent beat = w.Log.Get(want.FormedEvent)!;
        Assert.Equal(SystemTag.Growth, beat.System);
        Assert.Equal(new[] { arrived.Id }, beat.Causes);
        Assert.Equal(beat.Id, wren.Growth.Arc!.LastEvent);
        Assert.Equal(w.Tick, wren.Growth.Arc.StalledSinceTick);
    }

    [Fact]
    public void EveryCastMemberWantsSomethingOnceTheyHaveArrived()
    {
        for (ulong seed = 1; seed <= 3; seed++)
        {
            Simulation s = PastArrivals(seed);
            World w = s.World;
            foreach (Character c in w.Characters.Where(c => c.Named))
            {
                Want want = c.Growth.Want ?? throw new Xunit.Sdk.XunitException($"seed {seed}: {c.CastKey} wants nothing");
                SimEvent beat = w.Log.Get(want.FormedEvent)!;
                Assert.Equal(Db.WantById(want.TypeId).BeatKey, beat.Key);
                Assert.Contains(c.Id, beat.Actors);
                if (want.FormedTick > 0)
                    Assert.Matches("^event\\.incident\\.[a-z_]+_arrives$", w.Log.Get(Assert.Single(beat.Causes))!.Key); // wanderers, and peoples who come in their own season (ADR-0022)
                else
                    Assert.Empty(beat.Causes); // a starter's want formed with the world: nothing brought it about
            }
        }
    }

    /// <summary>A want about someone is about whoever else is in the camp that they get on with best (or, for a rival, worst) by nature.</summary>
    [Fact]
    public void AStartersWantAboutSomeoneIsAboutTheStarterTheyGetOnWithBestOrWorst()
    {
        int seen = 0;
        for (ulong seed = 1; seed <= 300; seed++)
        {
            World w = WorldGen.Create(Db, seed);
            List<Character> starters = w.Members().ToList();
            foreach (Character c in starters)
            {
                Want want = c.Growth.Want!;
                if (want.TargetKind != WantTargetKind.Person) continue;
                seen++;
                bool worst = Db.WantById(want.TypeId).Pick == "worst_chemistry";
                int Score(Character o) => worst ? -Relations.Chemistry(w, c, o) : Relations.Chemistry(w, c, o);
                List<Character> others = starters.Where(o => o.Id != c.Id).ToList();
                Assert.Contains(want.TargetId, others.Select(o => o.Id));
                Assert.Equal(others.Max(Score), Score(w.Get(want.TargetId)));
            }
        }
        Assert.True(seen >= 20, $"only {seen} starters wanted something about someone");
    }

    /// <summary>A newcomer's want about someone is about whoever in the camp, as they arrive, they get on with best (or, for a rival, worst).</summary>
    [Fact]
    public void ANewcomersWantAboutSomeoneIsAboutTheMemberTheyGetOnWithBestOrWorst()
    {
        int seen = 0;
        for (ulong seed = 1; seed <= 60; seed++)
        {
            World w = WorkTests.Only(seed, "you", "mira", "oren", "bram");
            List<Character> camp = w.Members().ToList();
            foreach (CastDef cd in Db.Cast.Where(cd => !cd.StartsInCamp && cd.Key != "bram"))
            {
                Character c = Cast(w, cd.Key);
                IncidentSystem.Fire(w, "wanderer_arrives", targetId: c.Id);
                c.Status = CharStatus.OffMap; // the next newcomer finds the same camp
                Want want = c.Growth.Want!;
                if (want.TargetKind != WantTargetKind.Person) continue;
                seen++;
                bool worst = Db.WantById(want.TypeId).Pick == "worst_chemistry";
                int Score(Character o) => worst ? -Relations.Chemistry(w, c, o) : Relations.Chemistry(w, c, o);
                Assert.Contains(want.TargetId, camp.Select(o => o.Id));
                Assert.Equal(camp.Max(Score), Score(w.Get(want.TargetId)));
            }
        }
        Assert.True(seen >= 20, $"only {seen} newcomers wanted something about someone");
    }

    /// <summary>
    /// Someone who wanders into an empty camp still wants what their nature says (CharacterNature.WantType),
    /// unless that want is about another person and there is nobody to want it about; only then do they
    /// want something that needs no one, drawn again from Truths.
    /// </summary>
    [Fact]
    public void AWandererWhoFindsTheCampEmptyKeepsTheirNatureUnlessItNeedsSomeone()
    {
        string[] wanderers = Db.Cast.Where(c => !c.StartsInCamp).Select(c => c.Key).ToArray();
        int kept = 0, redrawn = 0;
        for (ulong seed = 1; seed <= 40; seed++)
        {
            World w = WorkTests.Only(seed);
            foreach (string key in wanderers)
            {
                Character c = Cast(w, key);
                IncidentSystem.Fire(w, "wanderer_arrives", targetId: c.Id);
                WantDef nature = Db.WantById(WorldGen.NatureOf(Db, seed, key).WantType);
                Want want = c.Growth.Want!;
                if (nature.Target == "person")
                {
                    Assert.NotEqual("person", Db.WantById(want.TypeId).Target);
                    Assert.NotEqual(WantTargetKind.Person, want.TargetKind);
                    redrawn++;
                }
                else
                {
                    Assert.True(nature.Id == want.TypeId, $"seed {seed}: {key} is by nature '{nature.Id}' but alone wants '{want.TypeId}'");
                    kept++;
                }
                c.Status = CharStatus.OffMap;
            }
        }
        Assert.True(kept >= 20 && redrawn >= 20, $"kept {kept}, redrawn {redrawn}");
    }

    // ---- A want is part of a person's nature

    [Fact]
    public void NaturesCarryTheWantAndAreLazyAndOrderIndependent()
    {
        var forward = Db.Cast.ToDictionary(c => c.Key, c => WorldGen.NatureOf(Db, 21, c.Key));
        var backward = Db.Cast.Reverse().ToDictionary(c => c.Key, c => WorldGen.NatureOf(Db, 21, c.Key));
        foreach (CastDef c in Db.Cast)
        {
            Assert.Equal(forward[c.Key], backward[c.Key]);
            Assert.NotEqual("", forward[c.Key].WantType);
        }
    }

    [Fact]
    public void WantsAreStablePerSeed()
    {
        Simulation a = PastArrivals(9), b = PastArrivals(9);
        foreach (Character c in a.World.Characters.Where(c => c.Named))
            Assert.Equal(Json(c.Growth), Json(b.World.Get(c.Id).Growth));
    }

    /// <summary>
    /// A rewind keeps Truths and re-rolls Dice (ADR-0004). The second world's Dice are reseeded and its
    /// wanderers come in the opposite order, so its weeks play out differently; everyone still wants
    /// the same thing, because a person's first want is their nature.
    /// </summary>
    [Fact]
    public void WantsSurviveARewind()
    {
        var a = Simulation.Create(Db, 5);
        var b = Simulation.Create(Db, 5);
        b.World.Rng.Dice.Reseed(0x5EED);
        List<Arrival> arrivals = b.World.Arrivals;
        List<long> ticks = arrivals.Select(x => x.Tick).Reverse().ToList();
        for (int i = 0; i < arrivals.Count; i++) arrivals[i].Tick = ticks[i];
        long end = a.World.Arrivals.Max(x => x.Tick) + Db.World.TicksPerHour;
        a.Advance(end);
        b.Advance(end);
        Assert.NotEqual(a.StateHash(), b.StateHash());

        foreach (Character c in a.World.Characters.Where(c => c.Named))
        {
            // Someone not yet arrived in b has no want yet: when you die there and come back, the arrivals still to come
            // are drawn again (ADR-0030), so b can reach this tick with someone still off the map.
            if (b.World.Get(c.Id).Status == CharStatus.OffMap) continue;
            Want mine = c.Growth.Want!, theirs = b.World.Get(c.Id).Growth.Want!;
            Assert.Equal(WorldGen.NatureOf(Db, 5, c.CastKey).WantType, mine.TypeId);
            Assert.Equal(mine.TypeId, theirs.TypeId);
            Assert.Equal(mine.TargetKind, theirs.TargetKind);
            Assert.Equal(mine.TargetRef, theirs.TargetRef);
        }
    }

    [Fact]
    public void GrowthSurvivesSnapshotRestoreAndCountsInTheStateHash()
    {
        var s = Simulation.Create(Db, 7);
        s.Advance(8L * Db.World.TicksPerDay);
        Character mira = Cast(s.World, "mira");
        SimEvent why = Mark(s.World, "event.test.trial", mira.Id);
        Growth.ChangeTrait(s.World, mira, mira.Traits[1], NotHeld(mira), why.Id);
        Growth.AdvanceArc(s.World, mira, ArcStage.Obstacle, why.Id);

        Simulation r = Simulation.Restore(Db, s.Snapshot());
        foreach (Character c in s.World.Characters)
            Assert.Equal(Json(c.Growth), Json(r.World.Get(c.Id).Growth));
        Assert.Equal(s.StateHash(), r.StateHash());
        s.Advance(20_000);
        r.Advance(20_000);
        Assert.Equal(s.StateHash(), r.StateHash());

        string before = r.StateHash();
        Cast(r.World, "mira").Growth.Arc!.StalledSinceTick++;
        Assert.NotEqual(before, r.StateHash());
    }

    [Fact]
    public void SyntheticCharactersForTheRestoreBenchmarkGrowToo()
    {
        var s = Simulation.Create(Db, 4);
        WorldGen.AddSyntheticCharacters(s.World, 40);
        Assert.All(s.World.Members(), c =>
        {
            Assert.NotNull(c.Growth.Want);
            Assert.True(Growth.InStep(c));
        });
    }

    // ---- Changing a trait

    [Fact]
    public void ChangingATraitRefusesAChangeWithoutACause()
    {
        World w = WorkTests.Only(1, "mira");
        Character mira = Cast(w, "mira");
        string other = NotHeld(mira);
        List<string> traits = mira.Traits.ToList();
        Assert.Throws<ArgumentException>(() => Growth.ChangeTrait(w, mira, null, other, 0));
        Assert.Throws<ArgumentException>(() => Growth.ChangeTrait(w, mira, mira.Traits[1], other, 0));
        Assert.Throws<ArgumentException>(() => Growth.ChangeTrait(w, mira, null, other, -1));
        Assert.Throws<ArgumentException>(() => Growth.ChangeTrait(w, mira, null, other, 42));
        Assert.Equal(traits, mira.Traits);
        Assert.True(Growth.InStep(mira));
        Assert.Empty(w.Log.All);
    }

    [Fact]
    public void ChangingATraitRefusesWhatCannotChange()
    {
        World w = WorkTests.Only(1, "mira");
        Character mira = Cast(w, "mira");
        long why = Mark(w, "event.test.trial", mira.Id).Id;
        Assert.Throws<ArgumentException>(() => Growth.ChangeTrait(w, mira, null, null, why));
        Assert.Throws<ArgumentException>(() => Growth.ChangeTrait(w, mira, null, "protective", why));
        Assert.Throws<ArgumentException>(() => Growth.ChangeTrait(w, mira, NotHeld(mira), null, why));
        Assert.Throws<ContentException>(() => Growth.ChangeTrait(w, mira, null, "brave", why));
        mira.Traits.Add(NotHeld(mira));
        Assert.Throws<InvalidOperationException>(() => Growth.ChangeTrait(w, mira, null, NotHeld(mira), why));
    }

    /// <summary>
    /// A refused change leaves no trace. A trait content no longer has (a snapshot from before a mod
    /// dropped it) fails before anything changes, so no trait ever changes without its beat.
    /// </summary>
    [Fact]
    public void ARefusedTraitChangeChangesNothing()
    {
        World w = WorkTests.Only(1, "mira");
        Character mira = Cast(w, "mira");
        long why = Mark(w, "event.test.trial", mira.Id).Id;
        mira.Traits[1] = "forgotten";
        mira.Growth.TraitOrigins[1].TraitId = "forgotten";
        string before = Json(mira);
        int logged = w.Log.All.Count;

        Assert.Throws<ContentException>(() => Growth.ChangeTrait(w, mira, "forgotten", NotHeld(mira), why));
        Assert.Throws<ContentException>(() => Growth.ChangeTrait(w, mira, "forgotten", null, why));
        Assert.Equal(before, Json(mira));
        Assert.Equal(logged, w.Log.All.Count);
    }

    [Fact]
    public void ChangingATraitRecordsWhereItCameFromAndLogsABeat()
    {
        World w = WorkTests.Only(1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        w.Tick = 11L * Db.World.TicksPerDay + 5L * Db.World.TicksPerHour;
        SimEvent downed = w.Log.Emit(w, SystemTag.Combat, "event.combat.downed", new[] { oren.Id }, Array.Empty<long>(), null, EventArg.Char("a", oren.Id));
        string was = mira.Traits[1], now = NotHeld(mira);

        SimEvent beat = Growth.ChangeTrait(w, mira, was, now, downed.Id);
        Assert.Equal(new[] { "protective", now }, mira.Traits);
        Assert.True(Growth.InStep(mira));
        Assert.Equal(TraitOriginKind.Generated, mira.Growth.TraitOrigins[0].Kind);
        TraitOrigin origin = mira.Growth.TraitOrigins[1];
        Assert.Equal(TraitOriginKind.Mutated, origin.Kind);
        Assert.Equal(downed.Id, origin.CauseEvent);
        Assert.Equal(w.Tick, origin.Tick);
        Assert.Equal(SystemTag.Growth, beat.System);
        Assert.Equal(new[] { downed.Id }, beat.Causes);
        Assert.Equal(new[] { mira.Id }, beat.Actors);
        Assert.Empty(beat.Effects);
        Assert.Equal($"Mira, once {TraitName(was)}, became {TraitName(now)}.", EventText.Describe(w, beat));

        string third = NotHeld(mira);
        SimEvent gained = Growth.ChangeTrait(w, mira, null, third, downed.Id);
        Assert.Equal($"Mira became {TraitName(third)}.", EventText.Describe(w, gained));
        SimEvent lost = Growth.ChangeTrait(w, mira, "protective", null, gained.Id);
        Assert.Equal("Mira is no longer Protective.", EventText.Describe(w, lost));
        Assert.Equal(new[] { now, third }, mira.Traits);
        Assert.True(Growth.InStep(mira));
        Assert.All(mira.Growth.TraitOrigins, o => Assert.Equal(TraitOriginKind.Mutated, o.Kind));
    }

    [Fact]
    public void TraitBeatsSpeakToThePlayer()
    {
        World w = WorkTests.Only(1, "you");
        Character you = Cast(w, "you");
        long why = Mark(w, "event.test.trial", you.Id).Id;
        string lose = you.Traits[1];
        Assert.Equal($"You are no longer {TraitName(lose)}.", EventText.Describe(w, Growth.ChangeTrait(w, you, lose, null, why)));
        string gain = NotHeld(you);
        Assert.Equal($"You became {TraitName(gain)}.", EventText.Describe(w, Growth.ChangeTrait(w, you, null, gain, why)));
    }

    // ---- Moving an arc

    [Fact]
    public void AnArcMovesOneStageAtATimeAndEveryMoveNeedsACause()
    {
        var s = Simulation.Create(Db, 1);
        s.Step();
        World w = s.World;
        Character mira = Cast(w, "mira");
        Want want = mira.Growth.Want!;
        Arc arc = mira.Growth.Arc!;
        long formed = want.FormedEvent;
        Assert.NotEqual(0, formed);
        SimEvent trouble = Mark(w, "event.test.trouble", mira.Id);

        Assert.Throws<ArgumentException>(() => Growth.AdvanceArc(w, mira, ArcStage.Obstacle, 0));
        Assert.Throws<ArgumentException>(() => Growth.AdvanceArc(w, mira, ArcStage.Obstacle, w.Log.NextId));
        Assert.Throws<ArgumentException>(() => Growth.AdvanceArc(w, mira, ArcStage.TurningPoint, trouble.Id));
        Assert.Throws<ArgumentException>(() => Growth.AdvanceArc(w, mira, ArcStage.Resolution, trouble.Id));
        Assert.Throws<ArgumentException>(() => Growth.AdvanceArc(w, mira, ArcStage.Want, trouble.Id));
        Assert.Equal(ArcStage.Want, arc.Stage);

        w.Tick += 1_000;
        SimEvent obstacle = Growth.AdvanceArc(w, mira, ArcStage.Obstacle, trouble.Id);
        Assert.Equal(ArcStage.Obstacle, arc.Stage);
        Assert.Equal(w.Tick, arc.StalledSinceTick);
        Assert.Equal(obstacle.Id, arc.LastEvent);
        Assert.Equal(new[] { trouble.Id, formed }, obstacle.Causes);
        Assert.Equal(SystemTag.Growth, obstacle.System);
        Assert.Empty(obstacle.Effects);
        Assert.Equal(WantStatus.Active, want.Status);
        Assert.Equal("Mira found something standing in their way.", EventText.Describe(w, obstacle));
        Assert.Throws<ArgumentException>(() => Growth.AdvanceArc(w, mira, ArcStage.Obstacle, trouble.Id));

        SimEvent rescue = Mark(w, "event.test.rescue", mira.Id);
        SimEvent turn = Growth.AdvanceArc(w, mira, ArcStage.TurningPoint, rescue.Id);
        Assert.Equal(new[] { rescue.Id, obstacle.Id }, turn.Causes);
        Assert.Equal("Mira came to a turning point.", EventText.Describe(w, turn));

        SimEvent end = Growth.AdvanceArc(w, mira, ArcStage.Resolution, rescue.Id);
        Assert.Equal(new[] { rescue.Id, turn.Id }, end.Causes);
        Assert.Equal(ArcStage.Resolution, arc.Stage);
        Assert.Equal(WantStatus.Fulfilled, want.Status);
        Assert.Equal("Mira got what they wanted.", EventText.Describe(w, end));
        foreach (ArcStage stage in Enum.GetValues<ArcStage>())
            Assert.Throws<ArgumentException>(() => Growth.AdvanceArc(w, mira, stage, rescue.Id));
    }

    [Theory]
    [InlineData(ArcStage.Failed, WantStatus.Failed, "Oren could not get what they wanted.")]
    [InlineData(ArcStage.Abandoned, WantStatus.Abandoned, "Oren gave up on what they wanted.")]
    public void AnArcCanEndWithoutWhatItWantedFromAnyOpenStage(ArcStage ending, WantStatus status, string text)
    {
        for (int steps = 0; steps <= 2; steps++)
        {
            var s = Simulation.Create(Db, 2);
            s.Step();
            World w = s.World;
            Character oren = Cast(w, "oren");
            long why = Mark(w, "event.test.trouble", oren.Id).Id;
            if (steps >= 1) Growth.AdvanceArc(w, oren, ArcStage.Obstacle, why);
            if (steps >= 2) Growth.AdvanceArc(w, oren, ArcStage.TurningPoint, why);
            SimEvent e = Growth.AdvanceArc(w, oren, ending, why);
            Assert.Equal(ending, oren.Growth.Arc!.Stage);
            Assert.Equal(status, oren.Growth.Want!.Status);
            Assert.Equal(text, EventText.Describe(w, e));
        }
    }

    [Fact]
    public void ArcBeatsSpeakToThePlayer()
    {
        var s = Simulation.Create(Db, 1);
        s.Step();
        World w = s.World;
        Character you = Cast(w, "you");
        long why = Mark(w, "event.test.trouble", you.Id).Id;
        Assert.Equal("You found something standing in your way.", EventText.Describe(w, Growth.AdvanceArc(w, you, ArcStage.Obstacle, why)));
        Assert.Equal("You came to a turning point.", EventText.Describe(w, Growth.AdvanceArc(w, you, ArcStage.TurningPoint, why)));
        Assert.Equal("You got what you wanted.", EventText.Describe(w, Growth.AdvanceArc(w, you, ArcStage.Resolution, why)));
    }

    [Theory]
    [InlineData(ArcStage.Failed, "You could not get what you wanted.")]
    [InlineData(ArcStage.Abandoned, "You gave up on what you wanted.")]
    public void ArcEndingsSpeakToThePlayer(ArcStage ending, string text)
    {
        var s = Simulation.Create(Db, 1);
        s.Step();
        World w = s.World;
        Character you = Cast(w, "you");
        long why = Mark(w, "event.test.trouble", you.Id).Id;
        Assert.Equal(text, EventText.Describe(w, Growth.AdvanceArc(w, you, ending, why)));
    }

    [Fact]
    public void AnArcNeedsAWant()
    {
        World w = WorkTests.Only(1, "you");
        Character wren = Cast(w, "wren");
        long why = Mark(w, "event.test.trouble").Id;
        Assert.Throws<InvalidOperationException>(() => Growth.AdvanceArc(w, wren, ArcStage.Obstacle, why));
    }

    [Fact]
    public void AnArcThatMovesBeforeItsWantIsLoggedLogsTheWantFirst()
    {
        World w = WorldGen.Create(Db, 3);
        Character mira = Cast(w, "mira");
        long why = Mark(w, "event.test.trouble", mira.Id).Id;
        SimEvent obstacle = Growth.AdvanceArc(w, mira, ArcStage.Obstacle, why);
        long formed = mira.Growth.Want!.FormedEvent;
        Assert.InRange(formed, why + 1, obstacle.Id - 1);
        Assert.Equal(new[] { why, formed }, obstacle.Causes);
    }

    // ---- What M0 does not do yet

    /// <summary>
    /// BIBLE.md 5.3: in M0 nothing advances an arc or mutates a trait. Checked every hour of a year:
    /// every trait's origin runs in step with the traits and is still generated, a want never comes
    /// without its arc (or an arc without its want), everyone living in the camp has a want in the log,
    /// and no arc has moved. Every growth beat in the year is a want forming, and none ends a story.
    /// </summary>
    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    public void AYearOfPlayFormsWantsButChangesNoTraitAndMovesNoArc(ulong seed)
    {
        var s = Simulation.Create(Db, seed);
        World w = s.World;
        for (long hour = 0; hour < Db.World.TicksPerYear / Db.World.TicksPerHour; hour++)
        {
            s.Advance(Db.World.TicksPerHour);
            foreach (Character c in w.Characters)
            {
                string who = $"seed {seed}, {(c.Named ? c.CastKey : "character " + c.Id)} at tick {w.Tick}";
                CharacterGrowth g = c.Growth;
                Assert.True(Growth.InStep(c), $"{who}: trait origins out of step with traits");
                Assert.True(g.TraitOrigins.TrueForAll(o => o.Kind == TraitOriginKind.Generated), $"{who}: a trait changed");
                Assert.True((g.Want == null) == (g.Arc == null), $"{who}: a want and an arc must come together");
                if (c.Status == CharStatus.Member) Assert.True(g.Want is { FormedEvent: > 0 }, $"{who}: lives in the camp with no want in the log");
                if (!c.Named) Assert.Null(g.Want);
                if (g.Want == null) continue;
                Assert.True(g.Arc!.Stage == ArcStage.Want && g.Want.Status == WantStatus.Active, $"{who}: an arc moved ({g.Arc.Stage}, {g.Want.Status})");
            }
        }
        List<SimEvent> beats = w.Log.All.Where(e => e.System == SystemTag.Growth).ToList();
        Assert.Equal(w.Characters.Count(c => c.Named), beats.Count);
        Assert.All(beats, e =>
        {
            Assert.StartsWith("event.growth.want_", e.Key);
            Assert.Empty(e.Effects);
        });
    }
}
