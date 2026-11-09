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

    // ---- Dryad: forest ----

    private static void RunWork(World w, int ticks, Func<bool> until)
    {
        for (int t = 0; t < ticks; t++)
        {
            TravelSystem.Step(w);
            WorkSystem.Step(w);
            if (w.Tick % w.Content.World.TicksPerHour == 0) NeedsSystem.Hourly(w);
            w.Tick++;
            if (until()) return;
        }
    }

    /// <summary>
    /// A world where Oren stands by Dryope's tree, the roof wants timber and there is none in store, and
    /// the only other standing tree is a few tiles off (so the nearest tree to Oren is hers).
    /// </summary>
    private static (World W, Character Oren, Character Dryope, Tree Tree) ByTheBoundTree(ulong seed)
    {
        World w = WorkTests.Only(seed, "oren", "dryope");
        Character oren = Cast(w, "oren"), dryope = Cast(w, "dryope");
        Tree tree = Forest.TreeById(w, NatureNeeds.Get(dryope, Forest.NeedId)!.BondId)!;
        Tree other = w.Map.Trees.Where(t => t.Id != tree.Id && t.Pos.ChebyshevTo(tree.Pos) >= 4)
            .OrderBy(t => t.Pos.ChebyshevTo(tree.Pos)).ThenBy(t => t.Id).First();
        foreach (Tree t in w.Map.Trees) t.Felled = t.Id != tree.Id && t.Id != other.Id;
        w.Map.RebuildCaches();
        Stock.Take(w, "timber", 99);
        oren.Pos = Places.Adjacent(w, tree.Pos, tree.Pos);
        dryope.Pos = w.Map.Hearth with { Y = w.Map.Hearth.Y + 2 };
        NatureNeeds.Get(dryope, Forest.NeedId)!.Value = 1000;
        w.Tick = Hours(w, 10);
        return (w, oren, dryope, tree);
    }

    [Fact]
    public void EachDryadIsBoundToATreeNearTheCampByTheirNature()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            World a = WorldGen.Create(TestContent.Db, seed), b = WorldGen.Create(TestContent.Db, seed);
            int bond = NatureNeeds.Get(Cast(a, "dryope"), Forest.NeedId)!.BondId;
            Assert.Equal(bond, NatureNeeds.Get(Cast(b, "dryope"), Forest.NeedId)!.BondId);
            Tree tree = Forest.TreeById(a, bond)!;
            int closer = a.Map.Trees.Count(t => t.Pos.ChebyshevTo(a.Map.Hearth) < tree.Pos.ChebyshevTo(a.Map.Hearth));
            var def = TestContent.Db.NeedById(Forest.NeedId);
            Assert.InRange(closer, 0, def.Param("bondSkipNearest") + def.Param("bondCandidates"));
        }
    }

    [Fact]
    public void TheForestFeedsADryadAmongTreesAndFailsThemAwayFromThem()
    {
        World w = WorkTests.Only(1, "dryope");
        Character dryope = Cast(w, "dryope");
        NatureNeed forest = NatureNeeds.Get(dryope, Forest.NeedId)!;
        var def = w.Content.NeedById(Forest.NeedId);
        Tree tree = Forest.TreeById(w, forest.BondId)!;

        dryope.Pos = w.Map.Hearth with { Y = w.Map.Hearth.Y + 1 };
        forest.Value = 500;
        NeedsSystem.Hourly(w);
        Assert.Equal(500 - def.DecayPerHour, forest.Value);

        dryope.Pos = Places.Adjacent(w, tree.Pos, tree.Pos);
        forest.Value = 300;
        NeedsSystem.Hourly(w);
        Assert.True(forest.Value >= 300 - def.DecayPerHour + def.Param("restorePerTree") * def.Param("ownTreeWeight"), $"forest {forest.Value}");
        Assert.Contains(dryope.Thoughts, t => t.DefId == "among_trees");

        // Faint, far from the trees, they go and stand by their tree until the need is met.
        dryope.Pos = w.Map.Hearth with { Y = w.Map.Hearth.Y + 1 };
        dryope.Job = null;
        forest.Value = def.LowBelow - 10;
        Job? job = JobPicker.Choose(w, dryope);
        Assert.Equal(JobKind.TendTree, job?.Kind);
        Assert.Equal(tree.Id, job!.TargetId);
        RunWork(w, 6_000, () => forest.Value >= def.SatisfiedAt);
        Assert.True(forest.Value >= def.SatisfiedAt, $"forest {forest.Value}");
        Assert.Equal("Forest", StateApi.Character(w, dryope.Id).Needs[0].Label);
    }

    [Fact]
    public void ADryadNeverFellsATree()
    {
        World w = WorkTests.Only(1, "dryope");
        Character dryope = Cast(w, "dryope");
        Stock.Take(w, "timber", 99);
        dryope.Priorities.RemoveAll(p => p.WorkId == "gather_timber");
        dryope.Priorities.Add(new WorkPriority { WorkId = "gather_timber", Priority = 1 });
        dryope.Priorities.RemoveAll(p => p.WorkId == "forage");
        w.Tick = Hours(w, 10);
        Assert.True(Places.TimberNeeded(w) > 0);
        Assert.NotEqual(JobKind.GatherTimber, JobPicker.Choose(w, dryope)?.Kind);
    }

    /// <summary>The dryad's chain: Oren fells the tree Dryope is bound to; Dryope cries out, is hurt, and holds it against him.</summary>
    [Fact]
    public void FellingADryadsTreeWoundsThemAndMakesAGrudge()
    {
        (World w, Character oren, Character dryope, Tree tree) = ByTheBoundTree(1);
        int health = dryope.Health;
        RunWork(w, 3_000, () => tree.Felled);
        Assert.True(tree.Felled);

        SimEvent felled = w.Log.All.Single(e => e.Key == "event.work.felled_bound_tree");
        Assert.Equal(new[] { oren.Id, dryope.Id }, felled.Actors);
        Assert.Equal("Oren felled the tree Dryope was bound to.", EventText.Describe(w, felled));
        SimEvent cry = w.Log.All.Single(e => e.Key == "event.needs.tree_wound");
        Assert.Contains(felled.Id, cry.Causes);
        Assert.Contains(cry.Effects, f => f.Kind == EffectKind.BondSevered && f.A == dryope.Id && f.B == oren.Id && f.RefId == tree.Id);
        Assert.True(dryope.Health <= health - w.Content.NeedById(Forest.NeedId).Param("woundMin"));
        Assert.Contains(dryope.Thoughts, t => t.DefId == "own_tree_felled" && t.AboutId == oren.Id);

        Grudge g = Assert.Single(w.Grudges, x => x.HolderId == dryope.Id);
        Assert.Equal(oren.Id, g.TargetId);
        Story story = StoryEndingAt(w, g.FormedEvent);
        Assert.Equal(new[] { felled.Id, cry.Id, g.FormedEvent }, story.Chain);
        Assert.Equal(new[] { SystemTag.Work, SystemTag.Needs, SystemTag.Social }, story.Systems);
    }

    [Fact]
    public void ThoseWhoThinkWellOfADryadLeaveTheirTreeStanding()
    {
        (World w, Character oren, Character dryope, Tree tree) = ByTheBoundTree(1);
        Relations.ChangeOpinion(w, oren, dryope.Id, w.Content.NeedById(Forest.NeedId).Param("respectOpinion"), 0);
        Assert.True(Forest.Spares(w, oren, tree));
        Assert.NotEqual(tree.Id, Places.StandingTree(w, oren.Pos, oren)?.Id);
        RunWork(w, 3_000, () => w.Log.All.Any(e => e.Key == "event.work.felled_tree"));
        Assert.False(tree.Felled);
        SimEvent other = w.Log.All.First(e => e.Key == "event.work.felled_tree");
        SimEvent grief = w.Log.All.Single(e => e.Key == "event.social.mourned_tree");
        Assert.Contains(other.Id, grief.Causes);
        Assert.Contains(dryope.Thoughts, t => t.DefId == "tree_felled" && t.AboutId == oren.Id);
    }

    [Fact]
    public void ADryadCutOffFromTheForestWithersAway()
    {
        World w = WorkTests.Only(1, "dryope");
        Character dryope = Cast(w, "dryope");
        NatureNeed forest = NatureNeeds.Get(dryope, Forest.NeedId)!;
        foreach (Tree t in w.Map.Trees) t.Felled = true;
        w.Map.RebuildCaches();
        SimEvent cut = w.Log.Emit(w, SystemTag.Work, "event.test.last_tree", new[] { dryope.Id }, Array.Empty<long>());
        forest.LastEvent = cut.Id;
        for (int h = 0; h < 200 && dryope.Status == CharStatus.Member; h++)
        {
            NeedsSystem.Hourly(w);
            w.Tick += Hours(w, 1);
        }
        SimEvent death = w.Log.All.Single(e => e.Effects.Any(f => f.Kind == EffectKind.Death));
        Assert.Equal("event.needs.died_forest", death.Key);
        HashSet<long> ancestors = NeedsMoodTests.Ancestors(w, death.Id);
        Assert.Contains(cut.Id, ancestors);
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.forest_critical" && ancestors.Contains(e.Id));
    }

    // ---- Djinn: bargains ----

    /// <summary>A camp with the djinn and the named others; the djinn's service and price set (they are Truths, so tests pin them).</summary>
    private static (Simulation S, Character Qamar, NatureNeed Need) WithDjinn(ulong seed, string service, string price, params string[] others)
    {
        Simulation s = Camp(seed, others.Append("qamar").ToArray());
        Character qamar = Cast(s.World, "qamar");
        qamar.ArrivedTick = 0;
        NatureNeed need = NatureNeeds.Get(qamar, Bargains.NeedId)!;
        need.BondWork = service;
        need.BondItem = price;
        return (s, qamar, need);
    }

    private static Contract Struck(World w, Character qamar)
    {
        for (int t = 0; t < 6_000 && Bargains.Binding(w, qamar.Id) == null; t++)
        {
            TravelSystem.Step(w);
            WorkSystem.Step(w);
            w.Tick++;
        }
        return Bargains.Binding(w, qamar.Id) ?? throw new InvalidOperationException("no bargain was struck");
    }

    [Fact]
    public void ARestlessDjinnOffersABargainToWhomeverItThinksBestOf()
    {
        (Simulation s, Character qamar, NatureNeed need) = WithDjinn(1, "keep_watch", "timber", "you", "mira", "bram");
        World w = s.World;
        Character bram = Cast(w, "bram");
        Relations.ChangeOpinion(w, qamar, bram.Id, 30, 0);
        need.Value = w.Content.NeedById(Bargains.NeedId).LowBelow - 1;
        w.Tick = Hours(w, 10);
        Job? job = JobPicker.Choose(w, qamar);
        Assert.Equal(JobKind.OfferBargain, job?.Kind);
        Assert.Equal(bram.Id, job!.TargetId);

        Contract k = Struck(w, qamar);
        Assert.Equal(bram.Id, k.PatronId);
        Assert.Equal("keep_watch", k.ServiceWork);
        Assert.Equal("timber", k.PriceItem);
        SimEvent struck = w.Log.Get(k.FormedEvent)!;
        Assert.Contains(struck.Effects, f => f.Kind == EffectKind.ContractFormed && f.A == qamar.Id && f.B == bram.Id && f.RefId == k.Id);
        SimEvent offer = w.Log.Get(struck.Causes[0])!;
        Assert.Equal("Qamar offered Bram a bargain: to keep the night watch, for timber for the fire each dawn until the season turns.", EventText.Describe(w, offer));
        Assert.Equal("Bram struck a bargain with Qamar.", EventText.Describe(w, struck));

        // Bound, the djinn's need fills, it leans to its service, and the camp keeps timber in store for the price.
        int before = need.Value;
        NeedsSystem.Hourly(w);
        Assert.True(need.Value > before);
        Assert.Contains(qamar.Thoughts, t => t.DefId == "bound_by_bargain");
        Assert.Equal(w.Content.NeedById(Bargains.NeedId).Param("serviceBias"), NatureNeeds.WorkBias(w, qamar, "keep_watch"));
        Stock.Take(w, "timber", 99);
        Assert.Equal(2 * k.PriceAmount + w.Map.Roof.Count(r => !r.Sound) * 3, Places.TimberNeeded(w));
        Assert.Equal(k.FormedEvent, Bargains.DemandCause(w, "timber"));
    }

    [Fact]
    public void TheProudWillNotStrikeABargainTheCampCannotPay()
    {
        (Simulation s, Character qamar, NatureNeed need) = WithDjinn(1, "keep_watch", "timber", "hollis");
        World w = s.World;
        Character hollis = Cast(w, "hollis");
        Relations.ChangeOpinion(w, hollis, qamar.Id, 50, 0);
        Stock.Take(w, "timber", 99);
        Assert.Equal("event.social.bargain_refused_unsafe", Bargains.RefusalKey(w, hollis, qamar, "timber", 1));
        Stock.Add(w, "timber", 2, w.Map.Stockpile.Center);
        Assert.Null(Bargains.RefusalKey(w, hollis, qamar, "timber", 1));
        Relations.ChangeOpinion(w, hollis, qamar.Id, -50, 0);
        Assert.Equal("event.social.bargain_refused", Bargains.RefusalKey(w, hollis, qamar, "timber", 1));

        // Turned down, the djinn waits a while before it asks anyone again.
        need.Value = w.Content.NeedById(Bargains.NeedId).LowBelow - 1;
        w.Tick = Hours(w, 10);
        RunWork(w, 3_000, () => w.Log.All.Any(e => e.Key == "event.social.bargain_refused"));
        SimEvent no = w.Log.All.Single(e => e.Key == "event.social.bargain_refused");
        Assert.Equal("Hollis turned down Qamar's bargain.", EventText.Describe(w, no));
        Assert.Equal(no.Id, need.LastEvent);
        Assert.Contains(qamar.Thoughts, t => t.DefId == "bargain_refused" && t.AboutId == hollis.Id);
        qamar.Job = null;
        Assert.NotEqual(JobKind.OfferBargain, JobPicker.Choose(w, qamar)?.Kind);
        Assert.Empty(w.Contracts);
    }

    /// <summary>The djinn's chain: restless -> offers -> struck -> no berries at dawn in winter -> broken -> a grudge against the patron.</summary>
    [Fact]
    public void ABargainTheCampCannotPayIsBrokenAndRemembered()
    {
        (Simulation s, Character qamar, NatureNeed need) = WithDjinn(1, "forage", "berries", "you", "mira");
        World w = s.World;
        Character you = Cast(w, "you");
        need.Value = w.Content.NeedById(Bargains.NeedId).LowBelow + 1;
        w.Tick = (long)w.Content.World.TicksPerSeason * 3 - Hours(w, 4); // the eve of winter
        Stock.Add(w, "berries", 4, w.Map.Stockpile.Center);
        s.Advance(Hours(w, 8));
        Contract k = Bargains.Binding(w, qamar.Id) ?? w.Contracts.Single();
        Character patron = w.Get(k.PatronId);
        Assert.NotEqual(0, w.WinterEvent);

        // Winter: the stores run dry, and at dawn there is nothing to pay.
        Stock.Take(w, "berries", 99);
        foreach (Bush b in w.Map.Bushes) b.Berries = 0;
        s.Advance(w.Content.World.TicksPerDay);
        Assert.Equal(ContractStatus.Broken, k.Status);
        SimEvent broken = w.Log.Get(k.ClosedEvent)!;
        Assert.Equal("event.social.bargain_broken", broken.Key);
        Assert.Contains(broken.Effects, f => f.Kind == EffectKind.ContractBroken && f.A == qamar.Id && f.B == patron.Id);
        Assert.Contains(w.WinterEvent, broken.Causes);
        string with = patron.Id == you.Id ? "you" : EventText.Name(w, patron.Id);
        Assert.Equal($"The camp had no berries to give Qamar at dawn, and the bargain with {with} is broken.", EventText.Describe(w, broken));
        Assert.Contains(qamar.Thoughts, t => t.DefId == "bargain_betrayed" && t.AboutId == patron.Id);

        Grudge g = w.Grudges.Single(x => x.HolderId == qamar.Id && x.TargetId == patron.Id);
        Story story = StoryEndingAt(w, g.FormedEvent);
        Assert.Equal(k.FormedEvent, story.Chain[^3]);
        Assert.Equal(broken.Id, story.Chain[^2]);
        Assert.Contains(w.Log.All, e => e.Key == "event.social.bargain_offered" && story.Chain.Contains(e.Id));
        Assert.Contains(SystemTag.Social, story.Systems);
        Assert.True(story.Systems.Count >= 2);
    }

    [Fact]
    public void ABargainPaidToTheEndIsKept()
    {
        (Simulation s, Character qamar, NatureNeed need) = WithDjinn(1, "keep_watch", "timber", "you");
        World w = s.World;
        Character you = Cast(w, "you");
        need.Value = w.Content.NeedById(Bargains.NeedId).LowBelow - 1;
        w.Tick = Hours(w, 10);
        Contract k = Struck(w, qamar);
        Stock.Add(w, "timber", 30, w.Map.Stockpile.Center);
        int timber = Stock.Count(w, "timber");
        long paidBefore = w.Log.All.Count(e => e.Key == "event.social.bargain_paid");
        s.Advance(k.EndsTick - w.Tick + Hours(w, 1));
        Assert.Equal(ContractStatus.Kept, k.Status);
        int dawns = w.Log.All.Count(e => e.Key == "event.social.bargain_paid") - (int)paidBefore;
        Assert.True(dawns >= 1);
        SimEvent kept = w.Log.Get(k.ClosedEvent)!;
        Assert.Contains(kept.Effects, f => f.Kind == EffectKind.ContractKept);
        Assert.Equal("Qamar served out the bargain with you, and both kept their word.", EventText.Describe(w, kept));
        Assert.Contains(you.Thoughts, t => t.DefId == "bargain_honoured" && t.AboutId == qamar.Id);
        Assert.Equal(kept.Id, need.LastEvent);
    }

    /// <summary>Two peoples' stories in one chain: the djinn's price is timber, so a tree must fall, and it is the dryad's.</summary>
    [Fact]
    public void TheDjinnsFireCostTheDryadHerTree()
    {
        (World w, Character oren, Character dryope, Tree tree) = ByTheBoundTree(1);
        Character qamar = Cast(w, "qamar");
        qamar.Status = CharStatus.Member;
        qamar.Pos = w.Map.Hearth with { Y = w.Map.Hearth.Y + 3 };
        foreach (RoofSection r in w.Map.Roof) r.Sound = true;
        SimEvent struck = w.Log.Emit(w, SystemTag.Social, "event.social.bargain_struck", new[] { qamar.Id, oren.Id }, Array.Empty<long>());
        w.Contracts.Add(new Contract
        {
            Id = w.NewId(), BoundId = qamar.Id, PatronId = oren.Id, ServiceWork = "keep_watch", PriceItem = "timber", PriceAmount = 1,
            FormedEvent = struck.Id, NextDueTick = w.Tick + Hours(w, 20), EndsTick = w.Tick + Hours(w, 200), Status = ContractStatus.Open,
        });
        Assert.True(Places.TimberNeeded(w) > 0);
        RunWork(w, 3_000, () => tree.Felled);
        SimEvent felled = w.Log.All.Single(e => e.Key == "event.work.felled_bound_tree");
        Assert.Contains(struck.Id, felled.Causes);
        Grudge g = w.Grudges.Single(x => x.HolderId == dryope.Id);
        Assert.Equal(new[] { struck.Id, felled.Id }, StoryEndingAt(w, g.FormedEvent).Chain.Take(2));
    }

    [Fact]
    public void AWrongedDjinnLeftUnboundLetsTheFireLooseOnceAndThenLeaves()
    {
        (Simulation s, Character qamar, NatureNeed need) = WithDjinn(1, "keep_watch", "timber", "you", "mira");
        World w = s.World;
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        SimEvent wrong = w.Log.Emit(w, SystemTag.Social, "event.test.wrong", new[] { you.Id }, Array.Empty<long>());
        Relations.FormGrudge(w, qamar, you, 50, wrong.Id);
        Relations.ChangeOpinion(w, mira, qamar.Id, -60, 0);
        Relations.ChangeOpinion(w, you, qamar.Id, -60, 0);
        foreach (RoofSection r in w.Map.Roof) r.Sound = true; // nobody else takes timber from the stockpile meanwhile
        Stock.Add(w, "timber", 5, w.Map.Stockpile.Center);
        int timber = Stock.Count(w, "timber");
        need.Value = 100;
        w.Tick = Hours(w, 10);
        s.Advance(Hours(w, 2));
        SimEvent fire = w.Log.All.Single(e => e.Key == "event.social.djinn_trouble");
        Assert.Equal(timber - w.Content.NeedById(Bargains.NeedId).Param("troubleTimber"), Stock.Count(w, "timber"));
        Assert.Contains(w.Log.All.Single(e => e.Key == "event.social.grudge_formed" && e.Actors[0] == qamar.Id).Id, fire.Causes);
        Assert.Contains(mira.Thoughts, t => t.DefId == "fire_loosed" && t.AboutId == qamar.Id);
        s.Advance(Hours(w, 40));
        Assert.Single(w.Log.All, e => e.Key == "event.social.djinn_trouble");
        Assert.Equal(CharStatus.Left, qamar.Status);
        SimEvent left = w.Log.All.Single(e => e.Key == "event.social.left_camp" && e.Actors[0] == qamar.Id);
        Assert.Contains(fire.Id, NeedsMoodTests.Ancestors(w, left.Id));
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
