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

    // 30–39: the four peoples beyond human and vampire (ADR-0022).
    /// <summary>The work an undead came back to do (keyed by cast key).</summary>
    PurposeWork = 31,
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
}
