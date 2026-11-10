using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// Who you are reborn as (the player design §4, ADR-0030): your people, your gift and the length of your
/// life, all drawn once per world from Truths keyed by your cast key, so no death ever changes them and
/// across deaths you learn what you are. Everyone's age and lifespan come from Truths too (§6.1).
/// Pure functions of content and seed: they draw no Dice.
/// </summary>
public static class Rebirth
{
    /// <summary>The people you are reborn as: a weighted pick over rebirth.json's peoples (file order, mods merged), with <see cref="TruthId.PlayerPeople"/>.</summary>
    public static string PeopleOf(ContentDb content, ulong seed, string castKey)
    {
        var truths = new TruthRng { WorldSeed = seed };
        int total = 0;
        foreach (RebirthPeopleDef p in content.Rebirth.Peoples) total += Math.Max(0, p.Weight);
        int roll = truths.Range(TruthId.PlayerPeople, Fnv1a.Hash64(castKey), 0, 0, total);
        foreach (RebirthPeopleDef p in content.Rebirth.Peoples)
        {
            if (p.Weight <= 0) continue;
            if (roll < p.Weight) return p.Species;
            roll -= p.Weight;
        }
        throw new InvalidOperationException("rebirth.json has no people to roll.");
    }

    /// <summary>Your gift: a weighted pick over gifts.json (file order) by each gift's weight for your people, with <see cref="TruthId.PlayerGift"/>; "" if none has weight.</summary>
    public static string GiftOf(ContentDb content, ulong seed, string castKey, string people)
    {
        var truths = new TruthRng { WorldSeed = seed };
        int total = 0;
        foreach (GiftDef g in content.Gifts) total += Math.Max(0, g.WeightFor(people));
        if (total <= 0) return "";
        int roll = truths.Range(TruthId.PlayerGift, Fnv1a.Hash64(castKey), 0, 0, total);
        foreach (GiftDef g in content.Gifts)
        {
            int weight = Math.Max(0, g.WeightFor(people));
            if (roll < weight) return g.Id;
            roll -= weight;
        }
        return "";
    }

    /// <summary>The people for the otherworlder who starts in camp: chosen for testing, else rolled.</summary>
    public static string PeopleFor(ContentDb content, ulong seed, string castKey, RebirthChoice? choice)
    {
        if (choice?.People is not { } chosen) return PeopleOf(content, seed, castKey);
        if (content.Rebirth.People(chosen) == null)
            throw new ContentException($"rebirth.json: '{chosen}' is not a people you can be reborn as.");
        return chosen;
    }

    /// <summary>Is this cast entry the player: the otherworlder who starts in camp (cast.json has exactly one otherworlder).</summary>
    public static bool IsYou(CastDef cd) => cd.Otherworlder && cd.StartsInCamp;

    /// <summary>
    /// Sets your Truths in the run memory as you enter the world: your people, your gift (chosen, else rolled) and
    /// the dawn your soul's span ends, 40–50 years from arrival whatever the body (the founder's answer, ADR-0026 #3).
    /// </summary>
    public static void Begin(World w, Character you, RebirthChoice? choice)
    {
        ContentDb content = w.Content;
        FateBook fate = w.Fate;
        fate.PeopleId = you.SpeciesId;
        if (choice?.Gift is { } gift)
        {
            fate.GiftId = gift is "" or RebirthChoice.NoGift ? "" : content.GiftById(gift).Id;
        }
        else fate.GiftId = GiftOf(content, w.Seed, you.CastKey, you.SpeciesId);
        fate.ChosenPeople = choice?.People != null;
        fate.ChosenGift = choice?.Gift != null;
        fate.SoulEndTick = (long)SoulSpanDays(content, w.Seed, you.CastKey) * content.World.TicksPerDay;
    }

    /// <summary>How many days your soul lasts from arrival: within rebirth.json's soulSpanYears, with <see cref="TruthId.SoulSpan"/>.</summary>
    public static int SoulSpanDays(ContentDb content, ulong seed, string castKey)
    {
        int dpy = content.World.DaysPerSeason * content.World.SeasonsPerYear;
        int[] span = content.Rebirth.SoulSpanYears;
        return new TruthRng { WorldSeed = seed }.Range(TruthId.SoulSpan, Fnv1a.Hash64(castKey), 0, span[0] * dpy, span[1] * dpy + 1);
    }

    /// <summary>
    /// Sets when someone was born and when old age takes them, from Truths keyed by their key: their age when the
    /// world begins within their people's adultAtYears, and their body's lifespan within lifespanYears (none for
    /// the ageless). A people without age fields ages as a human.
    /// </summary>
    public static void SetAges(World w, Character c, string speciesId, string key)
    {
        WorldDef def = w.Content.World;
        SpeciesDef sp = w.Content.AgesAs(speciesId);
        int dpy = def.DaysPerSeason * def.SeasonsPerYear;
        ulong k = Fnv1a.Hash64(key);
        int adultDays = w.Rng.Truths.Range(TruthId.AdultAge, k, 0, sp.AdultAtYears[0] * dpy, (sp.AdultAtYears[1] + 1) * dpy);
        c.BirthTick = -(long)adultDays * def.TicksPerDay;
        c.OldAgeTick = 0;
        if (sp.Ageless) return;
        int lifeDays = w.Rng.Truths.Range(TruthId.Lifespan, k, 0, sp.LifespanYears[0] * dpy, (sp.LifespanYears[1] + 1) * dpy);
        c.OldAgeTick = c.BirthTick + (long)lifeDays * def.TicksPerDay;
    }

    /// <summary>Someone's age in whole years at a tick.</summary>
    public static int AgeYears(World w, Character c, long tick) => (int)((tick - c.BirthTick) / w.Content.World.TicksPerYear);
}
