using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using Return.Sim.Systems.Jobs;
using static Return.Tests.CultureHookTests;

namespace Return.Tests;

/// <summary>
/// The night watch kept in turns (ADR-0029; Schedule design 4.3–4.4, tests 5–8, 10, 12, 16, 17): handovers,
/// each reason to refuse with its cause, covers and the debts they repay or open, staying on and leaving, and the
/// empty post as the cause of a theft.
/// </summary>
public class WatchTests
{
    private static long Hours(World w, double h) => (long)(h * w.Content.World.TicksPerHour);

    private static Simulation Camp(ulong seed, params string[] keys) => ScheduleCommandTests.Camp(seed, keys);

    private static void Plain(Character c, params string[] traits) => c.Traits = traits.ToList();

    private static SimEvent Note(World w, string key, params int[] actors) => w.Log.Emit(w, SystemTag.Needs, key, actors, Array.Empty<long>());

    /// <summary>Puts someone to bed: in a bed, asleep, in a sleep job.</summary>
    internal static void Asleep(World w, Character c)
    {
        c.Pos = Places.SleepSpot(w, c);
        c.Path.Clear();
        c.Job = new Job { Kind = JobKind.Sleep, Target = c.Pos, Stage = 1, StartedTick = w.Tick };
        c.Asleep = true;
    }

    /// <summary>Puts someone at the post, holding a turn that ends at <paramref name="turnEnds"/>; returns the event that began it.</summary>
    internal static SimEvent OnWatch(World w, Character c, long turnEnds)
    {
        SimEvent began = w.Log.Emit(w, SystemTag.Work, "event.work.took_watch", new[] { c.Id }, Array.Empty<long>(), null, EventArg.Char("a", c.Id));
        c.Pos = w.Map.Passable(w.Map.WatchPost) ? w.Map.WatchPost : Places.Adjacent(w, w.Map.WatchPost, w.Map.WatchPost);
        c.Path.Clear();
        c.Asleep = false;
        c.Job = new Job { Kind = JobKind.KeepWatch, Target = c.Pos, Stage = 1, StartedTick = w.Tick, CauseEvent = began.Id };
        WatchState s = w.Watch;
        s.Active = true;
        s.WatcherId = c.Id;
        s.TurnEndsTick = turnEnds;
        s.TurnEvent = began.Id;
        s.FirstTurnTaken = true;
        s.EmptySinceTick = -1;
        return began;
    }

    private static SimEvent? RunUntil(Simulation s, Func<SimEvent, bool> match, long maxTicks)
    {
        int seen = s.World.Log.All.Count;
        for (long t = 0; t < maxTicks; t++)
        {
            s.Step();
            for (; seen < s.World.Log.All.Count; seen++)
                if (match(s.World.Log.All[seen])) return s.World.Log.All[seen];
        }
        return null;
    }

    private static SimEvent? RunUntil(Simulation s, string key, long maxTicks) => RunUntil(s, e => e.Key == key, maxTicks);

    // ---- Design test 5: a handover ----

    [Fact]
    public void AHandoverWalksToASleeperWhoWakesAndTakesThePost()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), mira = w.Characters.First(c => c.CastKey == "mira");
        Plain(you, "practical");
        Plain(mira, "protective");
        w.WatchFrom = 22;
        w.WatchTo = 6;
        w.Tick = Hours(w, 26);
        Asleep(w, mira);
        SimEvent began = OnWatch(w, you, w.Tick);
        s.Step();

        SimEvent wake = w.Log.All.Single(e => e.Key == "event.work.went_to_wake");
        Assert.Equal(new[] { you.Id, mira.Id }, wake.Actors);
        Assert.Contains(began.Id, wake.Causes);
        Assert.Equal("You went to wake Mira for their turn on watch.", EventText.Describe(w, wake));
        Assert.Equal(wake.Id, w.Watch.GapEvent); // the post stands empty while you walk, and this is why
        Assert.Equal(0, w.Watch.WatcherId);
        Assert.Equal(JobKind.WakeForWatch, you.Job?.Kind);

        SimEvent took = RunUntil(s, "event.work.took_watch", 4_000)!;
        Assert.Equal(mira.Id, took.Actors[0]);
        Assert.Equal(new[] { wake.Id }, took.Causes);
        Assert.Equal(mira.Id, w.Watch.WatcherId);
        Assert.Equal(0, w.Watch.GapEvent);
        Assert.Equal(took.Id, mira.LastDutyEvent);
        Assert.Contains(mira.Thoughts, t => t.DefId == "woken");
        Assert.NotEqual(0, you.LastWatchEndTick);
        RunUntil(s, _ => false, 600);
        Assert.True(you.Asleep, $"you: {you.Job?.Kind}"); // back to bed: it is your sleeping time
    }

    [Fact]
    public void SomeoneAwakeIsAskedFirstAndThePostIsNeverEmpty()
    {
        Simulation s = Camp(1, "you", "mira", "vale");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), mira = w.Characters.First(c => c.CastKey == "mira"),
            vale = w.Characters.First(c => c.CastKey == "vale");
        Plain(you, "practical");
        Plain(mira, "protective");
        Plain(vale, "anxious");
        w.Tick = Hours(w, 26);
        Asleep(w, mira);
        SimEvent began = OnWatch(w, you, w.Tick);
        vale.Job = null;
        s.Step();
        Assert.Equal(vale.Id, w.Watch.IncomingId);
        Assert.Equal(you.Id, w.Watch.WatcherId); // you wait at the post until Vale arrives
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.work.went_to_wake");
        SimEvent took = RunUntil(s, "event.work.took_watch", 3_000)!;
        Assert.Equal(vale.Id, took.Actors[0]);
        Assert.Equal(new[] { began.Id }, took.Causes);
        Assert.Equal(0, w.Watch.GapEvent);
        Assert.NotEqual(JobKind.KeepWatch, you.Job?.Kind);
        Assert.DoesNotContain(mira.Thoughts, t => t.DefId == "woken");
    }

    [Fact]
    public void NobodyIsHandedTheWatchInItsLastHour()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), mira = w.Characters.First(c => c.CastKey == "mira");
        Plain(you, "practical");
        Plain(mira, "protective");
        w.Tick = Hours(w, 24 + 5.5);
        Asleep(w, mira);
        OnWatch(w, you, w.Tick);
        s.Step();
        Assert.Equal(you.Id, w.Watch.WatcherId);
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.work.went_to_wake");
        RunUntil(s, _ => false, Hours(w, 0.6));
        Assert.Equal(0, w.Watch.WatcherId); // the watch hours ended: off duty, quietly
        Assert.False(w.Watch.Active);
        Assert.DoesNotContain(w.Log.All, e => e.Key is "event.work.left_watch_unrelieved" or "event.work.watch_unkept");
    }

    // ---- Design test 6: each reason to refuse cites its cause ----

    [Fact]
    public void EachReasonToRefuseCitesItsCause()
    {
        World w = Camp(1, "you", "wren", "nia", "mira").World;
        Character you = w.Characters.First(c => c.CastKey == "you"), wren = w.Characters.First(c => c.CastKey == "wren"),
            nia = w.Characters.First(c => c.CastKey == "nia"), mira = w.Characters.First(c => c.CastKey == "mira");
        Plain(wren, "anxious");
        Plain(nia, "desperate");
        Plain(mira, "protective");

        SimEvent hurt = Note(w, "event.test.hurt", wren.Id);
        wren.Health = 650; // a scratch, to the Anxious
        wren.LastHealthEvent = hurt.Id;
        Assert.Equal(new WatchSystem.Reason("event.social.watch_refused_hurt", hurt.Id, true), WatchSystem.Refusal(w, wren, you, draw: false));
        wren.Health = 1000;

        SimEvent duty = Note(w, "event.test.duty", wren.Id);
        wren.Rest = 250; // tired, to the Anxious
        wren.LastDutyEvent = duty.Id;
        Assert.Equal(new WatchSystem.Reason("event.social.watch_refused_tired", duty.Id, true), WatchSystem.Refusal(w, wren, you, draw: false));
        SimEvent worn = Note(w, "event.test.worn", wren.Id);
        wren.WornOutEvent = worn.Id;
        Assert.Equal(worn.Id, WatchSystem.Refusal(w, wren, you, draw: false)!.Value.Cause); // worn out comes first
        mira.Rest = 250;
        Assert.Null(WatchSystem.Refusal(w, mira, you, draw: false)); // tired only below 150 for anyone else
        mira.Rest = 900;

        SimEvent hunger = Note(w, "event.test.hunger", nia.Id);
        nia.Food = 210; // below the Desperate's mark
        nia.LastFoodEvent = hunger.Id;
        Assert.Equal(new WatchSystem.Reason("event.social.watch_refused_hungry", hunger.Id, true), WatchSystem.Refusal(w, nia, you, draw: false));

        SimEvent why = Note(w, "event.test.why", mira.Id);
        Grudge g = Relations.FormGrudge(w, mira, you, 30, why.Id);
        Assert.Equal(new WatchSystem.Reason("event.social.watch_refused_grudge", g.FormedEvent, false), WatchSystem.Refusal(w, mira, you, draw: false));
        Assert.Null(WatchSystem.Refusal(w, mira, wren, draw: false)); // only for whoever she bears the grudge
    }

    private static (bool Refused, string Hash) WakeAnIndependent(ulong seed)
    {
        Simulation s = Camp(seed, "you", "sera", "oren");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), sera = w.Characters.First(c => c.CastKey == "sera"),
            oren = w.Characters.First(c => c.CastKey == "oren");
        Plain(you, "practical");
        Plain(sera, "independent");
        Plain(oren, "oathbound");
        oren.Priorities.Find(p => p.WorkId == "keep_watch")!.Priority = 0; // never asked: he only sees
        w.Tick = Hours(w, 26);
        Asleep(w, sera);
        Point beside = Places.Adjacent(w, sera.Pos, sera.Pos);
        oren.Pos = beside;
        oren.Job = new Job { Kind = JobKind.Wander, Target = beside, TargetId = 100_000, Stage = 0, StartedTick = w.Tick };
        OnWatch(w, you, w.Tick);
        SimEvent? answer = RunUntil(s, e => e.Key.StartsWith("event.social.watch_refused", StringComparison.Ordinal) || e.Key == "event.work.took_watch", 4_000);
        Assert.NotNull(answer);
        if (answer!.Key == "event.work.took_watch") return (false, s.StateHash());
        Assert.Equal("event.social.watch_refused_own_way", answer.Key);
        Assert.Equal("Sera told you to find someone else.", EventText.Describe(w, answer));
        SimEvent wake = w.Log.All.Last(e => e.Key == "event.work.went_to_wake");
        Assert.Contains(wake.Id, answer.Causes);
        Assert.Contains(oren.Thoughts, t => t.DefId == "saw_rule_broken" && t.AboutId == sera.Id && t.CauseEvent == answer.Id);
        Assert.Contains(you.Thoughts, t => t.DefId == "watch_refused" && t.AboutId == sera.Id);
        Assert.Contains(sera.Thoughts, t => t.DefId == "refused_someone" && t.AboutId == you.Id);
        return (true, s.StateHash());
    }

    /// <summary>Going one's own way is the one compliance roll (DiceId.WatchDefy): the same seed gives the same answer, and an Oathbound witness thinks less of the refuser.</summary>
    [Fact]
    public void AnIndependentWokenForTheWatchMayGoTheirOwnWay()
    {
        bool refused = false, came = false;
        for (ulong seed = 1; seed <= 60 && !(refused && came); seed++)
        {
            (bool no, string hash) = WakeAnIndependent(seed);
            Assert.Equal(hash, WakeAnIndependent(seed).Hash);
            if (no) refused = true;
            else came = true;
        }
        Assert.True(refused && came, $"refused {refused}, came {came}");
    }

    // ---- Design test 7: covers ----

    private static (Simulation S, Character You, Character Wren, Character Mira) CoverCamp()
    {
        Simulation s = Camp(1, "you", "wren", "mira");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), wren = w.Characters.First(c => c.CastKey == "wren"),
            mira = w.Characters.First(c => c.CastKey == "mira");
        Plain(you, "practical");
        Plain(wren, "anxious");
        Plain(mira, "practical");
        w.Tick = Hours(w, 26);
        Asleep(w, wren);
        Asleep(w, mira);
        mira.LastWatchEndTick = 1; // Wren is due first
        OnWatch(w, you, w.Tick);
        return (s, you, wren, mira);
    }

    [Fact]
    public void KeepingAHurtRefusersTurnRepaysWhatTheKeeperOwedThem()
    {
        (Simulation s, Character you, Character wren, Character mira) = CoverCamp();
        World w = s.World;
        SimEvent hurt = Note(w, "event.test.hurt", wren.Id);
        wren.Health = 650;
        wren.LastHealthEvent = hurt.Id;
        Debt owed = Relations.FormDebt(w, mira, wren, DebtKind.Care, 1, Note(w, "event.test.tended", wren.Id, mira.Id).Id);

        SimEvent cover = RunUntil(s, "event.work.covered_watch", 5_000)!;
        SimEvent refused = w.Log.All.Single(e => e.Key == "event.social.watch_refused_hurt");
        Assert.Equal("Wren, still hurt, would not take the watch from you.", EventText.Describe(w, refused));
        Assert.Contains(hurt.Id, refused.Causes);
        Assert.Equal(new[] { mira.Id, wren.Id }, cover.Actors);
        Assert.Contains(refused.Id, cover.Causes);
        Assert.Equal("Mira kept the watch in Wren's place.", EventText.Describe(w, cover));
        SimEvent repaid = w.Log.All.Single(e => e.Key == "event.social.debt_repaid");
        Assert.Contains(cover.Id, repaid.Causes);
        Assert.Equal(DebtStatus.Repaid, owed.Status);
        Assert.Contains(wren.Thoughts, t => t.DefId == "watch_taken_for_me" && t.AboutId == mira.Id);
        Assert.Equal(mira.Id, w.Watch.WatcherId);
        Assert.DoesNotContain(you.Thoughts, t => t.DefId == "watch_refused"); // a good reason is not held against her
    }

    [Fact]
    public void KeepingAHurtRefusersTurnWithNothingOwedOpensAWatchDebt()
    {
        (Simulation s, Character you, Character wren, Character mira) = CoverCamp();
        World w = s.World;
        wren.Health = 650;
        wren.LastHealthEvent = Note(w, "event.test.hurt", wren.Id).Id;
        SimEvent cover = RunUntil(s, "event.work.covered_watch", 5_000)!;
        SimEvent owes = w.Log.All.Single(e => e.Key == "event.social.owes_watch");
        Assert.Equal(new[] { cover.Id }, owes.Causes);
        Effect formed = Assert.Single(owes.Effects);
        Assert.Equal((EffectKind.DebtFormed, wren.Id, mira.Id), (formed.Kind, formed.A, formed.B));
        Assert.Equal("Wren owes Mira for a watch kept in their place.", EventText.Describe(w, owes));
        Assert.Contains(w.Debts, d => d.Kind == DebtKind.Watch && d.DebtorId == wren.Id && d.CreditorId == mira.Id && d.Status == DebtStatus.Open);
        Assert.Equal("a watch kept for them", StateApi.Character(w, wren.Id).Debts.Single().Kind);
    }

    [Fact]
    public void KeepingTheTurnOfSomeoneWhoWouldNotGetUpSoursTheKeeper()
    {
        (Simulation s, Character you, Character wren, Character mira) = CoverCamp();
        World w = s.World;
        Relations.FormGrudge(w, wren, you, 30, Note(w, "event.test.why", wren.Id).Id);
        Relations.ChangeOpinion(w, mira, wren.Id, -35, 0);
        SimEvent took = RunUntil(s, "event.work.took_watch", 5_000)!;
        Assert.Equal(mira.Id, took.Actors[0]);
        SimEvent refused = w.Log.All.Single(e => e.Key == "event.social.watch_refused_grudge");
        Assert.Equal("Wren would not get up for you.", EventText.Describe(w, refused));
        Assert.Contains(you.Thoughts, t => t.DefId == "watch_refused" && t.AboutId == wren.Id);
        Assert.Contains(mira.Thoughts, t => t.DefId == "covered_for" && t.AboutId == wren.Id);
        Grudge g = Relations.ActiveGrudge(w, mira.Id, wren.Id)!; // opinion sank past -40
        Assert.Contains(refused.Id, w.Log.Get(g.FormedEvent)!.Causes);
    }

    // ---- Design test 8: staying on, leaving for food, asleep at the post ----

    [Fact]
    public void AnOathboundWatcherStaysOneTurnWhenNobodyComesAndThenLeaves()
    {
        Simulation s = Camp(1, "oren");
        World w = s.World;
        Character oren = w.Characters.First(c => c.CastKey == "oren");
        Plain(oren, "oathbound");
        w.WatchFrom = 18;
        w.WatchTo = 12;
        w.Tick = Hours(w, 24 + 19);
        SimEvent began = OnWatch(w, oren, w.Tick);
        s.Step();
        SimEvent stayed = w.Log.All.Single(e => e.Key == "event.work.stayed_on_watch");
        Assert.Equal("Oren stayed on watch when nobody came to take it.", EventText.Describe(w, stayed));
        Assert.Contains(began.Id, stayed.Causes);
        Assert.Equal(oren.Id, w.Watch.WatcherId);
        Assert.True(w.Watch.Extended);
        Assert.Equal(stayed.Id, oren.LastDutyEvent);

        SimEvent left = RunUntil(s, "event.work.left_watch_unrelieved", Hours(w, 4) + 60)!;
        Assert.Contains(stayed.Id, left.Causes);
        Assert.Equal(left.Id, w.Watch.GapEvent);
        Assert.Equal(0, w.Watch.WatcherId);
        Assert.Equal("Oren left the watch; nobody came to take it.", EventText.Describe(w, left));
    }

    [Fact]
    public void ADesperateWatcherLeavesTheWatchForFood()
    {
        Simulation s = Camp(1, "nia");
        World w = s.World;
        Character nia = w.Characters.First(c => c.CastKey == "nia");
        Plain(nia, "desperate");
        w.Tick = Hours(w, 24 + 20) + 30;
        OnWatch(w, nia, w.Tick + Hours(w, 3));
        SimEvent hunger = Note(w, "event.test.hunger", nia.Id);
        nia.Food = 210;
        nia.LastFoodEvent = hunger.Id;
        RunUntil(s, _ => false, 30);
        SimEvent left = w.Log.All.Single(e => e.Key == "event.work.left_watch_hungry");
        Assert.Equal(new[] { hunger.Id }, left.Causes);
        Assert.Equal(left.Id, w.Watch.GapEvent);
        Assert.Equal(0, w.Watch.WatcherId);
        Assert.Equal("Nia left the watch to look for food.", EventText.Describe(w, left));
    }

    [Fact]
    public void AWatcherWhoFallsAsleepLetsAThiefThroughAndTheTheftSaysWhy()
    {
        Simulation s = Camp(1, "you");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you");
        Plain(you, "practical");
        w.Tick = Hours(w, 24 + 1);
        OnWatch(w, you, w.Tick + Hours(w, 3));
        SimEvent duty = Note(w, "event.test.duty", you.Id);
        you.LastDutyEvent = duty.Id;
        you.Rest = 50;
        Stock.Add(w, "berries", 5, w.Map.StartStockpile.Center);
        s.Step();
        SimEvent asleep = w.Log.All.Single(e => e.Key == "event.needs.fell_asleep_on_watch");
        Assert.Equal(new[] { duty.Id }, asleep.Causes);
        Assert.Equal("You fell asleep at the watch post.", EventText.Describe(w, asleep));
        Assert.True(you.Asleep);
        Assert.Equal(asleep.Id, w.Watch.GapEvent);

        IncidentSystem.Fire(w, "night_thief");
        SimEvent stolen = RunUntil(s, e => e.Key.StartsWith("event.incident.stolen", StringComparison.Ordinal), 4_000)!;
        Assert.Equal("event.incident.stolen_unwatched", stolen.Key);
        Assert.Contains(asleep.Id, stolen.Causes);
        Assert.StartsWith("A thief made off with", EventText.Describe(w, stolen));
        Assert.EndsWith("nobody was on watch.", EventText.Describe(w, stolen));
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.combat.thief_spotted");
    }

    [Fact]
    public void AWatcherWoundedAtThePostLeavesItAndTheWoundIsWhy()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), mira = w.Characters.First(c => c.CastKey == "mira");
        Plain(you, "practical");
        w.Tick = Hours(w, 24 + 1);
        Asleep(w, mira);
        OnWatch(w, you, w.Tick + Hours(w, 3));
        SimEvent hurt = Note(w, "event.test.wound", you.Id);
        you.Health = 300;
        you.LastHealthEvent = hurt.Id;
        WatchSystem.Step(w);
        Assert.Equal(0, w.Watch.WatcherId);
        Assert.NotEqual(JobKind.KeepWatch, you.Job?.Kind);
        Assert.Equal(hurt.Id, w.Watch.GapEvent);
        WatchSystem.Step(w); // not taken back on as the watcher on the next step
        Assert.Equal(0, w.Watch.WatcherId);
    }

    /// <summary>
    /// Every way off the post has a line (ADR-0029): a watcher drawn off it by a fight goes back to it with the turn not done,
    /// whatever they turned to next (a meal, their bed), unless they are now starving, when they leave it to look for food and say so.
    /// </summary>
    [Fact]
    public void AWatcherBackFromAFightGoesBackToThePostUnlessStarving()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), mira = w.Characters.First(c => c.CastKey == "mira");
        Plain(you, "practical");
        w.Tick = Hours(w, 24 + 1);
        Asleep(w, mira);
        OnWatch(w, you, w.Tick + Hours(w, 3));

        // Hungry but not starving, they turned to a meal when the fight ended: back to the post.
        you.Food = NeedsSystem.HungryBelow - 50;
        you.Job = new Job { Kind = JobKind.Eat, Target = w.Map.StartStockpile.Center, StartedTick = w.Tick };
        WatchSystem.Step(w);
        Assert.Equal(you.Id, w.Watch.WatcherId);
        Assert.Equal(JobKind.KeepWatch, you.Job?.Kind);

        // Spent, they lay down: woken and sent back, where the watch decides whether they fall asleep at the post.
        Asleep(w, you);
        WatchSystem.Step(w);
        Assert.Equal(you.Id, w.Watch.WatcherId);
        Assert.Equal(JobKind.KeepWatch, you.Job?.Kind);
        Assert.False(you.Asleep);
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.work.left_watch", StringComparison.Ordinal));

        // Starving, they leave it to look for food, and that is why it stands empty.
        SimEvent hunger = Note(w, "event.test.hunger", you.Id);
        you.LastFoodEvent = hunger.Id;
        you.Food = NeedsSystem.StarvingBelow - 10;
        you.Job = new Job { Kind = JobKind.Eat, Target = w.Map.StartStockpile.Center, StartedTick = w.Tick };
        WatchSystem.Step(w);
        Assert.Equal(0, w.Watch.WatcherId);
        SimEvent left = Assert.Single(w.Log.All, e => e.Key == "event.work.left_watch_hungry");
        Assert.Equal(new[] { hunger.Id }, left.Causes);
        Assert.Equal(left.Id, w.Watch.GapEvent);
        Assert.Equal(JobKind.Eat, you.Job?.Kind); // their meal, not the post
    }

    /// <summary>A watcher who leaves the camp, or dies, while holding the post: their going is why it stands empty.</summary>
    [Fact]
    public void AWatcherWhoLeavesTheCampLeavesThePostEmptyForThatReason()
    {
        Simulation s = Camp(1, "you", "mira");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), mira = w.Characters.First(c => c.CastKey == "mira");
        Plain(mira, "practical");
        w.Tick = Hours(w, 24 + 1);
        Asleep(w, you);
        OnWatch(w, mira, w.Tick + Hours(w, 3));
        SimEvent gone = w.Log.Emit(w, SystemTag.Social, "event.social.left_camp", new[] { mira.Id }, Array.Empty<long>(),
            new[] { new Effect { Kind = EffectKind.Departure, A = mira.Id } }, EventArg.Char("a", mira.Id));
        mira.Status = CharStatus.Left;
        mira.Job = null;
        w.Tick += 1;
        WatchSystem.Step(w);
        Assert.Equal(0, w.Watch.WatcherId);
        Assert.Equal(gone.Id, w.Watch.GapEvent);
    }

    [Fact]
    public void WithNoWatchKeptATheftCitesOnlyTheThief()
    {
        Simulation s = Camp(1, "you");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you");
        Plain(you, "practical");
        w.WatchFrom = w.WatchTo = 0;
        w.Tick = Hours(w, 24 + 1);
        Asleep(w, you);
        Stock.Add(w, "berries", 5, w.Map.StartStockpile.Center);
        SimEvent thief = IncidentSystem.Fire(w, "night_thief")!;
        SimEvent stolen = RunUntil(s, e => e.Key.StartsWith("event.incident.stolen", StringComparison.Ordinal), 4_000)!;
        Assert.Equal("event.incident.stolen", stolen.Key);
        Assert.Equal(new[] { thief.Id }, stolen.Causes);
    }

    // ---- Design test 10: a story through the watch ----

    /// <summary>Sera will not get up for you, you leave the post, a thief takes the berries, and by morning Nia takes Bram's meal: one story, refusal to grudge.</summary>
    [Fact]
    public void ARefusalEmptiesThePostAndAStoryRunsFromItToAGrudge()
    {
        Simulation s = Camp(1, "you", "sera", "nia", "bram");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you"), sera = w.Characters.First(c => c.CastKey == "sera"),
            nia = w.Characters.First(c => c.CastKey == "nia"), bram = w.Characters.First(c => c.CastKey == "bram");
        Plain(you, "practical");
        Plain(sera, "practical");
        Plain(nia, "desperate");
        Plain(bram, "practical");
        foreach (Character c in new[] { nia, bram }) c.Priorities.Find(p => p.WorkId == "keep_watch")!.Priority = 0;
        w.Tick = Hours(w, 24 + 23);
        Asleep(w, sera);
        Asleep(w, nia);
        Asleep(w, bram);
        Relations.FormGrudge(w, sera, you, 30, Note(w, "event.test.old_quarrel", sera.Id).Id);
        OnWatch(w, you, w.Tick);
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 3, w.Map.StartStockpile.Center);

        SimEvent left = RunUntil(s, "event.work.left_watch_unrelieved", 4_000)!;
        SimEvent refused = w.Log.All.Single(e => e.Key == "event.social.watch_refused_grudge");
        IncidentSystem.Fire(w, "night_thief");
        SimEvent stolen = RunUntil(s, e => e.Key.StartsWith("event.incident.stolen", StringComparison.Ordinal), 4_000)!;
        Assert.Equal("event.incident.stolen_unwatched", stolen.Key);
        Assert.Contains(left.Id, stolen.Causes);

        // One berry is left, and two are hungry: the share-out sets it aside for Bram, and Nia, awake and Desperate, goes for it.
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 1, w.Map.StartStockpile.Center);
        Assert.Equal(stolen.Id, nia.LastFoodEvent);
        WorkSystem.Drop(w, nia);
        nia.Pos = w.Map.StartStockpile.Center;
        nia.Food = 200;
        bram.Food = 100;
        NeedsSystem.Hourly(w);
        Rations.Assign(w);
        Assert.True(Rations.HasReservation(w, bram.Id));
        SimEvent grudge = RunUntil(s, e => e.Key == "event.social.grudge_formed" && e.Actors[0] == bram.Id
            && e.Causes.Any(c => w.Log.Get(c)?.Key == "event.social.took_meal"), 3_000)!;
        Story story = StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true).Single(st => st.TerminalEventId == grudge.Id);
        Assert.Contains(refused.Id, story.Chain);
        Assert.Contains(stolen.Id, story.Chain);
        Assert.Contains(SystemTag.Social, story.Systems);
        Assert.Contains(SystemTag.Incident, story.Systems);
        Assert.Contains(SystemTag.Needs, story.Systems);
        Assert.Contains(SystemTag.Work, story.Systems);
    }

    // ---- Design test 12: a snapshot mid-handover ----

    [Fact]
    public void ASnapshotTakenMidHandoverRestoresToTheSameFuture()
    {
        Simulation s = Camp(4, "you", "mira", "wren");
        World w = s.World;
        Character you = w.Characters.First(c => c.CastKey == "you");
        w.Tick = Hours(w, 26);
        foreach (Character c in w.Characters.Where(c => c.Status == CharStatus.Member && c.Id != you.Id)) Asleep(w, c);
        OnWatch(w, you, w.Tick);
        s.Step();
        Assert.Equal(JobKind.WakeForWatch, you.Job?.Kind);
        for (int i = 0; i < 20; i++) s.Step(); // partway along the walk
        Assert.Equal(JobKind.WakeForWatch, you.Job?.Kind);
        Simulation copy = Simulation.Restore(TestContent.Db, s.Snapshot());
        Assert.Equal(s.StateHash(), copy.StateHash());
        s.Advance(Hours(w, 8));
        copy.Advance(Hours(w, 8));
        Assert.Equal(s.StateHash(), copy.StateHash());
    }

    // ---- Night folk and blood (design 4.6) ----

    [Fact]
    public void AThirstyVampireAsksSomeoneAwakeBeforeASleeperAndNeverTheWatcher()
    {
        World w = Camp(1, "ilex", "mira", "bram", "oren").World;
        Character ilex = w.Characters.First(c => c.CastKey == "ilex"), mira = w.Characters.First(c => c.CastKey == "mira"),
            bram = w.Characters.First(c => c.CastKey == "bram"), oren = w.Characters.First(c => c.CastKey == "oren");
        w.Tick = Hours(w, 24 + 23);
        Relations.ChangeOpinion(w, ilex, mira.Id, 50, 0);
        Relations.ChangeOpinion(w, ilex, oren.Id, 40, 0);
        Asleep(w, mira); // liked best, but asleep
        OnWatch(w, oren, w.Tick + Hours(w, 2)); // awake, but on watch
        bram.Job = null;
        Assert.Equal(bram.Id, BloodJobs.PickDonor(w, ilex)?.Id);
        bram.Status = CharStatus.OffMap;
        Assert.Equal(mira.Id, BloodJobs.PickDonor(w, ilex)?.Id); // a sleeper before the watcher, who is never asked
    }

    [Fact]
    public void ASleeperWokenForBloodShortOfRestRefusesAndSaysWhy()
    {
        World w = Camp(1, "ilex", "mira").World;
        Character ilex = w.Characters.First(c => c.CastKey == "ilex"), mira = w.Characters.First(c => c.CastKey == "mira");
        w.Tick = Hours(w, 24 + 23);
        Asleep(w, mira);
        mira.Rest = 500;
        ilex.Pos = Places.Adjacent(w, mira.Pos, mira.Pos);
        var job = new Job { Kind = JobKind.RequestBlood, TargetId = mira.Id, Target = mira.Pos, StartedTick = w.Tick };
        ilex.Job = job;
        Assert.True(BloodJobs.Request(w, ilex, job));
        SimEvent ask = w.Log.All.Single(e => e.Key == "event.social.blood_request");
        SimEvent no = w.Log.All.Single(e => e.Key == "event.social.blood_refused_woken");
        Assert.Equal(new[] { ask.Id }, no.Causes);
        Assert.Equal("Mira, woken from sleep, refused to give Ilex blood.", EventText.Describe(w, no));
        Assert.False(mira.Asleep);
        Assert.Contains(mira.Thoughts, t => t.DefId == "woken");
        Assert.Contains(ilex.Thoughts, t => t.DefId == "was_refused" && t.AboutId == mira.Id);
    }

    /// <summary>
    /// A sleeper who said no when woken is asked again once rested (BIBLE 8.4), not woken again and again through the night:
    /// back in bed and still short of rest they are passed over; rested, or up and about, they may be asked.
    /// </summary>
    [Fact]
    public void AVampireWakesNoSleeperAgainBeforeTheyHaveRested()
    {
        World w = Camp(1, "ilex", "mira").World;
        Character ilex = w.Characters.First(c => c.CastKey == "ilex"), mira = w.Characters.First(c => c.CastKey == "mira");
        w.Tick = Hours(w, 24 + 23);
        Asleep(w, mira);
        mira.Rest = 500;
        ilex.Pos = Places.Adjacent(w, mira.Pos, mira.Pos);
        var job = new Job { Kind = JobKind.RequestBlood, TargetId = mira.Id, Target = mira.Pos, StartedTick = w.Tick };
        ilex.Job = job;
        BloodJobs.Request(w, ilex, job);
        Assert.Single(w.Log.All, e => e.Key == "event.social.blood_refused_woken");
        ilex.Job = null;

        w.Tick += Hours(w, 2.5);
        Asleep(w, mira);
        mira.Rest = BloodJobs.WokenRefusesBelowRest - 50;
        Assert.Null(BloodJobs.PickDonor(w, ilex)); // still short of rest: not woken again
        mira.Rest = BloodJobs.WokenRefusesBelowRest;
        Assert.Equal(mira.Id, BloodJobs.PickDonor(w, ilex)?.Id); // rested: asked again
        mira.Rest = BloodJobs.WokenRefusesBelowRest - 50;
        mira.Asleep = false;
        mira.Job = null;
        Assert.Equal(mira.Id, BloodJobs.PickDonor(w, ilex)?.Id); // up and about: asked, and nobody is woken
    }

    // ---- Design test 16: the undead at the watch ----

    [Fact]
    public void TheUndeadBoundToTheWatchAreAskedFirstAndKeepItUntilTheirPurposeIsMet()
    {
        Simulation s = Camp(3, "ketil", "mira");
        World w = s.World;
        Character ketil = w.Characters.First(c => c.CastKey == "ketil"), mira = w.Characters.First(c => c.CastKey == "mira");
        NatureNeed purpose = NatureNeeds.Get(ketil, Purpose.NeedId)!;
        purpose.BondWork = "keep_watch";
        purpose.Value = 500;
        Plain(mira, "practical");
        for (int h = 0; h < 24; h++) mira.OwnHours.Add(new HourBlock { Hour = h, Block = DayBlock.Work }); // awake all night
        mira.Priorities.Find(p => p.WorkId == "keep_watch")!.Priority = 1;
        mira.LastWatchEndTick = 0; // she has waited longer
        ketil.LastWatchEndTick = Hours(w, 10);
        w.Tick = Hours(w, 24 + 23);
        Assert.NotEqual(JobKind.Sleep, JobPicker.Choose(w, ketil)?.Kind); // he never sleeps
        Assert.Equal("sleepless", ScheduleSystem.RhythmOf(w, ketil));
        ketil.Job = null;
        mira.Job = null;
        WatchSystem.Step(w);
        Assert.Equal(ketil.Id, w.Watch.IncomingId); // called by his nature: first, past fairness

        // At the post, short of his purpose, he keeps it past the turn's end, quietly.
        WorkSystem.Drop(w, ketil);
        w.Watch.IncomingId = 0;
        OnWatch(w, ketil, w.Tick);
        int events = w.Log.All.Count;
        WatchSystem.Step(w);
        Assert.Equal(ketil.Id, w.Watch.WatcherId);
        Assert.True(w.Watch.TurnEndsTick > w.Tick);
        Assert.Equal(events, w.Log.All.Count);

        // Met, he hands over like anyone.
        purpose.Value = 650;
        w.Watch.TurnEndsTick = w.Tick;
        mira.Job = null;
        WatchSystem.Step(w);
        Assert.Equal(mira.Id, w.Watch.IncomingId);
    }

    [Fact]
    public void KeepingNoWatchIsTheFirstCauseOfAWatchBoundUndeadsFading()
    {
        Simulation s = Camp(3, "ketil");
        World w = s.World;
        Character ketil = w.Characters.First(c => c.CastKey == "ketil");
        NatureNeed purpose = NatureNeeds.Get(ketil, Purpose.NeedId)!;
        purpose.BondWork = "keep_watch";
        purpose.Value = 365;
        w.Tick = Hours(w, 12);
        s.Enqueue(new SetWatchHours(w.Tick, 0, 0));
        SimEvent low = RunUntil(s, e => e.Key == "event.needs.purpose_low" && e.Actors.Contains(ketil.Id), Hours(w, 6))!;
        SimEvent order = w.Log.All.Single(e => e.Key == "event.world.watch_hours");
        Assert.Equal(new[] { order.Id }, low.Causes);
        Assert.Equal("You decided the camp keeps no watch.", EventText.Describe(w, order));
    }

    // ---- Design test 17: a festival night ----

    /// <summary>Mira and Oren keep the harvest fire (day 4 of autumn); returns the simulation just before its dusk.</summary>
    private static (Simulation S, long Dusk) FestivalNight(params string[] more)
    {
        Simulation s = Camp(1, new[] { "mira", "oren" }.Concat(more).ToArray());
        World w = s.World;
        Character mira = w.Characters.First(c => c.CastKey == "mira"), oren = w.Characters.First(c => c.CastKey == "oren");
        Plain(mira, "practical"); // nobody Protective: nobody but the watch would see a thief
        Plain(oren, "oathbound");
        Belong(mira, "hearth");
        Belong(oren, "hearth");
        Hold(w, "hearth", "clasp_greeting", "harvest_fire");
        w.Culture.FestivalDates.Clear();
        w.Culture.FestivalDates.Add(new FestivalDate { CustomId = "harvest_fire", HolderId = "hearth", Season = 2, Day = 3 });
        // An autumn camp with its roof mended and its stores full: nobody has work to do at night.
        foreach (RoofSection r in w.Map.Roof) r.Sound = true;
        Stock.Add(w, "berries", 30, w.Map.StartStockpile.Center);
        WorldDef def = w.Content.World;
        long dusk = (2L * def.DaysPerSeason + 3) * def.TicksPerDay + (long)def.DuskHour * def.TicksPerHour;
        w.Tick = dusk - 60;
        return (s, dusk);
    }

    [Fact]
    public void OnAFestivalNightNobodyKeepsWatchAndATheftCitesTheFestival()
    {
        (Simulation s, long dusk) = FestivalNight();
        World w = s.World;
        Assert.True(Festivals.RunsAt(w, dusk));
        Assert.False(Festivals.RunsAt(w, dusk - 1));
        Assert.False(Festivals.RunsAt(w, dusk + Hours(w, 3)));
        while (w.Tick <= dusk) s.Step();
        Assert.Single(w.Culture.Festivals); // begun in the hourly block, after the watch's step at that tick
        Assert.Equal(0, w.Watch.IncomingId + w.Watch.WatcherId);
        WatchSystem.Step(w); // with the festival's record in place, still nobody: the calendar decides, not the tick order
        Assert.Equal(0, w.Watch.IncomingId + w.Watch.WatcherId);
        SimEvent begins = w.Log.All.Single(e => e.Key == "event.culture.harvest_fire.begins");
        Assert.Equal(begins.Id, w.Watch.GapEvent);

        IncidentSystem.Fire(w, "night_thief");
        SimEvent stolen = RunUntil(s, e => e.Key.StartsWith("event.incident.stolen", StringComparison.Ordinal), Hours(w, 2.5))!;
        Assert.Equal("event.incident.stolen_unwatched", stolen.Key);
        Assert.Contains(begins.Id, stolen.Causes);
        Assert.DoesNotContain(w.Characters, c => c.Job is { Kind: JobKind.KeepWatch });
    }

    [Fact]
    public void AfterTheFestivalTheNightFolkTakeTheWatchAndACampOfDayFolkSleepsThroughIt()
    {
        (Simulation s, long dusk) = FestivalNight();
        World w = s.World;
        RunUntil(s, _ => false, dusk - w.Tick + Hours(w, 5));
        Assert.DoesNotContain(w.Log.All, e => e.Key is "event.work.took_watch" or "event.work.watch_unkept");

        (Simulation s2, long dusk2) = FestivalNight("vale");
        World w2 = s2.World;
        Plain(w2.Characters.First(c => c.CastKey == "vale"), "anxious");
        SimEvent took = RunUntil(s2, "event.work.took_watch", dusk2 - w2.Tick + Hours(w2, 5))!;
        Assert.Equal("vale", w2.Find(took.Actors[0])!.CastKey);
        Assert.True(took.Tick >= dusk2 + Hours(w2, 3), EventText.When(w2, took.Tick)); // after the fire, not during it
    }
}
