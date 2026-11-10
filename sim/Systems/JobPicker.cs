using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// Chooses what an idle person does next, in the demo's order of concerns: cover from the sun,
/// critical hunger, rest (and the injured lie down; everyone who tires in their sleeping hours), promises,
/// the injured, a Generous offer of blood, eating, prioritised work in working hours (a nap in free time
/// if tired), company, wandering. Returns a job with its target set; travel is
/// started by <see cref="WorkSystem"/>. Never orders anyone; it only reads state (and draws Dice for
/// wandering, and lets someone who keeps away from a meeting place say so: Zones.KeepAway).
/// </summary>
public static class JobPicker
{
    public const int InjuredBelow = 400;

    private static readonly string[] WorkOrder = { "care", "repair_roof", "gather_timber", "forage", "haul", "keep_watch" };

    /// <summary>The kinds of work a person's priorities apply to, in the order ties are broken.</summary>
    public static IReadOnlyList<string> PrioritizedWork => WorkOrder;

    public static Job? Choose(World w, Character c)
    {
        if (c.Downed || c.Status != CharStatus.Member) return null;
        SpeciesDef sp = Places.Species(w, c);
        bool burns = sp.DaylightBurnPerHour > 0;
        bool sunBlocked = burns && Places.SunDanger(w);

        if (sunBlocked && !w.Map.Covered(c.Pos))
        {
            Point spot = Places.CoveredSpot(w, c.Pos);
            if (spot != Point.None) return new Job { Kind = JobKind.SeekCover, Target = spot, CauseEvent = c.LastHealthEvent };
        }

        // One who cannot lie keeps their word before anything else, even their own hunger (ADR-0022).
        if (sp.CannotLie)
        {
            Job? word = Courtesy.KeepWordJob(w, c, sunBlocked);
            if (word != null) return word;
        }
        // Guest-bread: a keeper of guest-right feeds a promised guest before their own hunger, unless starving (ADR-0023).
        Job? guest = Culture.Hospitality.GuestJob(w, c, sunBlocked);
        if (guest != null) return guest;

        // In their sleeping hours only the hunger that would wake them sends them out to forage or ask for blood (ADR-0029);
        // short of that they eat what is in the stores, if anything, and go back to bed.
        DayBlock block = ScheduleSystem.BlockNow(w, c);
        bool roused = !(sp.Tires && block == DayBlock.Sleep) || c.Food < NeedsSystem.StarvingBelow || c.Food < Traits.Max(w, c, t => t.StealFoodBelow);
        if (c.Food < NeedsSystem.HungryBelow)
        {
            if (sp.DrinksBlood && !BloodJobs.RecentlyRebuffed(w, c))
            {
                Character? donor = roused ? BloodJobs.PickDonor(w, c) : null;
                if (donor != null && !(sunBlocked && !w.Map.Covered(donor.Pos)))
                    return new Job { Kind = JobKind.RequestBlood, TargetId = donor.Id, Target = donor.Pos, CauseEvent = c.LastFoodEvent };
            }
            else if (!sp.EatsNothing) // a rebuffed vampire falls through to here too, and forages for the camp
            {
                Job? eat = EatJob(w, c, allowTheft: true);
                if (eat != null) return eat;
                Job? forage = roused ? ForageJob(w, c, c.LastFoodEvent) : null; // foraging for their own hunger carries it (zones design 5.5)
                if (forage != null) return forage;
            }
        }

        // A need of their nature at its critical mark comes before rest; one that is merely low, after it (ADR-0022).
        bool nature = c.NatureNeeds.Count > 0;
        Job? urgent = nature ? NatureNeeds.Job(w, c, critical: true, sunBlocked) : null;
        if (urgent != null) return urgent;

        // The injured lie down to sleep; those who never tire never sleep (the undead), and mend on their feet.
        if (c.Health < InjuredBelow && sp.Tires) return SleepJob(w, c);
        // In their sleeping hours everyone who tires goes to bed (ADR-0029); exhaustion sends them in any hours.
        int restAt = 200 + Traits.Sum(w, c, t => t.RestThresholdAdd);
        if (sp.Tires && (c.Rest < restAt || block == DayBlock.Sleep)) return SleepJob(w, c);

        Job? natural = nature ? NatureNeeds.Job(w, c, critical: false, sunBlocked) : null;
        if (natural != null) return natural;

        Job? promise = PromiseJob(w, c);
        if (promise != null)
        {
            ScheduleSystem.WorkedLate(w, c, promise); // past their working hours, they say so once a day (ADR-0029)
            return promise;
        }

        if (Traits.Sum(w, c, t => t.CareUrgency) > 0 || PriorityOf(c, "care") == 1)
        {
            Job? rescue = RescueJob(w, c, sunBlocked);
            if (rescue != null) return rescue;
            Job? care = CareJob(w, c, sunBlocked);
            if (care != null) return care;
        }

        Job? repay = Culture.Honour.RepayJob(w, c, sunBlocked); // a debt of honour, when the creditor goes hungry (ADR-0023)
        if (repay != null) return repay;

        if (sp.CanDonateBlood && c.Health > 800 && OffersBloodUnasked(w, c))
        {
            Character? vampire = BloodJobs.NeedyVampire(w, c);
            if (vampire != null) return new Job { Kind = JobKind.GiveBlood, TargetId = vampire.Id, Target = vampire.Pos, CauseEvent = vampire.LastFoodEvent };
        }

        if (sp.EatsItem && c.Food < NeedsSystem.FedAt)
        {
            Job? eat = EatJob(w, c, allowTheft: false);
            if (eat != null) return eat;
        }

        Job? feast = Culture.Festivals.FeastJob(w, c, sunBlocked); // a festival comes before ordinary work (ADR-0023)
        if (feast != null) return feast;

        // Ordinary work only in working hours; in free time a nap if tired, then company and the fire (ADR-0029).
        bool free = block != DayBlock.Work;
        if (!free)
        {
            Job? work = WorkJob(w, c, sunBlocked);
            if (work != null) return work;
        }
        else if (sp.Tires && c.Rest < w.Content.World.Schedule.FreeTimeNapBelow) return SleepJob(w, c);

        if (c.Social < 400)
        {
            Job? talk = SocializeJob(w, c, sunBlocked);
            if (talk != null) return talk;
        }
        return WanderJob(w, c, sunBlocked, free);
    }

    /// <summary>
    /// A Generous person offers blood unasked, and so does a keeper of the blood-gift (ADR-0023). A
    /// Generous keeper of the blood taboo still offers: their nature over their people's custom.
    /// </summary>
    private static bool OffersBloodUnasked(World w, Character c) =>
        Traits.Any(w, c, t => t.OffersBlood) || Culture.Ways.BloodOffer(w, c) != null;

    public static int PriorityOf(Character c, string workId)
    {
        foreach (WorkPriority p in c.Priorities)
            if (p.WorkId == workId) return p.Priority;
        return 3;
    }

    private static Job? EatJob(World w, Character c, bool allowTheft)
    {
        string food = Places.Species(w, c).FoodSource;
        Point tile = Places.FoodTile(w, food);
        if (tile == Point.None) return null;
        if (Rations.HasReservation(w, c.Id) || Rations.Unreserved(w, food) > 0)
            return new Job { Kind = JobKind.Eat, Target = tile, CauseEvent = c.LastFoodEvent };
        if (allowTheft && c.Food < Traits.Max(w, c, t => t.StealFoodBelow))
            foreach (Reservation r in w.Reservations)
                if (r.CharacterId != c.Id && r.Count > 0)
                    return new Job { Kind = JobKind.Eat, Target = tile, TargetId = r.CharacterId, CauseEvent = c.LastFoodEvent };
        return null;
    }

    /// <summary>The nearest bush with berries to pick; <paramref name="cause"/> is why (their hunger when they forage for it, else 0).</summary>
    private static Job? ForageJob(World w, Character c, long cause = 0)
    {
        Bush? bush = Places.BushWithBerries(w, c.Pos);
        return bush == null ? null : new Job { Kind = JobKind.Forage, TargetId = bush.Id, Target = bush.Pos, CauseEvent = cause };
    }

    private static Job SleepJob(World w, Character c) => new() { Kind = JobKind.Sleep, Target = Places.SleepSpot(w, c), CauseEvent = c.LastRestEvent };

    /// <summary>An Oathbound debtor gives a hungry creditor food to keep a meal promise the camp is failing to keep.</summary>
    private static Job? PromiseJob(World w, Character c)
    {
        if (Traits.Sum(w, c, t => t.PromisePriority) <= 0) return null;
        long roofPromise = RoofPromiseEvent(w, c.Id);
        if (roofPromise != 0 && !(Places.BurnsInDaylight(w, c) && Places.SunDanger(w))
            && Stock.Count(w, "timber") >= w.Content.WorkById("repair_roof").TimberCost)
        {
            Job? roof = RoofJob(w, c, roofPromise);
            if (roof != null) return roof;
        }
        string food = Places.Species(w, c).FoodSource;
        if (!Places.Species(w, c).EatsItem) food = "berries";
        Point tile = Places.FoodTile(w, food);
        if (tile == Point.None) return null;
        bool canTake = Rations.HasReservation(w, c.Id) || Rations.Unreserved(w, food) > 0;
        if (!canTake) return null;
        foreach (Debt d in w.Debts)
        {
            if (d.Status != DebtStatus.Open || d.Kind != DebtKind.PromiseMeal || d.DebtorId != c.Id) continue;
            Character? creditor = w.Find(d.CreditorId);
            if (creditor == null || creditor.Status != CharStatus.Member || creditor.Food >= NeedsSystem.FedAt) continue;
            if (Rations.HasReservation(w, creditor.Id) || Places.Targeted(w, JobKind.KeepPromise, d.Id, c.Id)) continue;
            return new Job { Kind = JobKind.KeepPromise, TargetId = d.Id, Target = tile, CauseEvent = d.FormedEvent };
        }
        return null;
    }

    private static Job? CareJob(World w, Character c, bool sunBlocked)
    {
        if (c.Health < InjuredBelow) return null;
        Character? best = null;
        int bestDist = int.MaxValue;
        foreach (Character p in w.Characters)
        {
            if (p.Id == c.Id || p.Status != CharStatus.Member) continue;
            if (!p.Downed && p.Health >= InjuredBelow) continue;
            if (Places.Targeted(w, JobKind.Care, p.Id, c.Id)) continue;
            if (sunBlocked && !w.Map.Covered(p.Pos)) continue;
            int d = c.Pos.ChebyshevTo(p.Pos);
            if (d < bestDist) { best = p; bestDist = d; }
        }
        return best == null ? null : new Job { Kind = JobKind.Care, TargetId = best.Id, Target = best.Pos, CauseEvent = best.LastHealthEvent };
    }

    /// <summary>Someone has fallen where they cannot be tended (in the open, not in a bed): carry them to safety.</summary>
    private static Job? RescueJob(World w, Character c, bool sunBlocked)
    {
        if (c.Health < InjuredBelow) return null;
        Character? best = null;
        int bestDist = int.MaxValue;
        foreach (Character p in w.Characters)
        {
            if (p.Id == c.Id || p.Status != CharStatus.Member || !p.Downed) continue;
            if (OnBed(w, p.Pos) || (w.Map.Covered(p.Pos) && !Places.BurnsInDaylight(w, p))) continue;
            if (Places.Targeted(w, JobKind.Rescue, p.Id, c.Id)) continue;
            if (sunBlocked && !w.Map.Covered(p.Pos)) continue;
            int d = c.Pos.ChebyshevTo(p.Pos);
            if (d < bestDist) { best = p; bestDist = d; }
        }
        return best == null ? null : new Job { Kind = JobKind.Rescue, TargetId = best.Id, Target = best.Pos, CauseEvent = best.LastHealthEvent };
    }

    private static Job? RoofJob(World w, Character c, long cause)
    {
        foreach (RoofSection r in w.Map.Roof)
            if (!r.Sound && !Places.Targeted(w, JobKind.RepairRoof, r.Index, c.Id))
                return new Job { Kind = JobKind.RepairRoof, TargetId = r.Index, Target = Places.FoodTile(w, "timber"), CauseEvent = cause };
        return null;
    }

    /// <summary>The event that made the oldest open promise of a sound roof (by debtorId, or anyone's if 0); 0 if none.</summary>
    private static long RoofPromiseEvent(World w, int debtorId)
    {
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseRest && (debtorId == 0 || d.DebtorId == debtorId)) return d.FormedEvent;
        return 0;
    }

    private static bool OnBed(World w, Point p)
    {
        foreach (Bed b in w.Map.Beds)
            if (b.Pos == p) return true;
        return false;
    }

    private static Job? WorkJob(World w, Character c, bool sunBlocked)
    {
        int respect = Traits.ScalePct(w, c, t => t.PriorityRespectPct);
        bool fragile = c.Health < Traits.Max(w, c, t => t.RiskyWorkHealthBelow) || c.Rest < Traits.Max(w, c, t => t.RiskyWorkRestBelow);
        List<string> refused = Places.Species(w, c).RefusesWork;
        Job? best = null;
        int bestScore = int.MaxValue;
        foreach (string workId in WorkOrder)
        {
            int priority = PriorityOf(c, workId);
            if (priority <= 0 || refused.Contains(workId)) continue; // a dryad never fells a tree, whatever the priorities say
            WorkDef def = w.Content.WorkById(workId);
            if (def.Risky && fragile) continue;
            int score = priority * 100 - (Traits.WorkBias(w, c, workId) + NatureNeeds.WorkBias(w, c, workId)) * 60;
            score = 250 + (score - 250) * respect / 100;
            if (score >= bestScore) continue;
            Job? job = Candidate(w, c, workId, def, sunBlocked);
            if (job == null) continue;
            best = job;
            bestScore = score;
        }
        return best;
    }

    /// <summary>A job of this kind of work for c right now, if there is one (ignoring priorities).</summary>
    public static Job? WorkCandidate(World w, Character c, string workId, bool sunBlocked) =>
        Candidate(w, c, workId, w.Content.WorkById(workId), sunBlocked);

    private static Job? Candidate(World w, Character c, string workId, WorkDef def, bool sunBlocked)
    {
        switch (workId)
        {
            case "care":
                return CareJob(w, c, sunBlocked);
            case "repair_roof":
            {
                if (sunBlocked || Stock.Count(w, "timber") < def.TimberCost) return null;
                return RoofJob(w, c, RoofPromiseEvent(w, 0));
            }
            case "gather_timber":
            {
                // A djinn does not fell trees for its own price: the camp pays it (ADR-0022).
                if (sunBlocked || Places.TimberNeeded(w) - Bargains.OwnDemand(w, c, "timber") <= 0) return null;
                Tree? tree = Places.StandingTree(w, c.Pos, c);
                if (tree == null) return null;
                // The timber is wanted for a promised roof, else for a bargain's price (ADR-0022).
                long why = RoofPromiseEvent(w, 0);
                if (why == 0) why = Bargains.DemandCause(w, "timber");
                return new Job { Kind = JobKind.GatherTimber, TargetId = tree.Id, Target = Places.Adjacent(w, tree.Pos, c.Pos), CauseEvent = why };
            }
            case "forage":
            {
                int enough = 2 * Places.Eaters(w, "berries") + Bargains.Demand(w, "berries") - Bargains.OwnDemand(w, c, "berries");
                if (sunBlocked || (Stock.Count(w, "berries") >= enough && !Purpose.Seeks(w, c, workId))) return null;
                return ForageJob(w, c);
            }
            case "haul":
            {
                if (sunBlocked) return null;
                // Stores left where no stockpile keeps them now are carried first, then loose goods a stockpile would take (ADR-0028).
                foreach (ItemStack s in w.Items)
                    if (s.Stored && Haulable(w, c, s)) return new Job { Kind = JobKind.Haul, TargetId = s.Id, Target = s.Pos };
                foreach (ItemStack s in w.Items)
                    if (!s.Stored && Haulable(w, c, s)) return new Job { Kind = JobKind.Haul, TargetId = s.Id, Target = s.Pos };
                return null;
            }
            case "keep_watch":
                // The WatchSystem hands out the watch in turns (ADR-0029); this priority now says who is asked first.
                return null;
            default:
                return null;
        }
    }

    /// <summary>A stack worth carrying to a stockpile: not on a tile whose stockpile keeps it, some stockpile does, reachable, and nobody else on it.</summary>
    private static bool Haulable(World w, Character c, ItemStack s) =>
        !Zones.Accepts(w, s.Pos, s.DefId) && Zones.Keeps(w, s.DefId) && w.Map.Passable(s.Pos) && !Places.Targeted(w, JobKind.Haul, s.Id, c.Id);

    private static Job? SocializeJob(World w, Character c, bool sunBlocked)
    {
        Character? best = null;
        int bestDist = int.MaxValue;
        foreach (Character o in w.Characters)
        {
            if (o.Id == c.Id || o.Status != CharStatus.Member || o.Asleep || o.Downed) continue;
            if (sunBlocked && !w.Map.Covered(o.Pos)) continue;
            int d = c.Pos.ChebyshevTo(o.Pos);
            if (d < bestDist && d <= 25) { best = o; bestDist = d; }
        }
        if (best == null) return null;
        Point spot = Places.Adjacent(w, best.Pos, c.Pos);
        return spot == Point.None ? null : new Job { Kind = JobKind.Socialize, TargetId = best.Id, Target = spot };
    }

    /// <summary>
    /// The one wander rule (zones design 5.4, ADR-0028): the idle go to the meeting place they would
    /// rather sit in (where the people they like are), and sit apart near where they sleep when every
    /// meeting place holds someone they bear a grudge against; otherwise, or while the sun keeps them
    /// under cover, they wander near where they stand, drifting back to the hearth from afar. Schedule's
    /// free time slots in ahead of the meeting place (a species' own place) and after it (the hearth).
    /// </summary>
    private static Job? WanderJob(World w, Character c, bool sunBlocked, bool free)
    {
        if (!sunBlocked)
        {
            Zones.Gathering gathering = Zones.MeetingPlaceFor(w, c);
            Point spot = Point.None;
            if (gathering.Place != null) spot = Zones.TileIn(w, gathering.Place);
            else if (gathering.Shunned != null)
            {
                Zones.KeepAway(w, c, gathering);
                spot = Places.Near(w, Places.SleepSpot(w, c), 2);
            }
            if (spot != Point.None)
                return new Job { Kind = JobKind.Wander, Target = spot, TargetId = w.Rng.Dice[DiceSystem.Work].Range(DiceId.WanderWait, 60, 181) };
        }
        // With no meeting place to go to, free time draws people to the fire (radius 2), other hours keep them about the camp.
        Point centre = sunBlocked ? c.Pos : free || c.Pos.ChebyshevTo(w.Map.Hearth) > 8 ? w.Map.Hearth : c.Pos;
        int radius = sunBlocked ? 1 : free ? 2 : 4;
        Point p = Places.Near(w, centre, radius);
        if (p == Point.None || (sunBlocked && !w.Map.Covered(p))) p = c.Pos;
        int wait = w.Rng.Dice[DiceSystem.Work].Range(DiceId.WanderWait, 60, 181);
        return new Job { Kind = JobKind.Wander, Target = p, TargetId = wait };
    }
}
