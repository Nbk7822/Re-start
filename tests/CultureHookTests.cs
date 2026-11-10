using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using Return.Sim.Systems.Jobs;

namespace Return.Tests;

/// <summary>
/// Each culture hook (ADR-0023): its event key and system, what it cites and what it brings about.
/// Worlds are built, state set directly, and the system called, in the style of CausalityTests.
/// </summary>
public class CultureHookTests
{
    internal static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>Content where every custom of the named ids has its param forced (e.g. breach odds of 1000).</summary>
    internal static ContentDb Forced(params (string Custom, string Param, int Value)[] overrides)
    {
        var lines = new List<string>();
        foreach (CustomDef c in TestContent.Db.Customs)
        {
            var ps = new Dictionary<string, int>(c.Params);
            foreach ((string id, string param, int value) in overrides)
                if (id == c.Id) ps[param] = value;
            string args = string.Join(", ", ps.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"\"{p.Key}\": {p.Value}"));
            lines.Add($"{{ \"id\": \"{c.Id}\", \"kind\": \"{c.Kind}\", \"params\": {{ {args} }} }}");
        }
        string mod = CultureContentTests.Mod(("customs.json", "[" + string.Join(",\n", lines) + "]"));
        return ContentDb.Load(Path.Combine(TestPaths.Root, "content"), new[] { mod });
    }

    /// <summary>A camp of only these people, fed and rested by the hearth, from the given content.</summary>
    internal static World Camp(ContentDb db, ulong seed, params string[] keys)
    {
        World w = WorldGen.Create(db, seed);
        int i = 0;
        foreach (Character c in w.Characters)
        {
            if (!keys.Contains(c.CastKey)) { c.Status = CharStatus.OffMap; continue; }
            c.Status = CharStatus.Member;
            c.Food = 900; c.Rest = 900; c.Social = 900; c.Mood = 60;
            if (!w.Map.Passable(c.Pos)) c.Pos = new Point(w.Map.Hearth.X - 2 + i, w.Map.Hearth.Y + 2);
            i++;
        }
        return w;
    }

    /// <summary>Puts someone in a culture and faith, with a devotion; clears what they learned and whom they met.</summary>
    internal static void Belong(Character c, string culture, string faith = "", int devotion = 50)
    {
        c.CultureId = culture;
        c.FaithId = faith;
        c.Devotion = devotion;
        c.LearnedCustoms.Clear();
        c.Met.Clear();
    }

    /// <summary>Gives a seeded culture exactly these customs (the always-held greeting first, as seeding does).</summary>
    internal static void Hold(World w, string culture, params string[] customs)
    {
        SeededCulture s = Ways.CultureById(w, culture)!;
        s.CustomIds = customs.ToList();
    }

    internal static void HoldFaith(World w, string faith, params string[] customs)
    {
        SeededFaith s = Ways.FaithById(w, faith)!;
        s.CustomIds = customs.ToList();
    }

    // ---- G2, G4: first meetings ----

    [Fact]
    public void StrangersStumbleOnceAndLearn()
    {
        ContentDb db = Forced(("no_touch_greeting", "breachPermille", 1000), ("clasp_greeting", "breachPermille", 1000));
        World w = Camp(db, 1, "you", "mira", "oren", "nia", "bram");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren"), nia = Cast(w, "nia"), bram = Cast(w, "bram");
        Belong(you, "");
        Belong(mira, "reed"); Belong(oren, "reed");
        Belong(nia, "hearth"); Belong(bram, "hearth");
        Hold(w, "reed", "no_touch_greeting");
        Hold(w, "hearth", "clasp_greeting");

        int before = w.Log.All.Count;
        Assert.True(Greetings.FirstMeeting(w, you, mira));
        SimEvent breach = w.Log.All.Last();
        Assert.Equal("event.culture.no_touch_greeting.breach", breach.Key);
        Assert.Equal(SystemTag.Culture, breach.System);
        Assert.Equal(new[] { you.Id, mira.Id }, breach.Actors);
        Assert.Empty(breach.Causes);
        Assert.Contains(mira.Thoughts, t => t.DefId == "faux_pas" && t.AboutId == you.Id && t.CauseEvent == breach.Id);
        Assert.Equal(new[] { "no_touch_greeting" }, you.LearnedCustoms);
        Assert.Equal("You took Mira by the hand at their first meeting; " + db.Loc.Get(Ways.CultureById(w, "reed")!.NameKey) + " do not touch a stranger.",
            Return.Sim.Api.EventText.Describe(w, breach));

        // Learned: the next of that people is greeted properly.
        Assert.False(Greetings.FirstMeeting(w, you, oren));
        // Met before: nothing at all.
        Assert.False(Greetings.FirstMeeting(w, you, mira));
        Assert.Equal(before + 1, w.Log.All.Count);

        // Kin greet each other, with no event.
        Assert.False(Greetings.FirstMeeting(w, nia, bram));
        Assert.Contains(nia.Thoughts, t => t.DefId == "kin_greeting" && t.AboutId == bram.Id);
        Assert.Contains(bram.Thoughts, t => t.DefId == "kin_greeting" && t.AboutId == nia.Id);
        Assert.Equal(before + 1, w.Log.All.Count);
        Assert.Equal(new[] { bram.Id }, nia.Met);
    }

    [Fact]
    public void NobodyBreachesAGreetingTheyShare()
    {
        ContentDb db = Forced(("clasp_greeting", "breachPermille", 1000));
        World w = Camp(db, 1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        Belong(mira, "reed"); Belong(oren, "hearth");
        Hold(w, "reed", "clasp_greeting");
        Hold(w, "hearth", "clasp_greeting");
        Assert.False(Greetings.FirstMeeting(w, mira, oren));
        Assert.DoesNotContain(w.Log.All, e => e.System == SystemTag.Culture);
    }

    [Fact]
    public void TheOtherworlderStumblesThreeTimesAsOften()
    {
        int Breaches(bool outsider)
        {
            int n = 0;
            for (ulong seed = 1; seed <= 300; seed++)
            {
                World w = Camp(TestContent.Db, seed % 7 + 1, "you", "mira");
                w.Rng = Rng.FromSeed(seed);
                Character you = Cast(w, "you"), mira = Cast(w, "mira");
                Belong(you, outsider ? "" : "night");
                Belong(mira, "reed");
                Hold(w, "reed", "no_touch_greeting");
                Hold(w, "night", "veiled_greeting");
                if (Greetings.FirstMeeting(w, you, mira)) n++;
            }
            return n;
        }
        int outsider = Breaches(true), member = Breaches(false);
        Assert.InRange(member, 15, 60);      // 120 per mille of 300
        Assert.InRange(outsider, 75, 145);   // 360 per mille of 300
    }

    // ---- G1, G3, A1: warmth and insults ----

    [Fact]
    public void KinWarmStrangersCoolAndStancesLean()
    {
        World w = Camp(TestContent.Db, 1, "you", "mira", "oren", "ilex");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren"), ilex = Cast(w, "ilex");
        Belong(mira, "hearth"); Belong(oren, "hearth"); Belong(ilex, "night"); Belong(you, "");
        Assert.Equal(100 + CultureTuning.KinWarmth, Greetings.Warmth(w, mira, oren, 0));
        Assert.Equal(100, Greetings.Warmth(w, mira, you, 0));
        ilex.ArrivedTick = 1; w.Tick = 2;
        Assert.Equal(100 + CultureTuning.StrangerWarmth - 25, Greetings.Warmth(w, mira, ilex, 0)); // a newcomer, and the Hearthfolk are wary of vampires
        Assert.Equal(100 + CultureTuning.StrangerWarmth, Greetings.Warmth(w, ilex, mira, 0));
        Assert.Equal(0, Greetings.Warmth(w, mira, ilex, -2));
        Belong(mira, "reed");
        Assert.Equal(40, Ways.StanceInsult(w, mira, ilex));
        Assert.Equal(0, Ways.StanceInsult(w, ilex, mira));
    }

    // ---- Blood stance: taboo, gift, and stance in consent ----

    /// <summary>Puts b on a passable tile next to a, so jobs between them start in reach.</summary>
    internal static void NextTo(World w, Character a, Character b)
    {
        foreach (Point p in new[] { new Point(a.Pos.X + 1, a.Pos.Y), new Point(a.Pos.X - 1, a.Pos.Y), new Point(a.Pos.X, a.Pos.Y + 1), new Point(a.Pos.X, a.Pos.Y - 1) })
            if (w.Map.Passable(p)) { b.Pos = p; return; }
        throw new InvalidOperationException("no free tile next to " + a.CastKey);
    }

    private static (World W, Character Donor, Character Vampire) BloodCamp()
    {
        World w = Camp(TestContent.Db, 1, "mira", "ilex");
        Character mira = Cast(w, "mira"), ilex = Cast(w, "ilex");
        mira.Traits = new List<string> { "protective", "practical" };
        ilex.Traits = new List<string> { "proud", "practical" };
        Belong(mira, "night");
        Belong(ilex, "night");
        Hold(w, "night", "veiled_greeting", "blood_taboo");
        w.Tick = 22L * w.Content.World.TicksPerHour; // night: no sun
        NextTo(w, mira, ilex);
        return (w, mira, ilex);
    }

    [Fact]
    public void BloodTabooRefusesAndSaysWhy()
    {
        (World w, Character mira, Character ilex) = BloodCamp();
        Relations.ChangeOpinion(w, mira, ilex.Id, 20, 0);
        Assert.Equal("event.culture.blood_taboo.refused", BloodJobs.RefusalKey(w, mira, ilex));

        SimEvent thirst = w.Log.Emit(w, SystemTag.Needs, "event.needs.thirsting", new[] { ilex.Id }, Array.Empty<long>());
        var job = new Job { Kind = JobKind.RequestBlood, TargetId = mira.Id, Target = mira.Pos, CauseEvent = thirst.Id };
        Assert.True(BloodJobs.Request(w, ilex, job));
        SimEvent ask = w.Log.All.Single(e => e.Key == "event.social.blood_request");
        SimEvent no = w.Log.All.Last();
        Assert.Equal("event.culture.blood_taboo.refused", no.Key);
        Assert.Equal(SystemTag.Culture, no.System);
        Assert.Equal(new[] { ask.Id }, no.Causes);
        Assert.Equal(ilex.LastFoodEvent, no.Id);
        string nightName = w.Content.Loc.Get(Ways.CultureById(w, "night")!.NameKey);
        Assert.Equal("Mira refused to give Ilex blood; " + nightName + " forbid it.", Return.Sim.Api.EventText.Describe(w, no));
        // Not personal: 40% of the plain refusal's sting (Ilex is Proud, so the plain one is 160%).
        Assert.Equal(-10 * (160 * CultureTuning.CustomRefusalOpinionPct / 100) / 100, Relations.Opinion(w, ilex.Id, mira.Id));
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.BloodRefused && d.EventId == no.Id && d.DoerId == mira.Id);

        // Without the taboo the same donor consents; someone who would refuse anyway gives the plain reason.
        Hold(w, "night", "veiled_greeting");
        Assert.Null(BloodJobs.RefusalKey(w, mira, ilex));
        Hold(w, "night", "veiled_greeting", "blood_taboo");
        Relations.ChangeOpinion(w, mira, ilex.Id, -25, 0);
        Assert.Equal("event.social.blood_refused", BloodJobs.RefusalKey(w, mira, ilex));
    }

    [Fact]
    public void AStanceMovesConsent()
    {
        (World w, Character mira, Character ilex) = BloodCamp();
        Hold(w, "night", "veiled_greeting");
        Belong(mira, "hearth"); // the Hearthfolk are wary of vampires: consent needs an opinion of 10
        Hold(w, "hearth", "clasp_greeting");
        Relations.ChangeOpinion(w, mira, ilex.Id, 9, 0);
        Assert.Equal("event.social.blood_refused", BloodJobs.RefusalKey(w, mira, ilex));
        Relations.ChangeOpinion(w, mira, ilex.Id, 1, 0);
        Assert.Null(BloodJobs.RefusalKey(w, mira, ilex));
        Belong(mira, "ashen"); // the Ashen revere them: consent even when cool toward them
        Hold(w, "ashen", "bow_greeting");
        Relations.ChangeOpinion(w, mira, ilex.Id, -40, 0);
        Assert.Null(BloodJobs.RefusalKey(w, mira, ilex));
    }

    [Fact]
    public void BloodGiftOffersUnasked()
    {
        (World w, Character mira, Character ilex) = BloodCamp();
        Hold(w, "night", "veiled_greeting");
        Belong(mira, "night", "ember", 50);
        HoldFaith(w, "ember", "blood_gift");
        ilex.Food = 100;
        SimEvent thirst = w.Log.Emit(w, SystemTag.Needs, "event.needs.thirsting", new[] { ilex.Id }, Array.Empty<long>());
        ilex.LastFoodEvent = thirst.Id;

        Job? job = JobPicker.Choose(w, mira);
        Assert.Equal(JobKind.GiveBlood, job?.Kind);
        Assert.Equal(ilex.Id, job!.TargetId);
        Assert.False(BloodJobs.Give(w, mira, job));
        SimEvent offer = w.Log.All.Last();
        Assert.Equal("event.culture.blood_gift.offered", offer.Key);
        Assert.Equal(SystemTag.Culture, offer.System);
        Assert.Equal(new[] { thirst.Id }, offer.Causes);
        Assert.Equal(JobKind.RequestBlood, ilex.Job?.Kind);

        // Lapsed: the faith no longer binds, so no offer. Generous: the person's own offer, the plain key.
        mira.Devotion = 10;
        Assert.NotEqual(JobKind.GiveBlood, JobPicker.Choose(w, mira)?.Kind);
        mira.Traits = new List<string> { "generous", "practical" };
        ilex.Job = null;
        var generous = new Job { Kind = JobKind.GiveBlood, TargetId = ilex.Id, Target = ilex.Pos, CauseEvent = thirst.Id };
        BloodJobs.Give(w, mira, generous);
        Assert.Equal("event.social.blood_offered", w.Log.All.Last().Key);
    }

    // ---- O1, O2, H2: how a people weighs a promise ----

    private static SimEvent Mark(World w, string key, params int[] actors) => w.Log.Emit(w, SystemTag.Incident, key, actors, Array.Empty<long>());

    [Fact]
    public void PromisesWeighByCulture()
    {
        World w = Camp(TestContent.Db, 1, "you", "mira", "oren", "nia");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren"), nia = Cast(w, "nia");
        Belong(mira, "hearth"); Belong(oren, "hearth"); Belong(nia, "reed");
        Hold(w, "hearth", "clasp_greeting");
        Hold(w, "reed", "no_touch_greeting");
        SimEvent why = Mark(w, "event.test.why");

        // The Hearthfolk hold a promise heavy (150%): a heavier grudge, and word spreads to their kin.
        Assert.Equal(150, Ways.OathWeightPct(w, mira));
        SimEvent broken = Relations.Break(w, Relations.FormDebt(w, you, mira, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
        Assert.Equal(60, Relations.ActiveGrudge(w, mira.Id, you.Id)!.Intensity);
        Assert.Equal(-30 * 150 / 100, Relations.Opinion(w, mira.Id, you.Id));
        SimEvent spread = w.Log.All.Single(e => e.Key == "event.culture.word_spread");
        Assert.Equal(SystemTag.Culture, spread.System);
        Assert.Equal(new[] { broken.Id }, spread.Causes);
        Assert.Equal(new[] { you.Id, oren.Id }, spread.Actors);
        Assert.Contains(oren.Thoughts, t => t.DefId == "heard_oath_broken" && t.AboutId == you.Id && t.CauseEvent == spread.Id);
        Assert.DoesNotContain(nia.Thoughts, t => t.DefId == "heard_oath_broken");
        Assert.Equal("Word that you broke a promise spread among " + w.Content.Loc.Get(Ways.CultureById(w, "hearth")!.NameKey) + ".",
            Return.Sim.Api.EventText.Describe(w, spread));

        // The Reed Folk take a promise lightly (50%): a lighter grudge, and no word spreads.
        Assert.Equal(50, Ways.OathWeightPct(w, nia));
        Relations.Break(w, Relations.FormDebt(w, you, nia, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
        Assert.Equal(20, Relations.ActiveGrudge(w, nia.Id, you.Id)!.Intensity);
        Assert.Single(w.Log.All, e => e.Key == "event.culture.word_spread");

        // A kept promise warms by the same weight.
        Relations.Repay(w, Relations.FormDebt(w, mira, oren, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
        Assert.Equal(14 * 150 / 100, Relations.Opinion(w, oren.Id, mira.Id));
    }

    [Fact]
    public void AWrongedGuestHoldsAHeavierGrudge()
    {
        World w = Camp(TestContent.Db, 1, "you", "hollis");
        Character you = Cast(w, "you"), hollis = Cast(w, "hollis");
        Belong(hollis, "night");
        Hold(w, "night", "veiled_greeting", "guest_bread");
        hollis.ArrivedTick = 10;
        w.Tick = 20;
        SimEvent why = Mark(w, "event.test.why");
        SimEvent broken = Relations.Break(w, Relations.FormDebt(w, you, hollis, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
        SimEvent wronged = w.Log.All.Single(e => e.Key == "event.culture.guest_bread.wronged");
        Assert.Equal(SystemTag.Culture, wronged.System);
        Assert.Equal(new[] { broken.Id }, wronged.Causes);
        Grudge g = Relations.ActiveGrudge(w, hollis.Id, you.Id)!;
        Assert.Equal(60, g.Intensity); // 40 × 150% at an oath weight of 100%
        Assert.Equal(wronged.Id, g.CauseEvent);
        Assert.Contains(hollis.Thoughts, t => t.DefId == "guest_right_broken" && t.CauseEvent == wronged.Id);
        Assert.DoesNotContain(hollis.Thoughts, t => t.DefId == "promise_broken");
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.GuestWronged && d.EventId == wronged.Id && d.DoerId == you.Id);
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.OathBroken && d.EventId == broken.Id);

        // After their first week they are no longer a guest.
        Character mira = Cast(w, "mira");
        mira.Status = CharStatus.Member;
        Belong(mira, "night");
        mira.ArrivedTick = 10;
        w.Tick = 10 + 8L * w.Content.World.TicksPerDay;
        Relations.Break(w, Relations.FormDebt(w, you, mira, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
        Assert.Single(w.Log.All, e => e.Key == "event.culture.guest_bread.wronged");
        Assert.Equal(40, Relations.ActiveGrudge(w, mira.Id, you.Id)!.Intensity);
    }

    // ---- H1: guest-bread ----

    private static (World W, Character You, Character Host, Character Guest, Debt Promise) GuestCamp(ContentDb db)
    {
        World w = Camp(db, 1, "you", "mira", "hollis");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), hollis = Cast(w, "hollis");
        you.Traits = new List<string> { "practical", "anxious" };
        Belong(mira, "night"); Belong(hollis, "reed");
        Hold(w, "night", "veiled_greeting", "guest_bread");
        Hold(w, "reed", "no_touch_greeting");
        w.Tick = 2L * w.Content.World.TicksPerDay + 10L * w.Content.World.TicksPerHour;
        hollis.ArrivedTick = w.Tick - 300;
        hollis.Food = 280;
        SimEvent arrived = Mark(w, "event.test.arrived", hollis.Id);
        Debt promise = Relations.FormDebt(w, you, hollis, DebtKind.PromiseMeal, 1, arrived.Id, w.Tick + 48L * 300);
        hollis.LastFoodEvent = promise.FormedEvent;
        Stock.Take(w, "berries", 99);
        return (w, you, mira, hollis, promise);
    }

    private static void RunJob(World w, Character c, int ticks = 3_000)
    {
        for (int t = 0; t < ticks && c.Job != null; t++)
        {
            TravelSystem.Step(w);
            if (JobRunner.Tick(w, c, c.Job)) c.Job = null;
            w.Tick++;
        }
        Assert.Null(c.Job);
    }

    [Fact]
    public void GuestBreadKeepsTheCampsPromise()
    {
        (World w, Character you, Character mira, Character hollis, Debt promise) = GuestCamp(TestContent.Db);
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center);
        w.Reservations.Add(new Reservation { CharacterId = mira.Id, Count = 1 });
        mira.Food = 400; // hungry enough to hold a share, not starving

        Job? job = JobPicker.Choose(w, mira);
        Assert.Equal(JobKind.KeepPromise, job?.Kind);
        Assert.Equal(promise.Id, job!.TargetId);
        Assert.Single(w.Culture.GuestOffers, o => o.HostId == mira.Id && o.DebtId == promise.Id && o.Accepted);
        long hunger = hollis.LastFoodEvent;
        mira.Job = job;
        RunJob(w, mira);

        SimEvent shared = w.Log.All.Single(e => e.Key == "event.culture.guest_bread.shared");
        Assert.Equal(SystemTag.Culture, shared.System);
        Assert.Equal(new[] { promise.FormedEvent, hunger }.Distinct(), shared.Causes);
        SimEvent kept = w.Log.All.Single(e => e.Key == "event.social.promise_kept");
        Assert.Contains(shared.Id, kept.Causes);
        SimEvent owes = w.Log.All.Single(e => e.Key == "event.social.owes_meal");
        Assert.Equal(new[] { kept.Id }, owes.Causes);
        Assert.Equal(new[] { hollis.Id, mira.Id }, owes.Actors);
        Assert.False(Rations.HasReservation(w, mira.Id));
        Assert.Equal(shared.Id, mira.LastFoodEvent);
        Assert.Equal(DebtStatus.Repaid, promise.Status);
        Assert.Contains(hollis.Thoughts, t => t.DefId == "guest_bread_received" && t.AboutId == mira.Id);
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.GuestHonoured && d.DoerId == mira.Id);
    }

    [Fact]
    public void WithNoShareTheHostPicksTheGuestAMeal()
    {
        (World w, _, Character mira, Character hollis, Debt promise) = GuestCamp(TestContent.Db);
        Bush bush = w.Map.Bushes.First();
        bush.Berries = 1;
        foreach (Bush b in w.Map.Bushes) if (b != bush) b.Berries = 0;
        Job? job = JobPicker.Choose(w, mira);
        Assert.Equal(JobKind.KeepPromise, job?.Kind);
        Assert.Equal(bush.Pos, job!.Target);
        mira.Job = job;
        RunJob(w, mira, 6_000);
        Assert.Equal(0, bush.Berries);
        Assert.Single(w.Log.All, e => e.Key == "event.culture.guest_bread.foraged");
        Assert.Equal(DebtStatus.Repaid, promise.Status);
        Assert.True(hollis.Food > 280);
    }

    [Fact]
    public void AHostDecidesOncePerPromise()
    {
        (World w, _, Character mira, _, Debt promise) = GuestCamp(Forced(("guest_bread", "offerPermille", 0)));
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center);
        w.Reservations.Add(new Reservation { CharacterId = mira.Id, Count = 1 });
        Assert.NotEqual(JobKind.KeepPromise, JobPicker.Choose(w, mira)?.Kind);
        long draws = w.Rng.Dice[DiceSystem.Culture].Draws;
        Assert.Single(w.Culture.GuestOffers, o => o.HostId == mira.Id && o.DebtId == promise.Id && !o.Accepted);
        Assert.NotEqual(JobKind.KeepPromise, JobPicker.Choose(w, mira)?.Kind);
        Assert.Equal(draws, w.Rng.Dice[DiceSystem.Culture].Draws);
        Assert.Single(w.Culture.GuestOffers);
    }

    [Fact]
    public void AShareOutDoesNotBreakAPromiseWhileAHostIsOnTheWay()
    {
        (World w, Character you, Character mira, Character hollis, Debt promise) = GuestCamp(TestContent.Db);
        hollis.HungerStage = 1;
        you.Food = 100; mira.Food = 900;
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center);
        mira.Job = new Job { Kind = JobKind.KeepPromise, TargetId = promise.Id, Target = w.Map.Stockpile.Center };
        Rations.Assign(w); // you are hungrier: Hollis is left out, hungry
        Assert.Equal(DebtStatus.Open, promise.Status);
        mira.Job = null;
        hollis.SkippedRation = false;
        Rations.Assign(w);
        Assert.Equal(DebtStatus.Broken, promise.Status);
    }

    [Fact]
    public void StancesShiftWarmthInsultAndConsent()
    {
        Assert.Equal((50, -20, 30), CultureTuning.Stance("revere"));
        Assert.Equal((-25, 20, -10), CultureTuning.Stance("wary"));
        Assert.Equal((-50, 40, -30), CultureTuning.Stance("fear"));
        Assert.Equal((-25, 60, -20), CultureTuning.Stance("scorn"));
        Assert.Equal((0, 0, 0), CultureTuning.Stance(null));

        World w = Camp(TestContent.Db, 1, "mira", "ilex");
        Character mira = Cast(w, "mira"), ilex = Cast(w, "ilex");
        foreach ((string culture, string? stance) in new[] { ("hearth", "wary"), ("reed", "fear"), ("ashen", "revere"), ("night", (string?)null) })
        {
            Belong(mira, culture);
            Assert.Equal(stance, Ways.Stance(w, mira, ilex));
            Assert.Equal(CultureTuning.Stance(stance).Consent, Ways.StanceConsent(w, mira, ilex));
        }
        Belong(mira, "");
        Assert.Null(Ways.Stance(w, mira, ilex));
    }
}
