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
        if (stock >= eaters.Count) return;

        eaters.Sort((a, b) => Compare(w, a, b));
        for (int i = 0; i < eaters.Count; i++)
        {
            Character c = eaters[i];
            if (i < stock)
            {
                w.Reservations.Add(new Reservation { CharacterId = c.Id, Count = 1 });
                continue;
            }
            if (c.SkippedRation) continue;
            SimEvent e = w.Log.Emit(w, SystemTag.Needs, "event.needs.went_without", new[] { c.Id },
                new[] { w.PolicyEvent, c.LastFoodEvent }, null, EventArg.Char("a", c.Id));
            c.SkippedRation = true;
            c.LastFoodEvent = e.Id;
        }
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
                int bySelf = (b.Otherworlder ? 1 : 0).CompareTo(a.Otherworlder ? 1 : 0);
                if (bySelf != 0) return bySelf;
                break;
            }
        }
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
