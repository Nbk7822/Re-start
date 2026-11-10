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

    /// <summary>The camp's watch and the tuning of hours and overwork (ADR-0029).</summary>
    public ScheduleDef Schedule { get; init; } = new();

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

    /// <summary>Work their nature will not do, whatever their priorities say (a dryad never fells a tree).</summary>
    public List<string> RefusesWork { get; init; } = new();

    /// <summary>They cannot lie (the fae): they say hard truths where others insult, and what they owe they have sworn to repay, first.</summary>
    public bool CannotLie { get; init; }

    /// <summary>Grudges they hold fade a point only every this many days: slights are remembered long (the fae).</summary>
    public int GrudgeDecayEveryDays { get; init; } = 1;

    /// <summary>[min, max] age in years when the world begins (a Truth per person); empty = ages as a human (ADR-0030).</summary>
    public int[] AdultAtYears { get; init; } = Array.Empty<int>();

    /// <summary>[min, max] years a body lasts (a Truth per person); empty with <see cref="Ageless"/> false = ages as a human.</summary>
    public int[] LifespanYears { get; init; } = Array.Empty<int>();

    /// <summary>The body never ages; only the soul's span ends their life (vampires, the undead, the fae).</summary>
    public bool Ageless { get; init; }

    /// <summary>Whether this species sets its own ages (otherwise it ages as a human).</summary>
    public bool HasAges => AdultAtYears.Length == 2 && (Ageless || LifespanYears.Length == 2);
    /// <summary>The kind of day they keep (rhythms.json); "" for the default: diurnal if they tire, sleepless if not (ADR-0029).</summary>
    public string Rhythm { get; init; } = "";

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

    /// <summary>Skips risky work (the watch among it) below this rest: the Anxious (ADR-0029).</summary>
    public int RiskyWorkRestBelow { get; init; }
    public int PriorityRespectPct { get; init; } = 100;
    public List<WorkBias> WorkBias { get; init; } = new();

    /// <summary>Will not strike a bargain the camp could not pay (Proud; ADR-0022).</summary>
    public bool RefusesUnsafeBargain { get; init; }
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

/// <summary>A range of hours of one kind in a kind of day: from inclusive, to exclusive, wrapping past midnight when to is less than from.</summary>
public sealed class RhythmRange
{
    public int From { get; init; }
    public int To { get; init; }

    /// <summary>sleep, work or free (a DayBlock by its content id).</summary>
    public string Block { get; init; } = "";
}

/// <summary>
/// A kind of day (ADR-0029): the camp day a species keeps by default, as ranges of sleep, work and free time
/// that cover every hour once. Whether someone sleeps at all is their species' tires, never the kind of day.
/// </summary>
public sealed class RhythmDef
{
    public string Id { get; init; } = "";

    /// <summary>The day's title in the Schedule list ("The camp's day").</summary>
    public string NameKey { get; init; } = "";

    /// <summary>The day named mid-sentence ("the camp's day"), for the Chronicle.</summary>
    public string CampDayKey { get; init; } = "";

    public List<RhythmRange> Day { get; init; } = new();
}

/// <summary>Hours of a watch: from inclusive, to exclusive, wrapping past midnight; equal means no watch.</summary>
public sealed class WatchRangeDef
{
    public int From { get; init; }
    public int To { get; init; }
}

/// <summary>world.json's schedule block (ADR-0029): the default watch, the turn and wake rules, and the tuning of overwork.</summary>
public sealed class ScheduleDef
{
    public WatchRangeDef Watch { get; init; } = new();
    public int WatchTurnHours { get; init; } = 4;
    public int WakeTries { get; init; } = 2;
    public int OverworkHours { get; init; } = 12;
    public int WornOutDutyHours { get; init; } = 8;
    public int NoFreeTimeAwakeHours { get; init; } = 14;
    public int TiredAccidentPct { get; init; } = 200;
    public int AsleepOnWatchRestBelow { get; init; } = 60;
    public int FreeTimeNapBelow { get; init; } = 400;
}
