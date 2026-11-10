using System;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Systems;
using SimCalendar = Return.Sim.Core.Calendar;

namespace Return.Game.Logic;

/// <summary>
/// Which peoples' silhouette language someone is drawn in (BIBLE 3, ADR-0022's look). It comes from
/// their species' nature in content, never from a name: vampires drink blood; dryads need the forest,
/// djinn a bargain, the fae courtesy, the undead purpose (the need ids the sim's behaviours are named
/// by). Flags, so a hybrid that inherits two natures wears both looks (fixed decision 5).
/// </summary>
[Flags]
public enum Kin
{
    None = 0,
    Vampire = 1,
    Dryad = 2,
    Djinn = 4,
    Fae = 8,
    Undead = 16,
}

/// <summary>What a wound gives: blood, a dryad's sap, the undead's grave dust, a djinn's ash.</summary>
public enum Ichor
{
    Blood,
    Sap,
    Dust,
    Ash,
}

/// <summary>A dryad's leaves keep the season of the trees: green, turning in autumn, bare in winter.</summary>
public enum Leaves
{
    Green,
    Turning,
    Bare,
}

/// <summary>
/// The view's reading of the four peoples' natures (Godot-free, so the tests compile it): their kin,
/// what their wounds give, how their need stands, the season of a dryad's leaves and when a dryad
/// glows by her tree. Read-only over World.
/// </summary>
public static class Kinship
{
    /// <summary>A species' kin, from what it drinks and the needs of its nature.</summary>
    public static Kin Of(SpeciesDef species)
    {
        Kin kin = species.DrinksBlood ? Kin.Vampire : Kin.None;
        foreach (string need in species.Needs)
            kin |= need switch
            {
                Forest.NeedId => Kin.Dryad,
                Bargains.NeedId => Kin.Djinn,
                Courtesy.NeedId => Kin.Fae,
                Purpose.NeedId => Kin.Undead,
                _ => Kin.None,
            };
        return kin;
    }

    public static Kin Of(World w, Character c) => Of(w.Content.SpeciesById(c.SpeciesId));

    /// <summary>
    /// What wounds give: the undead dust (the dead do not bleed, whatever else they are), a djinn the
    /// ash of living flame, a dryad sap; everyone else blood.
    /// </summary>
    public static Ichor IchorOf(Kin kin) =>
        (kin & Kin.Undead) != 0 ? Ichor.Dust
        : (kin & Kin.Djinn) != 0 ? Ichor.Ash
        : (kin & Kin.Dryad) != 0 ? Ichor.Sap
        : Ichor.Blood;

    public static Ichor IchorOf(World w, Character c) => IchorOf(Of(w, c));

    /// <summary>The colour a wound soaks into clothes: dark red blood, amber sap, pale grave dust, charcoal ash.</summary>
    public static Rgb StainOf(Ichor ichor) => ichor switch
    {
        Ichor.Sap => new Rgb(0.40f, 0.25f, 0.07f),
        Ichor.Dust => new Rgb(0.56f, 0.54f, 0.49f),
        Ichor.Ash => new Rgb(0.13f, 0.12f, 0.11f),
        _ => new Rgb(0.36f, 0.04f, 0.04f),
    };

    /// <summary>How the worst need of someone's nature stands: 0 met, 1 low, 2 critical (0 for those with none).</summary>
    public static int NatureStage(Character c)
    {
        int stage = 0;
        foreach (NatureNeed n in c.NatureNeeds) stage = Math.Max(stage, Math.Clamp(n.Stage, 0, 2));
        return stage;
    }

    /// <summary>The leaves of the season: bare in the last season of the year (winter), turning in the one before it.</summary>
    public static Leaves LeavesAt(long tick, WorldDef def)
    {
        if (def.SeasonsPerYear <= 0 || def.DaysPerSeason <= 0) return Leaves.Green;
        int season = SimCalendar.Season(tick, def);
        return season == def.SeasonsPerYear - 1 ? Leaves.Bare
            : def.SeasonsPerYear > 2 && season == def.SeasonsPerYear - 2 ? Leaves.Turning
            : Leaves.Green;
    }

    /// <summary>
    /// How strongly a dryad glows, 0..1: only at night (<paramref name="night"/> 0 day … 1 deep night),
    /// only beside the living tree she is bound to (full within the forest need's treeRange, fading over
    /// two tiles beyond it), never once it is felled, never for anyone but a member with that bond.
    /// </summary>
    public static float GroveGlow(World w, Character c, float night)
    {
        if (night <= 0.01f || c.Status != CharStatus.Member || NatureNeeds.Get(c, Forest.NeedId) is not { BondId: > 0 } n) return 0f;
        if (Forest.TreeById(w, n.BondId) is not { Felled: false } tree) return 0f;
        int range = w.Content.NeedById(Forest.NeedId).Param("treeRange");
        int dist = c.Pos.ChebyshevTo(tree.Pos);
        float near = dist <= range ? 1f : Math.Clamp(1f - (dist - range) / 2f, 0f, 1f);
        return Math.Clamp(night, 0f, 1f) * near;
    }
}
