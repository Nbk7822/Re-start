using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using Return.Sim.Systems.Jobs;
using static Return.Tests.CultureHookTests;

namespace Return.Tests;

/// <summary>Debts of honour (O3; ADR-0023): a keeper repays an old debt with food when the creditor goes hungry.</summary>
public class HonourTests
{
    private static (World W, Character Ilex, Character Bram, Debt Debt, Bush Bush) Owed()
    {
        World w = Camp(TestContent.Db, 1, "ilex", "bram");
        Character ilex = Cast(w, "ilex"), bram = Cast(w, "bram");
        Belong(ilex, "night");
        Belong(bram, "reed");
        Hold(w, "night", "veiled_greeting", "debt_of_honour");
        Hold(w, "reed", "no_touch_greeting");
        Stock.Take(w, "berries", 99);
        Bush bush = w.Map.Bushes.First();
        foreach (Bush b in w.Map.Bushes) b.Berries = b == bush ? 2 : 0;
        w.Tick = 1L * w.Content.World.TicksPerDay + 21L * w.Content.World.TicksPerHour;
        SimEvent gave = w.Log.Emit(w, SystemTag.Work, "event.work.gave_blood", new[] { bram.Id, ilex.Id }, Array.Empty<long>());
        Debt debt = Relations.FormDebt(w, ilex, bram, DebtKind.Blood, 1, gave.Id);
        return (w, ilex, bram, debt, bush);
    }

    [Fact]
    public void DebtsOfHonourAreRepaidWhenTheCreditorIsHungry()
    {
        (World w, Character ilex, Character bram, Debt debt, Bush bush) = Owed();
        long hour = w.Content.World.TicksPerHour;
        int minAge = w.Content.CustomById("debt_of_honour").Arg("minDebtAgeHours");

        // Too soon, and while the creditor is fed: nothing.
        w.Tick = debt.FormedTick + (minAge - 1) * hour;
        bram.HungerStage = 1; bram.Food = 250;
        Assert.Null(Honour.RepayJob(w, ilex, sunBlocked: false));
        w.Tick = debt.FormedTick + (minAge + 1) * hour;
        bram.HungerStage = 0; bram.Food = 600;
        Assert.Null(Honour.RepayJob(w, ilex, sunBlocked: false));

        bram.HungerStage = 1; bram.Food = 250;
        SimEvent hungry = w.Log.Emit(w, SystemTag.Needs, "event.needs.hungry", new[] { bram.Id }, Array.Empty<long>());
        bram.LastFoodEvent = hungry.Id;
        Job? job = JobPicker.Choose(w, ilex);
        Assert.Equal(JobKind.RepayDebt, job?.Kind);
        Assert.Equal(debt.Id, job!.TargetId);
        Assert.Equal(bush.Pos, job.Target);
        ilex.Job = job;
        for (int t = 0; t < 6_000 && ilex.Job != null; t++)
        {
            TravelSystem.Step(w);
            if (JobRunner.Tick(w, ilex, ilex.Job)) ilex.Job = null;
            w.Tick++;
        }
        Assert.Null(ilex.Job);
        Assert.Equal(1, bush.Berries);

        SimEvent repaid = w.Log.All.Single(e => e.Key == "event.culture.debt_of_honour.repaid");
        Assert.Equal(SystemTag.Culture, repaid.System);
        Assert.Equal(new[] { ilex.Id, bram.Id }, repaid.Actors);
        Assert.Equal(new[] { debt.FormedEvent, hungry.Id }, repaid.Causes);
        SimEvent closed = w.Log.All.Single(e => e.Key == "event.social.debt_repaid");
        Assert.Contains(repaid.Id, closed.Causes);
        Assert.Contains(closed.Effects, f => f.Kind == EffectKind.DebtRepaid && f.RefId == debt.Id);
        Assert.Equal(DebtStatus.Repaid, debt.Status);
        Assert.True(bram.Food > 250);
        Assert.Contains(bram.Thoughts, t => t.DefId == "debt_honoured" && t.AboutId == ilex.Id && t.CauseEvent == repaid.Id);
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.DebtRepaid && d.DoerId == ilex.Id);
        string night = w.Content.Loc.Get(Ways.CultureById(w, "night")!.NameKey);
        Assert.Equal("Ilex brought Bram food to settle what they owed, as " + night + " do.", Return.Sim.Api.EventText.Describe(w, repaid));
    }

    [Fact]
    public void OnlyKeepersRepayAndNeverWithBerriesForTheUndying()
    {
        (World w, Character ilex, Character bram, Debt debt, _) = Owed();
        w.Tick = debt.FormedTick + 50L * w.Content.World.TicksPerHour;
        bram.HungerStage = 1; bram.Food = 250;
        Hold(w, "night", "veiled_greeting");
        Assert.Null(Honour.RepayJob(w, ilex, sunBlocked: false));

        // A debt to a blood-drinker is not settled with berries.
        Hold(w, "night", "veiled_greeting", "debt_of_honour");
        Belong(bram, "night");
        bram.SpeciesId = "vampire";
        Assert.Null(Honour.RepayJob(w, ilex, sunBlocked: false));
    }

    [Fact]
    public void WithNoBushToPickTheKeeperTakesTheirOwnShare()
    {
        (World w, Character ilex, Character bram, Debt debt, Bush bush) = Owed();
        bush.Berries = 0;
        w.Tick = debt.FormedTick + 50L * w.Content.World.TicksPerHour;
        bram.HungerStage = 1; bram.Food = 250;
        Assert.Null(Honour.RepayJob(w, ilex, sunBlocked: false));
        Stock.Add(w, "berries", 1, w.Map.Stockpile.Center);
        Job? job = Honour.RepayJob(w, ilex, sunBlocked: false);
        Assert.Equal(Places.FoodTile(w, "berries"), job?.Target);
    }
}
