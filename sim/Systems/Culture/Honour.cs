using Return.Sim.Core;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// Debts of honour (O3; ADR-0023). A keeper who owes someone for care, blood, a meal or a rescue, once
/// the debt is old enough (the custom's minDebtAgeHours), settles it when their creditor goes hungry:
/// they bring them food, from a bush, the free stores or their own share. That closes the debts that otherwise stay open for good (most blood
/// debts did), and the creditor thinks the better of them.
/// </summary>
public static class Honour
{
    /// <summary>
    /// A Repay-debt job for c (to a bush or the stockpile), or null. A keeper the sun keeps indoors
    /// waits for dark: the bushes, the stockpile and most creditors are in the open.
    /// </summary>
    public static Job? RepayJob(World w, Character c, bool sunBlocked)
    {
        if (c.HungerStage >= 2 || sunBlocked || Ways.Keeps(w, c, "debt_of_honour") is not { } custom) return null;
        long minAge = (long)custom.Def.Arg("minDebtAgeHours") * w.Content.World.TicksPerHour;
        foreach (Debt d in w.Debts)
        {
            if (d.Status != DebtStatus.Open || d.IsPromise || d.DebtorId != c.Id || w.Tick - d.FormedTick < minAge) continue;
            Character? creditor = w.Find(d.CreditorId);
            if (creditor == null || creditor.Status != CharStatus.Member || creditor.HungerStage < 1 || Places.Species(w, creditor).DrinksBlood) continue;
            if (Places.Targeted(w, JobKind.RepayDebt, d.Id, c.Id)) continue;
            Point target = Point.None;
            if (Places.BushWithBerries(w, c.Pos) is { } bush) target = bush.Pos;
            else if (Rations.Unreserved(w, "berries") > 0 || Rations.HasReservation(w, c.Id)) target = Places.FoodTile(w, "berries");
            if (target == Point.None) continue;
            return new Job { Kind = JobKind.RepayDebt, TargetId = d.Id, Target = target, CauseEvent = d.FormedEvent };
        }
        return null;
    }
}
