using System.Text.Json.Serialization;
using Return.Sim.Core;

namespace Return.Sim;

public enum CharStatus
{
    OffMap,
    Member,
    Left,
    Dead,
    Hostile,
}

public enum Facing
{
    S,
    N,
    E,
    W,
}

/// <summary>
/// A person in the world. Needs and health are 0..1000 (1000 = fully satisfied); mood is 0..100.
/// Event-id fields (Last*Event) hold the most recent event that explains the matching need, so
/// later events can cite it as a cause; 0 means none.
/// </summary>
public sealed class Character
{
    public int Id { get; set; }
    public string CastKey { get; set; } = "";
    public string NameKey { get; set; } = "";
    public string SpeciesId { get; set; } = "";
    public string RoleId { get; set; } = "";
    public List<string> Traits { get; set; } = new();
    public bool Otherworlder { get; set; }
    public bool Named { get; set; }
    public CharStatus Status { get; set; }
    public long ArrivedTick { get; set; }

    public Point Pos { get; set; } = Point.None;
    public List<Point> Path { get; set; } = new();
    public int MoveTicks { get; set; }
    public Facing Facing { get; set; }

    public int Food { get; set; } = 750;
    public int Rest { get; set; } = 800;
    public int Social { get; set; } = 700;
    public int Health { get; set; } = 1000;
    public bool Downed { get; set; }
    public bool Asleep { get; set; }
    public int Mood { get; set; } = 50;
    public int HungerStage { get; set; }
    public long StarvingSince { get; set; }
    public bool SkippedRation { get; set; }

    public List<Thought> Thoughts { get; set; } = new();
    public List<Opinion> Opinions { get; set; } = new();
    public Job? Job { get; set; }
    public Carry? Carrying { get; set; }
    public List<WorkPriority> Priorities { get; set; } = new();

    public long LastFoodEvent { get; set; }
    public long LastRestEvent { get; set; }
    public long LastHealthEvent { get; set; }

    /// <summary>Trait origins, want and arc (fixed decision 12, ADR-0021).</summary>
    public CharacterGrowth Growth { get; set; } = new();
    // Culture and faith (ADR-0023). Culture, faith and devotion are Truths keyed by cast key; the
    // otherworlder has none ("", "", 0). Devotion is 0..100 and only ever shown as a word.
    public string CultureId { get; set; } = "";
    public string FaithId { get; set; } = "";
    public int Devotion { get; set; }
    /// <summary>Customs of other cultures this person learned by breaching them; ordinal order.</summary>
    public List<string> LearnedCustoms { get; set; } = new();
    /// <summary>Ids of everyone this person has spoken with; ascending.</summary>
    public List<int> Met { get; set; } = new();
    /// <summary>Needs that come from this person's nature (their species), beyond food, rest and company (ADR-0022).</summary>
    public List<NatureNeed> NatureNeeds { get; set; } = new();

    /// <summary>When they were born (negative for anyone older than the world), from a Truth; and the dawn after which old age takes them, 0 for the ageless (ADR-0030).</summary>
    public long BirthTick { get; set; }
    public long OldAgeTick { get; set; }

    /// <summary>Health a wound is costing them each hour until someone tends it, the wound's event, and the line a death of it reads (ADR-0030).</summary>
    public int Bleed { get; set; }
    public long BleedCause { get; set; }
    public string BleedDeath { get; set; } = "";
    /// <summary>The calendar day they last said they kept away from a meeting place (once a day at most; ADR-0028); −1 for never.</summary>
    public int LastKeptAwayDay { get; set; } = -1;

    // The home area's night memory (zones step 3, ADR-0028). A night belongs to the day its dusk fell on, so 02:00 on
    // day 21 is night 20; these hold that number, int.MinValue for never (the small hours of day 0 are night −1, so −1
    // cannot mean never).
    /// <summary>The night they last rolled whether to go their own way under the home area.</summary>
    public int NightRollDay { get; set; } = int.MinValue;
    /// <summary>Tonight's answer, valid only while NightRollDay is tonight: the reason they go their own way (Compliance's), or "" if they keep the rule.</summary>
    public string DefiesTonight { get; set; } = "";
    /// <summary>The night they last went out after dark (a breach is said once a night).</summary>
    public int LastBreachDay { get; set; } = int.MinValue;
    /// <summary>The night they last stayed in hungry as asked (said once a night).</summary>
    public int LastStayedInDay { get; set; } = int.MinValue;
    /// <summary>The night they last felt kept in (an Independent's kept_in, once a night).</summary>
    public int LastKeptInDay { get; set; } = int.MinValue;

    // The Schedule (ADR-0029).
    /// <summary>Hours this person keeps instead of their kind's camp day, in hour order; empty means the camp's day.</summary>
    public List<HourBlock> OwnHours { get; set; } = new();
    /// <summary>The event that last set their own hours (0 if never).</summary>
    public long OwnHoursEvent { get; set; }
    /// <summary>What they mostly did in the most recent occurrence of each clock hour (24 entries, by hour).</summary>
    public List<LivedKind> Lived { get; set; } = new();
    /// <summary>Samples of the hour under way.</summary>
    public HourTally HourTally { get; set; } = new();
    /// <summary>When they last came off the watch (for turns kept fairly); 0 if never.</summary>
    public long LastWatchEndTick { get; set; }
    /// <summary>The event that began the duty that tires them now (a watch kept, work late, a longer working day); 0 if none.</summary>
    public long LastDutyEvent { get; set; }
    /// <summary>Their "worn out from long hours" event while they are worn out; 0 once rested again.</summary>
    public long WornOutEvent { get; set; }

    [JsonIgnore]
    public bool IsActive => Status is CharStatus.Member or CharStatus.Hostile;
}
