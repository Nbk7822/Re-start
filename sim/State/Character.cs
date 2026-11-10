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

    /// <summary>Health a wound is costing them each hour until someone tends it, and the wound's event (ADR-0030).</summary>
    public int Bleed { get; set; }
    public long BleedCause { get; set; }

    [JsonIgnore]
    public bool IsActive => Status is CharStatus.Member or CharStatus.Hostile;
}
