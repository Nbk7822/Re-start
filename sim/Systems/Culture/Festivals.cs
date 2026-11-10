using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// Seasonal festivals (S1, S2, F3; ADR-0023). On its seeded day, at dusk, a festival held by a culture
/// or a faith begins if any of its keepers is in camp: a feast from the stores if there is enough to
/// spare (one berry each to the hungriest), else a lean fire. Everyone may gather at the hearth, and
/// festival-goers talk more warmly. When it ends, those who came feel the better for it, and each
/// keeper who came sets down (or eases) their slightest grudge against another who came.
/// </summary>
public static class Festivals
{
    public static void Hourly(World w)
    {
        End(w);
        Begin(w);
    }

    /// <summary>Are both at a festival under way right now?</summary>
    public static bool BothAttend(World w, int a, int b)
    {
        foreach (FestivalRun run in w.Culture.Festivals)
            if (run.Attendees.BinarySearch(a) >= 0 && run.Attendees.BinarySearch(b) >= 0) return true;
        return false;
    }

    /// <summary>
    /// A Feast job at the hearth while a festival is under way, for anyone not starving, downed or
    /// kept in by the sun. The spot beside the hearth is chosen without randomness (culture never
    /// draws from another system's stream).
    /// </summary>
    public static Job? FeastJob(World w, Character c, bool sunBlocked)
    {
        if (w.Culture.Festivals.Count == 0 || c.HungerStage >= 2 || c.Downed || sunBlocked) return null;
        bool attending = true;
        foreach (FestivalRun run in w.Culture.Festivals)
            if (run.Attendees.BinarySearch(c.Id) < 0) attending = false;
        if (attending && c.Pos.ChebyshevTo(w.Map.Hearth) <= 1) return null;
        Point spot = Places.Adjacent(w, w.Map.Hearth, c.Pos);
        return new Job { Kind = JobKind.Feast, Target = spot == Point.None ? c.Pos : spot, StartedTick = w.Tick, CauseEvent = w.Culture.Festivals[0].StartEvent };
    }

    /// <summary>Someone reached the hearth: they are at every festival under way.</summary>
    public static void Attend(World w, Character c)
    {
        foreach (FestivalRun run in w.Culture.Festivals)
        {
            int at = run.Attendees.BinarySearch(c.Id);
            if (at < 0) run.Attendees.Insert(~at, c.Id);
        }
    }

    private static void Begin(World w)
    {
        WorldDef def = w.Content.World;
        if (Calendar.Hour(w.Tick, def) != def.DuskHour) return;
        int season = Calendar.Season(w.Tick, def), day = Calendar.DayOfSeason(w.Tick, def);
        foreach (FestivalDate date in w.Culture.FestivalDates)
        {
            if (date.Season != season || date.Day != day) continue;
            var keepers = new List<int>();
            foreach (Character m in w.Members())
                if (Ways.IsKeeper(m, date.HolderId, date.HolderIsFaith)) keepers.Add(m.Id);
            if (keepers.Count == 0) continue;
            Start(w, date, keepers);
        }
    }

    private static void Start(World w, FestivalDate date, List<int> keepers)
    {
        CustomDef custom = w.Content.CustomById(date.CustomId);
        string holder = Ways.HolderNameKey(w, date.HolderId, date.HolderIsFaith) ?? "";
        int perHead = custom.Arg("feastPerHead");
        var eaters = new List<Character>();
        foreach (Character m in w.Members())
            if (!m.Asleep && !Places.Species(w, m).DrinksBlood) eaters.Add(m);
        eaters.Sort((a, b) => a.Food != b.Food ? a.Food.CompareTo(b.Food) : a.Id.CompareTo(b.Id));
        int spare = Rations.Unreserved(w, "berries");
        SimEvent begins;
        bool feasted = false;
        if (perHead > 0 && spare >= keepers.Count && eaters.Count > 0)
        {
            // A feast only from what nobody has a claim on: one berry a head, hungriest first (F3).
            int n = Stock.Take(w, "berries", Math.Min(perHead * eaters.Count, spare));
            int nutrition = w.Content.ItemById("berries").Nutrition;
            begins = w.Log.Emit(w, SystemTag.Culture, custom.EventKey("begins"), keepers, Array.Empty<long>(), null,
                EventArg.Key("ways", holder), EventArg.Num("n", n), EventArg.Item("item", "berries"));
            for (int i = 0; i < n; i++)
            {
                Character eater = eaters[i % eaters.Count];
                eater.Food = Math.Min(1000, eater.Food + nutrition);
                NeedsSystem.AfterFed(eater);
            }
            feasted = true;
        }
        else
        {
            begins = w.Log.Emit(w, SystemTag.Culture, custom.EventKey(perHead > 0 ? "begins_lean" : "begins"), keepers, Array.Empty<long>(), null,
                EventArg.Key("ways", holder));
        }
        w.Culture.Festivals.Add(new FestivalRun
        {
            CustomId = date.CustomId, HolderId = date.HolderId, HolderIsFaith = date.HolderIsFaith, StartEvent = begins.Id,
            EndsTick = w.Tick + (long)custom.Arg("hours") * w.Content.World.TicksPerHour, Feasted = feasted,
        });
    }

    private static void End(World w)
    {
        for (int r = 0; r < w.Culture.Festivals.Count; r++)
        {
            FestivalRun run = w.Culture.Festivals[r];
            if (w.Tick < run.EndsTick) continue;
            CustomDef custom = w.Content.CustomById(run.CustomId);
            bool lean = custom.Arg("feastPerHead") > 0 && !run.Feasted;
            int forgive = custom.Arg("forgive");
            foreach (int id in run.Attendees)
            {
                if (w.Find(id) is not { Status: CharStatus.Member } c) continue;
                MoodSystem.AddThought(w, c, lean ? "festival_lean" : "festival", 0, run.StartEvent);
                if (forgive > 0 && Ways.IsKeeper(c, run.HolderId, run.HolderIsFaith)) Forgive(w, c, run, custom, forgive);
            }
            w.Culture.Festivals.RemoveAt(r--);
        }
    }

    /// <summary>A keeper's slightest grudge against another festival-goer (ties: lowest id) eases by the custom's forgiveness, and ends at 0.</summary>
    private static void Forgive(World w, Character keeper, FestivalRun run, CustomDef custom, int forgive)
    {
        Grudge? slightest = null;
        foreach (Grudge g in w.Grudges)
        {
            if (!g.Active || g.HolderId != keeper.Id || run.Attendees.BinarySearch(g.TargetId) < 0) continue;
            if (slightest == null || g.Intensity < slightest.Intensity || (g.Intensity == slightest.Intensity && g.Id < slightest.Id)) slightest = g;
        }
        if (slightest == null) return;
        slightest.Intensity -= forgive;
        if (slightest.Intensity > 0) return;
        slightest.Active = false;
        SimEvent e = w.Log.Emit(w, SystemTag.Culture, custom.EventKey("forgave"), new[] { keeper.Id, slightest.TargetId },
            new[] { slightest.FormedEvent, run.StartEvent },
            new[] { new Effect { Kind = EffectKind.GrudgeEnded, A = keeper.Id, B = slightest.TargetId, RefId = slightest.Id } },
            EventArg.Char("a", keeper.Id), EventArg.Char("b", slightest.TargetId));
        MoodSystem.AddThought(w, keeper, "grudge_set_down", slightest.TargetId, e.Id);
    }
}
