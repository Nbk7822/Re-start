using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// The view's side of who you are and Return by Death (ADR-0030): the stepper stops at a return, watching across a
/// return changes nothing (ADR-0019), and the words of the opening card, the return's cards, your inspector lines,
/// the Lost futures tab and your gift's order.
/// </summary>
public class FateViewTests
{
    private const int Day = 7_200, Hour = 300;

    public FateViewTests() => Ui.Init(TestContent.Db.Loc);

    [Fact]
    public void TheStepperStopsAtAReturnAndNeverWitnessesTheNewWorld()
    {
        var sim = Simulation.Create(TestContent.Db, 15);
        sim.Advance(Day + 6 * Hour);
        World dying = sim.World;
        var seen = new List<World>();
        Assert.True(sim.ForceDeath());
        long ran = ViewStepper.Run(sim, 400, seen.Add);
        Assert.Equal(1, ran); // the step that returned ends the batch
        Assert.NotSame(dying, sim.World);
        Assert.Empty(seen);
        Assert.Equal(400, ViewStepper.Run(sim, 400, seen.Add));
        Assert.All(seen, w => Assert.Same(sim.World, w));
    }

    /// <summary>ADR-0019's guarantee across a return: a world watched by the map's gore log (reset at the return, as MapView does) hashes the same as one nobody watched.</summary>
    [Fact]
    public void WatchingAcrossAReturnChangesNothing()
    {
        Simulation watched = Simulation.Create(TestContent.Rolled, 16), plain = Simulation.Create(TestContent.Rolled, 16);
        var gore = new GoreLog();
        int returns = 0;
        bool forced = false;
        for (long step = 0; step < 3 * Day; )
        {
            if (!forced && step >= Day + 3 * Hour)
            {
                forced = watched.ForceDeath();
                Assert.Equal(forced, plain.ForceDeath());
            }
            long ran = ViewStepper.Run(watched, Math.Min(137, 3 * Day - step), gore.Observe);
            for (long i = 0; i < ran; i++) plain.Step();
            if (watched.World.Fate.Returns > returns)
            {
                returns = watched.World.Fate.Returns;
                gore = new GoreLog(); // MapView.ResetFor
                gore.Observe(watched.World);
            }
            step += ran;
            gore.Observe(watched.World);
        }
        Assert.True(returns >= 1);
        Assert.Equal(plain.StateHash(), watched.StateHash());
    }

    [Fact]
    public void TheOpeningCardSaysWhatYouWokeAs()
    {
        World w = Simulation.Create(TestContent.Rolled, 3, new RebirthChoice("vampire", Gifts.SatchelId)).World;
        IReadOnlyList<string> card = FateText.Opening(w);
        Assert.Equal("You died on Earth.", card[0]);
        Assert.Equal("You woke here, in the ruins of a camp, as a vampire. The sun will burn you. Blood will feed you, if someone lets you drink.", card[1]);
        Assert.Equal("Something came with you that was not yours on Earth. You have not found it yet.", card[2]);
        World giftless = Simulation.Create(TestContent.Db, 3).World;
        Assert.Equal(2, FateText.Opening(giftless).Count);
    }

    [Fact]
    public void YourInspectorLinesAndTheReturnCards()
    {
        var sim = Simulation.Create(TestContent.Rolled, 12, new RebirthChoice("human", Gifts.FamiliarId));
        sim.Advance(Day);
        FateText.PlayerLines before = FateText.Player(sim.World);
        Assert.StartsWith("Human · reborn here · age ", before.Subtitle);
        Assert.Equal("Your kind live to about 70 here. Your soul came with one lifetime.", before.Life);
        Assert.Equal("Something came with you. You have not found it yet.", before.Gift);
        int lost = sim.World.Fate.Returns;
        Assert.Equal(lost.ToString(System.Globalization.CultureInfo.InvariantCulture), before.LivesLost);
        Assert.True(sim.ForceDeath());
        sim.Step();
        World w = sim.World;
        Assert.Equal("(soak) You fell dead.", FateText.DeathCard(w, w.Fate.LostFutures.Count - 1));
        IReadOnlyList<string> back = FateText.CameBack(w);
        Assert.Equal($"Day {w.Tick / Day + 1}, {w.Tick % Day / Hour:00}:{w.Tick % Hour * 60 / Hour:00}. You came back.", back[0]);
        Assert.Equal("You remember everything.", back[1]);
        Assert.Equal((lost + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), FateText.Player(w).LivesLost);

        World vampire = Simulation.Create(TestContent.Rolled, 9, new RebirthChoice("vampire")).World;
        Assert.Equal("Your body does not age. Your soul came with one lifetime.", FateText.Player(vampire).Life);
    }

    [Fact]
    public void TheLostFuturesTabListsEachFutureNewestFirst()
    {
        var sim = Simulation.Create(TestContent.Db, 18);
        Assert.Equal("No future has been lost yet.", FateText.LostFutures(sim.World).Single().Text);
        sim.Advance(Day + 4 * Hour);
        Assert.True(sim.ForceDeath());
        sim.Step();
        sim.Advance(Day + 8 * Hour);
        Assert.True(sim.ForceDeath());
        sim.Step();
        IReadOnlyList<FateText.LostLine> lines = FateText.LostFutures(sim.World);
        var headings = lines.Where(l => l.Kind == FateText.LineKind.Heading).ToList();
        Assert.Equal(2, headings.Count);
        Assert.All(headings, h => Assert.StartsWith("A future that never came · Day ", h.Text));
        Assert.Equal(2, lines.Count(l => l.Kind == FateText.LineKind.Death && l.Text == "(soak) You fell dead."));
        Assert.DoesNotContain(lines, l => l.Text.Contains('⟦'));
        // Newest first: the first heading is the later future.
        LostFuture newest = sim.World.Fate.LostFutures[^1];
        Assert.Contains(Return.Sim.Api.EventText.When(sim.World, newest.DeathTick), headings[0].Text);
    }

    [Fact]
    public void YourGiftsOrderShowsOnlyOnceYouHaveFoundIt()
    {
        var sim = Simulation.Create(TestContent.Rolled, 4, new RebirthChoice("human", Gifts.SatchelId));
        var drafts = new OrderDrafts();
        Assert.Null(FateText.Order(sim.World, drafts));
        sim.World.Fate.GiftAwakened = true;
        FateText.GiftOrder order = FateText.Order(sim.World, drafts)!;
        Assert.Equal("Your satchel", order.Title);
        Assert.Equal(new[] { "Keep a reserve for yourself", "Share it when someone is starving", "Keep nothing back" }, order.Choices.Select(c => c.Text));
        Assert.Equal("keep", order.Current);
        drafts.Add(new SetStandingOrder(0, "gift.satchel", "share"));
        Assert.Equal("share", FateText.Order(sim.World, drafts)!.Current);
        drafts.Flush(sim);
        Assert.Single(sim.World.PendingCommands);
        sim.Step();
        Assert.Equal("share", Gifts.Order(sim.World, Gifts.Held(sim.World)!.Order!));
        Assert.Null(FateText.Order(Simulation.Create(TestContent.Db, 4).World, drafts)); // no gift, no order
    }
}
