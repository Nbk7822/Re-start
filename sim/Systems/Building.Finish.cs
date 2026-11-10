using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// Finishing a piece (build design 4.5) and taking things down (4.5, 4.11). Finishing a building is never a
/// story's ending by itself: no event here carries a persistent effect, except through the promises,
/// grudges and debts it touches.
/// </summary>
public static partial class Building
{
    /// <summary>
    /// The last tick of work. A wall or door is never finished on someone's tile; a wall that would shut people in
    /// is refused, and nobody tries it again until the map changes. Otherwise the piece is built.
    /// </summary>
    private static bool Finish(World w, Character c, Blueprint bp, BuildingDef def)
    {
        var tile = new Point(bp.Area.X, bp.Area.Y);
        if (def.Kind is Wall or Door && InTheWay(w, tile))
        {
            bp.Blocked = BlockedReason.InTheWay;
            bp.BlockedRevision = w.Map.Revision;
            return true;
        }
        if (def.Kind == Wall && WouldShutIn(w, tile))
        {
            bp.Blocked = BlockedReason.WouldShutIn;
            bp.BlockedRevision = w.Map.Revision;
            w.Log.Emit(w, SystemTag.Work, "event.work.refused_to_shut_in", new[] { c.Id }, new[] { bp.PlanEvent }, null,
                EventArg.Char("a", c.Id), EventArg.Key("one", def.AKey));
            return true;
        }
        Complete(w, c, bp, def);
        return true;
    }

    /// <summary>
    /// Makes the piece: a wall or door writes through to the terrain, a roof section and a bed are appended; the map
    /// changes; the builder is proud of a bed, a roof section or a whole plan; a new roof section keeps a roof promise
    /// as a repair does; and building rolls for an accident like other risky work.
    /// </summary>
    private static void Complete(World w, Character c, Blueprint bp, BuildingDef def)
    {
        var tile = new Point(bp.Area.X, bp.Area.Y);
        var builtBy = new List<int> { c.Id };
        foreach (int id in bp.Builders)
            if (id != c.Id) builtBy.Add(id);
        w.Blueprints.Remove(bp);
        long accidentCause = bp.PlanEvent;
        switch (def.Kind)
        {
            case Wall or Door:
            {
                TerrainKind ground = w.Map.At(tile);
                MoveAside(w, tile);
                w.Map.Set(tile, def.Kind == Wall ? TerrainKind.Wall : TerrainKind.Door);
                InsertById(w.Map.Structures, new Structure { Id = bp.Id, DefId = def.Id, Pos = tile, Ground = ground, BuiltBy = builtBy, PlanEvent = bp.PlanEvent }, s => s.Id);
                w.Map.Changed();
                if (w.Blueprints.Exists(b => b.PlanEvent == bp.PlanEvent)) break;
                // The plan's last piece: one line for the whole run, so a wall does not crowd the Chronicle.
                var pieces = w.Map.Structures.FindAll(s => s.PlanEvent == bp.PlanEvent);
                SimEvent done = w.Log.Emit(w, SystemTag.Work, "event.work.plan_finished", new[] { c.Id }, new[] { bp.PlanEvent }, null,
                    EventArg.Char("a", c.Id), EventArg.Num("n", pieces.Count), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey));
                foreach (Structure s in pieces) s.BuiltEvent = done.Id;
                MoodSystem.AddThought(w, c, "built_something", 0, done.Id);
                accidentCause = done.Id;
                break;
            }
            case Roof:
            {
                var section = new RoofSection { Index = w.Map.Roof.Count, Id = bp.Id, DefId = def.Id, Area = bp.Area, Sound = true, BuiltBy = builtBy };
                w.Map.Roof.Add(section);
                w.Map.Changed();
                SimEvent built = Built(w, c, bp, def, RoofPromise(w));
                section.BuiltEvent = built.Id;
                foreach (Debt d in w.Debts)
                    if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseRest) Relations.Repay(w, d, built.Id);
                accidentCause = built.Id;
                break;
            }
            default:
            {
                var bed = new Bed { Id = bp.Id, DefId = def.Id, Pos = tile, BuiltBy = builtBy };
                InsertById(w.Map.Beds, bed, b => b.Id);
                w.Map.Changed();
                bed.BuiltEvent = Built(w, c, bp, def, 0).Id;
                accidentCause = bed.BuiltEvent;
                break;
            }
        }
        JobRunner.RollAccident(w, c, w.Content.WorkById("build"), accidentCause);
    }

    /// <summary>"Sera built a bed.": one per bed or roof section, citing the plan, whoever was passed over for it, and a roof promise it keeps.</summary>
    private static SimEvent Built(World w, Character c, Blueprint bp, BuildingDef def, long promise)
    {
        SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.built", new[] { c.Id }, new[] { bp.PlanEvent, bp.PassedOverEvent, promise }, null,
            EventArg.Char("a", c.Id), EventArg.Key("one", def.AKey));
        MoodSystem.AddThought(w, c, "built_something", 0, e.Id);
        return e;
    }

    /// <summary>Loose items on a tile a wall or door is taking are moved aside, without an event.</summary>
    private static void MoveAside(World w, Point tile)
    {
        Point aside = Places.Adjacent(w, tile, tile);
        if (aside == Point.None) return;
        foreach (ItemStack s in w.Items)
            if (s.Pos == tile) s.Pos = aside;
    }

    private static void InsertById<T>(List<T> list, T item, Func<T, int> id)
    {
        int at = list.Count;
        while (at > 0 && id(list[at - 1]) > id(item)) at--;
        list.Insert(at, item);
    }

    // ---- Taking down ----

    /// <summary>Something marked to be taken down this person can reach: the oldest mark first, then the nearest, then (y, x).</summary>
    private static Job? TakeDownJob(World w, Character c)
    {
        (TargetKind Kind, int Id, long Mark, Point Stand)? best = null;
        int bestDist = int.MaxValue;
        bool[]? reach = null;
        void Consider(TargetKind kind, int id, long mark, Rect area, bool beside)
        {
            if (mark == 0 || Targeted(w, JobKind.TakeDown, kind, id, c.Id)) return;
            if (best is { } b && mark > b.Mark) return;
            reach ??= Reach(w.Map, new[] { c.Pos }, Point.None);
            Point stand = beside ? Beside(w, area, c.Pos, reach) : UnderOrBeside(w, area, c.Pos, reach);
            if (stand == Point.None) return;
            int d = stand.ChebyshevTo(c.Pos);
            if (best is { } cur && mark == cur.Mark && d >= bestDist) return;
            best = (kind, id, mark, stand);
            bestDist = d;
        }
        foreach (Structure s in w.Map.Structures)
            if (!s.Removed) Consider(TargetKind.Structure, s.Id, s.MarkedEvent, new Rect(s.Pos.X, s.Pos.Y, 1, 1), beside: true);
        foreach (Bed b in w.Map.Beds) Consider(TargetKind.Bed, b.Id, b.MarkedEvent, new Rect(b.Pos.X, b.Pos.Y, 1, 1), beside: false);
        foreach (RoofSection r in w.Map.Roof)
            if (!r.Removed) Consider(TargetKind.Roof, r.Id, r.MarkedEvent, r.Area, beside: false);
        return best is { } t ? new Job { Kind = JobKind.TakeDown, TargetKind = t.Kind, TargetId = t.Id, Target = t.Stand, CauseEvent = t.Mark } : null;
    }

    private static Point UnderOrBeside(World w, Rect area, Point from, bool[] reach)
    {
        Map m = w.Map;
        if (m.Passable(area.Center) && reach[m.Index(area.Center)]) return area.Center;
        for (int y = area.Y; y < area.Y + area.H; y++)
            for (int x = area.X; x < area.X + area.W; x++)
            {
                var p = new Point(x, y);
                if (m.Passable(p) && reach[m.Index(p)]) return p;
            }
        return Beside(w, area, from, reach);
    }

    /// <summary>The take-down job: walk to it, work half its building time, and take it down. A mark lifted meanwhile ends it.</summary>
    internal static bool TakeDown(World w, Character c, Job j)
    {
        (BuildingDef def, Rect area, long mark)? target = j.TargetKind switch
        {
            TargetKind.Structure when StructureById(w, j.TargetId) is { } s => (w.Content.BuildingById(s.DefId), new Rect(s.Pos.X, s.Pos.Y, 1, 1), s.MarkedEvent),
            TargetKind.Bed when BedById(w, j.TargetId) is { } b => (Def(w, b), new Rect(b.Pos.X, b.Pos.Y, 1, 1), b.MarkedEvent),
            TargetKind.Roof when RoofById(w, j.TargetId) is { } r => (Def(w, r), r.Area, r.MarkedEvent),
            _ => null,
        };
        if (target is not { } t || t.mark == 0) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            j.Stage = 1;
            j.Progress = 0;
            return false;
        }
        c.Facing = TravelSystem.FacingFor(c.Pos, t.area.Center);
        if (!JobRunner.Work(j, t.def.TakeDownWorkFor(t.area))) return false;
        Remove(w, c, j.TargetKind, j.TargetId, t.def, t.area, t.mark);
        return true;
    }

    /// <summary>
    /// Takes a thing down: half its material comes back as a loose stack. A bed's owner loses it; whoever built a
    /// thing sees it torn down; both blame whoever marked it, the player, and a grudge forms past the line. A wall
    /// run is told once, when its last piece comes down.
    /// </summary>
    private static void Remove(World w, Character c, TargetKind kind, int id, BuildingDef def, Rect area, long mark)
    {
        Map m = w.Map;
        int back = def.ReturnedOnTakeDown(area);
        var builders = new List<int>();
        int owner = 0;
        SimEvent? e = null;
        switch (kind)
        {
            case TargetKind.Structure:
            {
                Structure s = StructureById(w, id)!;
                m.Set(s.Pos, s.Ground);
                s.Removed = true;
                s.TakenDownBy = c.Id;
                m.Changed();
                if (back > 0) Stock.Add(w, def.Material.Item, back, s.Pos);
                if (m.Structures.Exists(x => !x.Removed && x.MarkedEvent == mark)) break; // more of this gesture still to come down
                e = TookDown(w, c.Id, mark, builders);
                break;
            }
            case TargetKind.Bed:
            {
                Bed b = BedById(w, id)!;
                owner = b.OwnerId;
                if (Builder(b.BuiltBy) is int by and not 0) builders.Add(by);
                m.Beds.Remove(b);
                m.Changed();
                if (back > 0) Stock.Add(w, def.Material.Item, back, b.Pos);
                e = owner != 0
                    ? w.Log.Emit(w, SystemTag.Work, "event.work.took_down_bed_of", new[] { c.Id, owner }, new[] { mark }, null,
                        EventArg.Char("a", c.Id), EventArg.Char("b", owner))
                    : w.Log.Emit(w, SystemTag.Work, "event.work.took_down", new[] { c.Id }, new[] { mark }, null,
                        EventArg.Char("a", c.Id), EventArg.Num("n", 1), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey));
                break;
            }
            default:
            {
                RoofSection r = RoofById(w, id)!;
                r.Removed = true;
                r.Sound = false;
                m.Changed();
                if (Builder(r.BuiltBy) is int by and not 0) builders.Add(by);
                Point site = SiteTile(w, r.Area);
                if (back > 0 && site != Point.None) Stock.Add(w, def.Material.Item, back, site);
                e = w.Log.Emit(w, SystemTag.Work, "event.work.took_down", new[] { c.Id }, new[] { mark }, null,
                    EventArg.Char("a", c.Id), EventArg.Num("n", 1), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey));
                break;
            }
        }
        if (e == null)
        {
            JobRunner.RollAccident(w, c, w.Content.WorkById("take_down"), mark);
            return;
        }
        if (owner != 0) Blame(w, owner, "lost_bed", e.Id);
        foreach (int b in builders) Blame(w, b, "work_torn_down", e.Id);
        JobRunner.RollAccident(w, c, w.Content.WorkById("take_down"), e.Id);
    }

    /// <summary>
    /// The walls and doors of one gesture that came down, told once: "Oren took down 13 walls.", or "... 3 things." when the
    /// gesture held more than one kind (it used to be named after whichever piece came down last). Gathers their builders.
    /// </summary>
    private static SimEvent TookDown(World w, int taker, long mark, List<int> builders)
    {
        List<Structure> gesture = w.Map.Structures.FindAll(x => x.Removed && x.MarkedEvent == mark);
        foreach (Structure x in gesture)
            if (Builder(x.BuiltBy) is int b and not 0 && !builders.Contains(b)) builders.Add(b);
        BuildingDef def = w.Content.BuildingById(gesture[0].DefId);
        return gesture.Exists(x => x.DefId != def.Id)
            ? w.Log.Emit(w, SystemTag.Work, "event.work.took_down_mixed", new[] { taker }, new[] { mark }, null,
                EventArg.Char("a", taker), EventArg.Num("n", gesture.Count))
            : w.Log.Emit(w, SystemTag.Work, "event.work.took_down", new[] { taker }, new[] { mark }, null,
                EventArg.Char("a", taker), EventArg.Num("n", gesture.Count), EventArg.Key("one", def.AKey), EventArg.Key("many", def.PluralKey));
    }

    /// <summary>
    /// The player left standing what was left of a gesture some of which had already come down: what came down is told now,
    /// by one who took part of it down, and its builders see it torn down, as if the last piece had come down.
    /// </summary>
    private static void GestureLeftStanding(World w, long mark)
    {
        if (w.Map.Structures.Exists(x => !x.Removed && x.MarkedEvent == mark)) return;
        Structure? last = w.Map.Structures.FindLast(x => x.Removed && x.MarkedEvent == mark);
        if (last == null || last.TakenDownBy == 0) return;
        var builders = new List<int>();
        SimEvent e = TookDown(w, last.TakenDownBy, mark, builders);
        foreach (int b in builders) Blame(w, b, "work_torn_down", e.Id);
    }

    /// <summary>The one who lost something thinks less of the player who marked it; past the grudge line, a grudge forms, as after insults.</summary>
    private static void Blame(World w, int who, string thought, long cause)
    {
        if (who == w.PlayerId || w.Find(who) is not { Status: CharStatus.Member } c || w.Find(w.PlayerId) is not { } player) return;
        MoodSystem.AddThought(w, c, thought, player.Id, cause);
        if (Relations.Opinion(w, c.Id, player.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, c.Id, player.Id) == null)
            Relations.FormGrudge(w, c, player, 30, cause);
    }
}
