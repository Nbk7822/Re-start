using Return.Sim.Content;
using Return.Sim.Events;

namespace Return.Sim.Systems.Jobs;

/// <summary>Eating (and the Desperate taking a reserved meal), sleeping, wandering, seeking company.</summary>
public static class NeedsJobs
{
    public static bool Eat(World w, Character c, Job j)
    {
        if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
        if (!JobRunner.Work(j, w.Content.WorkById("eat").DurationTicks)) return false;
        string food = Places.Species(w, c).FoodSource;
        if (j.TargetId != 0 && Rations.HasReservation(w, j.TargetId) && !Rations.HasReservation(w, c.Id) && Rations.Unreserved(w, food) == 0)
        {
            TakeReservedMeal(w, c, w.Get(j.TargetId), food);
            return true;
        }
        if (Rations.HasReservation(w, c.Id) || Rations.Unreserved(w, food) > 0) NeedsSystem.Eat(w, c, j.CauseEvent);
        return true;
    }

    /// <summary>The demo's Desperate theft: takes the meal set aside for someone else, who will remember.</summary>
    public static SimEvent? TakeReservedMeal(World w, Character thief, Character victim, string food)
    {
        if (Stock.Take(w, food, 1) == 0) return null;
        Rations.Consume(w, victim.Id);
        ItemDef item = w.Content.ItemById(food);
        thief.Food = Math.Min(1000, thief.Food + item.Nutrition);
        SimEvent e = JobRunner.Emit(w, SystemTag.Social, "event.social.took_meal", new[] { thief.Id, victim.Id }, new[] { thief.LastFoodEvent },
            EventArg.Char("a", thief.Id), EventArg.Char("b", victim.Id), EventArg.Item("item", item.Id));
        MoodSystem.AddThought(w, thief, "ate", 0, e.Id);
        NeedsSystem.AfterFed(thief);
        MoodSystem.AddThought(w, victim, "meal_stolen", thief.Id, e.Id);
        victim.LastFoodEvent = e.Id;
        Relations.FormGrudge(w, victim, thief, 35, e.Id);
        return e;
    }

    public static bool Sleep(World w, Character c, Job j)
    {
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            j.Stage = 1;
            c.Asleep = true;
            c.Facing = Facing.S;
            return false;
        }
        bool injured = c.Health < JobPicker.InjuredBelow;
        bool wake = injured
            ? c.Health >= 600 && c.Rest >= 600
            : c.Food < NeedsSystem.StarvingBelow || c.Rest >= 950 || (!Places.SleepHours(w, c) && c.Rest >= 600);
        if (Places.BurnsInDaylight(w, c) && Places.Daylight(w) && !w.Map.Covered(c.Pos)) wake = true;
        // A need of their nature that harms them comes before rest (ADR-0022): rested enough, they get up to answer it.
        if (!wake && c.Rest >= 600 && c.NatureNeeds.Count > 0)
            wake = NatureNeeds.WakesFor(w, c, Places.BurnsInDaylight(w, c) && Places.SunDanger(w));
        if (!wake) return false;
        c.Asleep = false;
        bool covered = w.Map.Covered(c.Pos);
        MoodSystem.AddThought(w, c, covered ? "slept_in_shelter" : "slept_outside", 0, 0);
        return true;
    }

    public static bool Wander(World w, Character c, Job j)
    {
        if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
        return JobRunner.Work(j, Math.Max(1, j.TargetId));
    }

    public static bool Socialize(World w, Character c, Job j)
    {
        Character? other = w.Find(j.TargetId);
        if (other == null || other.Status != CharStatus.Member) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.Near(w, c, j, other)) return JobRunner.Failed(j);
            j.Stage = 1;
        }
        return JobRunner.Work(j, w.Content.WorkById("socialize").DurationTicks);
    }
}
