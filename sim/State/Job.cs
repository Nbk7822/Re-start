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

    // The player (ADR-0030).
    /// <summary>You, taking a meal from your satchel to someone starving.</summary>
    GiftShare,
    // The Build menu (ADR-0027).
    /// <summary>Fetching what a plan lacks, then building it.</summary>
    Build,
    /// <summary>Carrying material from the stock to a plan short of it (under haul).</summary>
    Deliver,
    /// <summary>Taking down something marked to be taken down (under build).</summary>
    TakeDown,
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

    /// <summary>What TargetId names, for jobs aimed at a plan or a built thing (ADR-0027); None for every other job.</summary>
    public TargetKind TargetKind { get; set; }
}

/// <summary>Player-set intention for a work type (content work id); 1 = first, 4 = last, 0 = never.</summary>
public sealed class WorkPriority
{
    public string WorkId { get; set; } = "";
    public int Priority { get; set; }
}
