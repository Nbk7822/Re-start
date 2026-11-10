using Return.Sim.Core;

namespace Return.Sim;

public enum JobKind
{
    Idle,
    Wander,
    Eat,
    Sleep,
    SeekCover,
    Forage,
    GatherTimber,
    Haul,
    RepairRoof,
    KeepWatch,
    Care,
    Rescue,
    RequestBlood,
    GiveBlood,
    KeepPromise,
    Socialize,
    Fight,
    Flee,
    Leave,
    Arrive,
    Raid,
    /// <summary>At a festival by the hearth (ADR-0023).</summary>
    Feast,
    /// <summary>Bringing a hungry creditor food to settle a debt of honour (ADR-0023).</summary>
    RepayDebt,

    // The four peoples (ADR-0022).
    /// <summary>A dryad standing by their tree (or another living one) to draw on the forest.</summary>
    TendTree,
    /// <summary>A djinn going to someone to offer the camp a bargain.</summary>
    OfferBargain,

    /// <summary>A watcher walking to wake the next sleeper for their turn on watch (ADR-0029).</summary>
    WakeForWatch,
}

/// <summary>What a character is doing right now. Stage and Progress are job-specific counters.</summary>
public sealed class Job
{
    public JobKind Kind { get; set; }
    public int TargetId { get; set; }
    public Point Target { get; set; } = Point.None;
    public int Stage { get; set; }
    public int Progress { get; set; }
    public long StartedTick { get; set; }
    public long CauseEvent { get; set; }
}

/// <summary>Player-set intention for a work type (content work id); 1 = first, 4 = last, 0 = never.</summary>
public sealed class WorkPriority
{
    public string WorkId { get; set; } = "";
    public int Priority { get; set; }
}
