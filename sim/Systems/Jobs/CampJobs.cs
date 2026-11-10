using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Jobs;

/// <summary>Camp work: foraging, felling timber, hauling, repairing the shelter roof, keeping watch.</summary>
public static class CampJobs
{
    public static bool Forage(World w, Character c, Job j)
    {
        WorkDef def = w.Content.WorkById("forage");
        switch (j.Stage)
        {
            case 0:
                if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                j.Stage = 1;
                return false;
            case 1:
            {
                if (!JobRunner.Work(j, def.DurationTicks)) return false;
                Bush? bush = w.Map.Bushes.Find(b => b.Id == j.TargetId);
                int n = bush == null ? 0 : Math.Min(def.Yield, bush.Berries);
                if (n == 0) return true;
                bush!.Berries -= n;
                c.Carrying = new Carry { DefId = "berries", Count = n };
                SimEvent e = JobRunner.Emit(w, SystemTag.Work, "event.work.foraged", new[] { c.Id }, Array.Empty<long>(),
                    EventArg.Char("a", c.Id), EventArg.Num("n", n), EventArg.Item("item", "berries"));
                Gifts.Foraged(w, c, e); // your satchel fills as you forage (ADR-0030)
                JobRunner.RollAccident(w, c, def, e.Id);
                JobRunner.Next(w, c, j, Stock.DropTile(w, "berries"));
                return JobRunner.Failed(j);
            }
            default:
                if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                JobRunner.Deliver(w, c);
                return true;
        }
    }

    public static bool GatherTimber(World w, Character c, Job j)
    {
        WorkDef def = w.Content.WorkById("gather_timber");
        switch (j.Stage)
        {
            case 0:
                if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                j.Stage = 1;
                return false;
            case 1:
            {
                Tree? tree = w.Map.Trees.Find(t => t.Id == j.TargetId);
                if (tree == null || tree.Felled) return true;
                c.Facing = TravelSystem.FacingFor(c.Pos, tree.Pos);
                if (!JobRunner.Work(j, def.DurationTicks)) return false;
                tree.Felled = true;
                w.Map.RebuildCaches();
                c.Carrying = new Carry { DefId = "timber", Count = def.Yield };
                // The felling cites why the timber was wanted (a promised roof); a dryad's tree also cites the feller's grudge against them (ADR-0022).
                Character? dryad = Forest.BoundTo(w, tree);
                SimEvent e = dryad == null
                    ? JobRunner.Emit(w, SystemTag.Work, "event.work.felled_tree", new[] { c.Id }, new[] { j.CauseEvent }, EventArg.Char("a", c.Id))
                    : JobRunner.Emit(w, SystemTag.Work, "event.work.felled_bound_tree", new[] { c.Id, dryad.Id },
                        new[] { j.CauseEvent, Relations.ActiveGrudge(w, c.Id, dryad.Id)?.FormedEvent ?? 0 }, EventArg.Char("a", c.Id), EventArg.Char("b", dryad.Id));
                Culture.Omens.RecordDeed(w, DeedKind.TreeFelled, e.Id, c.Id);
                Culture.SacredTrees.Felled(w, c, tree, e);
                Forest.Felled(w, c, tree, e);
                JobRunner.RollAccident(w, c, def, e.Id);
                JobRunner.Next(w, c, j, Stock.DropTile(w, "timber"));
                return JobRunner.Failed(j);
            }
            default:
                if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                JobRunner.Deliver(w, c);
                return true;
        }
    }

    public static bool Haul(World w, Character c, Job j)
    {
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            ItemStack? stack = w.Items.Find(s => s.Id == j.TargetId);
            if (stack == null || stack.Pos != c.Pos) return true;
            c.Carrying = new Carry { DefId = stack.DefId, Count = stack.Count };
            w.Items.Remove(stack);
            JobRunner.Next(w, c, j, Stock.DropTile(w, c.Carrying.DefId));
            return JobRunner.Failed(j);
        }
        if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
        JobRunner.Deliver(w, c);
        return true;
    }

    public static bool RepairRoof(World w, Character c, Job j)
    {
        WorkDef def = w.Content.WorkById("repair_roof");
        RoofSection section = w.Map.Roof[j.TargetId];
        switch (j.Stage)
        {
            case 0:
            {
                if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                if (section.Sound) return true;
                int taken = Stock.Take(w, "timber", def.TimberCost);
                if (taken < def.TimberCost)
                {
                    if (taken > 0) Stock.Add(w, "timber", taken, c.Pos);
                    return true;
                }
                c.Carrying = new Carry { DefId = "timber", Count = taken };
                JobRunner.Next(w, c, j, section.Area.Center);
                return JobRunner.Failed(j);
            }
            case 1:
                if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                j.Stage = 2;
                return false;
            default:
            {
                if (!JobRunner.Work(j, def.DurationTicks)) return false;
                section.Sound = true;
                section.MendedBy = c.Id; // those who sleep under it think the better of whoever mended it (ADR-0027)
                c.Carrying = null;
                // "of the shelter roof": the old camp's sections, not one raised elsewhere or taken down (ADR-0027 review).
                List<RoofSection> shelter = w.Map.Roof.FindAll(r => r.BuiltBy.Count == 0 && !r.Removed);
                int sound = shelter.FindAll(r => r.Sound).Count;
                SimEvent e = JobRunner.Emit(w, SystemTag.Work, "event.work.roof_repaired", new[] { c.Id }, new[] { j.CauseEvent },
                    EventArg.Char("a", c.Id), EventArg.Num("n", sound), EventArg.Num("total", shelter.Count));
                foreach (Character m in w.Members()) MoodSystem.AddThought(w, m, "roof_sound", 0, e.Id);
                foreach (Debt d in w.Debts)
                    if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseRest) Relations.Repay(w, d, e.Id);
                JobRunner.RollAccident(w, c, def, e.Id);
                return true;
            }
        }
    }

    public static bool KeepWatch(World w, Character c, Job j)
    {
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            j.Stage = 1;
            c.Facing = Facing.S;
            return false;
        }
        j.Progress++;
        bool sun = Places.BurnsInDaylight(w, c) ? Places.SunDanger(w) : Places.Daylight(w);
        return sun || c.Rest < 120 || c.Food < NeedsSystem.StarvingBelow;
    }
}
