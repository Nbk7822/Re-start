using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// The Build menu (ADR-0027): the kinds of building the simulation has behaviour for, where they may
/// stand, and lookups over plans and built things. Placement rules, plans, the work of building and
/// beds live in the partial files beside this one. Nothing here draws a random number: plans are taken
/// in id order, ties broken by distance and then by (y, x).
/// </summary>
public static partial class Building
{
    public const string Wall = "wall";
    public const string Door = "door";
    public const string Roof = "roof";
    public const string Bed = "bed";

    /// <summary>The terrain a building may be placed on (buildings.json placeOn).</summary>
    public static readonly IReadOnlyList<string> PlaceableTerrain = new[] { "grass", "soil", "floor" };

    public static Bed? BedAt(World w, Point p)
    {
        foreach (Bed b in w.Map.Beds)
            if (b.Pos == p) return b;
        return null;
    }

    public static Bed? BedById(World w, int id)
    {
        foreach (Bed b in w.Map.Beds)
            if (b.Id == id) return b;
        return null;
    }

    /// <summary>A roof section over p that has not been taken down, sound or broken.</summary>
    public static RoofSection? StandingRoofOver(World w, Point p)
    {
        foreach (RoofSection r in w.Map.Roof)
            if (!r.Removed && r.Area.Contains(p)) return r;
        return null;
    }

    public static RoofSection? RoofById(World w, int id)
    {
        foreach (RoofSection r in w.Map.Roof)
            if (r.Id == id && !r.Removed) return r;
        return null;
    }

    public static Structure? StructureAt(World w, Point p)
    {
        foreach (Structure s in w.Map.Structures)
            if (s.Pos == p) return s;
        return null;
    }

    public static Structure? StructureById(World w, int id)
    {
        foreach (Structure s in w.Map.Structures)
            if (s.Id == id) return s;
        return null;
    }

    public static Blueprint? BlueprintById(World w, int id)
    {
        foreach (Blueprint bp in w.Blueprints)
            if (bp.Id == id) return bp;
        return null;
    }

    public static BuildingDef Def(World w, Blueprint bp) => w.Content.BuildingById(bp.DefId);

    /// <summary>The building a roof section is; the old camp's count as "roof".</summary>
    public static BuildingDef Def(World w, RoofSection r) => w.Content.BuildingById(r.DefId.Length > 0 ? r.DefId : Roof);

    /// <summary>The building a bed is; the old camp's bedrolls count as "bed".</summary>
    public static BuildingDef Def(World w, Bed b) => w.Content.BuildingById(b.DefId.Length > 0 ? b.DefId : Bed);

    /// <summary>What a plan still lacks of its material.</summary>
    public static int Lacking(World w, Blueprint bp) => Math.Max(0, Def(w, bp).Cost(bp.Area) - bp.Delivered);

    /// <summary>The bed someone owns, if any.</summary>
    public static Bed? OwnedBed(World w, int characterId)
    {
        if (characterId == 0) return null;
        foreach (Bed b in w.Map.Beds)
            if (b.OwnerId == characterId) return b;
        return null;
    }

    /// <summary>The first person who built a thing (whoever finished it); 0 for the old camp's.</summary>
    public static int Builder(List<int> builtBy) => builtBy.Count > 0 ? builtBy[0] : 0;

    /// <summary>
    /// A passable tile on a plan's site where its material can lie: the area's centre, else its first passable
    /// tile in (y, x) order, else a passable neighbour of the centre; Point.None if there is none.
    /// </summary>
    public static Point SiteTile(World w, Rect area)
    {
        Map m = w.Map;
        if (m.Passable(area.Center)) return area.Center;
        for (int y = area.Y; y < area.Y + area.H; y++)
            for (int x = area.X; x < area.X + area.W; x++)
            {
                var p = new Point(x, y);
                if (m.Passable(p)) return p;
            }
        return Places.Adjacent(w, area.Center, area.Center);
    }

    /// <summary>Is someone already doing this kind of job on this plan or built thing?</summary>
    internal static bool Targeted(World w, JobKind kind, TargetKind target, int id, int exceptId)
    {
        foreach (Character o in w.Characters)
            if (o.Id != exceptId && o.IsActive && o.Job is { } j && j.Kind == kind && j.TargetKind == target && j.TargetId == id) return true;
        return false;
    }
}
