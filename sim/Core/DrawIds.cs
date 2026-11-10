namespace Return.Sim.Core;

/// <summary>
/// Purposes for Truths queries. Each value feeds the Truths hash, so changing or reusing a value
/// changes every world that uses it: never renumber, never reuse; only append (ADR-0004).
/// </summary>
public enum TruthId : ushort
{
    Terrain = 1,
    RiverPath = 2,
    RockPlacement = 3,
    TreePlacement = 4,
    BushPlacement = 5,
    CampPlacement = 6,
    SecondaryTrait = 7,
    PairChemistry = 8,

    // 20-29: character growth (fixed decision 12, ADR-0021).
    /// <summary>The want a person is drawn to by nature, keyed by cast key.</summary>
    InitialWant = 20,
    /// <summary>Breaks a tie between equally fitting people when a want is about someone, keyed by cast key.</summary>
    WantTarget = 21,
    // 30–39: the four peoples beyond human and vampire (ADR-0022).
    /// <summary>The tree a dryad is bound to (keyed by cast key).</summary>
    BoundTree = 30,
    /// <summary>The work an undead came back to do (keyed by cast key).</summary>
    PurposeWork = 31,
    /// <summary>The service a djinn offers in its bargains (keyed by cast key).</summary>
    BargainService = 32,
    /// <summary>The price a djinn asks for that service (keyed by cast key).</summary>
    BargainPrice = 33,
    // Culture and faith (ADR-0023; values 40-49 are this track's). The index argument names the fact.
    /// <summary>Per culture template: 0 name, 1 main faith, 2 + k the k-th custom drawn from its pool.</summary>
    CultureNature = 40,
    /// <summary>Per faith template: 0 name, 1 + k the k-th custom drawn from its pool.</summary>
    FaithNature = 41,
    /// <summary>Per god slot: 0 domain, 1 name, 2 epithet, 3 wrath, 4 grace.</summary>
    GodNature = 42,
    /// <summary>Per person: 0 culture, 1 whether they follow another faith, 2 which, 3 devotion.</summary>
    PersonBelonging = 43,
    SacredTree = 44,
    FestivalDate = 45,
    // 70–79: the Schedule (ADR-0029). None drawn yet; a fixed body clock would take 70.
}

/// <summary>Tags for Dice draws, used for diffing divergent runs. Append only.</summary>
public enum DiceId : ushort
{
    ArrivalDay = 1,
    ArrivalHour = 2,
    SocialInitiate = 3,
    SocialInsult = 4,
    SocialDeepTalk = 5,
    WanderTarget = 6,
    WanderWait = 7,
    AccidentRoll = 8,
    ArrivalEdge = 9,
    ArrivalFood = 10,
    RainStart = 11,
    RainLength = 12,
    WindfallRoll = 13,
    WindfallKind = 14,
    WindfallRow = 15,
    SpoilRoll = 16,
    SpoilLoss = 17,
    ThiefRoll = 18,
    ThiefEdge = 19,
    AccidentDamage = 20,
    CombatHit = 21,
    CombatDamage = 22,
    ArrivalOrder = 23,

    // 120–139: the four peoples beyond human and vampire (ADR-0022).
    /// <summary>The day, within their window, someone who comes in their own season arrives (cast.json arrivalDays).</summary>
    PeopleArrivalDay = 120,
    PeopleArrivalHour = 121,
    /// <summary>How badly a dryad is hurt when their tree is felled.</summary>
    TreeWound = 122,
    // Culture and faith (ADR-0023; values 140-159 are this track's). All draw from DiceSystem.Culture.
    FauxPas = 140,
    GuestOffer = 141,
    OmenRead = 142,

    // 180–199: zones (ADR-0028). Compliance rolls draw from DiceSystem.Work, each with its rule's own tag.
    /// <summary>Whether someone goes their own way tonight under the home area (zones step 3), through Compliance.Defies. Nothing draws it yet.</summary>
    ZoneDefy = 180,

    // 200–219: the Schedule (ADR-0029). Compliance rolls on DiceSystem.Work, each with its own tag.
    /// <summary>Whether someone goes their own way over a change to their day (Compliance.Defies), once per applied command; never the player.</summary>
    DayChangeDefy = 200,
    /// <summary>Whether someone woken for the watch goes their own way (Compliance.Defies), only when no reason of body or grudge applies.</summary>
    WatchDefy = 201,
}

/// <summary>One Dice substream per system. Values must stay contiguous from 0 (they index the stream list).</summary>
public enum DiceSystem : byte
{
    World = 0,
    Needs = 1,
    Mood = 2,
    Social = 3,
    Work = 4,
    Travel = 5,
    Incident = 6,
    Combat = 7,
    /// <summary>Trait mutation and evolution rolls (fixed decision 12). Reserved now so the values stay contiguous; M0 draws nothing from it.</summary>
    Growth = 8,
    Culture = 9,
}
