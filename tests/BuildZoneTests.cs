using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Where Build (ADR-0027) and zones (ADR-0028) meet, settled at their merge: nothing is built on a stockpile, wherever the
/// player has put it (a roof may go over one), and no stockpile or meeting place is marked over a wall, a door or a plan
/// for something that stands on the ground (a roof plan overhead is fine).
/// </summary>
public class BuildZoneTests
{
    private static Rect Tile(Point p) => new(p.X, p.Y, 1, 1);
    private static string Reason(World w, string building, Point p) => Building.Check(w, building, new[] { Tile(p) })[0].Reason;
    private static ZoneDef Stockpile(World w) => w.Content.ZoneById("stockpile");

    [Fact]
    public void NothingIsBuiltOnAStockpileWhereverItLiesButARoofMayGoOverIt()
    {
        World w = WorkTests.Only(1, "you");
        Point yard = BuildPlanTests.Yard(w);
        Assert.Equal("", Reason(w, "wall", yard));
        new MarkZone(w.Tick, 0, "stockpile", Zones.FromRect(new Rect(yard.X, yard.Y, 2, 2))).Apply(w);
        Assert.True(Zones.Accepts(w, yard, "berries"));
        foreach (string building in new[] { "wall", "door", "bed" })
            Assert.Equal(Building.ReasonStockpile, Reason(w, building, yard));
        Assert.NotEqual(Building.ReasonStockpile, Reason(w, "roof", yard)); // overhead: a roof keeps the stores dry
        Assert.Equal("a stockpile lies there", w.Content.Loc.Get(Building.ReasonStockpile));

        // The first stockpile taken away, its old ground is open again: only a stockpile's tiles refuse.
        Point old = new(w.Map.StartStockpile.X, w.Map.StartStockpile.Y);
        Zone first = w.Map.Zones.First(z => z.Kind == "stockpile" && Zones.Contains(z.Tiles, old));
        new RemoveZone(w.Tick, first.Id).Apply(w);
        Assert.Null(Zones.UseZoneAt(w, old));
        Assert.Equal("", Reason(w, "wall", old));
    }

    [Fact]
    public void NoStockpileIsMarkedOverAWallADoorOrAGroundPlanButOneMayLieUnderARoofPlan()
    {
        World w = WorkTests.Only(1, "you");
        Point bed = BuildPlanTests.Yard(w), door = BuildPlanTests.Yard(w, 1), roofed = BuildPlanTests.Yard(w, 2), open = BuildPlanTests.Yard(w, 3);
        Building.Place(w, "bed", new[] { Tile(bed) }, 0);
        Assert.Single(w.Blueprints, b => b.Area.Contains(bed));
        w.Map.Structures.Add(new Structure { Id = w.NewId(), DefId = "door", Pos = door, Ground = w.Map.At(door) });
        w.Map.Terrain[w.Map.Index(door)] = (byte)TerrainKind.Door;
        w.Map.Changed();
        w.Blueprints.Add(new Blueprint { Id = w.NewId(), DefId = "roof", Area = Tile(roofed) });

        Assert.Equal("zone_check.planned", Zones.Check(w, Stockpile(w), bed));
        Assert.Equal("zone_check.built", Zones.Check(w, Stockpile(w), door));
        Assert.Null(Zones.Check(w, Stockpile(w), roofed));
        Assert.Null(Zones.Check(w, Stockpile(w), open));
        Assert.Null(Zones.Check(w, w.Content.ZoneById("home"), door)); // the home area lies over anything
        Assert.Equal("A building is planned here", w.Content.Loc.Get("zone_check.planned"));
        Assert.Equal("Something is built here", w.Content.Loc.Get("zone_check.built"));

        // Marking a row across them keeps only the tiles that may hold it.
        new MarkZone(w.Tick, 0, "stockpile", Zones.FromRect(new Rect(bed.X, bed.Y, 4, 1))).Apply(w);
        Zone made = w.Map.Zones.Last(z => z.Kind == "stockpile");
        Assert.Equal(new[] { roofed, open }, Zones.Each(made.Tiles).ToArray());
    }
}
