using Return.Sim.Content;
using Return.Sim.Events;

namespace Return.Sim.Systems.Jobs;

/// <summary>Tending the injured (which creates or repays a debt) and an Oathbound person keeping a meal promise themselves.</summary>
public static class CareJobs
{
    public static bool Care(World w, Character c, Job j)
    {
        Character? patient = w.Find(j.TargetId);
        if (patient == null || patient.Status != CharStatus.Member) return true;
        if (!patient.Downed && patient.Health >= JobPicker.InjuredBelow) return true;
        WorkDef def = w.Content.WorkById("care");
        if (j.Stage == 0)
        {
            if (!JobRunner.Near(w, c, j, patient)) return JobRunner.Failed(j);
            j.Stage = 1;
            j.Progress = 0;
            return false;
        }
        if (c.Pos.ChebyshevTo(patient.Pos) > 1)
        {
            j.Stage = 0;
            return false;
        }
        c.Facing = TravelSystem.FacingFor(c.Pos, patient.Pos);
        if (!JobRunner.Work(j, def.DurationTicks)) return false;

        bool supplied = def.BerryCost > 0 && Stock.Take(w, "berries", def.BerryCost) == def.BerryCost;
        int heal = supplied ? def.Yield : def.Yield / 2;
        patient.Health = Math.Min(1000, patient.Health + heal);
        if (patient.Downed && patient.Health >= NeedsSystem.RecoverAt) patient.Downed = false;
        SimEvent e = JobRunner.Emit(w, SystemTag.Work, "event.work.cared_for", new[] { c.Id, patient.Id }, new[] { patient.LastHealthEvent },
            EventArg.Char("a", c.Id), EventArg.Char("b", patient.Id));
        MoodSystem.AddThought(w, patient, "cared_for", c.Id, e.Id);
        SettleOrOwe(w, helper: c, helped: patient, DebtKind.Care, e.Id);
        return true;
    }

    /// <summary>If the helper owed the helped person something, this repays it; otherwise the helped person now owes the helper.</summary>
    public static void SettleOrOwe(World w, Character helper, Character helped, DebtKind kind, long cause)
    {
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && !d.IsPromise && d.DebtorId == helper.Id && d.CreditorId == helped.Id)
            {
                Relations.Repay(w, d, cause);
                return;
            }
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && d.Kind == kind && d.DebtorId == helped.Id && d.CreditorId == helper.Id)
                return;
        Relations.FormDebt(w, helped, helper, kind, 1, cause);
    }

    public static bool KeepPromise(World w, Character c, Job j)
    {
        Debt? debt = w.Debts.Find(d => d.Id == j.TargetId);
        if (debt == null || debt.Status != DebtStatus.Open) return true;
        Character? creditor = w.Find(debt.CreditorId);
        if (creditor == null || creditor.Status != CharStatus.Member) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            if (!Rations.HasReservation(w, c.Id) && Rations.Unreserved(w, "berries") == 0) return true;
            if (Stock.Take(w, "berries", 1) == 0) return true;
            Rations.Consume(w, c.Id);
            c.Carrying = new Carry { DefId = "berries", Count = 1 };
            j.Stage = 1;
            return false;
        }
        if (!JobRunner.Near(w, c, j, creditor)) return JobRunner.Failed(j);
        ItemDef berries = w.Content.ItemById("berries");
        creditor.Food = Math.Min(1000, creditor.Food + berries.Nutrition);
        c.Carrying = null;
        SimEvent e = JobRunner.Emit(w, SystemTag.Work, "event.work.shared_meal", new[] { c.Id, creditor.Id }, new[] { debt.FormedEvent, creditor.LastFoodEvent },
            EventArg.Char("a", c.Id), EventArg.Char("b", creditor.Id));
        MoodSystem.AddThought(w, creditor, "ate", 0, e.Id);
        NeedsSystem.AfterFed(creditor);
        Relations.Repay(w, debt, e.Id);
        return true;
    }
}
