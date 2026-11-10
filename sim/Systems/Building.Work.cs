using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// The work of building (build design 4.5 and 4.7), chosen through people's priorities like any other
/// work: delivering (under haul), building and taking down (under build). Timber is scarce and shared, so
/// whoever commits the last of it to one plan may pass over someone counting on another.
/// </summary>
public static partial class Building
{
    // ---- Choosing ----

    /// <summary>
    /// The builder's right: someone without a bed puts a bed plan first, unless they give way to others
    /// (Generous, read through the trait that gives up its own share; the design adds no trait field).
    /// </summary>
    public static bool BuildersRight(World w, Character c) => OwnedBed(w, c.Id) == null && Places.Tires(w, c) && !GivesWay(w, c);

    /// <summary>Generous people give way: they never take the last of the timber from someone counting on it, nor claim the builder's right.</summary>
    public static bool GivesWay(World w, Character c) => Traits.Any(w, c, t => t.SharesFood);

    /// <summary>A bed plan this person could build now, for the builder's right.</summary>
    internal static Job? BedPlanJob(World w, Character c, bool sunBlocked) => sunBlocked ? null : BestPlan(w, c, onlyKind: Bed, cause: 0);

    /// <summary>A roof plan an Oathbound debtor of a promised roof could build now, citing the promise.</summary>
    internal static Job? RoofPlanJob(World w, Character c, long promise) => BestPlan(w, c, onlyKind: Roof, cause: promise);

    /// <summary>The build work: something marked to be taken down, else the oldest plan they can reach and supply.</summary>
    internal static Job? BuildJob(World w, Character c, bool sunBlocked)
    {
        if (sunBlocked) return null;
        return TakeDownJob(w, c) ?? BestPlan(w, c, onlyKind: null, cause: 0);
    }

    /// <summary>
    /// The plan this builder would take: the oldest plan (by its plan event) they can reach and supply, nearest
    /// piece first, then (y, x). Skipped: plans another builder or a deliverer is on, a wall someone stands on, a
    /// roof still waiting for walls, a plan refused as shutting people in until the map changes, and, for those who
    /// give way, a plan whose timber would pass someone over.
    /// </summary>
    private static Job? BestPlan(World w, Character c, string? onlyKind, long cause)
    {
        Blueprint? best = null;
        Point bestStand = Point.None;
        int bestDist = int.MaxValue;
        bool[]? reach = null;
        foreach (Blueprint bp in w.Blueprints)
        {
            if (best != null && bp.PlanEvent > best.PlanEvent) break; // ids and plan events rise together: nothing older follows
            BuildingDef def = Def(w, bp);
            if (onlyKind != null && def.Kind != onlyKind) continue;
            if (!BuildableNow(w, c, bp, def)) continue;
            int lacking = Lacking(w, bp);
            if (lacking > Stock.Count(w, def.Material.Item)) continue;
            if (lacking > 0 && GivesWay(w, c) && PassedOver(w, bp, def, lacking, c.Id).Count > 0) continue;
            reach ??= Reach(w.Map, new[] { c.Pos }, Point.None);
            Point stand = StandTile(w, bp, def, c.Pos, reach);
            if (stand == Point.None) continue;
            int d = stand.ChebyshevTo(c.Pos);
            if (best == null || d < bestDist || (d == bestDist && (bp.Area.Y < best.Area.Y || (bp.Area.Y == best.Area.Y && bp.Area.X < best.Area.X))))
            {
                best = bp;
                bestStand = stand;
                bestDist = d;
            }
        }
        if (best == null) return null;
        Point target = Lacking(w, best) > 0 ? Places.FoodTile(w, Def(w, best).Material.Item) : bestStand;
        return new Job { Kind = JobKind.Build, TargetKind = TargetKind.Blueprint, TargetId = best.Id, Target = target, Stage = 0, CauseEvent = cause };
    }

    /// <summary>Not taken by another builder or a deliverer, not blocked, its walls standing (a roof), nobody in the way (a wall or door).</summary>
    private static bool BuildableNow(World w, Character c, Blueprint bp, BuildingDef def)
    {
        if (Targeted(w, JobKind.Build, TargetKind.Blueprint, bp.Id, c.Id) || Targeted(w, JobKind.Deliver, TargetKind.Blueprint, bp.Id, c.Id)) return false;
        if (bp.Blocked == BlockedReason.WouldShutIn && bp.BlockedRevision == w.Map.Revision) return false;
        if (def.Kind == Roof && !Supported(w, bp.Area, def)) return false;
        if (def.Kind is Wall or Door && InTheWay(w, new Point(bp.Area.X, bp.Area.Y))) return false;
        return true;
    }

    /// <summary>
    /// The deliver work (under haul): carry material from the stock to the oldest plan still short of it that
    /// nobody else is supplying. While a roof promise is open, deliverers leave enough timber in stock to mend or
    /// raise one roof section; builders may still take it, and that is how someone gets passed over.
    /// </summary>
    internal static Job? DeliverJob(World w, Character c)
    {
        bool[]? reach = null;
        foreach (Blueprint bp in w.Blueprints)
        {
            int lacking = Lacking(w, bp);
            if (lacking == 0) continue;
            if (Targeted(w, JobKind.Deliver, TargetKind.Blueprint, bp.Id, c.Id) || Targeted(w, JobKind.Build, TargetKind.Blueprint, bp.Id, c.Id)) continue;
            BuildingDef def = Def(w, bp);
            int take = Math.Min(lacking, Available(w, def.Material.Item));
            if (take <= 0) continue;
            if (GivesWay(w, c) && PassedOver(w, bp, def, take, c.Id).Count > 0) continue;
            reach ??= Reach(w.Map, new[] { c.Pos }, Point.None);
            Point site = SiteTile(w, bp.Area);
            if (site == Point.None || !reach[w.Map.Index(site)]) continue;
            Point source = Places.FoodTile(w, def.Material.Item);
            if (source == Point.None || !reach[w.Map.Index(source)]) continue;
            return new Job { Kind = JobKind.Deliver, TargetKind = TargetKind.Blueprint, TargetId = bp.Id, Target = source };
        }
        return null;
    }

    /// <summary>What deliverers may take of an item: the stock, less one roof section's repair while a roof promise is open.</summary>
    private static int Available(World w, string item)
    {
        int stock = Stock.Count(w, item);
        if (item == "timber" && RoofPromise(w) != 0) stock -= w.Content.WorkById("repair_roof").TimberCost;
        return stock;
    }

    /// <summary>Where a builder stands: beside a wall or door, on a bed's tile, under a roof section (else beside it); reachable, nearest first.</summary>
    private static Point StandTile(World w, Blueprint bp, BuildingDef def, Point from, bool[] reach)
    {
        Map m = w.Map;
        bool Ok(Point p) => m.Passable(p) && reach[m.Index(p)];
        var tile = new Point(bp.Area.X, bp.Area.Y);
        if (def.Kind is Wall or Door) return Beside(w, bp.Area, from, reach);
        if (def.Kind == Bed) return Ok(tile) ? tile : Point.None;
        if (Ok(bp.Area.Center)) return bp.Area.Center;
        for (int y = bp.Area.Y; y < bp.Area.Y + bp.Area.H; y++)
            for (int x = bp.Area.X; x < bp.Area.X + bp.Area.W; x++)
                if (Ok(new Point(x, y))) return new Point(x, y);
        return Beside(w, bp.Area, from, reach);
    }

    /// <summary>The reachable, passable tile next to an area nearest to <paramref name="from"/> (ties by y, then x), or Point.None.</summary>
    private static Point Beside(World w, Rect area, Point from, bool[] reach)
    {
        Map m = w.Map;
        Point best = Point.None;
        int bestDist = int.MaxValue;
        for (int y = area.Y - 1; y <= area.Y + area.H; y++)
            for (int x = area.X - 1; x <= area.X + area.W; x++)
            {
                var p = new Point(x, y);
                if (area.Contains(p) || !m.Passable(p) || !reach[m.Index(p)]) continue;
                int d = p.ChebyshevTo(from);
                if (d < bestDist) { best = p; bestDist = d; }
            }
        return best;
    }

    // ---- Doing ----

    /// <summary>
    /// The deliver job: to the stock (stage 0), take what the plan lacks and carry it to the site (stage 1), where
    /// it is committed to the plan. A delivery never leaves a plan holding more than it costs: any excess is left on the site.
    /// </summary>
    internal static bool Deliver(World w, Character c, Job j)
    {
        Blueprint? bp = BlueprintById(w, j.TargetId);
        if (bp == null) return true;
        BuildingDef def = Def(w, bp);
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            int take = Math.Min(Lacking(w, bp), Available(w, def.Material.Item));
            if (take <= 0) return true;
            Commit(w, c, bp, def, take);
            JobRunner.Next(w, c, j, SiteTile(w, bp.Area));
            return JobRunner.Failed(j);
        }
        if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
        Unload(w, c, bp);
        return true;
    }

    /// <summary>
    /// The build job: fetch what the plan lacks (stage 0) and carry it to where they will stand (stage 1), then work
    /// (stage 2). Progress lives on the plan, so anyone can carry on after an interruption. Finishing checks that
    /// nobody is in the way and that a wall would not shut anyone in.
    /// </summary>
    internal static bool Build(World w, Character c, Job j)
    {
        Blueprint? bp = BlueprintById(w, j.TargetId);
        if (bp == null) return true;
        BuildingDef def = Def(w, bp);
        switch (j.Stage)
        {
            case 0:
            {
                int lacking = Lacking(w, bp);
                if (lacking > 0)
                {
                    if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                    if (Stock.Count(w, def.Material.Item) < lacking) return true; // someone took it first
                    Commit(w, c, bp, def, lacking);
                }
                Point stand = StandTile(w, bp, def, c.Pos, Reach(w.Map, new[] { c.Pos }, Point.None));
                if (stand == Point.None) return true;
                JobRunner.Next(w, c, j, stand);
                return JobRunner.Failed(j);
            }
            case 1:
                if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
                Unload(w, c, bp);
                j.Stage = 2;
                return false;
            default:
            {
                if (Lacking(w, bp) > 0) return true;
                if (def.Kind == Roof && !Supported(w, bp.Area, def))
                {
                    bp.Blocked = BlockedReason.WaitingForWalls;
                    bp.BlockedRevision = w.Map.Revision;
                    return true;
                }
                c.Facing = TravelSystem.FacingFor(c.Pos, bp.Area.Center);
                if (!bp.Builders.Contains(c.Id)) bp.Builders.Add(c.Id);
                if (!bp.Contributors.Contains(c.Id)) bp.Contributors.Add(c.Id);
                if (bp.Progress < def.WorkFor(bp.Area)) bp.Progress++;
                if (bp.Progress < def.WorkFor(bp.Area)) return false;
                return Finish(w, c, bp, def);
            }
        }
    }

    /// <summary>Takes material from the stock for a plan: it is now carried, committed to the plan. Whoever it leaves without is passed over.</summary>
    private static void Commit(World w, Character c, Blueprint bp, BuildingDef def, int take)
    {
        List<Character> passed = PassedOver(w, bp, def, take, c.Id);
        int taken = Stock.Take(w, def.Material.Item, take);
        if (taken <= 0) return;
        c.Carrying = new Carry { DefId = def.Material.Item, Count = taken };
        if (passed.Count > 0) PassOver(w, c, bp, def, passed);
    }

    /// <summary>Puts what someone carries onto the plan's site, up to what it lacks; any excess lies on the site as a loose stack.</summary>
    private static void Unload(World w, Character c, Blueprint bp)
    {
        if (c.Carrying is not { } carry || carry.DefId != Def(w, bp).Material.Item) return;
        int put = Math.Min(carry.Count, Lacking(w, bp));
        bp.Delivered += put;
        if (put > 0 && !bp.Contributors.Contains(c.Id)) bp.Contributors.Add(c.Id);
        int left = carry.Count - put;
        c.Carrying = null;
        if (left > 0) Stock.Add(w, carry.DefId, left, c.Pos);
    }

    // ---- Passing over (build design 4.7) ----

    /// <summary>
    /// Who would be passed over if <paramref name="take"/> of a plan's material left the stock now for plan
    /// <paramref name="x"/>: those with a stake in another plan (an open roof promise: any roof plan or broken roof
    /// section; no bed, and none standing free for them tonight: any bed plan) that the stock could cover before and
    /// no longer can, unless x meets the same stake. Never the taker. In id order.
    /// </summary>
    public static List<Character> PassedOver(World w, Blueprint x, BuildingDef xDef, int take, int takerId)
    {
        var passed = new List<Character>();
        string item = xDef.Material.Item;
        int before = Stock.Count(w, item), after = before - take;
        if (take <= 0) return passed;
        if (xDef.Kind != Roof && Uncovered(BestNeed(w, x, item, Roof), before, after))
            foreach (Debt d in w.Debts)
                if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseRest && d.CreditorId != takerId
                    && w.Find(d.CreditorId) is { Status: CharStatus.Member } creditor && !passed.Contains(creditor))
                    passed.Add(creditor);
        if (xDef.Kind != Bed && Uncovered(BestNeed(w, x, item, Bed), before, after))
            foreach (Character m in w.Members())
                if (m.Id != takerId && m.Named && Places.Tires(w, m) && OwnedBed(w, m.Id) == null && !FreeBedFor(w, m) && !passed.Contains(m)) passed.Add(m);
        passed.Sort((a, b) => a.Id.CompareTo(b.Id));
        return passed;
    }

    /// <summary>
    /// Does an unclaimed bed stand where this person could lie down in it tonight (under a roof, for one who burns in
    /// daylight)? Then they are not counting on a bed plan: the last of the timber going elsewhere passes them by.
    /// </summary>
    public static bool FreeBedFor(World w, Character c)
    {
        bool burns = Places.BurnsInDaylight(w, c);
        foreach (Bed b in w.Map.Beds)
            if (b.OwnerId == 0 && (!burns || w.Map.Covered(b.Pos))) return true;
        return false;
    }

    private static bool Uncovered(int need, int before, int after) => need <= before && need > after;

    /// <summary>The least of an item that would still get someone a thing of this kind: the plan of that kind (other than x) lacking least, or for a roof a broken section's repair.</summary>
    private static int BestNeed(World w, Blueprint x, string item, string kind)
    {
        int best = int.MaxValue;
        foreach (Blueprint y in w.Blueprints)
        {
            if (y.Id == x.Id) continue;
            BuildingDef def = Def(w, y);
            if (def.Kind == kind && def.Material.Item == item) best = Math.Min(best, Lacking(w, y));
        }
        if (kind == Roof && item == "timber")
            foreach (RoofSection r in w.Map.Roof)
                if (!r.Sound && !r.Removed) best = Math.Min(best, w.Content.WorkById("repair_roof").TimberCost);
        return best;
    }

    /// <summary>
    /// "Sera used the last of the timber on a bed.": one event, its actors the taker and everyone passed over, its
    /// cause the plan. Each person passed over thinks less of the taker; the creditor of a roof promise remembers it
    /// as why they still have no roof, so a broken promise cites it.
    /// </summary>
    private static void PassOver(World w, Character taker, Blueprint x, BuildingDef def, List<Character> passed)
    {
        var actors = new List<int> { taker.Id };
        foreach (Character p in passed) actors.Add(p.Id);
        SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.used_last_materials", actors, new[] { x.PlanEvent }, null,
            EventArg.Char("a", taker.Id), EventArg.Item("item", def.Material.Item), EventArg.Key("one", def.AKey));
        x.PassedOverEvent = e.Id;
        foreach (Character p in passed)
        {
            MoodSystem.AddThought(w, p, "passed_over", taker.Id, e.Id);
            if (HoldsRoofPromise(w, p.Id)) p.LastRestEvent = e.Id;
        }
    }

    private static bool HoldsRoofPromise(World w, int creditorId)
    {
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseRest && d.CreditorId == creditorId) return true;
        return false;
    }

    /// <summary>The event that made the oldest open promise of a sound roof; 0 if none.</summary>
    public static long RoofPromise(World w)
    {
        foreach (Debt d in w.Debts)
            if (d.Status == DebtStatus.Open && d.Kind == DebtKind.PromiseRest) return d.FormedEvent;
        return 0;
    }

    // ---- Felling follows demand ----

    /// <summary>What plans still lack of an item (Places.TimberNeeded adds it to the broken roof and bargains).</summary>
    public static int Owed(World w, string item)
    {
        int n = 0;
        foreach (Blueprint bp in w.Blueprints)
            if (Def(w, bp).Material.Item == item) n += Lacking(w, bp);
        return n;
    }

    /// <summary>The plan event of the oldest plan short of an item, as why a tree is felled; 0 if none.</summary>
    public static long PlanShortOf(World w, string item)
    {
        foreach (Blueprint bp in w.Blueprints)
            if (Def(w, bp).Material.Item == item && Lacking(w, bp) > 0) return bp.PlanEvent;
        return 0;
    }

    // ---- Checks made at build time ----

    /// <summary>Every tile of a roof section is within its support range of a standing wall or door.</summary>
    public static bool Supported(World w, Rect area, BuildingDef def)
    {
        for (int y = area.Y; y < area.Y + area.H; y++)
            for (int x = area.X; x < area.X + area.W; x++)
                if (!NearWall(w, new Point(x, y), def.SupportRange, null, standingOnly: true)) return false;
        return true;
    }

    /// <summary>Someone active (a member or a hostile) or a body stands here.</summary>
    public static bool InTheWay(World w, Point p)
    {
        foreach (Character c in w.Characters)
            if ((c.IsActive || c.Status == CharStatus.Dead) && c.Pos == p) return true;
        return false;
    }

    /// <summary>
    /// Would a wall here shut people in? One flood fill from the hearth with the tile blocked, against one without:
    /// every member who could reach the hearth, and every stockpile (ADR-0028: there may be several, anywhere), must
    /// still be reachable.
    /// </summary>
    public static bool WouldShutIn(World w, Point wall)
    {
        Map m = w.Map;
        var seeds = new List<Point>();
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (dx != 0 || dy != 0) seeds.Add(new Point(m.Hearth.X + dx, m.Hearth.Y + dy));
        bool[] before = Reach(m, seeds, Point.None), after = Reach(m, seeds, wall);
        foreach (Character c in w.Members())
            if (m.InBounds(c.Pos) && before[m.Index(c.Pos)] && !after[m.Index(c.Pos)]) return true;
        foreach (Zone z in m.Zones)
        {
            if (!Zones.IsStores(w, z)) continue;
            bool stockBefore = false, stockAfter = false;
            foreach (Point p in Zones.Each(z.Tiles))
            {
                if (!m.InBounds(p)) continue;
                stockBefore |= before[m.Index(p)];
                stockAfter |= after[m.Index(p)];
            }
            if (stockBefore && !stockAfter) return true;
        }
        return false;
    }

    /// <summary>
    /// Every tile reachable from the seeds (passable ones), moving as people do: 8 ways, never cutting a corner.
    /// <paramref name="blocked"/> is treated as impassable too (Point.None for none).
    /// </summary>
    public static bool[] Reach(Map m, IReadOnlyList<Point> seeds, Point blocked)
    {
        var seen = new bool[m.Width * m.Height];
        bool Open(Point p) => m.Passable(p) && p != blocked;
        var queue = new Queue<Point>();
        foreach (Point s in seeds)
            if (Open(s) && !seen[m.Index(s)]) { seen[m.Index(s)] = true; queue.Enqueue(s); }
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var q = new Point(p.X + dx, p.Y + dy);
                    if (!Open(q) || seen[m.Index(q)]) continue;
                    if (dx != 0 && dy != 0 && (!Open(new Point(p.X + dx, p.Y)) || !Open(new Point(p.X, p.Y + dy)))) continue;
                    seen[m.Index(q)] = true;
                    queue.Enqueue(q);
                }
        }
        return seen;
    }
}
