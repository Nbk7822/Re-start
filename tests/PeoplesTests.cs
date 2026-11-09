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

    // ---- Fae: courtesy, hard truths, a binding word ----

    /// <summary>The fae's chain: you promise a newcomer a meal, the camp leaves them out, the promise breaks, it stings Fionn, and Fionn holds it against you.</summary>
    [Fact]
    public void ABrokenPromiseInCampStingsTheFaeWhoHoldTheOathBreakerToAccount()
    {
        World w = WorkTests.Only(1, "you", "mira", "wren", "fionn");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), wren = Cast(w, "wren"), fionn = Cast(w, "fionn");
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center);
        you.Food = 900; fionn.Food = 900; mira.Food = 100; wren.Food = 250; wren.HungerStage = 1;
        var promise = new Debt
        {
            Id = w.NewId(), DebtorId = you.Id, CreditorId = wren.Id, Kind = DebtKind.PromiseMeal, Status = DebtStatus.Open,
            FormedTick = w.Tick, DueTick = w.Tick + Hours(w, 48),
        };
        promise.FormedEvent = w.Log.Emit(w, SystemTag.Social, "event.social.joined_promised_meal", new[] { wren.Id, you.Id }, Array.Empty<long>(),
            new[] { new Effect { Kind = EffectKind.DebtFormed, A = you.Id, B = wren.Id, RefId = promise.Id } },
            EventArg.Char("a", wren.Id), EventArg.Char("b", you.Id), EventArg.Num("n", 2)).Id;
        w.Debts.Add(promise);
        NatureNeed courtesy = NatureNeeds.Get(fionn, Courtesy.NeedId)!;
        int before = courtesy.Value, health = fionn.Health;

        Rations.Assign(w);

        SimEvent broken = w.Log.Get(promise.ClosedEvent)!;
        Assert.Equal("event.social.promise_broken", broken.Key);
        SimEvent sting = w.Log.All.Single(e => e.Key == "event.social.word_wound");
        Assert.Contains(broken.Id, sting.Causes);
        Assert.Equal("The broken word stung Fionn like cold iron.", EventText.Describe(w, sting));
        var def = w.Content.NeedById(Courtesy.NeedId);
        Assert.Equal(before - def.Param("brokenWordLoss"), courtesy.Value);
        Assert.Equal(health - def.Param("brokenWordHurt"), fionn.Health);
        Assert.DoesNotContain(fionn.Thoughts, t => t.DefId == "wounded");
        Assert.Contains(fionn.Thoughts, t => t.DefId == "word_wound" && t.AboutId == you.Id);

        Grudge g = w.Grudges.Single(x => x.HolderId == fionn.Id);
        Assert.Equal(you.Id, g.TargetId);
        Story story = StoryEndingAt(w, g.FormedEvent);
        SimEvent without = w.Log.All.Single(e => e.Key == "event.needs.went_without" && e.Actors[0] == wren.Id);
        Assert.Equal(new[] { without.Id, broken.Id, sting.Id, g.FormedEvent }, story.Chain);
        Assert.Contains(promise.FormedEvent, NeedsMoodTests.Ancestors(w, g.FormedEvent));
        Assert.Equal(new[] { SystemTag.Needs, SystemTag.Social }, story.Systems);
    }

    [Fact]
    public void KeptPromisesAndCourtesyHeartenTheFaeAndInsultsCutThem()
    {
        World w = WorkTests.Only(1, "you", "wren", "fionn");
        Character you = Cast(w, "you"), wren = Cast(w, "wren"), fionn = Cast(w, "fionn");
        NatureNeed courtesy = NatureNeeds.Get(fionn, Courtesy.NeedId)!;
        var def = w.Content.NeedById(Courtesy.NeedId);
        courtesy.Value = 300;
        Debt d = Relations.FormDebt(w, you, wren, DebtKind.PromiseMeal, 1, 0, w.Tick + 1_000);
        Relations.Repay(w, d, d.FormedEvent);
        Assert.Equal(300 + def.Param("keptWordGain"), courtesy.Value);
        Assert.Contains(fionn.Thoughts, t => t.DefId == "words_kept");

        SimEvent insult = SocialSystem.Insult(w, wren, fionn);
        Assert.Equal(300 + def.Param("keptWordGain") - def.Param("slightLoss"), courtesy.Value);
        Assert.Equal(insult.Id, courtesy.LastEvent);
        Courtesy.Courteous(w, fionn, deep: false);
        Assert.Equal(300 + def.Param("keptWordGain") - def.Param("slightLoss") + def.Param("chatGain"), courtesy.Value);
        Assert.Equal(new[] { "food", "courtesy", "rest", "social", "health" }, StateApi.Character(w, fionn.Id).Needs.Select(n => n.Need));
    }

    [Fact]
    public void TheFaeSayHardTruthsWhereOthersWouldInsult()
    {
        World w = WorkTests.Only(1, "you", "mira", "fionn");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), fionn = Cast(w, "fionn");
        fionn.Pos = you.Pos with { X = you.Pos.X + 1 };
        mira.Pos = you.Pos with { Y = you.Pos.Y + 1 };
        SimEvent wrong = w.Log.Emit(w, SystemTag.Social, "event.social.promise_broken", new[] { you.Id, mira.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", you.Id), EventArg.Char("b", mira.Id));
        Grudge g = Relations.FormGrudge(w, fionn, you, 30, wrong.Id);

        // With a wrong to name, it is said before everyone; whoever is near hears what you did, and saying it eases the grudge.
        SimEvent truth = Courtesy.HardTruth(w, fionn, you);
        Assert.Equal("event.social.hard_truth", truth.Key);
        Assert.Equal(new[] { g.FormedEvent }, truth.Causes);
        Assert.Equal("Fionn told you plainly, before everyone, what you had done.", EventText.Describe(w, truth));
        Assert.Contains(you.Thoughts, t => t.DefId == "told_hard_truth" && t.AboutId == fionn.Id);
        Assert.Contains(mira.Thoughts, t => t.DefId == "heard_hard_truth" && t.AboutId == you.Id);
        Assert.Equal(30 - w.Content.NeedById(Courtesy.NeedId).Param("truthEasesGrudge"), g.Intensity);

        // A hearer who already thought poorly of you now holds it against you.
        Relations.ChangeOpinion(w, mira, you.Id, -35, 0);
        SimEvent again = Courtesy.HardTruth(w, fionn, you);
        Grudge heard = w.Grudges.Single(x => x.HolderId == mira.Id);
        Assert.Contains(again.Id, w.Log.Get(heard.FormedEvent)!.Causes);

        // With nothing to name, they say plainly what they think, and nobody else is told anything.
        SimEvent plain = Courtesy.HardTruth(w, fionn, mira);
        Assert.Equal("event.social.plain_truth", plain.Key);

        // In company the fae never insult: the same roll says a truth instead.
        for (int t = 0; t < 4000; t++)
        {
            w.Tick = (long)t * w.Content.World.SocialEveryTicks;
            SocialSystem.Step(w);
        }
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.social.insulted" && e.Actors[0] == fionn.Id);
        Assert.True(w.Log.All.Count(e => e.Key is "event.social.hard_truth" or "event.social.plain_truth" && e.Actors[0] == fionn.Id) > 3);
    }

    [Fact]
    public void TheFaeRememberSlightsLong()
    {
        World w = WorkTests.Only(1, "wren", "fionn");
        Character wren = Cast(w, "wren"), fionn = Cast(w, "fionn");
        Grudge fae = Relations.FormGrudge(w, fionn, wren, 10, 0);
        Grudge human = Relations.FormGrudge(w, wren, fionn, 10, 0);
        for (int day = 1; day <= 8; day++)
        {
            w.Tick = (long)day * w.Content.World.TicksPerDay;
            SocialSystem.Hourly(w);
        }
        Assert.Equal(2, human.Intensity);
        Assert.Equal(10 - 8 / TestContent.Db.SpeciesById("fae").GrudgeDecayEveryDays, fae.Intensity);
    }

    /// <summary>A fae welcomed into camp swears to repay the welcome, and does so before anything else, from their own share, the moment their host goes hungry.</summary>
    [Fact]
    public void AFaeKeepsTheirWordBeforeTheirOwnHunger()
    {
        World w = WorkTests.Only(1, "you");
        Character you = Cast(w, "you"), fionn = Cast(w, "fionn");
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 5, w.Map.Stockpile.Center);
        SimEvent arrived = IncidentSystem.Fire(w, "wanderer_arrives", targetId: fionn.Id)!;
        Assert.Equal("event.incident.fae_arrives", arrived.Key);
        SimEvent joined = IncidentSystem.Join(w, fionn, arrived.Id);
        Debt welcome = w.Debts.Single(d => d.DebtorId == fionn.Id);
        Assert.Equal(DebtKind.Welcome, welcome.Kind);
        Assert.Contains(joined.Effects, f => f.Kind == EffectKind.DebtFormed && f.A == fionn.Id && f.B == you.Id && f.RefId == welcome.Id);
        SimEvent oath = w.Log.Get(welcome.FormedEvent)!;
        Assert.Empty(oath.Effects);
        Assert.Equal("Fionn swore to repay you for the welcome.", EventText.Describe(w, oath));

        // Both hungry, one berry set aside for Fionn: Fionn carries it to you instead of eating it.
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center);
        fionn.Job = null;
        fionn.Pos = w.Map.Hearth with { Y = w.Map.Hearth.Y + 2 };
        you.Food = 120; fionn.Food = 200; fionn.HungerStage = 1; you.HungerStage = 2;
        w.Reservations.Clear();
        w.Reservations.Add(new Reservation { CharacterId = fionn.Id, Count = 1 });
        Assert.Equal(JobKind.KeepPromise, JobPicker.Choose(w, fionn)?.Kind);
        RunWork(w, 3_000, () => welcome.Status != DebtStatus.Open);

        Assert.Equal(DebtStatus.Repaid, welcome.Status);
        SimEvent kept = w.Log.Get(welcome.ClosedEvent)!;
        Assert.Equal("Fionn kept their word to you.", EventText.Describe(w, kept));
        SimEvent meal = w.Log.All.Single(e => e.Key == "event.work.shared_meal");
        Assert.Equal(meal.Id, fionn.LastFoodEvent);
        Assert.Contains(fionn.Thoughts, t => t.DefId == "kept_my_word");
        Story story = StoryEndingAt(w, kept.Id);
        Assert.Equal(new[] { arrived.Id, joined.Id, oath.Id, meal.Id, kept.Id }, story.Chain);
    }

    [Fact]
    public void WhatAFaeOwesIsSworn()
    {
        World w = WorkTests.Only(1, "mira", "fionn");
        Character mira = Cast(w, "mira"), fionn = Cast(w, "fionn");
        Debt d = Relations.FormDebt(w, fionn, mira, DebtKind.Care, 1, 0);
        Assert.Equal("Fionn swore to repay Mira for tending their wounds.", EventText.Describe(w, w.Log.Get(d.FormedEvent)!));
        Debt human = Relations.FormDebt(w, mira, fionn, DebtKind.Care, 1, 0);
        Assert.Equal("event.social.owes_care", w.Log.Get(human.FormedEvent)!.Key);
    }

    [Fact]
    public void AFaeWhoCannotBearTheBrokenWordsLeaves()
    {
        World w = WorkTests.Only(1, "you", "fionn");
        Character fionn = Cast(w, "fionn");
        NatureNeed courtesy = NatureNeeds.Get(fionn, Courtesy.NeedId)!;
        SimEvent broken = w.Log.Emit(w, SystemTag.Social, "event.test.broken", new[] { fionn.Id }, Array.Empty<long>());
        Courtesy.WordBroken(w, 0, broken);
        Courtesy.WordBroken(w, 0, broken);
        Courtesy.WordBroken(w, 0, broken);
        var def = w.Content.NeedById(Courtesy.NeedId);
        Assert.True(courtesy.Value < def.CriticalBelow);
        for (int h = 0; h <= def.LeavesAfterCriticalHours + 1 && fionn.Status == CharStatus.Member; h++)
        {
            w.Tick += Hours(w, 1);
            NeedsSystem.Hourly(w);
            SocialSystem.Hourly(w);
        }
        Assert.Equal(CharStatus.Left, fionn.Status);
        SimEvent left = w.Log.All.Single(e => e.Key == "event.social.left_camp");
        HashSet<long> ancestors = NeedsMoodTests.Ancestors(w, left.Id);
        Assert.Contains(broken.Id, ancestors);
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.courtesy_critical" && ancestors.Contains(e.Id));
    }

    // ---- Reads, snapshots and determinism with all four peoples in camp ----

    /// <summary>Day 22 of seed 3: every people has arrived; bargains, bonds and nature needs are live state.</summary>
    private static Simulation AllPeoples(ulong seed = 3, long days = 22)
    {
        Simulation s = Simulation.Create(TestContent.Db, seed);
        s.Enqueue(new SetRationPolicy(0, RationPolicy.PromisesFirst));
        s.Advance(days * s.World.Content.World.TicksPerDay);
        return s;
    }

    [Fact]
    public void StateApiShowsEachPeoplesNeedsAndBonds()
    {
        Simulation s = AllPeoples();
        World w = s.World;
        foreach (string key in new[] { "ketil", "dryope", "qamar", "fionn" })
            Assert.NotEqual(CharStatus.OffMap, Cast(w, key).Status);

        NatureNeedState forest = StateApi.NatureNeeds(w, Cast(w, "dryope").Id).Single();
        Tree tree = Forest.TreeById(w, NatureNeeds.Get(Cast(w, "dryope"), Forest.NeedId)!.BondId)!;
        Assert.Equal(tree.Felled ? "Their tree was cut down" : $"Bound to the tree at ({tree.Pos.X}, {tree.Pos.Y})", forest.Bond);
        Assert.Contains(forest.Standing, new[] { "met", "low", "critical" });

        NatureNeedState purpose = StateApi.NatureNeeds(w, Cast(w, "ketil").Id).Single();
        Assert.StartsWith("Bound to: ", purpose.Bond);

        Character qamar = Cast(w, "qamar");
        NatureNeedState bargain = StateApi.NatureNeeds(w, qamar.Id).Single();
        Assert.StartsWith("Offers to ", bargain.Bond);
        IReadOnlyList<ContractState> contracts = StateApi.Contracts(w, qamar.Id);
        Assert.Equal(w.Contracts.Count(k => k.BoundId == qamar.Id), contracts.Count);
        if (Bargains.Binding(w, qamar.Id) is { } k)
        {
            Assert.Contains("bound by a bargain with", bargain.Bond);
            Assert.Contains(contracts, c => c.Id == k.Id && c.Status == "open");
        }

        Assert.Equal("Courtesy", StateApi.NatureNeeds(w, Cast(w, "fionn").Id).Single().Label);
        Assert.Empty(StateApi.NatureNeeds(w, Cast(w, "mira").Id));
        Assert.DoesNotContain("⟦", StateApi.Json(StateApi.NatureNeeds(w, qamar.Id)) + StateApi.Json(contracts));
    }

    [Fact]
    public void ThePeoplesNaturesSurviveASnapshot()
    {
        Simulation straight = AllPeoples(5, 21);
        byte[] snap = straight.Snapshot();
        Simulation restored = Simulation.Restore(TestContent.Db, snap);
        Assert.Equal(straight.StateHash(), restored.StateHash());
        Character a = Cast(straight.World, "dryope"), b = Cast(restored.World, "dryope");
        Assert.Equal(a.NatureNeeds.Single().BondId, b.NatureNeeds.Single().BondId);
        Assert.Equal(straight.World.Contracts.Count, restored.World.Contracts.Count);

        long more = 6L * straight.World.Content.World.TicksPerDay;
        straight.Advance(more);
        restored.Advance(more);
        Assert.Equal(straight.StateHash(), restored.StateHash());
        Assert.NotEmpty(straight.World.Contracts);
    }

    [Fact]
    public void SameSeedSameWorldWithThePeoples()
    {
        Assert.Equal(AllPeoples(9, 30).StateHash(), AllPeoples(9, 30).StateHash());
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
