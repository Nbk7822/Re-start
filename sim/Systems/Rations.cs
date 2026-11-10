using Return.Sim.Content;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// The ration policy (from the demo). Each hour, if stockpile food cannot feed everyone who is
/// hungry, food is set aside in policy order; whoever is left out "goes without", an event caused by
/// the player's policy decision and by whatever last hit their food.
/// </summary>
public static class Rations
{
    public static void Assign(World w)
    {
        w.Reservations.Clear();
        var eaters = new List<Character>();
        string? food = null;
        foreach (Character c in w.Members())
        {
            SpeciesDef sp = w.Content.SpeciesById(c.SpeciesId);
            if (sp.DrinksBlood || c.Downed || c.Food >= NeedsSystem.FedAt) continue;
            food ??= sp.FoodSource;
            if (sp.FoodSource == food) eaters.Add(c);
        }
        if (food == null || eaters.Count == 0) return;
        int stock = Stock.Count(w, food);
        if (stock >= eaters.Count || stock == 0) return; // with nothing to share out, nobody goes without "so others could eat"

        var byNeed = new List<Character>(eaters);
        byNeed.Sort(ByNeed);
        eaters.Sort((a, b) => Compare(w, a, b));
        var wentWithout = new List<(Character, long)>();
        for (int i = 0; i < eaters.Count; i++)
        {
            Character c = eaters[i];
            if (i < stock)
            {
                w.Reservations.Add(new Reservation { CharacterId = c.Id, Count = 1 });
                continue;
            }
            if (!c.SkippedRation)
            {
                // The player's policy is a cause only if it changed the outcome: hungriest-first would have fed them.
                long policy = byNeed.IndexOf(c) < stock ? w.PolicyEvent : 0;
                SimEvent e = w.Log.Emit(w, SystemTag.Needs, "event.needs.went_without", new[] { c.Id },
                    new[] { policy, c.LastFoodEvent }, null, EventArg.Char("a", c.Id));
                c.SkippedRation = true;
                c.LastFoodEvent = e.Id;
                wentWithout.Add((c, e.Id));
            }
            if (!BreakMealPromisesTo(w, c)) TakeSomeoneElsesShare(w, c, food);
        }
        Culture.LeaderLast.AfterShareOut(w, wentWithout); // who ate first, the leader or the led (ADR-0023)
    }

    /// <summary>
    /// A promised meal is a first claim on food: sharing food out and leaving its creditor out while
    /// they are hungry breaks it (as in the demo), unless a keeper of guest-right is already bringing
    /// the guest food (ADR-0023). True if someone is.
    /// </summary>
    private static bool BreakMealPromisesTo(World w, Character c)
    {
        if (c.HungerStage < 1) return false;
        bool hosted = false;
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseMeal && d.CreditorId == c.Id)
            {
                if (Culture.Hospitality.Underway(w, d)) { hosted = true; continue; }
                Relations.Break(w, d, c.LastFoodEvent);
            }
        return hosted;
    }

    /// <summary>
    /// The Desperate, left out while critically hungry, go straight for a meal set aside for someone
    /// else (the theft itself happens at the stockpile, in the Eat job, if the meal is still there).
    /// </summary>
    private static void TakeSomeoneElsesShare(World w, Character c, string food)
    {
        if (c.Asleep || c.Carrying != null || c.Food >= Traits.Max(w, c, t => t.StealFoodBelow)) return;
        if (c.Job is { Kind: JobKind.Eat or JobKind.Fight or JobKind.Flee or JobKind.Rescue or JobKind.Arrive }) return;
        Core.Point tile = Places.FoodTile(w, food);
        if (tile == Core.Point.None) return;
        foreach (Reservation r in w.Reservations)
        {
            if (r.CharacterId == c.Id || r.Count <= 0) continue;
            WorkSystem.Drop(w, c);
            c.Job = new Job { Kind = JobKind.Eat, Target = tile, TargetId = r.CharacterId, StartedTick = w.Tick, CauseEvent = c.LastFoodEvent };
            return;
        }
    }

    /// <summary>
    /// Food reaching the stockpile is shared out at once, so the policy decides who eats it rather than
    /// whoever walks past first before the next hourly check.
    /// </summary>
    public static void AfterDeposit(World w, string defId, Core.Point at)
    {
        if (!w.Map.Stockpile.Contains(at)) return;
        foreach (Character c in w.Members())
            if (w.Content.SpeciesById(c.SpeciesId).FoodSource == defId) { Assign(w); return; }
    }

    public static int Reserved(World w)
    {
        int n = 0;
        foreach (Reservation r in w.Reservations) n += r.Count;
        return n;
    }

    public static bool HasReservation(World w, int characterId)
    {
        foreach (Reservation r in w.Reservations)
            if (r.CharacterId == characterId && r.Count > 0) return true;
        return false;
    }

    /// <summary>Food in the stockpile that nobody has a claim on.</summary>
    public static int Unreserved(World w, string food) => Math.Max(0, Stock.Count(w, food) - Reserved(w));

    /// <summary>Uses up one of the character's claims, if they have one.</summary>
    public static void Consume(World w, int characterId)
    {
        for (int i = 0; i < w.Reservations.Count; i++)
        {
            if (w.Reservations[i].CharacterId != characterId) continue;
            if (--w.Reservations[i].Count <= 0) w.Reservations.RemoveAt(i);
            return;
        }
    }

    private static int Compare(World w, Character a, Character b)
    {
        switch (w.Policy)
        {
            case RationPolicy.PromisesFirst:
            {
                long da = PromiseDue(w, a.Id), db = PromiseDue(w, b.Id);
                int byPromise = da.CompareTo(db);
                if (byPromise != 0) return byPromise;
                break;
            }
            case RationPolicy.SelfFirst:
            {
                int bySelf = (b.Id == w.PlayerId ? 1 : 0).CompareTo(a.Id == w.PlayerId ? 1 : 0);
                if (bySelf != 0) return bySelf;
                break;
            }
        }
        return ByNeed(a, b);
    }

    /// <summary>The default order: hungriest first, then by id.</summary>
    private static int ByNeed(Character a, Character b)
    {
        int byFood = a.Food.CompareTo(b.Food);
        return byFood != 0 ? byFood : a.Id.CompareTo(b.Id);
    }

    /// <summary>Due tick of the earliest open meal promise owed to this person, or long.MaxValue.</summary>
    private static long PromiseDue(World w, int creditorId)
    {
        long due = long.MaxValue;
        foreach (Debt d in w.Debts)
            if (d.Kind == DebtKind.PromiseMeal && d.Status == DebtStatus.Open && d.CreditorId == creditorId && d.DueTick < due) due = d.DueTick;
        return due;
    }
}
