using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// The six M0 incidents. Wanderers arrive on their scheduled tick; the director rolls the rest.
/// Every incident emits an Incident event and records itself as the cause of whatever it touches
/// (a person's food, health or rest), so its consequences chain back to it.
/// </summary>
public static class IncidentSystem
{
    public static void Hourly(World w)
    {
        foreach (Arrival a in w.Arrivals)
            if (!a.Done && a.Tick <= w.Tick)
            {
                a.Done = true;
                Character? c = w.Find(a.CharacterId);
                if (c != null && c.Status == CharStatus.OffMap) Arrive(w, c);
            }
        RainUpkeep(w);
        Director.Hourly(w);
    }

    /// <summary>Fires an incident by id. targetId names the person it happens to, when there is one.</summary>
    public static SimEvent? Fire(World w, string incidentId, long cause = 0, int targetId = 0) => incidentId switch
    {
        "rain" => StartRain(w),
        "food_spoils" => Spoil(w),
        "work_accident" => Accident(w, cause, targetId),
        "night_thief" => SendThief(w),
        "windfall" => Windfall(w),
        "wanderer_arrives" => targetId != 0 && w.Find(targetId) is { Status: CharStatus.OffMap } c ? Arrive(w, c) : null,
        _ => throw new ContentException($"incidents.json: no behaviour for incident '{incidentId}'."),
    };

    public static SimEvent Arrive(World w, Character c)
    {
        DiceStream dice = w.Rng.Dice[DiceSystem.Incident];
        c.Status = CharStatus.Member;
        c.ArrivedTick = w.Tick;
        c.Pos = EdgeTile(w, DiceId.ArrivalEdge);
        SpeciesDef sp = Places.Species(w, c);
        c.Food = sp.EatsNothing ? 1000 : dice.Range(DiceId.ArrivalFood, 280, 560);
        string key = sp.ArrivalKey.Length > 0 ? sp.ArrivalKey : "event.incident.wanderer_arrives";
        SimEvent e = w.Log.Emit(w, SystemTag.Incident, key, new[] { c.Id }, Array.Empty<long>(), null, EventArg.Char("a", c.Id));
        c.LastFoodEvent = e.Id;
        Growth.EnterWorld(w, c, e.Id);
        Point near = Zones.ArrivalSpot(w, c.Pos); // received at the meeting place (ADR-0028)
        c.Job = new Job { Kind = JobKind.Arrive, Target = near, StartedTick = w.Tick, CauseEvent = e.Id };
        TravelSystem.Start(w, c, near);
        return e;
    }

    /// <summary>
    /// A wanderer who reaches the hearth is invited to stay. If the camp cannot give them what they
    /// need right now (a meal when there is no spare food; a sound roof, for those who shun the sun,
    /// when none is mended), the inviter promises it within two days (ADR-0014). One event carries the
    /// recruitment and any promise, so joining alone is not yet a story; what happens to the promise is.
    /// </summary>
    public static SimEvent Join(World w, Character c, long arrivalEvent)
    {
        Character? inviter = null;
        foreach (Character m in w.Members())
            if (m.Id != c.Id && (inviter == null || (m.Id == w.PlayerId && inviter.Id != w.PlayerId))) inviter = m;
        if (inviter == null)
            return w.Log.Emit(w, SystemTag.Social, "event.social.joined_alone", new[] { c.Id }, new[] { arrivalEvent },
                new[] { new Effect { Kind = EffectKind.Recruitment, A = c.Id } }, EventArg.Char("a", c.Id));

        DebtKind? need = UnmetNeed(w, c);
        if (need == null)
        {
            SimEvent welcomed = w.Log.Emit(w, SystemTag.Social, "event.social.joined", new[] { c.Id, inviter.Id }, new[] { arrivalEvent },
                new[] { new Effect { Kind = EffectKind.Recruitment, A = c.Id, B = inviter.Id } },
                EventArg.Char("a", c.Id), EventArg.Char("b", inviter.Id));
            MoodSystem.AddThought(w, c, "joined_camp", inviter.Id, welcomed.Id);
            Courtesy.SwearWelcome(w, c, inviter, welcomed);
            return welcomed;
        }

        bool rest = need == DebtKind.PromiseRest;
        var promise = new Debt
        {
            Id = w.NewId(), DebtorId = inviter.Id, CreditorId = c.Id, Kind = need.Value, Weight = 1,
            CauseEvent = arrivalEvent, FormedTick = w.Tick, Status = DebtStatus.Open,
            DueTick = w.Tick + (long)w.Content.World.PromiseDueHours * w.Content.World.TicksPerHour,
        };
        w.Debts.Add(promise);
        SimEvent joined = w.Log.Emit(w, SystemTag.Social, rest ? "event.social.joined_promised_rest" : "event.social.joined_promised_meal",
            new[] { c.Id, inviter.Id }, new[] { arrivalEvent },
            new[]
            {
                new Effect { Kind = EffectKind.Recruitment, A = c.Id, B = inviter.Id },
                new Effect { Kind = EffectKind.DebtFormed, A = inviter.Id, B = c.Id, RefId = promise.Id },
            },
            EventArg.Char("a", c.Id), EventArg.Char("b", inviter.Id), EventArg.Num("n", Math.Max(1, (w.Content.World.PromiseDueHours + 12) / 24)));
        promise.FormedEvent = joined.Id;
        if (!rest) c.LastFoodEvent = joined.Id;
        MoodSystem.AddThought(w, c, "joined_camp", inviter.Id, joined.Id);
        Courtesy.SwearWelcome(w, c, inviter, joined);
        return joined;
    }

    /// <summary>
    /// What the camp cannot give a newcomer right now, as the promise it would take; null if nothing.
    /// Food is short when nothing in the stockpile is free for them to eat now (everything left is
    /// set aside for others, or there is nothing at all).
    /// </summary>
    public static DebtKind? UnmetNeed(World w, Character c)
    {
        if (Places.BurnsInDaylight(w, c)) return w.Map.Roof.Exists(r => r.Sound) ? null : DebtKind.PromiseRest;
        SpeciesDef sp = Places.Species(w, c);
        if (!sp.EatsItem) return null; // a meal is no use to someone who eats nothing from the stores
        return Rations.Unreserved(w, sp.FoodSource) > 0 ? null : DebtKind.PromiseMeal;
    }

    private static SimEvent? StartRain(World w)
    {
        if (w.Weather.Raining) return null;
        IncidentDef def = w.Content.IncidentById("rain");
        int hours = w.Rng.Dice[DiceSystem.Incident].Range(DiceId.RainLength, def.Param("minHours"), def.Param("maxHours") + 1);
        SimEvent e = w.Log.Emit(w, SystemTag.Incident, "event.incident.rain", Array.Empty<int>(), Array.Empty<long>());
        w.Weather.Raining = true;
        w.Weather.RainEndsTick = w.Tick + (long)hours * w.Content.World.TicksPerHour;
        w.Weather.RainEvent = e.Id;
        Culture.Omens.OnSign(w, "rain", e); // the faithful may read it as a god's verdict on a recent deed (ADR-0023)
        return e;
    }

    private static void RainUpkeep(World w)
    {
        if (!w.Weather.Raining) return;
        if (w.Tick >= w.Weather.RainEndsTick)
        {
            w.Weather.Raining = false;
            return;
        }
        int soak = w.Content.IncidentById("rain").Param("soakRestPerHour");
        foreach (Character c in w.Members())
        {
            if (w.Map.Covered(c.Pos)) continue;
            MoodSystem.AddThought(w, c, "soaked", 0, w.Weather.RainEvent);
            if (!c.Asleep) continue;
            c.Rest = Math.Max(0, c.Rest - soak);
            c.LastRestEvent = w.Weather.RainEvent;
        }
    }

    private static SimEvent? Spoil(World w)
    {
        IncidentDef def = w.Content.IncidentById("food_spoils");
        // The damp reaches only stores lying in the open: a sound roof or the covered ruins keep food dry (ADR-0028).
        List<ItemStack> open = Stock.InTheOpen(w, "berries");
        if (Stock.Total(open) < def.Param("minBerries")) return null;
        int lost = Stock.Take(w, open, w.Rng.Dice[DiceSystem.Incident].Range(DiceId.SpoilLoss, def.Param("lossMin"), def.Param("lossMax") + 1));
        SimEvent e = w.Log.Emit(w, SystemTag.Incident, "event.incident.food_spoils", Array.Empty<int>(), Array.Empty<long>(), null,
            EventArg.Num("n", lost), EventArg.Item("item", "berries"));
        foreach (Character m in w.Members())
            if (w.Content.SpeciesById(m.SpeciesId).FoodSource == "berries") m.LastFoodEvent = e.Id;
        Culture.Omens.OnSign(w, "food_spoils", e);
        return e;
    }

    private static SimEvent? Accident(World w, long cause, int targetId)
    {
        Character? c = w.Find(targetId);
        if (c == null || c.Status != CharStatus.Member) return null;
        IncidentDef def = w.Content.IncidentById("work_accident");
        int damage = w.Rng.Dice[DiceSystem.Incident].Range(DiceId.AccidentDamage, def.Param("damageMin"), def.Param("damageMax") + 1);
        SimEvent e = w.Log.Emit(w, SystemTag.Incident, "event.incident.work_accident", new[] { c.Id }, new[] { cause }, null, EventArg.Char("a", c.Id));
        Culture.Omens.OnSign(w, "work_accident", e);
        CombatSystem.Hurt(w, c, damage, e.Id, "event.incident.died_accident", SystemTag.Incident);
        return e;
    }

    private static SimEvent? SendThief(World w)
    {
        IncidentDef def = w.Content.IncidentById("night_thief");
        var thief = new Character
        {
            Id = w.NewId(), NameKey = "npc.thief.name", SpeciesId = "human", Named = false, Status = CharStatus.Hostile,
            Health = def.Param("health"), Food = 1000, Rest = 1000, Social = 1000, Traits = new List<string>(),
        };
        thief.Pos = EdgeTile(w, DiceId.ThiefEdge);
        w.Characters.Add(thief);
        SimEvent e = w.Log.Emit(w, SystemTag.Incident, "event.incident.night_thief", new[] { thief.Id }, Array.Empty<long>(), null, EventArg.Char("a", thief.Id));
        thief.Job = new Job { Kind = JobKind.Raid, Target = RaidTarget(w), StartedTick = w.Tick, CauseEvent = e.Id };
        TravelSystem.Start(w, thief, thief.Job.Target);
        return e;
    }

    /// <summary>A thief goes for the oldest stored berries; with none, for the first stockpile that keeps berries (ADR-0028).</summary>
    private static Point RaidTarget(World w)
    {
        Point food = Places.FoodTile(w, "berries");
        if (food != Point.None) return food;
        Zone? pile = Zones.FirstKeeping(w, "berries");
        return pile == null ? Places.Adjacent(w, w.Map.Hearth, w.Map.Hearth) : Zones.Landmark(pile);
    }

    private static SimEvent? Windfall(World w)
    {
        IncidentDef def = w.Content.IncidentById("windfall");
        DiceStream dice = w.Rng.Dice[DiceSystem.Incident];
        string item = dice.Range(DiceId.WindfallKind, 0, 2) == 0 ? "berries" : "timber";
        int y = dice.Range(DiceId.WindfallRow, 1, w.Map.Height - 1);
        Point spot = Point.None;
        for (int x = w.Map.Width - 1; x >= 0; x--)
        {
            var p = new Point(x, y);
            if (w.Map.Passable(p)) { spot = p; break; }
        }
        if (spot == Point.None) return null;
        int amount = def.Param("amount");
        Stock.Add(w, item, amount, spot);
        SimEvent e = w.Log.Emit(w, SystemTag.Incident, item == "berries" ? "event.incident.windfall_berries" : "event.incident.windfall_timber",
            Array.Empty<int>(), Array.Empty<long>(), null, EventArg.Num("n", amount), EventArg.Item("item", item));
        foreach (Character m in w.Members()) MoodSystem.AddThought(w, m, "windfall", 0, e.Id);
        Culture.Omens.OnSign(w, "windfall", e);
        return e;
    }

    /// <summary>A passable tile on the west or south edge, chosen from Dice.</summary>
    public static Point EdgeTile(World w, DiceId draw)
    {
        DiceStream dice = w.Rng.Dice[DiceSystem.Incident];
        Map m = w.Map;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            bool west = dice.Range(draw, 0, 2) == 0;
            Point p = west ? new Point(0, dice.Range(draw, 1, m.Height - 1)) : new Point(dice.Range(draw, 1, m.Width - 10), m.Height - 1);
            // An edge tile walled off from the camp by rock or river would strand whoever comes in there
            // (a dryad withered in a corner on her fourth day, seed 3), so only a tile with a way to the hearth will do.
            if (m.Passable(p) && Pathfinder.Find(m, p, Places.Adjacent(w, m.Hearth, p)) != null) return p;
        }
        return Places.Adjacent(w, m.Hearth, m.Hearth);
    }
}
