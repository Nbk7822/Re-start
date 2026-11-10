using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// What the player's three Build commands do when they apply (build design 4.3). Each checks itself again,
/// because a plan drafted while paused applies a tick or more later. The player is the actor of every
/// event here: plans are the player's intentions, never orders to anyone.
/// </summary>
public static partial class Building
{
    /// <summary>
    /// One plan: places every piece that is valid now (a roof section is dropped whole if any tile is not)
    /// and says how many were marked out, or that none could be and why.
    /// </summary>
    internal static void Place(World w, string buildingId, IReadOnlyList<Rect> pieces, int forId)
    {
        BuildingDef? def = w.Content.TryBuilding(buildingId);
        if (def == null || pieces.Count == 0) return;
        IReadOnlyList<PieceVerdict> verdicts = Check(w, def.Id, pieces);
        int ok = 0;
        string reason = "";
        foreach (PieceVerdict v in verdicts)
            if (v.Ok) ok++;
            else if (reason.Length == 0) reason = v.Reason;
        if (ok == 0)
        {
            w.Log.Emit(w, SystemTag.World, "event.world.plan_failed", PlayerActors(w), Array.Empty<long>(), null,
                EventArg.Num("n", pieces.Count), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey), EventArg.Key("reason", reason));
            return;
        }
        SimEvent planned = w.Log.Emit(w, SystemTag.World, "event.world.planned", PlayerActors(w), Array.Empty<long>(), null,
            EventArg.Char("a", w.PlayerId), EventArg.Num("n", ok), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey));
        foreach (PieceVerdict v in verdicts)
            if (v.Ok) w.Blueprints.Add(new Blueprint { Id = w.NewId(), DefId = def.Id, Area = v.Area, PlanEvent = planned.Id, ForId = forId });
    }

    /// <summary>
    /// Calls off plans: whatever was delivered drops on the site as a loose stack for hauling. A plan that
    /// had material delivered, work done or pieces built says it was called off; one that had none leaves no trace.
    /// </summary>
    internal static void CallOff(World w, IReadOnlyList<int> blueprintIds)
    {
        var plans = new List<(long Plan, string DefId, int Count, bool Started)>();
        for (int i = 0; i < w.Blueprints.Count; i++)
        {
            Blueprint bp = w.Blueprints[i];
            if (!blueprintIds.Contains(bp.Id)) continue;
            BuildingDef def = Def(w, bp);
            if (bp.Delivered > 0)
            {
                Point site = SiteTile(w, bp.Area);
                if (site != Point.None) Stock.Add(w, def.Material.Item, bp.Delivered, site);
            }
            int at = plans.FindIndex(p => p.Plan == bp.PlanEvent && p.DefId == bp.DefId);
            bool started = bp.Delivered > 0 || bp.Progress > 0;
            if (at < 0) plans.Add((bp.PlanEvent, bp.DefId, 1, started));
            else plans[at] = (plans[at].Plan, plans[at].DefId, plans[at].Count + 1, plans[at].Started || started);
            w.Blueprints.RemoveAt(i--);
        }
        foreach ((long plan, string defId, int count, bool started) in plans)
        {
            if (!started && !AnythingBuiltFrom(w, plan)) continue;
            BuildingDef def = w.Content.BuildingById(defId);
            w.Log.Emit(w, SystemTag.World, "event.world.plan_called_off", PlayerActors(w), new[] { plan }, null,
                EventArg.Char("a", w.PlayerId), EventArg.Num("n", count), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey));
        }
    }

    /// <summary>
    /// Marks built things to be taken down, or unmarks them. Marking says so once per gesture; unmarking says
    /// nothing. The old stone walls and the old roof cannot be taken down until there is stone (slice 3).
    /// </summary>
    internal static void Mark(World w, IReadOnlyList<BuiltRef> targets, bool marked)
    {
        var newly = new List<(BuiltRef Ref, string DefId)>();
        foreach (BuiltRef t in targets)
        {
            if (!Markable(w, t, out string defId, out long current)) continue;
            if (!marked)
            {
                SetMark(w, t, 0);
                continue;
            }
            if (current != 0 || newly.Exists(n => n.Ref == t)) continue;
            newly.Add((t, defId));
        }
        if (newly.Count == 0) return;
        string first = newly[0].DefId;
        bool mixed = newly.Exists(n => n.DefId != first);
        BuildingDef def = w.Content.BuildingById(first);
        SimEvent e = mixed
            ? w.Log.Emit(w, SystemTag.World, "event.world.marked_take_down_mixed", PlayerActors(w), Array.Empty<long>(), null,
                EventArg.Char("a", w.PlayerId), EventArg.Num("n", newly.Count))
            : w.Log.Emit(w, SystemTag.World, "event.world.marked_take_down", PlayerActors(w), Array.Empty<long>(), null,
                EventArg.Char("a", w.PlayerId), EventArg.Num("n", newly.Count), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey));
        foreach ((BuiltRef t, string _) in newly) SetMark(w, t, e.Id);
    }

    /// <summary>Can this built thing be marked (it exists, and is not the old camp's stone or roof), what is it, and is it marked already?</summary>
    public static bool Markable(World w, BuiltRef t, out string defId, out long marked)
    {
        defId = "";
        marked = 0;
        switch (t.Kind)
        {
            case TargetKind.Roof when RoofById(w, t.Id) is { } r && r.BuiltBy.Count > 0:
                defId = Def(w, r).Id;
                marked = r.MarkedEvent;
                return true;
            case TargetKind.Bed when BedById(w, t.Id) is { } b:
                defId = Def(w, b).Id;
                marked = b.MarkedEvent;
                return true;
            case TargetKind.Structure when StructureById(w, t.Id) is { } s:
                defId = s.DefId;
                marked = s.MarkedEvent;
                return true;
            default:
                return false;
        }
    }

    private static void SetMark(World w, BuiltRef t, long eventId)
    {
        switch (t.Kind)
        {
            case TargetKind.Roof when RoofById(w, t.Id) is { } r: r.MarkedEvent = eventId; break;
            case TargetKind.Bed when BedById(w, t.Id) is { } b: b.MarkedEvent = eventId; break;
            case TargetKind.Structure when StructureById(w, t.Id) is { } s: s.MarkedEvent = eventId; break;
        }
    }

    /// <summary>Did any piece of this plan get built (a wall or door standing, a bed or roof section whose building cites the plan)?</summary>
    private static bool AnythingBuiltFrom(World w, long plan)
    {
        foreach (Structure s in w.Map.Structures)
            if (s.PlanEvent == plan) return true;
        foreach (Bed b in w.Map.Beds)
            if (b.BuiltEvent != 0 && w.Log.Get(b.BuiltEvent) is { } e && e.Causes.Contains(plan)) return true;
        foreach (RoofSection r in w.Map.Roof)
            if (r.BuiltEvent != 0 && w.Log.Get(r.BuiltEvent) is { } e && e.Causes.Contains(plan)) return true;
        return false;
    }

    /// <summary>The player, as the actor of their own plans.</summary>
    private static int[] PlayerActors(World w) => w.Find(w.PlayerId) is { } p ? new[] { p.Id } : Array.Empty<int>();
}
