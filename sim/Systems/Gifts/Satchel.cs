using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// Bottomless satchel (the item box of isekai; the player design §4.4): a store only you can reach. Nothing in it
/// spoils, no thief reaches it, and the ration policy does not count it. You fill it when you forage (unless your
/// order is to keep nothing back, or berries cannot feed you and there is nobody to keep them for) and eat from it first
/// when hungry, so the policy cannot leave you out; under "share"
/// you take a meal from it to anyone starving, and they owe you. It is seen: someone going hungry who watches you eat
/// from nowhere resents it, and repeated sightings harden into a grudge; anyone else's first sighting in a timeline
/// unsettles them. It wakes on your first forage (by Day 2 in a dream) and grows each time a meal from it lifts
/// someone out of starving.
/// </summary>
public static class Satchel
{
    public const string Keep = "keep";
    public const string Share = "share";
    public const string Empty = "empty";

    private static string Choice(World w, GiftDef g) => g.Order == null ? Keep : Gifts.Order(w, g.Order);

    public static int Capacity(World w, GiftDef g) => g.Num("capacity", w.Gift.Tier);

    /// <summary>Your forage just filled your hands: some of it goes into the satchel, citing the forage. Under "keep nothing back", what it holds goes to the stores instead.</summary>
    public static void Foraged(World w, Character you, SimEvent forage)
    {
        GiftDef g = Gifts.Held(w)!;
        if (!w.Fate.GiftAwakened) Gifts.Awaken(w, forage.Id, dream: false);
        if (you.Carrying is not { DefId: "berries" } carry) return;
        string choice = Choice(w, g);
        if (choice == Empty)
        {
            carry.Count += w.Gift.Held;
            w.Gift.Held = 0;
            return;
        }
        // A reserve for yourself is nothing to someone berries cannot feed (a vampire, the undead, a dryad, a djinn): kept
        // back, it would only be the camp's food gone where nobody can eat it. Their satchel fills only to share.
        if (choice == Keep && Places.Species(w, you).FoodSource != "berries") return;
        int n = Math.Min(Capacity(w, g) - w.Gift.Held, carry.Count);
        if (n <= 0) return;
        carry.Count -= n;
        w.Gift.Held += n;
        if (carry.Count == 0) you.Carrying = null;
        w.Log.Emit(w, SystemTag.Gift, "event.gift.satchel.stashed", new[] { you.Id }, new[] { forage.Id }, null,
            EventArg.Char("a", you.Id), EventArg.Num("n", n), EventArg.Item("item", "berries"));
    }

    /// <summary>Hungry and awake, you eat from the satchel before food is shared out, and whoever sees it takes it as they will.</summary>
    public static void Hourly(World w, Character you, GiftDef g)
    {
        if (w.Gift.Held <= 0 || you.Downed || you.Asleep || you.Food >= NeedsSystem.HungryBelow) return;
        if (Places.Species(w, you).FoodSource != "berries") return; // a store of berries feeds only those who eat them
        ItemDef berries = w.Content.ItemById("berries");
        w.Gift.Held--;
        you.Food = Math.Min(1000, you.Food + berries.Nutrition);
        SimEvent ate = w.Log.Emit(w, SystemTag.Needs, "event.gift.satchel.ate", new[] { you.Id }, new[] { you.LastFoodEvent }, null,
            EventArg.Char("a", you.Id), EventArg.Item("item", "berries"));
        MoodSystem.AddThought(w, you, "ate", 0, ate.Id);
        NeedsSystem.AfterFed(you);
        Seen(w, you, g, ate, eating: true);
    }

    /// <summary>
    /// Who sees food come from nowhere. Someone going hungry (hungry now, or left out at a share-out and not fed since)
    /// who sees you eat it resents it, citing your meal and their going without; past the grudge line, they hold a grudge.
    /// Anyone else seeing it for the first time in a timeline is unsettled.
    /// </summary>
    private static void Seen(World w, Character you, GiftDef g, SimEvent meal, bool eating)
    {
        int sight = g.Num("sight");
        foreach (Character m in w.Characters)
        {
            if (m.Id == you.Id || m.Status != CharStatus.Member || m.Asleep || m.Downed || m.Pos.ChebyshevTo(you.Pos) > sight) continue;
            if (meal.Actors.Count > 1 && meal.Actors[1] == m.Id) continue; // the one you fed
            bool goingWithout = (m.HungerStage >= 1 || m.SkippedRation) && Places.Species(w, m).FoodSource == "berries";
            if (eating && goingWithout)
            {
                SimEvent seen = w.Log.Emit(w, SystemTag.Social, "event.gift.satchel.seen", new[] { m.Id, you.Id }, new[] { meal.Id, m.LastFoodEvent }, null,
                    EventArg.Char("a", m.Id), EventArg.Char("b", you.Id));
                MoodSystem.AddThought(w, m, "ate_from_nowhere", you.Id, seen.Id);
                if (Relations.Opinion(w, m.Id, you.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, m.Id, you.Id) == null)
                    Relations.FormGrudge(w, m, you, g.Num("grudgeIntensity"), seen.Id);
                continue;
            }
            int at = w.Gift.SeenBy.BinarySearch(m.Id);
            if (at >= 0) continue;
            w.Gift.SeenBy.Insert(~at, m.Id);
            MoodSystem.AddThought(w, m, "saw_vanish", you.Id, meal.Id);
        }
    }

    /// <summary>Under "share", someone starving whom you can reach: go and take them a meal from the satchel.</summary>
    public static Job? ShareJob(World w, Character you, bool sunBlocked)
    {
        GiftDef g = Gifts.Held(w)!;
        if (w.Gift.Held <= 0 || Choice(w, g) != Share) return null;
        Character? best = null;
        int bestDist = int.MaxValue;
        foreach (Character m in w.Characters)
        {
            if (m.Id == you.Id || m.Status != CharStatus.Member || m.HungerStage < 2 || Places.Species(w, m).FoodSource != "berries") continue;
            if (sunBlocked && !w.Map.Covered(m.Pos)) continue;
            int d = you.Pos.ChebyshevTo(m.Pos);
            if (d < bestDist) { best = m; bestDist = d; }
        }
        return best == null ? null : new Job { Kind = JobKind.GiftShare, TargetId = best.Id, Target = best.Pos, CauseEvent = best.LastFoodEvent };
    }

    /// <summary>The GiftShare job: reach them and give them a meal from the satchel. They owe you for it; lifting them out of starving grows the gift.</summary>
    public static bool ShareMeal(World w, Character you, Job j)
    {
        Character? m = w.Find(j.TargetId);
        GiftDef? g = Gifts.Held(w);
        if (g == null || m == null || m.Status != CharStatus.Member || w.Gift.Held <= 0 || m.HungerStage < 2) return true;
        if (!JobRunner.Near(w, you, j, m)) return JobRunner.Failed(j);
        ItemDef berries = w.Content.ItemById("berries");
        bool starving = m.Food < NeedsSystem.StarvingBelow;
        w.Gift.Held--;
        m.Food = Math.Min(1000, m.Food + berries.Nutrition);
        SimEvent shared = w.Log.Emit(w, SystemTag.Gift, "event.gift.satchel.shared", new[] { you.Id, m.Id }, new[] { m.LastFoodEvent }, null,
            EventArg.Char("a", you.Id), EventArg.Char("b", m.Id));
        MoodSystem.AddThought(w, m, "ate", 0, shared.Id);
        NeedsSystem.AfterFed(m);
        Relations.FormDebt(w, m, you, DebtKind.Meal, 1, shared.Id);
        Seen(w, you, g, shared, eating: false);
        if (starving && m.Food >= NeedsSystem.StarvingBelow) Gifts.Count(w, shared.Id);
        return true;
    }
}
