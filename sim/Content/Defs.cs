namespace Return.Sim.Content;

// Definitions loaded from /content. Read-only after load; never part of world state.
// Every player-facing string is a loc key (ADR-0007).

public sealed class DirectorDef
{
    public int PressurePerHour { get; init; }
    public int MajorCost { get; init; }
    public int MinHoursBetweenMajors { get; init; }
}

public sealed class WorldDef
{
    public int TicksPerHour { get; init; }
    public int HoursPerDay { get; init; }
    public int DaysPerSeason { get; init; }
    public int SeasonsPerYear { get; init; }
    public int DawnHour { get; init; }
    public int DuskHour { get; init; }
    public int TicksPerSecond { get; init; }
    public int[] SpeedMultipliers { get; init; } = Array.Empty<int>();
    public string[] SeasonKeys { get; init; } = Array.Empty<string>();
    public int MapWidth { get; init; }
    public int MapHeight { get; init; }
    public int SocialEveryTicks { get; init; }
    public int StartBerries { get; init; }
    public int StartTimber { get; init; }
    public int PromiseDueHours { get; init; }
    public DirectorDef Director { get; init; } = new();

    public int TicksPerDay => TicksPerHour * HoursPerDay;
    public int TicksPerSeason => TicksPerDay * DaysPerSeason;
    public long TicksPerYear => (long)TicksPerSeason * SeasonsPerYear;
}

public sealed class SpeciesDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public string FoodSource { get; init; } = "";
    public int FoodDecayPerHour { get; init; }
    public int RestDecayPerHour { get; init; }
    public int SocialDecayPerHour { get; init; }
    public int SleepRestorePerHour { get; init; }
    public int DaylightBurnPerHour { get; init; }
    public int TicksPerTile { get; init; }
    public bool CanDonateBlood { get; init; }
    public int BloodPerFeed { get; init; }
    public int DonorHealthCost { get; init; }
    public int DonorRestCost { get; init; }

    /// <summary>False for those who never tire or sleep (the undead): rest stays full (ADR-0022).</summary>
    public bool Tires { get; init; } = true;

    /// <summary>Needs from needs.json that this species' nature adds to food, rest and company (ADR-0022).</summary>
    public List<string> Needs { get; init; } = new();

    /// <summary>For this many days after they arrive, the living take less warmth from talking with them and feel uneasy (the undead).</summary>
    public int UnsettlesForDays { get; init; }

    /// <summary>How their arrival reads (a loc key with {a}); empty = the wanderer's "hungry and alone".</summary>
    public string ArrivalKey { get; init; } = "";

    public bool DrinksBlood => FoodSource == "blood";

    /// <summary>foodSource "none": they eat nothing from the stores and never go hungry (ADR-0022).</summary>
    public bool EatsNothing => FoodSource == NoFood;

    /// <summary>They eat an item from the stockpile (berries in M0), so rationing, spoilage and theft touch them.</summary>
    public bool EatsItem => !DrinksBlood && !EatsNothing;

    public const string NoFood = "none";
}

public sealed class WorkBias
{
    public string Work { get; init; } = "";
    public int Add { get; init; }
}

public sealed class TraitDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public string DescKey { get; init; } = "";
    public int CareUrgency { get; init; }
    public int PromisePriority { get; init; }
    public int InsultPermilleAdd { get; init; }
    public int InsultOpinionScalePct { get; init; } = 100;
    public int ConsentBias { get; init; }
    public bool FightsThieves { get; init; }
    public int FleeHealthBelow { get; init; }
    public int StealFoodBelow { get; init; }
    public bool OffersBlood { get; init; }
    public bool SharesFood { get; init; }
    public int RestThresholdAdd { get; init; }
    public int RiskyWorkHealthBelow { get; init; }
    public int PriorityRespectPct { get; init; } = 100;
    public List<WorkBias> WorkBias { get; init; } = new();
}

public sealed class ItemDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public int Nutrition { get; init; }
    public int SpoilDays { get; init; }
    public int StackLimit { get; init; } = 40;
}

public sealed class WorkDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public int DurationTicks { get; init; }
    public int RestCostPerHour { get; init; }
    public int AccidentPermille { get; init; }
    public int Yield { get; init; }
    public bool Risky { get; init; }
    public int TimberCost { get; init; }
    public int BerryCost { get; init; }
}

public sealed class RolePriority
{
    public string Work { get; init; } = "";
    public int Priority { get; init; }
}

public sealed class RoleDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public List<RolePriority> Priorities { get; init; } = new();
}

public sealed class ThoughtDef
{
    public string Id { get; init; } = "";
    public string TextKey { get; init; } = "";
    public int Mood { get; init; }
    public int Opinion { get; init; }
    public int DurationHours { get; init; }
    public string Emote { get; init; } = "none";
    public int StackLimit { get; init; } = 1;
}

public sealed class IncidentDef
{
    public string Id { get; init; } = "";
    public string TextKey { get; init; } = "";
    public string Kind { get; init; } = "";
    public Dictionary<string, int> Params { get; init; } = new();

    public int Param(string name) =>
        Params.TryGetValue(name, out int v) ? v : throw new ContentException($"incidents.json: '{Id}' has no param '{name}'.");
}

/// <summary>
/// A need that comes from a species' nature, beyond food, rest and company (ADR-0022). Thresholds,
/// thoughts, threshold events, harm and departure are shared code driven by these fields; how the
/// need is met or wounded is the behaviour named by its id (like incidents), tuned by params.
/// </summary>
public sealed class NeedDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public int Start { get; init; }
    public int DecayPerHour { get; init; }
    public int LowBelow { get; init; }
    public int CriticalBelow { get; init; }
    public int SatisfiedAt { get; init; }
    public string LowThought { get; init; } = "";
    public string CriticalThought { get; init; } = "";
    public string LowEventKey { get; init; } = "";
    public string CriticalEventKey { get; init; } = "";

    /// <summary>Health lost each hour while the need is critical; 0 = it does not harm.</summary>
    public int CriticalHealthPerHour { get; init; }

    /// <summary>The death text when that harm kills.</summary>
    public string DeathKey { get; init; } = "";

    /// <summary>Hours critical (and still low) after which they leave the camp; 0 = it never drives them away.</summary>
    public int LeavesAfterCriticalHours { get; init; }

    /// <summary>Works the need can be bound to (one is chosen per person from Truths).</summary>
    public List<string> BondWorks { get; init; } = new();

    /// <summary>Items the need can ask for (one is chosen per person from Truths).</summary>
    public List<string> BondItems { get; init; } = new();

    public Dictionary<string, int> Params { get; init; } = new();

    public int Param(string name) =>
        Params.TryGetValue(name, out int v) ? v : throw new ContentException($"needs.json: '{Id}' has no param '{name}'.");
}

public sealed class CastDef
{
    public string Key { get; init; } = "";
    public string NameKey { get; init; } = "";
    public string Species { get; init; } = "";
    public string SignatureTrait { get; init; } = "";
    public string Role { get; init; } = "";
    public bool Otherworlder { get; init; }
    public bool StartsInCamp { get; init; }
    public string Color { get; init; } = "#888888";

    /// <summary>
    /// [first, last] day (as the log numbers them, Day 1 first) of the first year within which they
    /// arrive, on a day drawn from Dice; empty = shuffled in with the other wanderers (ADR-0022).
    /// </summary>
    public int[] ArrivalDays { get; init; } = Array.Empty<int>();
}
