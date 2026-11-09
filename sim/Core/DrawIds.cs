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
}
