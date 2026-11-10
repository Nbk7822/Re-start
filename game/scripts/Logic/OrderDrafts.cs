using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Systems;

namespace Return.Game.Logic;

/// <summary>
/// Player choices made while time is paused. A later choice for the same thing (the ration policy;
/// one person's priority for one kind of work; one zone; one kind of day's camp day, the watch hours, one
/// person's own day) replaces the earlier one, and when time moves
/// the drafts go out together as commands, skipping any that would change nothing. So changing your mind
/// while paused leaves no trace in the world or its history. Godot-free so the tests compile it.
/// </summary>
public sealed class OrderDrafts
{
    private readonly List<Command> _drafts = new();
    private int _lastDraftZone;

    public int Count => _drafts.Count;

    /// <summary>The drafts in the order they will be sent.</summary>
    public IReadOnlyList<Command> All => _drafts;

    /// <summary>
    /// Keeps a choice as a draft, replacing any earlier draft for the same thing. A zone's drafts share
    /// one slot (ADR-0028): the items a stockpile keeps fold into its marking, and removing a zone drops
    /// its other drafts (all of them, for a zone drafted while paused, which then never existed).
    /// </summary>
    public void Add(Command command)
    {
        switch (command)
        {
            case SetZoneItems items:
            {
                int at = _drafts.FindIndex(d => d is MarkZone m && m.ZoneId == items.ZoneId);
                if (at >= 0)
                {
                    var mark = (MarkZone)_drafts[at];
                    _drafts.RemoveAt(at);
                    _drafts.Add(mark with { Items = items.Items });
                    return;
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
                _drafts.RemoveAll(d => ZoneOf(d) == remove.ZoneId);
                if (remove.ZoneId < 0) return;
                break;
        }
        _drafts.RemoveAll(d => SameSlot(d, command));
        _drafts.Add(command);
    }

    /// <summary>An id for a zone made while paused: negative, so it never names a real zone, and unique among the drafts.</summary>
    public int NewZoneId() => --_lastDraftZone;

    /// <summary>The drafted ration policy, if any.</summary>
    public RationPolicy? Policy
    {
        get
        {
            RationPolicy? policy = null;
            foreach (Command d in _drafts)
                if (d is SetRationPolicy p) policy = p.Policy;
            return policy;
        }
    }

    /// <summary>The drafted priority for one person and one kind of work, if any.</summary>
    public int? Priority(int characterId, string workId)
    {
        int? priority = null;
        foreach (Command d in _drafts)
            if (d is SetWorkPriority p && p.CharacterId == characterId && p.WorkId == workId) priority = p.Priority;
        return priority;
    }

    /// <summary>The drafted camp day of a kind of day, if any (ADR-0029).</summary>
    public IReadOnlyList<DayBlock>? CampDay(string rhythm)
    {
        IReadOnlyList<DayBlock>? day = null;
        foreach (Command d in _drafts)
            if (d is SetCampDay c && c.Rhythm == rhythm) day = c.Hours;
        return day;
    }

    /// <summary>The drafted watch hours, if any.</summary>
    public (int From, int To)? WatchHours()
    {
        (int, int)? hours = null;
        foreach (Command d in _drafts)
            if (d is SetWatchHours h) hours = (h.From, h.To);
        return hours;
    }

    /// <summary>The drafted own hours of one person, if any (null entries keep the camp's day).</summary>
    public IReadOnlyList<DayBlock?>? PersonDay(int characterId)
    {
        IReadOnlyList<DayBlock?>? day = null;
        foreach (Command d in _drafts)
            if (d is SetPersonDay p && p.CharacterId == characterId) day = p.Hours;
        return day;
    }

    /// <summary>Is any schedule command drafted? (The Schedule list then says the change waits for time to move.)</summary>
    public bool HasScheduleDrafts
    {
        get
        {
            foreach (Command d in _drafts)
                if (d is SetCampDay or SetWatchHours or SetPersonDay) return true;
            return false;
        }
    }

    /// <summary>Is any zone command drafted? (The Zones list then says the change waits for time to move.)</summary>
    public bool HasZoneDrafts
    {
        get
        {
            foreach (Command d in _drafts)
                if (ZoneOf(d) != 0) return true;
            return false;
        }
    }

    /// <summary>Sends every draft that still changes something, stamped with the current tick, and clears them.</summary>
    public void Flush(Simulation sim)
    {
        World w = sim.World;
        foreach (Command d in _drafts)
        {
            bool changes = d switch
            {
                SetRationPolicy p => p.Policy != w.Policy,
                SetWorkPriority p => w.Find(p.CharacterId) is { Status: CharStatus.Member } c && JobPicker.PriorityOf(c, p.WorkId) != p.Priority,
                MarkZone or RemoveZone or SetZoneItems => Zones.Changes(w, d),
                SetCampDay or SetWatchHours or SetPersonDay => ScheduleSystem.Changes(w, d),
                _ => true,
            };
            if (!changes) continue;
            Command send = d is MarkZone { ZoneId: < 0 } m ? m with { ZoneId = 0 } : d;
            sim.Enqueue(send with { Tick = w.Tick });
        }
        _drafts.Clear();
    }

    private IReadOnlyList<string>? DraftedItems(int zoneId)
    {
        foreach (Command d in _drafts)
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
        (MarkZone or SetZoneItems, MarkZone or SetZoneItems) => ZoneOf(a) == ZoneOf(b) && !(a is MarkZone && b is SetZoneItems),
        (SetCampDay x, SetCampDay y) => x.Rhythm == y.Rhythm,
        (SetWatchHours, SetWatchHours) => true,
        (SetPersonDay x, SetPersonDay y) => x.CharacterId == y.CharacterId,
        _ => false,
    };
}
