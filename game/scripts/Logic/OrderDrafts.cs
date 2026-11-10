using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Systems;

namespace Return.Game.Logic;

/// <summary>
/// Player choices made while time is paused. A later choice for the same thing (the ration policy;
/// one person's priority for one kind of work) replaces the earlier one, and when time moves the
/// drafts go out together as commands, skipping any that would change nothing. So changing your mind
/// while paused leaves no trace in the world or its history. Godot-free so the tests compile it.
/// </summary>
public sealed class OrderDrafts
{
    private readonly List<Command> _drafts = new();

    public int Count => _drafts.Count;

    /// <summary>Forgets every draft: after a return they were choices made in a world that is gone (ADR-0030).</summary>
    public void Clear() => _drafts.Clear();

    /// <summary>Keeps a choice as a draft, replacing any earlier draft for the same thing.</summary>
    public void Add(Command command)
    {
        _drafts.RemoveAll(d => SameSlot(d, command));
        _drafts.Add(command);
    }

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

    /// <summary>The drafted choice for a gift's standing order (ADR-0030), if any.</summary>
    public string? StandingOrder(string orderId)
    {
        string? choice = null;
        foreach (Command d in _drafts)
            if (d is SetStandingOrder s && s.OrderId == orderId) choice = s.ChoiceId;
        return choice;
    }

    /// <summary>The drafted priority for one person and one kind of work, if any.</summary>
    public int? Priority(int characterId, string workId)
    {
        int? priority = null;
        foreach (Command d in _drafts)
            if (d is SetWorkPriority p && p.CharacterId == characterId && p.WorkId == workId) priority = p.Priority;
        return priority;
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
                SetStandingOrder s => Gifts.Held(w)?.Order is { } order && order.Id == s.OrderId && Gifts.Order(w, order) != s.ChoiceId,
                _ => true,
            };
            if (changes) sim.Enqueue(d with { Tick = w.Tick });
        }
        _drafts.Clear();
    }

    private static bool SameSlot(Command a, Command b) => (a, b) switch
    {
        (SetRationPolicy, SetRationPolicy) => true,
        (SetWorkPriority x, SetWorkPriority y) => x.CharacterId == y.CharacterId && x.WorkId == y.WorkId,
        (SetStandingOrder x, SetStandingOrder y) => x.OrderId == y.OrderId,
        _ => false,
    };
}
