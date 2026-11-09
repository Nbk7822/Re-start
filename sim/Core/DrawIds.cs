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
