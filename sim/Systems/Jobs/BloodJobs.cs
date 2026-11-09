using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Jobs;

/// <summary>
/// Vampires feed only with consent (BIBLE §4.4). A thirsty vampire asks the human they like best;
/// a Generous human may offer unasked. Giving blood costs the donor health and rest and puts the
/// vampire in their debt; a refusal is remembered by both.
/// </summary>
public static class BloodJobs
{
    public static Character? PickDonor(World w, Character vampire)
    {
        Character? best = null;
        int bestOpinion = int.MinValue, bestDist = int.MaxValue;
        foreach (Character h in w.Characters)
        {
            if (h.Status != CharStatus.Member || h.Id == vampire.Id || h.Downed) continue;
            if (!Places.Species(w, h).CanDonateBlood || RefusedRecently(vampire, h.Id)) continue;
            if (Places.Targeted(w, JobKind.RequestBlood, h.Id, vampire.Id)) continue;
            int op = Relations.Opinion(w, vampire.Id, h.Id), d = vampire.Pos.ChebyshevTo(h.Pos);
            if (op > bestOpinion || (op == bestOpinion && d < bestDist))
            {
                best = h;
                bestOpinion = op;
                bestDist = d;
            }
        }
        return best;
    }

    public static Character? NeedyVampire(World w, Character donor)
    {
        Character? best = null;
        int bestDist = int.MaxValue;
        foreach (Character v in w.Characters)
        {
            if (v.Status != CharStatus.Member || v.Id == donor.Id || !Places.Species(w, v).DrinksBlood) continue;
            if (v.Food >= 170 || BeingFed(w, v) || RefusedRecently(v, donor.Id) || InSun(w, v)) continue;
            int d = donor.Pos.ChebyshevTo(v.Pos);
            if (d < bestDist) { best = v; bestDist = d; }
        }
        return best;
    }

    public static bool Consents(World w, Character donor, Character vampire) => RefusalKey(w, donor, vampire) == null;

    /// <summary>Null if the donor agrees; otherwise the loc key for why they refuse (hungry, weak, low mood, or simply unwilling).</summary>
    public static string? RefusalKey(World w, Character donor, Character vampire)
    {
        if (donor.HungerStage >= 1) return "event.social.blood_refused_hungry";
        if (donor.Downed || donor.Health < Math.Max(600, Traits.Max(w, donor, t => t.RiskyWorkHealthBelow) + 100)) return "event.social.blood_refused_weak";
        if (donor.Mood < 35) return "event.social.blood_refused_mood";
        int required = -Traits.Sum(w, donor, t => t.ConsentBias);
        return Relations.Opinion(w, donor.Id, vampire.Id) >= required ? null : "event.social.blood_refused";
    }

    public static bool Request(World w, Character vampire, Job j)
    {
        Character? donor = w.Find(j.TargetId);
        if (donor == null || donor.Status != CharStatus.Member) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.Near(w, vampire, j, donor)) return JobRunner.Failed(j);
            SimEvent ask = JobRunner.Emit(w, SystemTag.Social, "event.social.blood_request", new[] { vampire.Id, donor.Id }, new[] { j.CauseEvent },
                EventArg.Char("a", vampire.Id), EventArg.Char("b", donor.Id));
            string? refusal = RefusalKey(w, donor, vampire);
            if (refusal != null)
            {
                Refuse(w, vampire, donor, ask.Id, refusal);
                return true;
            }
            WorkSystem.Drop(w, donor);
            donor.Job = new Job { Kind = JobKind.GiveBlood, TargetId = vampire.Id, Target = donor.Pos, Stage = 1, StartedTick = w.Tick, CauseEvent = ask.Id };
            j.Stage = 1;
            j.Progress = 0;
            j.CauseEvent = ask.Id;
            return false;
        }
        if (vampire.Pos.ChebyshevTo(donor.Pos) > 1) return true;
        if (!JobRunner.Work(j, w.Content.WorkById("give_blood").DurationTicks)) return false;
        Transfer(w, vampire, donor, j.CauseEvent);
        return true;
    }

    public static bool Give(World w, Character donor, Job j)
    {
        Character? vampire = w.Find(j.TargetId);
        if (vampire == null || vampire.Status != CharStatus.Member) return true;
        if (j.Stage >= 1)
            return vampire.Job is not { Kind: JobKind.RequestBlood } vj || vj.TargetId != donor.Id;
        if (InSun(w, vampire)) return true;
        if (!JobRunner.Near(w, donor, j, vampire)) return JobRunner.Failed(j);
        SimEvent offer = JobRunner.Emit(w, SystemTag.Social, "event.social.blood_offered", new[] { donor.Id, vampire.Id }, new[] { vampire.LastFoodEvent },
            EventArg.Char("a", donor.Id), EventArg.Char("b", vampire.Id));
        if (vampire.Downed)
        {
            Transfer(w, vampire, donor, offer.Id);
            return true;
        }
        WorkSystem.Drop(w, vampire);
        vampire.Job = new Job { Kind = JobKind.RequestBlood, TargetId = donor.Id, Target = vampire.Pos, Stage = 1, StartedTick = w.Tick, CauseEvent = offer.Id };
        j.Stage = 1;
        return false;
    }

    public static SimEvent Transfer(World w, Character vampire, Character donor, long cause)
    {
        SpeciesDef vsp = Places.Species(w, vampire);
        donor.Health = Math.Max(1, donor.Health - vsp.DonorHealthCost);
        donor.Rest = Math.Max(0, donor.Rest - vsp.DonorRestCost);
        vampire.Food = Math.Min(1000, vampire.Food + vsp.BloodPerFeed);
        NeedsSystem.AfterFed(vampire);
        SimEvent e = JobRunner.Emit(w, SystemTag.Work, "event.work.gave_blood", new[] { donor.Id, vampire.Id }, new[] { cause },
            EventArg.Char("a", donor.Id), EventArg.Char("b", vampire.Id));
        donor.LastHealthEvent = e.Id;
        MoodSystem.AddThought(w, donor, "gave_blood", 0, e.Id);
        MoodSystem.AddThought(w, vampire, "received_blood", donor.Id, e.Id);
        CareJobs.SettleOrOwe(w, helper: donor, helped: vampire, DebtKind.Blood, e.Id);
        return e;
    }

    private static void Refuse(World w, Character vampire, Character donor, long askId, string key)
    {
        long because = key switch
        {
            "event.social.blood_refused_hungry" => donor.LastFoodEvent,
            "event.social.blood_refused_weak" => donor.LastHealthEvent,
            _ => 0,
        };
        SimEvent no = JobRunner.Emit(w, SystemTag.Social, key, new[] { vampire.Id, donor.Id }, new[] { askId, because },
            EventArg.Char("a", vampire.Id), EventArg.Char("b", donor.Id));
        MoodSystem.AddThought(w, vampire, "was_refused", donor.Id, no.Id, Traits.ScalePct(w, vampire, t => t.InsultOpinionScalePct));
        MoodSystem.AddThought(w, donor, "refused_someone", vampire.Id, no.Id);
        vampire.LastFoodEvent = no.Id;
        if (Relations.Opinion(w, vampire.Id, donor.Id) <= SocialSystem.GrudgeAtOpinion)
            Relations.FormGrudge(w, vampire, donor, 25, no.Id);
    }

    public const int RebuffCooldownHours = 2;

    /// <summary>After a refusal a vampire lets a couple of hours pass before asking anyone else.</summary>
    public static bool RecentlyRebuffed(World w, Character vampire)
    {
        ThoughtDef def = w.Content.ThoughtById("was_refused");
        long window = (long)(def.DurationHours - RebuffCooldownHours) * w.Content.World.TicksPerHour;
        foreach (Thought t in vampire.Thoughts)
            if (t.DefId == "was_refused" && t.ExpiresTick - w.Tick > window) return true;
        return false;
    }

    /// <summary>A vampire caught outside in (or just before) daylight must reach cover first; feeding waits.</summary>
    private static bool InSun(World w, Character v) => Places.BurnsInDaylight(w, v) && Places.SunDanger(w) && !w.Map.Covered(v.Pos);

    private static bool RefusedRecently(Character asker, int otherId)
    {
        foreach (Thought t in asker.Thoughts)
            if (t.DefId == "was_refused" && t.AboutId == otherId) return true;
        return false;
    }

    private static bool BeingFed(World w, Character vampire)
    {
        if (vampire.Job is { Kind: JobKind.RequestBlood, Stage: >= 1 }) return true;
        return Places.Targeted(w, JobKind.GiveBlood, vampire.Id);
    }
}
