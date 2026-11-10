using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Systems.Culture;

namespace Return.Sim.Systems.Jobs;

/// <summary>Jobs culture asks of people (ADR-0023): gathering at the hearth for a festival, and settling a debt of honour.</summary>
public static class CultureJobs
{
    /// <summary>Walk to the hearth, join every festival under way, and stay until they end.</summary>
    public static bool Feast(World w, Character c, Job j)
    {
        if (w.Culture.Festivals.Count == 0) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            Festivals.Attend(w, c);
            c.Facing = TravelSystem.FacingFor(c.Pos, w.Map.Hearth);
            j.Stage = 1;
        }
        return false;
    }

    /// <summary>
    /// Fetch a berry (pick it from the bush at the target, or take it from the stores: a free one, or
    /// one's own share) and bring it to the hungry creditor, settling the debt. Picking draws no
    /// accident roll: culture never draws from another system's stream.
    /// </summary>
    public static bool RepayDebt(World w, Character c, Job j)
    {
        Debt? debt = w.Debts.Find(d => d.Id == j.TargetId);
        if (debt == null || debt.Status != DebtStatus.Open) return true;
        Character? creditor = w.Find(debt.CreditorId);
        if (creditor == null || creditor.Status != CharStatus.Member) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            if (w.Map.BushAt(j.Target) is { } bush)
            {
                if (!JobRunner.Work(j, w.Content.WorkById("forage").DurationTicks)) return false;
                if (bush.Berries <= 0) return true;
                bush.Berries--;
            }
            else
            {
                bool own = Rations.HasReservation(w, c.Id);
                if (!own && Rations.Unreserved(w, "berries") == 0) return true;
                if (Stock.Take(w, "berries", 1, c.Pos) == 0) return true;
                if (own) Rations.Consume(w, c.Id);
            }
            c.Carrying = new Carry { DefId = "berries", Count = 1 };
            j.Stage = 1;
            j.Progress = 0;
            return false;
        }
        if (!JobRunner.Near(w, c, j, creditor)) return JobRunner.Failed(j);
        c.Carrying = null;
        KeptCustom custom = Ways.Keeps(w, c, "debt_of_honour") ?? throw new InvalidOperationException(c.CastKey + " keeps no debt of honour");
        SimEvent repaid = w.Log.Emit(w, SystemTag.Culture, custom.Def.EventKey("repaid"), new[] { c.Id, creditor.Id },
            new[] { debt.FormedEvent, creditor.LastFoodEvent }, null,
            EventArg.Char("a", c.Id), EventArg.Char("b", creditor.Id), EventArg.Key("ways", custom.HolderNameKey));
        ItemDef berries = w.Content.ItemById("berries");
        creditor.Food = Math.Min(1000, creditor.Food + berries.Nutrition);
        MoodSystem.AddThought(w, creditor, "ate", 0, repaid.Id);
        NeedsSystem.AfterFed(creditor);
        Relations.Repay(w, debt, repaid.Id);
        MoodSystem.AddThought(w, creditor, "debt_honoured", c.Id, repaid.Id);
        return true;
    }
}
