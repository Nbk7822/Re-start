using System.Collections.Generic;
using System.Linq;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Systems;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game.Logic;

/// <summary>
/// Player choices made while time is paused. A later choice for the same thing (the ration policy;
/// one person's priority for one kind of work; marking or unmarking one built thing to be taken down)
/// replaces the earlier one, and when time moves the drafts go out together as commands, skipping any
/// that would change nothing. A plan is its own slot, with a local draft id, so its pieces can be called
/// off one by one before time moves. So changing your mind while paused leaves no trace in the world or
/// its history. Godot-free so the tests compile it.
/// </summary>
public sealed class OrderDrafts
{
    private readonly List<(int Id, Command Command)> _drafts = new();
    private int _nextId = 1;

    public int Count => _drafts.Count;

    /// <summary>
    /// Keeps a choice as a draft and returns its draft id. A plan (PlaceBlueprint) is always a new draft. A mark
    /// or unmark replaces any earlier draft for the same thing; a call-off adds only plans not already being
    /// called off; any other choice replaces an earlier draft for the same thing.
    /// </summary>
    public int Add(Command command)
    {
        switch (command)
        {
            case PlaceBlueprint:
                break;
            case MarkTakeDown mark:
                for (int i = 0; i < _drafts.Count; i++)
                {
                    if (_drafts[i].Command is not MarkTakeDown earlier) continue;
                    var left = new List<BuiltRef>();
                    foreach (BuiltRef t in earlier.Targets)
                        if (!Contains(mark.Targets, t)) left.Add(t);
                    if (left.Count == earlier.Targets.Count) continue;
                    if (left.Count == 0) _drafts.RemoveAt(i--);
                    else _drafts[i] = (_drafts[i].Id, earlier with { Targets = left });
                }
                break;
            case CancelBlueprints cancel:
            {
                var fresh = new List<int>();
                foreach (int id in cancel.BlueprintIds)
                    if (!CallingOff(id) && !fresh.Contains(id)) fresh.Add(id);
                if (fresh.Count == 0) return 0;
                command = cancel with { BlueprintIds = fresh };
                break;
            }
            default:
                _drafts.RemoveAll(d => SameSlot(d.Command, command));
                break;
        }
        int draftId = _nextId++;
        _drafts.Add((draftId, command));
        return draftId;
    }

    /// <summary>The drafted ration policy, if any.</summary>
    public RationPolicy? Policy
    {
        get
        {
            RationPolicy? policy = null;
            foreach ((int _, Command d) in _drafts)
                if (d is SetRationPolicy p) policy = p.Policy;
            return policy;
        }
    }

    /// <summary>The drafted priority for one person and one kind of work, if any.</summary>
    public int? Priority(int characterId, string workId)
    {
        int? priority = null;
        foreach ((int _, Command d) in _drafts)
            if (d is SetWorkPriority p && p.CharacterId == characterId && p.WorkId == workId) priority = p.Priority;
        return priority;
    }

    /// <summary>The drafted plans, in the order they were made, each with its draft id.</summary>
    public IReadOnlyList<(int Id, PlaceBlueprint Plan)> Placements
    {
        get
        {
            var plans = new List<(int, PlaceBlueprint)>();
            foreach ((int id, Command d) in _drafts)
                if (d is PlaceBlueprint p) plans.Add((id, p));
            return plans;
        }
    }

    /// <summary>A drafted mark (true) or unmark (false) for one built thing, if any.</summary>
    public bool? Marked(BuiltRef target)
    {
        bool? marked = null;
        foreach ((int _, Command d) in _drafts)
            if (d is MarkTakeDown m && Contains(m.Targets, target)) marked = m.Marked;
        return marked;
    }

    /// <summary>Is a placed plan drafted to be called off?</summary>
    public bool CallingOff(int blueprintId)
    {
        foreach ((int _, Command d) in _drafts)
            if (d is CancelBlueprints c && c.BlueprintIds.Contains(blueprintId)) return true;
        return false;
    }

    /// <summary>Calls off one piece of a drafted plan: the piece is dropped, and the draft with it once no piece is left. True if there was such a piece.</summary>
    public bool CallOff(int draftId, SimRect piece)
    {
        for (int i = 0; i < _drafts.Count; i++)
        {
            if (_drafts[i].Id != draftId || _drafts[i].Command is not PlaceBlueprint plan) continue;
            var left = new List<SimRect>();
            foreach (SimRect r in plan.Pieces)
                if (r != piece) left.Add(r);
            if (left.Count == plan.Pieces.Count) return false;
            if (left.Count == 0) _drafts.RemoveAt(i);
            else _drafts[i] = (draftId, plan with { Pieces = left });
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sends every draft that still changes something, stamped with the current tick, and clears them: a plan
    /// that still has a tile that can take it (the sim checks every piece again when it applies), only the marks
    /// and unmarks that change something, and call-offs of plans that are still there.
    /// </summary>
    public void Flush(Simulation sim)
    {
        World w = sim.World;
        foreach ((int _, Command d) in _drafts)
        {
            Command? send = d switch
            {
                SetRationPolicy p => p.Policy != w.Policy ? p : null,
                SetWorkPriority p => w.Find(p.CharacterId) is { Status: CharStatus.Member } c && JobPicker.PriorityOf(c, p.WorkId) != p.Priority ? p : null,
                PlaceBlueprint p => BuildTool.OkCount(StateApi.CanPlace(w, p.BuildingId, p.Pieces, Queued(w), QueuedCallOffs(w))) > 0 ? p : null,
                MarkTakeDown m => Changing(w, m) is { Count: > 0 } targets ? m with { Targets = targets } : null,
                CancelBlueprints c => StillThere(w, c.BlueprintIds) is { Count: > 0 } ids ? c with { BlueprintIds = ids } : null,
                _ => d,
            };
            if (send != null) sim.Enqueue(send with { Tick = w.Tick });
        }
        _drafts.Clear();
    }

    /// <summary>
    /// The pieces asked for but not yet on the map that a new plan must not clash with: those queued this tick,
    /// then the drafted ones, each checked in order against the world and those before it (a piece that could not
    /// go blocks nothing), with the plans being called off treated as gone.
    /// </summary>
    public static List<PlannedPiece> Planned(World w, OrderDrafts drafts)
    {
        List<int> gone = CallingOff(w, drafts);
        List<PlannedPiece> planned = Queued(w, gone);
        foreach ((int _, PlaceBlueprint plan) in drafts.Placements) AddPlaceable(w, plan, planned, gone);
        return planned;
    }

    /// <summary>The pieces of the plans queued this tick that will go, in order (those being called off treated as gone).</summary>
    public static List<PlannedPiece> Queued(World w, IReadOnlyCollection<int>? gone = null)
    {
        gone ??= QueuedCallOffs(w);
        var planned = new List<PlannedPiece>();
        foreach (Command c in w.PendingCommands)
            if (c is PlaceBlueprint plan) AddPlaceable(w, plan, planned, gone);
        return planned;
    }

    /// <summary>
    /// The placed plans asked to be called off but still on the map: drafted, or queued this tick. A new plan may take their
    /// tiles, since the call-off applies first: drafts go out in the order they were made, and the sim applies one tick's
    /// commands in the order they came.
    /// </summary>
    public static List<int> CallingOff(World w, OrderDrafts drafts)
    {
        List<int> ids = QueuedCallOffs(w);
        foreach ((int _, Command d) in drafts._drafts)
            if (d is CancelBlueprints c)
                foreach (int id in c.BlueprintIds)
                    if (!ids.Contains(id)) ids.Add(id);
        return ids;
    }

    /// <summary>The placed plans a call-off queued this tick will take off the map.</summary>
    public static List<int> QueuedCallOffs(World w)
    {
        var ids = new List<int>();
        foreach (Command c in w.PendingCommands)
            if (c is CancelBlueprints cancel)
                foreach (int id in cancel.BlueprintIds)
                    if (!ids.Contains(id)) ids.Add(id);
        return ids;
    }

    private static void AddPlaceable(World w, PlaceBlueprint plan, List<PlannedPiece> planned, IReadOnlyCollection<int> gone)
    {
        foreach (SimRect r in Building.Placeable(w, plan.BuildingId, plan.Pieces, planned, gone))
            planned.Add(new PlannedPiece(plan.BuildingId, r));
    }

    private static List<BuiltRef> Changing(World w, MarkTakeDown m)
    {
        var targets = new List<BuiltRef>();
        foreach (BuiltRef t in m.Targets)
            if (Building.Markable(w, t, out _, out long marked) && (marked != 0) != m.Marked) targets.Add(t);
        return targets;
    }

    private static List<int> StillThere(World w, IReadOnlyList<int> ids)
    {
        var there = new List<int>();
        foreach (int id in ids)
            if (Building.BlueprintById(w, id) != null) there.Add(id);
        return there;
    }

    private static bool Contains(IReadOnlyList<BuiltRef> list, BuiltRef t)
    {
        foreach (BuiltRef x in list)
            if (x == t) return true;
        return false;
    }

    private static bool SameSlot(Command a, Command b) => (a, b) switch
    {
        (SetRationPolicy, SetRationPolicy) => true,
        (SetWorkPriority x, SetWorkPriority y) => x.CharacterId == y.CharacterId && x.WorkId == y.WorkId,
        _ => false,
    };
}
