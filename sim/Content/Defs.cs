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

    public bool DrinksBlood => FoodSource == "blood";
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
}

/// <summary>
/// How likely a person is to want something: <see cref="Base"/> plus the add for each of their traits,
/// their species, their role and, for the player, <see cref="Otherworlder"/>. The adds sum; a total at or
/// below zero means never, so a want meant only for one species or for the otherworlder takes all its
/// weight from that one axis (ADR-0021).
/// </summary>
public sealed class WantWeights
{
    public int Base { get; init; }
    public Dictionary<string, int> Traits { get; init; } = new();
    public Dictionary<string, int> Species { get; init; } = new();
    public Dictionary<string, int> Roles { get; init; } = new();
    public int Otherworlder { get; init; }
}

/// <summary>A kind of want (fixed decision 12): what a person wants out of life, what it is about, and who tends to want it.</summary>
public sealed class WantDef
{
    /// <summary>The camp's places a want can be about: features every generated map has (WorldGen).</summary>
    public static readonly IReadOnlyList<string> Places = new[] { "shelter", "hearth", "stockpile", "watch_post", "covered_ruins" };

    public string Id { get; init; } = "";

    /// <summary>The want in plain words ("To keep the camp safe"); "&lt;key&gt;_you", when present, for the player.</summary>
    public string TextKey { get; init; } = "";

    /// <summary>The story beat logged when the want forms; "&lt;key&gt;_you_a" when the player wants it.</summary>
    public string BeatKey { get; init; } = "";

    /// <summary>"none", "person", "place" or "item".</summary>
    public string Target { get; init; } = "none";

    /// <summary>For a person want: "best_chemistry" or "worst_chemistry".</summary>
    public string Pick { get; init; } = "";

    /// <summary>For a place want: one of <see cref="Places"/>.</summary>
    public string Place { get; init; } = "";

    /// <summary>For an item want: an items.json id.</summary>
    public string Item { get; init; } = "";

    public WantWeights Weights { get; init; } = new();
}
