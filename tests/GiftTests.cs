using Return.Runner;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// The three slice-1 gifts (the player design §4.2–4.4, ADR-0030): the satchel, Fortune's favourite and Familiar faces,
/// how each wakes, acts, is seen and grows, the standing order, and that gifts draw only from the Fate stream.
/// </summary>
public class GiftTests
{
    private const int Day = 7_200, Hour = 300;

    private static Character You(World w) => w.Get(w.PlayerId);
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>A world where you are a human holding the gift, and only the named cast members are in camp, fed, rested and near the hearth.</summary>
    private static Simulation Camp(string gift, ulong seed, params string[] keys)
    {
        var sim = Simulation.Create(TestContent.Rolled, seed, new RebirthChoice("human", gift));
        World w = sim.World;
        int i = 0;
        foreach (Character c in w.Characters)
        {
            if (c.Id != w.PlayerId && !keys.Contains(c.CastKey)) { c.Status = CharStatus.OffMap; continue; }
            c.Status = CharStatus.Member;
            c.Food = Math.Max(c.Food, 900); c.Rest = 900; c.Social = 900; c.Mood = 60;
            c.Pos = new Point(w.Map.Hearth.X - 2 + i, w.Map.Hearth.Y + 2);
            i++;
        }
        w.Arrivals.Clear();
        return sim;
    }

    private static Story StoryEndingAt(World w, long terminal) =>
        StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true).Single(s => s.TerminalEventId == terminal);

    // ---- Bottomless satchel ----

    [Fact]
    public void TheSatchelFillsWhenYouForageAndFeedsYouWhenHungry()
    {
        var sim = Simulation.Create(TestContent.Rolled, 2, new RebirthChoice("human", Gifts.SatchelId));
        SimEvent? stashed = null;
        for (int i = 0; i < 2 * Day && stashed == null; i++)
        {
            sim.Step();
            stashed = sim.World.Log.All.LastOrDefault(e => e.Key == "event.gift.satchel.stashed");
        }
        World w = sim.World;
        Assert.NotNull(stashed);
        SimEvent forage = w.Log.Get(stashed!.Causes.Single())!;
        Assert.Equal("event.work.foraged", forage.Key);
        SimEvent woke = w.Log.All.Single(e => e.Key == "event.gift.satchel.awakened");
        Assert.Equal(forage.Id, woke.Causes.Single()); // your first forage woke it
        Assert.True(w.Fate.GiftAwakened);
        Assert.InRange(w.Gift.Held, 1, 3);
        Assert.StartsWith("You slipped", EventText.Describe(w, stashed));

        // Hungry, you eat from it first, before food is shared out.
        Character you = You(w);
        int held = w.Gift.Held;
        you.Food = 250;
        you.Asleep = false;
        Gifts.Hourly(w);
        Assert.Equal(held - 1, w.Gift.Held);
        Assert.True(you.Food >= 600);
        SimEvent ate = w.Log.All.Last(e => e.Key == "event.gift.satchel.ate");
        Assert.Equal("You ate berries from your satchel.", EventText.Describe(w, ate)); // as the log says "Mira ate berries."
    }

    [Fact]
    public void EatingFromNowhereBreedsResentment()
    {
        Simulation sim = Camp(Gifts.SatchelId, 3, "nia", "mira");
        World w = sim.World;
        Character you = You(w), nia = Cast(w, "nia"), mira = Cast(w, "mira");
        w.Fate.GiftAwakened = true;
        w.Gift.Held = 3;
        SimEvent hunger = w.Log.Emit(w, SystemTag.Needs, "event.needs.hungry", new[] { nia.Id }, Array.Empty<long>(), null, EventArg.Char("a", nia.Id));
        nia.Food = 200;
        nia.HungerStage = 1;
        nia.LastFoodEvent = hunger.Id;
        SimEvent? grudge = null;
        for (int meal = 0; meal < 3; meal++)
        {
            you.Food = 250;
            Gifts.Hourly(w);
            grudge ??= w.Log.All.LastOrDefault(e => e.Key == "event.social.grudge_formed" && e.Actors[0] == nia.Id);
        }
        SimEvent seen = w.Log.All.First(e => e.Key == "event.gift.satchel.seen");
        Assert.Equal("Nia, going hungry, saw you eat from nowhere.", EventText.Describe(w, seen));
        Assert.Contains(hunger.Id, seen.Causes);
        Assert.Contains(nia.Thoughts, t => t.DefId == "ate_from_nowhere" && t.AboutId == you.Id);
        Assert.NotNull(grudge); // repeated sightings form a grudge through the opinion line
        Story story = StoryEndingAt(w, grudge!.Id);
        Assert.Contains(SystemTag.Needs, story.Systems);
        Assert.Contains(SystemTag.Social, story.Systems);
        // Mira, not hungry, only saw something vanish, once.
        Assert.Single(mira.Thoughts, t => t.DefId == "saw_vanish");
        Assert.DoesNotContain(mira.Thoughts, t => t.DefId == "ate_from_nowhere");
    }

    [Fact]
    public void SharingTheSatchelLiftsTheStarvingAndTheyOweYou()
    {
        Simulation sim = Camp(Gifts.SatchelId, 4, "nia");
        World w = sim.World;
        Character you = You(w), nia = Cast(w, "nia");
        w.Fate.GiftAwakened = true;
        w.Gift.Held = 3;
        sim.Enqueue(new SetStandingOrder(w.Tick, "gift.satchel", Satchel.Share));
        int shares = 0;
        for (int round = 0; round < 3; round++)
        {
            SimEvent starving = w.Log.Emit(w, SystemTag.Needs, "event.needs.starving", new[] { nia.Id }, Array.Empty<long>(), null, EventArg.Char("a", nia.Id));
            nia.Food = 80;
            nia.HungerStage = 2;
            nia.LastFoodEvent = starving.Id;
            for (int i = 0; i < Hour && w.Log.All.Count(e => e.Key == "event.gift.satchel.shared") == shares; i++) sim.Step();
            shares++;
            SimEvent shared = w.Log.All.Last(e => e.Key == "event.gift.satchel.shared");
            Assert.Equal(starving.Id, shared.Causes.Single());
            Assert.Equal("You took Nia a meal from your satchel.", EventText.Describe(w, shared));
            Assert.True(nia.Food >= NeedsSystem.StarvingBelow);
            SimEvent owes = w.Log.All.Last(e => e.Key == "event.social.owes_meal");
            Assert.Contains(shared.Id, owes.Causes);
            Assert.Equal(EffectKind.DebtFormed, owes.Effects.Single().Kind);
            Assert.Contains(SystemTag.Gift, StoryEndingAt(w, owes.Id).Systems);
        }
        // Three lifts out of starving: the satchel grows to its second stage.
        SimEvent grew = w.Log.All.Single(e => e.Key == "event.gift.grew");
        Assert.Equal(EffectKind.GiftGrew, grew.Effects.Single().Kind);
        Assert.Equal("Your gift grew stronger: Bottomless satchel now holds 6 meals.", EventText.Describe(w, grew));
        Assert.Equal(1, w.Gift.Tier);
        Assert.Equal(6, Satchel.Capacity(w, Gifts.Held(w)!));
        Assert.Equal(1, w.Fate.GiftMastery);
    }

    [Fact]
    public void NoThiefOrRotReachesTheSatchelAndTheRationsDoNotCountIt()
    {
        Simulation sim = Camp(Gifts.SatchelId, 5, "mira", "oren");
        World w = sim.World;
        w.Gift.Held = 3;
        Stock.Take(w, "berries", 1_000);
        Assert.Equal(0, Rations.Unreserved(w, "berries"));
        IncidentSystem.Fire(w, "night_thief");
        sim.Advance(Day + 6 * Hour); // the thief comes and goes; a day turns, and food rots
        Assert.Equal(3, sim.World.Gift.Held);
        Assert.Equal(Stock.Count(sim.World, "berries"), Rations.Unreserved(sim.World, "berries") + Rations.Reserved(sim.World));
    }

    // ---- Fortune's favourite ----

    [Fact]
    public void FortunesFavouriteTurnsHarmOnTheNearest()
    {
        Simulation sim = Camp(Gifts.LuckId, 6, "mira", "oren", "bram");
        World w = sim.World;
        Character you = You(w), mira = Cast(w, "mira"), oren = Cast(w, "oren"), bram = Cast(w, "bram");
        you.Pos = new Point(w.Map.Hearth.X - 3, w.Map.Hearth.Y + 3);
        mira.Pos = new Point(you.Pos.X + 1, you.Pos.Y);
        oren.Pos = new Point(you.Pos.X + 4, you.Pos.Y);
        bram.Pos = new Point(you.Pos.X + 9, you.Pos.Y);
        SimEvent forage = w.Log.Emit(w, SystemTag.Work, "event.work.foraged", new[] { you.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", you.Id), EventArg.Num("n", 2), EventArg.Item("item", "berries"));
        int miraBefore = mira.Health, youBefore = you.Health, orenOpinion = Relations.Opinion(w, oren.Id, you.Id);
        long fateDraws = w.Rng.Dice[DiceSystem.Fate].Draws;

        SimEvent turned = IncidentSystem.Fire(w, "work_accident", forage.Id, you.Id)!;
        Assert.Equal("event.gift.luck.turned_accident", turned.Key);
        Assert.Equal("An accident meant for you found Mira instead.", EventText.Describe(w, turned));
        Assert.Equal(forage.Id, turned.Causes.Single());
        Assert.Equal(youBefore, you.Health);
        Assert.True(mira.Health < miraBefore);
        Assert.Equal(turned.Id, mira.LastHealthEvent);
        Assert.Equal(fateDraws, w.Rng.Dice[DiceSystem.Fate].Draws); // one person in reach: no tie, no draw
        Assert.Contains(mira.Thoughts, t => t.DefId == "ill_luck" && t.AboutId == you.Id);
        Assert.Equal(orenOpinion - 4, Relations.Opinion(w, oren.Id, you.Id)); // a witness thinks a little less of you
        Assert.DoesNotContain(bram.Thoughts, t => t.DefId == "ill_luck");
        SimEvent woke = w.Log.All.Single(e => e.Key == "event.gift.luck.awakened");
        Assert.Equal(turned.Id, woke.Causes.Single());
        Assert.Equal("You found your gift: harm meant for you finds someone near you.", EventText.Describe(w, woke));

        // Two equally near: the Fate stream breaks the tie, and nothing else draws.
        oren.Pos = new Point(you.Pos.X - 1, you.Pos.Y);
        long[] others = w.Rng.Dice.Streams.Where(s => s.System != DiceSystem.Fate).Select(s => s.Draws).ToArray();
        (Character hurt, long cause) = Gifts.TurnHarm(w, you, 50, forage.Id, blow: true);
        Assert.Contains(hurt.Id, new[] { mira.Id, oren.Id });
        Assert.Equal("event.gift.luck.turned_blow", w.Log.Get(cause)!.Key);
        Assert.Equal(fateDraws + 1, w.Rng.Dice[DiceSystem.Fate].Draws);
        Assert.Equal(others, w.Rng.Dice.Streams.Where(s => s.System != DiceSystem.Fate).Select(s => s.Draws).ToArray());

        // Nobody in reach: the harm is yours.
        mira.Pos = oren.Pos = new Point(you.Pos.X + 6, you.Pos.Y);
        Assert.Same(you, Gifts.TurnHarm(w, you, 50, forage.Id, blow: true).Hurt);

        // A third turned harm: the second stage reaches two paces.
        Assert.Equal(0, w.Gift.Tier);
        mira.Pos = new Point(you.Pos.X + 2, you.Pos.Y);
        Assert.Same(you, Gifts.TurnHarm(w, you, 50, forage.Id, blow: true).Hurt); // two paces is out of reach at the first stage
        mira.Pos = new Point(you.Pos.X + 1, you.Pos.Y);
        Gifts.TurnHarm(w, you, 50, forage.Id, blow: true);
        Assert.Equal(1, w.Gift.Tier);
        Assert.Single(w.Log.All, e => e.Key == "event.gift.grew");
        mira.Pos = new Point(you.Pos.X + 2, you.Pos.Y);
        Assert.Same(mira, Gifts.TurnHarm(w, you, 50, forage.Id, blow: true).Hurt);
    }

    [Fact]
    public void TheTurnedBlowChainsIntoAGrudge()
    {
        Simulation sim = Camp(Gifts.LuckId, 7, "mira");
        World w = sim.World;
        Character you = You(w), mira = Cast(w, "mira");
        mira.Pos = new Point(you.Pos.X + 1, you.Pos.Y);
        SimEvent forage = w.Log.Emit(w, SystemTag.Work, "event.work.foraged", new[] { you.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", you.Id), EventArg.Num("n", 2), EventArg.Item("item", "berries"));
        IncidentSystem.Fire(w, "work_accident", forage.Id, you.Id);
        Relations.ChangeOpinion(w, you, mira.Id, -38 - Relations.Opinion(w, you.Id, mira.Id), 0);
        SimEvent insult = SocialSystem.Insult(w, mira, you); // Mira's worst thought about you is the harm that found her
        SimEvent grudge = w.Log.All.Last(e => e.Key == "event.social.grudge_formed");
        Assert.Contains(w.Log.All.Single(e => e.Key == "event.gift.luck.turned_accident").Id, insult.Causes);
        Story story = StoryEndingAt(w, grudge.Id);
        Assert.Contains(SystemTag.Gift, story.Systems);
        Assert.Contains(SystemTag.Social, story.Systems);
        Assert.True(story.Chain.Count >= 3);
    }

    [Fact]
    public void DeathPassesYouByOnceASeason()
    {
        Simulation sim = Camp(Gifts.LuckId, 8, "mira", "oren");
        World w = sim.World;
        Character you = You(w), mira = Cast(w, "mira");
        w.Gift.Tier = 2; // the last stage
        w.Fate.GiftAwakened = true;
        foreach (Character c in w.Members()) if (c.Id != you.Id) c.Pos = new Point(you.Pos.X + 10 + c.Id % 3, you.Pos.Y);
        you.Health = 200;
        (Character hurt, long cause) = Gifts.TurnHarm(w, you, 120, 0, blow: true); // it would fell you, and nobody is in reach
        Assert.NotSame(you, hurt);
        Assert.Equal("event.gift.luck.death_passed", w.Log.Get(cause)!.Key);
        Assert.Same(you, Gifts.TurnHarm(w, you, 120, 0, blow: true).Hurt); // once a season
        Assert.Same(you, Gifts.TurnHarm(w, you, 10, 0, blow: true).Hurt); // and only harm that would fell you
        Assert.NotNull(mira);
    }

    // ---- Familiar faces ----

    [Fact]
    public void FamiliarFacesWarmThoseCloseToYouInALostFuture()
    {
        var sim = Simulation.Create(TestContent.Rolled, 9, new RebirthChoice("human", Gifts.FamiliarId));
        sim.Advance(Day + 6 * Hour);
        World dying = sim.World;
        Character mira = Cast(dying, "mira"), oren = Cast(dying, "oren");
        Relations.ChangeOpinion(dying, mira, dying.PlayerId, 80 - Relations.Opinion(dying, mira.Id, dying.PlayerId), 0);
        Relations.ChangeOpinion(dying, oren, dying.PlayerId, -50 - Relations.Opinion(dying, oren.Id, dying.PlayerId), 0);
        Assert.True(sim.ForceDeath());
        sim.Step();
        World w = sim.World;
        World atKnot = sim.KnotWorld(w.Fate.Knots.Single(k => k.Tick == w.Tick).Id)!;
        Assert.Equal(80, w.Fate.Echoes.Single(e => e.PersonId == mira.Id).Opinion);
        SimEvent knew = w.Log.All.Single(e => e.Key == "event.gift.familiar.knew_you");
        Assert.Equal("Mira looked at you as if they had known you for years.", EventText.Describe(w, knew));
        Assert.Equal(Relations.Opinion(atKnot, mira.Id, atKnot.PlayerId) + 20, Relations.Opinion(w, mira.Id, w.PlayerId)); // a quarter of 80
        Assert.Contains(Cast(w, "mira").Thoughts, t => t.DefId == "familiar");
        SimEvent uneasy = w.Log.All.Single(e => e.Key == "event.gift.familiar.uneasy");
        Assert.Equal("Oren looked at you warily, as if remembering something you had done.", EventText.Describe(w, uneasy));
        Assert.Equal(Relations.Opinion(atKnot, oren.Id, atKnot.PlayerId) - 12, Relations.Opinion(w, oren.Id, w.PlayerId));
        SimEvent back = w.Log.All.Single(e => e.Key == Fate.ReturnedKey);
        Assert.Contains(back.Id, knew.Causes);
        SimEvent woke = w.Log.All.Single(e => e.Key == "event.gift.familiar.awakened");
        Assert.Equal(knew.Id, woke.Causes.Single());
    }

    [Fact]
    public void AFamiliarFaceWhoBecomesYourFriendGrowsTheGift()
    {
        var sim = Simulation.Create(TestContent.Rolled, 10, new RebirthChoice("human", Gifts.FamiliarId));
        sim.Advance(Day + 6 * Hour);
        World dying = sim.World;
        foreach (string key in new[] { "mira", "oren" })
        {
            Character x = Cast(dying, key);
            Relations.ChangeOpinion(dying, x, dying.PlayerId, 100 - Relations.Opinion(dying, x.Id, dying.PlayerId), 0);
            Relations.ChangeOpinion(dying, You(dying), x.Id, 20, 0);
        }
        Assert.True(sim.ForceDeath());
        sim.Step();
        World w = sim.World;
        foreach (string key in new[] { "mira", "oren" })
        {
            Character x = Cast(w, key);
            Relations.ChangeOpinion(w, x, w.PlayerId, 40, 0);
            Relations.ChangeOpinion(w, You(w), x.Id, 10, 0);
        }
        sim.Advance(2 * Hour);
        w = sim.World;
        SimEvent grew = w.Log.All.Single(e => e.Key == "event.gift.grew");
        Story story = StoryEndingAt(w, grew.Id);
        Assert.Contains(SystemTag.Fate, story.Systems);
        Assert.Contains(SystemTag.Gift, story.Systems);
        Assert.Equal(1, w.Gift.Tier);
    }

    [Fact]
    public void AGiftThatHasNotWokenByItsDayArrivesInADream()
    {
        var sim = Simulation.Create(TestContent.Rolled, 11, new RebirthChoice("human", Gifts.FamiliarId));
        Assert.True(sim.AdvanceTo(9L * Day + 7 * Hour));
        SimEvent dream = sim.World.Log.All.Single(e => e.Key == "event.gift.familiar.dreamed");
        Assert.Equal(9L * Day + 6 * Hour, dream.Tick); // dawn of Day 10
        Assert.True(sim.World.Fate.GiftAwakened);
        Assert.Equal("You dreamed of faces you had never seen, who all knew your name.", EventText.Describe(sim.World, dream));
    }

    // ---- Shared rules ----

    [Fact]
    public void AStageReachedBeforeNeedsHalfTheCount()
    {
        Simulation sim = Camp(Gifts.SatchelId, 12, "mira");
        World w = sim.World;
        w.Fate.GiftAwakened = true;
        Return.Sim.Content.GiftDef g = Gifts.Held(w)!;
        Assert.Equal(3, Gifts.Needed(w, g, 1));
        for (int i = 0; i < 3; i++) Gifts.Count(w, 0);
        Assert.Equal(1, w.Gift.Tier);
        Assert.Equal(1, w.Fate.GiftMastery);
        w.Gift.Tier = 0; // as a return to a knot before it would
        w.Gift.Progress = 0;
        Assert.Equal(2, Gifts.Needed(w, g, 1));
        Assert.Equal(8, Gifts.Needed(w, g, 2));
        Gifts.Count(w, 0);
        Gifts.Count(w, 0);
        Assert.Equal(1, w.Gift.Tier);
    }

    [Fact]
    public void TheSatchelOrderIsYoursAlone()
    {
        Simulation sim = Camp(Gifts.SatchelId, 13, "mira");
        sim.Enqueue(new SetStandingOrder(0, "gift.satchel", "share"));
        sim.Enqueue(new SetStandingOrder(0, "gift.satchel", "hoard")); // not a choice: nothing
        sim.Enqueue(new SetStandingOrder(0, "gift.other", "share")); // not your gift's: nothing
        sim.Step();
        World w = sim.World;
        StandingOrder order = Assert.Single(w.StandingOrders);
        Assert.Equal(("gift.satchel", "share"), (order.OrderId, order.ChoiceId));
        Assert.Equal("You decided to share your satchel with anyone starving.", EventText.Describe(w, w.Log.All.Single(e => e.Key == "event.world.standing_order")));
        Assert.Equal("share", StateApi.Player(w).Gift!.Order);

        // Without the gift, the same order changes nothing.
        var plain = Simulation.Create(TestContent.Db, 13);
        plain.Enqueue(new SetStandingOrder(0, "gift.satchel", "share"));
        plain.Step();
        Assert.Empty(plain.World.StandingOrders);
    }

    [Fact]
    public void TheStandInPlayerPicksTheSatchelOrderBySeedOnceItKnowsTheGift()
    {
        var orders = new HashSet<string>();
        for (ulong seed = 1; seed <= 9; seed++)
        {
            Simulation sim = StoryLog.Simulate(TestContent.Rolled, seed, 1, new RunSetup(new RebirthChoice("human", Gifts.SatchelId)));
            orders.Add(Gifts.Order(sim.World, Gifts.Held(sim.World)!.Order!));
        }
        Assert.Equal(new[] { "empty", "keep", "share" }, orders.OrderBy(o => o, StringComparer.Ordinal));
    }

    /// <summary>A gift draws only from the Fate stream, and only when it acts (§10): no other stream is named in the gifts' code, and their draw tags appear nowhere else.</summary>
    [Fact]
    public void GiftDrawsStayInTheirStream()
    {
        string dir = Path.Combine(TestPaths.Root, "sim", "Systems", "Gifts");
        foreach (string file in Directory.GetFiles(dir, "*.cs"))
        {
            string text = File.ReadAllText(file);
            foreach (DiceSystem s in Enum.GetValues<DiceSystem>())
                if (s != DiceSystem.Fate) Assert.DoesNotContain("DiceSystem." + s + "]", text);
        }
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestPaths.Root, "sim"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith("DrawIds.cs", StringComparison.Ordinal)) continue;
            string text = File.ReadAllText(file);
            if (text.Contains("DiceId.LuckVictim", StringComparison.Ordinal)) Assert.EndsWith("Luck.cs", file);
            if (text.Contains("DiceId.FateKnot", StringComparison.Ordinal)) Assert.EndsWith("Fate.cs", file);
        }
        // Holding a gift that never acts draws nothing: a quiet day with Familiar faces and no return.
        var sim = Simulation.Create(TestContent.Rolled, 14, new RebirthChoice("human", Gifts.FamiliarId));
        sim.Advance(2 * Day);
        Assert.Equal(0, sim.World.Rng.Dice[DiceSystem.Fate].Draws);
    }
}
