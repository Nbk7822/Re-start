using Return.Sim.Core;

namespace Return.Sim;

/// <summary>Why a plan is not being built right now (ADR-0027). Snapshots store the name, so values are only appended.</summary>
public enum BlockedReason
{
    None,
    /// <summary>A roof section waits until every tile is close enough to a standing wall or door.</summary>
    WaitingForWalls,
    /// <summary>A builder refused to finish a wall that would shut people in; nobody tries again until the map changes.</summary>
    WouldShutIn,
    /// <summary>Someone (or a body) stands where a wall or door would go.</summary>
    InTheWay,
}

/// <summary>What a job is aimed at, beside its TargetId (ADR-0027). Snapshots store the name, so values are only appended.</summary>
public enum TargetKind
{
    None,
    Blueprint,
    Roof,
    Bed,
    Structure,
}

/// <summary>A built thing, named by its kind and id: what taking down is aimed at.</summary>
public readonly record struct BuiltRef(TargetKind Kind, int Id);

/// <summary>
/// A plan on the grid: one piece of what the player marked out (one tile, or one roof section of up to
/// 4×4). Pieces of one gesture share <see cref="PlanEvent"/>. Materials delivered to it are committed:
/// they have left the stock and lie on the site. Progress is kept when a builder is interrupted, so
/// anyone can carry on. When it is finished it becomes the thing it planned and keeps its id.
/// </summary>
public sealed class Blueprint
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public Rect Area { get; set; }

    /// <summary>The event that placed it (shared by every piece of one plan).</summary>
    public long PlanEvent { get; set; }

    /// <summary>Material lying on the site, committed to this plan; never more than it costs.</summary>
    public int Delivered { get; set; }

    /// <summary>Work ticks done so far.</summary>
    public int Progress { get; set; }

    /// <summary>Everyone who delivered material to it or worked on it, in the order they first did.</summary>
    public List<int> Contributors { get; set; } = new();

    /// <summary>Those who worked on building it (a subset of <see cref="Contributors"/>), in the order they first did.</summary>
    public List<int> Builders { get; set; } = new();

    /// <summary>For graves, later: who it is for. 0 = nobody in particular.</summary>
    public int ForId { get; set; }

    public BlockedReason Blocked { get; set; }

    /// <summary>The map revision at which <see cref="Blocked"/> was found; a would-shut-in plan is tried again once the map has changed.</summary>
    public int BlockedRevision { get; set; }

    /// <summary>The event in which someone took the last of the material for this plan and passed others over; 0 if none.</summary>
    public long PassedOverEvent { get; set; }
}

/// <summary>
/// A built wall, door (and later grave): the kinds the old camp has no record for. A wall or door writes
/// through to the terrain (Wall or Door), and taking it down restores <see cref="Ground"/>.
/// </summary>
public sealed class Structure
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public Point Pos { get; set; }

    /// <summary>The terrain it replaced, restored when it is taken down.</summary>
    public TerrainKind Ground { get; set; }

    /// <summary>Who built it: whoever finished it first, then anyone else who worked on it.</summary>
    public List<int> BuiltBy { get; set; } = new();

    /// <summary>For walls and doors, the event that finished their plan (0 until the whole plan is done).</summary>
    public long BuiltEvent { get; set; }
    public long PlanEvent { get; set; }

    /// <summary>For graves, later: who lies here.</summary>
    public int ForId { get; set; }

    /// <summary>The event that marked it to be taken down; 0 if it is not marked (kept once it is taken down).</summary>
    public long MarkedEvent { get; set; }

    /// <summary>
    /// Taken down: the terrain is back to <see cref="Ground"/>, and the record stays (like a roof section's) so
    /// a wall run taken down is told once, with whoever built it, and who built what is not forgotten.
    /// </summary>
    public bool Removed { get; set; }
}
