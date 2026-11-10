using System.Text.Json.Serialization;
using Return.Sim.Events;

namespace Return.Sim;

/// <summary>
/// Player intent, stamped with the tick it takes effect. Pending commands and the applied-command
/// log are part of every snapshot (ADR-0004). Commands set intentions; they never order a unit.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SetRationPolicy), "setRationPolicy")]
[JsonDerivedType(typeof(SetWorkPriority), "setWorkPriority")]
[JsonDerivedType(typeof(SetStandingOrder), "setStandingOrder")]
[JsonDerivedType(typeof(PlaceBlueprint), "placeBlueprint")]
[JsonDerivedType(typeof(CancelBlueprints), "cancelBlueprints")]
[JsonDerivedType(typeof(MarkTakeDown), "markTakeDown")]
public abstract record Command(long Tick)
{
    /// <summary>Order of enqueueing; ties on the same tick apply in this order.</summary>
    public long Seq { get; init; }

    internal abstract void Apply(World w);
}

/// <summary>Who eats first when food runs short.</summary>
public sealed record SetRationPolicy(long Tick, RationPolicy Policy) : Command(Tick)
{
    internal override void Apply(World w)
    {
        w.Policy = Policy;
        Character? self = w.Find(w.PlayerId);
        SimEvent e = w.Log.Emit(w, SystemTag.World, "event.world.ration_policy",
            self == null ? Array.Empty<int>() : new[] { self.Id }, Array.Empty<long>(), null,
            EventArg.Key("policy", PolicyKey(Policy)));
        w.PolicyEvent = e.Id;
    }

    public static string PolicyKey(RationPolicy p) => p switch
    {
        RationPolicy.PromisesFirst => "policy.promises_first",
        RationPolicy.SelfFirst => "policy.self_first",
        _ => "policy.hungriest",
    };
}

/// <summary>How much a person should favour a kind of work (content work id; 1 first … 4 last, 0 never).</summary>
public sealed record SetWorkPriority(long Tick, int CharacterId, string WorkId, int Priority) : Command(Tick)
{
    internal override void Apply(World w)
    {
        Character? c = w.Find(CharacterId);
        if (c == null) return;
        w.Content.WorkById(WorkId);
        WorkPriority? existing = null;
        foreach (WorkPriority p in c.Priorities)
            if (p.WorkId == WorkId) { existing = p; break; }
        if (existing == null) c.Priorities.Add(new WorkPriority { WorkId = WorkId, Priority = Priority });
        else existing.Priority = Priority;
        SimEvent e = w.Log.Emit(w, SystemTag.World, "event.world.work_priority", new[] { c.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", c.Id), EventArg.Key("work", w.Content.WorkById(WorkId).NameKey), EventArg.Num("n", Priority));
        Systems.Purpose.OnPriority(w, c, WorkId, Priority, e.Id);
    }
}

/// <summary>
/// A standing order only you can give: one of your gift's choices (the satchel: keep a reserve, share with the
/// starving, keep nothing back; ADR-0030). You never roll compliance against your own order. An order for a gift you
/// do not hold, or a choice it does not have, changes nothing.
/// </summary>
public sealed record SetStandingOrder(long Tick, string OrderId, string ChoiceId) : Command(Tick)
{
    internal override void Apply(World w)
    {
        if (Systems.Gifts.Held(w)?.Order is not { } order || order.Id != OrderId || !order.Choices.Contains(ChoiceId)) return;
        int at = w.StandingOrders.FindIndex(o => o.OrderId == OrderId);
        if (at >= 0) w.StandingOrders[at].ChoiceId = ChoiceId;
        else
        {
            int i = 0;
            while (i < w.StandingOrders.Count && string.CompareOrdinal(w.StandingOrders[i].OrderId, OrderId) < 0) i++;
            w.StandingOrders.Insert(i, new StandingOrder { OrderId = OrderId, ChoiceId = ChoiceId });
        }
        Character? self = w.Find(w.PlayerId);
        w.Log.Emit(w, SystemTag.World, "event.world.standing_order", self == null ? Array.Empty<int>() : new[] { self.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", w.PlayerId), EventArg.Key("choice", order.DecidedKey(ChoiceId)));
    }
}

// The Build menu (ADR-0027): plans are intentions. People choose who builds, when, and whether.

/// <summary>Marks out one plan of a building: each piece is a tile (a roof section: a rectangle up to its maxSide). Pieces not valid when it applies are dropped.</summary>
public sealed record PlaceBlueprint(long Tick, string BuildingId, IReadOnlyList<Core.Rect> Pieces, int ForId = 0) : Command(Tick)
{
    internal override void Apply(World w) => Systems.Building.Place(w, BuildingId, Pieces, ForId);
}

/// <summary>Calls off plans; whatever was delivered to them drops on the site for hauling.</summary>
public sealed record CancelBlueprints(long Tick, IReadOnlyList<int> BlueprintIds) : Command(Tick)
{
    internal override void Apply(World w) => Systems.Building.CallOff(w, BlueprintIds);
}

/// <summary>Marks built things to be taken down (Marked = true) or unmarks them.</summary>
public sealed record MarkTakeDown(long Tick, IReadOnlyList<BuiltRef> Targets, bool Marked) : Command(Tick)
{
    internal override void Apply(World w) => Systems.Building.Mark(w, Targets, Marked);
}
