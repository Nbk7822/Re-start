using System.Collections.Generic;
using System.Linq;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Systems;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game.Logic;

/// <summary>
/// Player choices made while time is paused. A later choice for the same thing (the ration policy;
/// one person's priority for one kind of work; a gift's standing order; marking or unmarking one built
/// thing to be taken down; one zone; one kind of day's camp day, the watch hours, one person's own day)
/// replaces the earlier one, and when time moves the drafts go out together as commands, skipping any
/// that would change nothing. A plan is its own slot, with a local draft id, so its pieces can be called
/// off one by one before time moves. So changing your mind while paused leaves no trace in the world or
/// its history. Godot-free so the tests compile it.
/// </summary>
public sealed class OrderDrafts
{
    private readonly List<(int Id, Command Command)> _drafts = new();
    private int _nextId = 1;
    private int _lastDraftZone;

    public int Count => _drafts.Count;

    /// <summary>The drafts in the order they will be sent.</summary>
    public IReadOnlyList<Command> All
    {
        get
        {
            var all = new List<Command>(_drafts.Count);
            foreach ((int _, Command d) in _drafts) all.Add(d);
            return all;
        }
    }

    /// <summary>Forgets every draft: after a return they were choices made in a world that is gone (ADR-0030).</summary>
    public void Clear() => _drafts.Clear();

    /// <summary>
    /// Keeps a choice as a draft and returns its draft id. A plan (PlaceBlueprint) is always a new draft. A mark
    /// or unmark replaces any earlier draft for the same thing; a call-off adds only plans not already being
    /// called off. A zone's drafts share one slot (ADR-0028): the items a stockpile keeps fold into its marking,
    /// and removing a zone drops its other drafts (all of them, for a zone drafted while paused, which then never
    /// existed). Any other choice replaces an earlier draft for the same thing.
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
            case SetZoneItems items:
            {
                int at = _drafts.FindIndex(d => d.Command is MarkZone m && m.ZoneId == items.ZoneId);
                if (at >= 0)
                {
                    (int id, Command marked) = _drafts[at];
                    _drafts.RemoveAt(at);
                    _drafts.Add((id, ((MarkZone)marked) with { Items = items.Items }));
                    return id;
                }
                break;
            }
            case MarkZone mark when mark.Items == null:
            {
                IReadOnlyList<string>? kept = DraftedItems(mark.ZoneId);
                if (kept != null) command = mark with { Items = kept };
                break;
            }
            case RemoveZone remove:
                _drafts.RemoveAll(d => ZoneOf(d.Command) == remove.ZoneId);
                if (remove.ZoneId < 0) return 0;
                break;
        }
        _drafts.RemoveAll(d => SameSlot(d.Command, command));
        int draftId = _nextId++;
        _drafts.Add((draftId, command));
        return draftId;
    }

    /// <summary>An id for a zone made while paused: negative, so it never names a real zone, and unique among the drafts.</summary>
    public int NewZoneId() => --_lastDraftZone;

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

    /// <summary>The drafted choice for a gift's standing order (ADR-0030), if any.</summary>
    public string? StandingOrder(string orderId)
    {
        string? choice = null;
        foreach ((int _, Command d) in _drafts)
            if (d is SetStandingOrder s && s.OrderId == orderId) choice = s.ChoiceId;
        return choice;
    }

    /// <summary>The drafted priority for one person and one kind of work, if any.</summary>
    public int? Priority(int characterId, string workId)
    {
        int? priority = null;
        foreach ((int _, Command d) in _drafts)
            if (d is SetWorkPriority p && p.CharacterId == characterId && p.WorkId == workId) priority = p.Priority;
        return priority;
    }

    /// <summary>The drafted camp day of a kind of day, if any (ADR-0029).</summary>
    public IReadOnlyList<DayBlock>? CampDay(string rhythm)
    {
        IReadOnlyList<DayBlock>? day = null;
        foreach ((int _, Command d) in _drafts)
            if (d is SetCampDay c && c.Rhythm == rhythm) day = c.Hours;
        return day;
    }

    /// <summary>The drafted watch hours, if any.</summary>
    public (int From, int To)? WatchHours()
    {
        (int, int)? hours = null;
        foreach ((int _, Command d) in _drafts)
            if (d is SetWatchHours h) hours = (h.From, h.To);
        return hours;
    }

    /// <summary>The drafted own hours of one person, if any (null entries keep the camp's day).</summary>
    public IReadOnlyList<DayBlock?>? PersonDay(int characterId)
    {
        IReadOnlyList<DayBlock?>? day = null;
        foreach ((int _, Command d) in _drafts)
            if (d is SetPersonDay p && p.CharacterId == characterId) day = p.Hours;
        return day;
    }

    /// <summary>Is any schedule command drafted? (The Schedule list then says the change waits for time to move.)</summary>
    public bool HasScheduleDrafts
    {
        get
        {
            foreach ((int _, Command d) in _drafts)
                if (d is SetCampDay or SetWatchHours or SetPersonDay) return true;
            return false;
        }
    }

    /// <summary>Is any zone command drafted? (The Zones list then says the change waits for time to move.)</summary>
    public bool HasZoneDrafts
    {
        get
        {
            foreach ((int _, Command d) in _drafts)
                if (ZoneOf(d) != 0) return true;
            return false;
        }
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
    /// that still has a tile that can take it once the drafts sent before it apply (the sim checks every piece again
    /// when it applies), only the marks and unmarks that change something, call-offs of plans that are still there,
    /// and zone and schedule choices that change something (a zone drafted while paused goes out as a new zone).
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
                SetStandingOrder s => Gifts.Held(w)?.Order is { } order && order.Id == s.OrderId && Gifts.Order(w, order) != s.ChoiceId ? s : null,
                PlaceBlueprint p => BuildTool.OkCount(BuildTool.Check(w, CampPreview.Of(w), p.BuildingId, p.Pieces)) > 0 ? p : null,
                MarkTakeDown m => Changing(w, m) is { Count: > 0 } targets ? m with { Targets = targets } : null,
                CancelBlueprints c => StillThere(w, c.BlueprintIds) is { Count: > 0 } ids ? c with { BlueprintIds = ids } : null,
                MarkZone or RemoveZone or SetZoneItems => !Zones.Changes(w, d) ? null : d is MarkZone { ZoneId: < 0 } z ? z with { ZoneId = 0 } : d,
                SetCampDay or SetWatchHours or SetPersonDay => ScheduleSystem.Changes(w, d) ? d : null,
                _ => d,
            };
            if (send != null) sim.Enqueue(send with { Tick = w.Tick });
        }
        _drafts.Clear();
    }

    /// <summary>
    /// The pieces asked for but not yet on the map that a new plan must not clash with: those queued this tick, then the
    /// drafted ones, each checked in order against the world, the zones and plans before it (a piece that could not go
    /// blocks nothing), with the plans being called off treated as gone (<see cref="CampPreview"/>, which reads all three).
    /// </summary>
    public static List<PlannedPiece> Planned(World w, OrderDrafts drafts) => CampPreview.Of(w, drafts.All).Planned;

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

    private IReadOnlyList<string>? DraftedItems(int zoneId)
    {
        foreach ((int _, Command d) in _drafts)
        {
            if (d is MarkZone m && m.ZoneId == zoneId && m.Items != null) return m.Items;
            if (d is SetZoneItems s && s.ZoneId == zoneId) return s.Items;
        }
        return null;
    }

    /// <summary>The zone a draft is about; 0 for a draft that is not about a zone.</summary>
    private static int ZoneOf(Command c) => c switch
    {
        MarkZone m => m.ZoneId,
        RemoveZone r => r.ZoneId,
        SetZoneItems s => s.ZoneId,
        _ => 0,
    };

    private static bool SameSlot(Command a, Command b) => (a, b) switch
    {
        (SetRationPolicy, SetRationPolicy) => true,
        (SetWorkPriority x, SetWorkPriority y) => x.CharacterId == y.CharacterId && x.WorkId == y.WorkId,
        (SetStandingOrder x, SetStandingOrder y) => x.OrderId == y.OrderId,
        (MarkZone or SetZoneItems, MarkZone or SetZoneItems) => ZoneOf(a) == ZoneOf(b) && !(a is MarkZone && b is SetZoneItems),
        (SetCampDay x, SetCampDay y) => x.Rhythm == y.Rhythm,
        (SetWatchHours, SetWatchHours) => true,
        (SetPersonDay x, SetPersonDay y) => x.CharacterId == y.CharacterId,
        _ => false,
    };
}
