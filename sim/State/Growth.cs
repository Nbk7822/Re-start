namespace Return.Sim;

/// <summary>Where a trait came from (fixed decision 12, ADR-0021).</summary>
public enum TraitOriginKind
{
    /// <summary>Part of the person's nature: cast.json's signature trait, or the secondary trait drawn from Truths.</summary>
    Generated,

    /// <summary>Changed by something that happened to them; the origin names that event and when.</summary>
    Mutated,
}

/// <summary>
/// The origin of one trait. <see cref="CharacterGrowth.TraitOrigins"/> runs parallel to
/// <see cref="Character.Traits"/>: the same length, the same order, the same ids. Only
/// <see cref="Systems.Growth.ChangeTrait"/> changes a trait after creation, and it keeps the two in step.
/// </summary>
public sealed class TraitOrigin
{
    public string TraitId { get; set; } = "";
    public TraitOriginKind Kind { get; set; }

    /// <summary>The event that changed the trait (an incident or what followed one); 0 for a generated trait.</summary>
    public long CauseEvent { get; set; }

    /// <summary>When the trait changed; 0 for a generated trait.</summary>
    public long Tick { get; set; }
}

/// <summary>What a want is about.</summary>
public enum WantTargetKind
{
    None,
    Person,
    Place,
    Item,
}

/// <summary>Whether a want is still held, and if not, how it ended. Set only by the arc's ending stages.</summary>
public enum WantStatus
{
    Active,
    Fulfilled,
    Failed,
    Abandoned,
}

/// <summary>
/// The shape of a person's arc toward their want: want, obstacle, turning point, resolution, or an
/// ending without it (failed, abandoned). Values are serialised by name; the order an arc moves in is
/// spelled out in <see cref="Systems.Growth.CanMove"/>, not taken from the values.
/// </summary>
public enum ArcStage
{
    Want,
    Obstacle,
    TurningPoint,
    Resolution,
    Failed,
    Abandoned,
}

/// <summary>
/// What a person wants: a type from wants.json, what it is about, and whether it is still held
/// (the founder's want{type, target, state}).
/// </summary>
public sealed class Want
{
    /// <summary>A wants.json id.</summary>
    public string TypeId { get; set; } = "";

    public WantTargetKind TargetKind { get; set; }

    /// <summary>The character the want is about, when <see cref="TargetKind"/> is Person; otherwise 0.</summary>
    public int TargetId { get; set; }

    /// <summary>The place or item id the want is about, when <see cref="TargetKind"/> is Place or Item; otherwise "".</summary>
    public string TargetRef { get; set; } = "";

    public WantStatus Status { get; set; }

    public long FormedTick { get; set; }

    /// <summary>The story beat that logged this want; 0 until it is logged (the camp starters' wants form at creation and are logged on the first tick).</summary>
    public long FormedEvent { get; set; }
}

/// <summary>A person's arc toward their want (the founder's arc{stage, stalledSinceTick}).</summary>
public sealed class Arc
{
    public ArcStage Stage { get; set; }

    /// <summary>When the arc last moved (or began); the director will look for arcs stalled since long ago (M1).</summary>
    public long StalledSinceTick { get; set; }

    /// <summary>The arc's latest beat (the want forming, or the last stage change); the next stage cites it.</summary>
    public long LastEvent { get; set; }
}

/// <summary>
/// A person's growth (fixed decision 12): where each trait came from, what they want, and how far
/// their arc has come. M0 holds the data and logs beats; nothing advances arcs or mutates traits yet
/// (BIBLE.md 5.3, ADR-0021).
/// </summary>
public sealed class CharacterGrowth
{
    /// <summary>One entry per trait, parallel to <see cref="Character.Traits"/>.</summary>
    public List<TraitOrigin> TraitOrigins { get; set; } = new();

    /// <summary>Null until the person enters the world (camp starters at creation, newcomers on arrival).</summary>
    public Want? Want { get; set; }

    /// <summary>Null exactly when <see cref="Want"/> is.</summary>
    public Arc? Arc { get; set; }
}
