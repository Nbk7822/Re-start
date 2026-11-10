using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Hourly needs: food (or blood) and social decay, rest drains while awake and refills while asleep,
/// starvation and sunlight hurt, health recovers when fed and rested. Hunger crossing a threshold
/// emits an event caused by whatever last affected that person's food, which is how a spoilage,
/// a theft or a ration decision becomes the start of a story.
/// </summary>
public static class NeedsSystem
{
    public const int HungryBelow = 300;
    public const int StarvingBelow = 150;
    public const int FedAt = 450;
    public const int DownedBelow = 150;
    public const int RecoverAt = 300;
    public const int StarvationDamagePerHour = 10;

    public static void Hourly(World w)
    {
        WorldDef def = w.Content.World;
        bool daylight = Calendar.IsDaylight(w.Tick, def);
        if (Calendar.IsDayStart(w.Tick, def))
        {
            RotFood(w);
            if (w.Tick % def.TicksPerSeason == 0) SeasonTurns(w);
        }

        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member) continue;
            SpeciesDef sp = w.Content.SpeciesById(c.SpeciesId);

            c.Food = Math.Max(0, c.Food - sp.FoodDecayPerHour);
            if (!sp.Tires) c.Rest = 1000;
            else if (c.Asleep) c.Rest = Math.Min(1000, c.Rest + sp.SleepRestorePerHour + Building.RestBonus(w, c, sp.SleepRestorePerHour));
            else c.Rest = Math.Max(0, c.Rest - sp.RestDecayPerHour - JobRestCost(w, c));
            c.Social = Math.Max(0, c.Social - sp.SocialDecayPerHour);
            UpdateHunger(w, c, sp);

            int damage = 0;
            string deathKey = "event.needs.died";
            if (c.Food == 0)
            {
                damage += StarvationDamagePerHour;
                deathKey = sp.DrinksBlood ? "event.needs.died_blood" : "event.needs.died_starving";
                c.LastHealthEvent = c.LastFoodEvent;
            }
            if (c.NatureNeeds.Count > 0) damage += NatureNeeds.Hourly(w, c, ref deathKey);
            if (c.Bleed > 0)
            {
                // A wound nobody has tended goes on bleeding, and a death of it is the wound's own (ADR-0030).
                damage += c.Bleed;
                deathKey = c.BleedDeath.Length > 0 ? c.BleedDeath : "event.combat.died_wounds";
                c.LastHealthEvent = c.BleedCause;
            }
            if (sp.DaylightBurnPerHour > 0 && daylight && !w.Map.Covered(c.Pos))
            {
                // Only harm that is still going on (starving now, or burning last hour) is a cause of this burn.
                long ongoing = c.Food == 0 || IsRecent(w, c.LastHealthEvent) ? c.LastHealthEvent : 0;
                SimEvent burn = w.Log.Emit(w, SystemTag.Needs, "event.needs.sun_burn", new[] { c.Id }, new[] { ongoing }, null, EventArg.Char("a", c.Id));
                MoodSystem.AddThought(w, c, "burned_by_sun", 0, burn.Id);
                damage += sp.DaylightBurnPerHour;
                deathKey = "event.needs.died_sun";
                c.LastHealthEvent = burn.Id;
            }

            if (damage > 0) c.Health -= damage;
            else if (c.Food >= HungryBelow && c.Rest >= 200 && c.Health < 1000) c.Health = Math.Min(1000, c.Health + (c.Asleep ? 25 : 10));

            if (c.Health <= 0)
            {
                Lifecycle.Die(w, c, SystemTag.Needs, deathKey, c.LastHealthEvent);
                continue;
            }
            if (!c.Downed && c.Health < DownedBelow)
            {
                c.Downed = true;
                c.Asleep = false;
                c.Path.Clear();
                SimEvent fell = w.Log.Emit(w, SystemTag.Needs, "event.needs.collapsed", new[] { c.Id }, new[] { c.LastHealthEvent }, null, EventArg.Char("a", c.Id));
                c.LastHealthEvent = fell.Id;
            }
            else if (c.Downed && c.Health >= RecoverAt)
            {
                c.Downed = false;
            }

            if (c.Rest < 150) MoodSystem.AddThought(w, c, "exhausted", 0, c.LastRestEvent);
            if (c.Social < 200) MoodSystem.AddThought(w, c, "lonely", 0, 0);
        }
    }

    /// <summary>Eats one unit of the species' food from the stockpile. Returns null if there is none.</summary>
    public static SimEvent? Eat(World w, Character c, long cause)
    {
        SpeciesDef sp = w.Content.SpeciesById(c.SpeciesId);
        if (!sp.EatsItem || Stock.Take(w, sp.FoodSource, 1, c.Pos) == 0) return null;
        Rations.Consume(w, c.Id);
        ItemDef item = w.Content.ItemById(sp.FoodSource);
        c.Food = Math.Min(1000, c.Food + item.Nutrition);
        SimEvent e = w.Log.Emit(w, SystemTag.Needs, "event.needs.ate", new[] { c.Id }, new[] { cause }, null,
            EventArg.Char("a", c.Id), EventArg.Item("item", item.Id));
        MoodSystem.AddThought(w, c, "ate", 0, e.Id);
        AfterFed(c);
        KeepMealPromises(w, c, e.Id);
        return e;
    }

    /// <summary>As in the demo, a meal promise is kept when its creditor is fed from camp stores before it falls due.</summary>
    private static void KeepMealPromises(World w, Character c, long ateEvent)
    {
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseMeal && d.CreditorId == c.Id)
                Relations.Repay(w, d, ateEvent);
    }

    /// <summary>Called when a character's food need is restored by any means.</summary>
    public static void AfterFed(Character c)
    {
        if (c.Food < FedAt) return;
        c.HungerStage = 0;
        c.SkippedRation = false;
        c.LastFoodEvent = 0;
    }

    private static void UpdateHunger(World w, Character c, SpeciesDef sp)
    {
        if (sp.EatsNothing) return;
        bool blood = sp.DrinksBlood;
        if (c.LastFoodEvent == 0 && !blood && w.WinterEvent != 0 && c.Food < HungryBelow) c.LastFoodEvent = w.WinterEvent;
        if (c.HungerStage < 1 && c.Food < HungryBelow)
        {
            SimEvent e = w.Log.Emit(w, SystemTag.Needs, blood ? "event.needs.thirsting" : "event.needs.hungry",
                new[] { c.Id }, new[] { c.LastFoodEvent }, null, EventArg.Char("a", c.Id));
            c.HungerStage = 1;
            c.LastFoodEvent = e.Id;
        }
        if (c.HungerStage < 2 && c.Food < StarvingBelow)
        {
            SimEvent e = w.Log.Emit(w, SystemTag.Needs, blood ? "event.needs.blood_starved" : "event.needs.starving",
                new[] { c.Id }, new[] { c.LastFoodEvent }, null, EventArg.Char("a", c.Id));
            c.HungerStage = 2;
            c.StarvingSince = w.Tick;
            c.LastFoodEvent = e.Id;
        }
        if (c.Food >= FedAt) c.HungerStage = 0;
        if (c.HungerStage == 1) MoodSystem.AddThought(w, c, blood ? "thirsting" : "hungry", 0, c.LastFoodEvent);
        else if (c.HungerStage == 2) MoodSystem.AddThought(w, c, "starving", 0, c.LastFoodEvent);
    }

    /// <summary>
    /// Each season opens with an event. Winter's frost strips the bushes (they only trickle until
    /// spring), so it becomes the cause of the hunger that follows (see UpdateHunger) for anyone who
    /// eats berries and has no more recent cause.
    /// </summary>
    private static void SeasonTurns(World w)
    {
        WorldDef def = w.Content.World;
        int season = Calendar.Season(w.Tick, def);
        bool lean = season == def.SeasonsPerYear - 1;
        SimEvent e = w.Log.Emit(w, SystemTag.World, lean ? "event.world.winter_begins" : "event.world.season_begins",
            Array.Empty<int>(), Array.Empty<long>(), null, EventArg.Key("season", def.SeasonKeys[season]));
        w.WinterEvent = lean ? e.Id : 0;
        if (!lean) return;
        foreach (Bush b in w.Map.Bushes) b.Berries = 0;
    }

    private static bool IsRecent(World w, long eventId) =>
        w.Log.Get(eventId) is { } e && w.Tick - e.Tick <= w.Content.World.TicksPerHour;

    private static int JobRestCost(World w, Character c)
    {
        if (c.Job == null) return 0;
        WorkDef? work = w.Content.TryWork(Names.Snake(c.Job.Kind.ToString()));
        return work?.RestCostPerHour ?? 0;
    }

    private static void RotFood(World w)
    {
        var rotted = new List<string>();
        var counts = new List<int>();
        foreach (ItemStack s in w.Items)
        {
            if (s.SpoilTick == 0 || s.SpoilTick > w.Tick) continue;
            int i = rotted.IndexOf(s.DefId);
            if (i < 0) { rotted.Add(s.DefId); counts.Add(s.Count); }
            else counts[i] += s.Count;
            s.Count = 0;
        }
        w.Items.RemoveAll(s => s.Count <= 0);
        for (int i = 0; i < rotted.Count; i++)
        {
            SimEvent e = w.Log.Emit(w, SystemTag.Needs, "event.needs.food_rotted", Array.Empty<int>(), Array.Empty<long>(), null,
                EventArg.Num("n", counts[i]), EventArg.Item("item", rotted[i]));
            foreach (Character m in w.Members())
                if (m.LastFoodEvent == 0 && w.Content.SpeciesById(m.SpeciesId).FoodSource == rotted[i]) m.LastFoodEvent = e.Id;
        }
    }
}
