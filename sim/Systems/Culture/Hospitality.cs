using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// Guest-bread (H1; ADR-0023). When the camp has promised a newcomer a meal it cannot give, a keeper
/// of guest-right who is not starving brings the guest food themselves: their own share of the stores
/// if they hold one, otherwise berries they go out and pick. That keeps the camp's promise and puts the
/// guest in the host's debt; a host who gave their own share goes hungry for it. Each host decides
/// once per promise (a Dice roll on the Culture stream), so the odds are the custom's, not one roll
/// per job pick. While a host is on the way, a share-out that leaves the guest out does not break the
/// promise.
/// </summary>
public static class Hospitality
{
    /// <summary>
    /// A Keep-promise job in host mode for c (to the stockpile for their share, or to a bush), or null.
    /// The starving, the injured (who lie down before keeping any promise) and those the sun keeps
    /// indoors do not go.
    /// </summary>
    public static Job? GuestJob(World w, Character c, bool sunBlocked)
    {
        if (c.HungerStage >= 2 || sunBlocked || c.Health < JobPicker.InjuredBelow) return null;
        if (Ways.Keeps(w, c, "guest_right") is not { } custom) return null;
        foreach (Debt d in w.Debts)
        {
            if (d.Status != DebtStatus.Open || d.Kind != DebtKind.PromiseMeal || d.DebtorId == c.Id) continue;
            Character? guest = w.Find(d.CreditorId);
            if (guest == null || guest.Status != CharStatus.Member || guest.Id == c.Id || !Greetings.Newcomer(w, guest)) continue;
            if (guest.Food >= NeedsSystem.FedAt || Rations.HasReservation(w, guest.Id) || Places.Targeted(w, JobKind.KeepPromise, d.Id, c.Id)) continue;
            Point target = Point.None;
            if (Rations.HasReservation(w, c.Id)) target = Places.FoodTile(w, "berries");
            if (target == Point.None && Places.BushWithBerries(w, c.Pos) is { } bush) target = bush.Pos;
            if (target == Point.None || !Decide(w, c, d, custom)) continue;
            return new Job { Kind = JobKind.KeepPromise, TargetId = d.Id, Target = target, CauseEvent = d.FormedEvent };
        }
        return null;
    }

    /// <summary>Is a host (not the promiser) on the way with food for this promise?</summary>
    public static bool Underway(World w, Debt promise)
    {
        foreach (Character c in w.Characters)
            if (c.Id != promise.DebtorId && c.IsActive && c.Job is { Kind: JobKind.KeepPromise } j && j.TargetId == promise.Id) return true;
        return false;
    }

    /// <summary>Whether this host gives for this promise: rolled the first time, remembered after.</summary>
    private static bool Decide(World w, Character host, Debt d, KeptCustom custom)
    {
        foreach (GuestOffer o in w.Culture.GuestOffers)
            if (o.HostId == host.Id && o.DebtId == d.Id) return o.Accepted;
        bool accepted = w.Rng.Dice[DiceSystem.Culture].Chance(DiceId.GuestOffer, custom.Def.Arg("offerPermille"));
        w.Culture.GuestOffers.Add(new GuestOffer { HostId = host.Id, DebtId = d.Id, Accepted = accepted });
        return accepted;
    }

    /// <summary>
    /// The host hands the guest the food: the custom's event ("shared" for the host's own share,
    /// "foraged" for berries they picked; citing the promise and the guest's hunger), the guest's
    /// thanks, the camp's promise kept, and the guest now owing the host a meal. A host who gave up
    /// their own share goes hungry for it, so the gift becomes the cause of their next hunger; berries
    /// picked for the guest cost the host nothing they would have eaten, so they change no cause.
    /// Returns the event.
    /// </summary>
    public static SimEvent Deliver(World w, Character host, Character guest, Debt promise, bool picked)
    {
        KeptCustom custom = Ways.Keeps(w, host, "guest_right") ?? throw new InvalidOperationException(host.CastKey + " keeps no guest-right");
        SimEvent shared = w.Log.Emit(w, SystemTag.Culture, custom.Def.EventKey(picked ? "foraged" : "shared"), new[] { host.Id, guest.Id },
            new[] { promise.FormedEvent, guest.LastFoodEvent }, null,
            EventArg.Char("a", host.Id), EventArg.Char("b", guest.Id), EventArg.Key("ways", custom.HolderNameKey));
        MoodSystem.AddThought(w, guest, "ate", 0, shared.Id);
        MoodSystem.AddThought(w, guest, "guest_bread_received", host.Id, shared.Id);
        NeedsSystem.AfterFed(guest);
        // The guest owes the host for the promise the host kept: one arc, so the debt cites the keeping.
        SimEvent kept = Relations.Repay(w, promise, shared.Id);
        Relations.FormDebt(w, guest, host, DebtKind.Meal, 1, kept.Id);
        // A share given away is food the host does not eat (a blood-drinker's hunger is not for berries).
        if (!picked && !Places.Species(w, host).DrinksBlood) host.LastFoodEvent = shared.Id;
        Omens.RecordDeed(w, DeedKind.GuestHonoured, shared.Id, host.Id);
        Omens.RecordDeed(w, DeedKind.MealShared, shared.Id, host.Id);
        return shared;
    }

    /// <summary>Forgets decisions about promises that have closed.</summary>
    public static void Prune(World w)
    {
        w.Culture.GuestOffers.RemoveAll(o => w.Debts.Find(d => d.Id == o.DebtId) is not { Status: DebtStatus.Open });
    }
}
