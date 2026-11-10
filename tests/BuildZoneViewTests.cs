using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Where Build's and zones' choices meet before they apply (the merge review): the Build tool's checks see stockpiles drafted
/// or queued, and the marking tool's see plans drafted or queued, through one walk in the order the choices will go out
/// (<see cref="CampPreview"/>) and the checks the commands themselves make, so what the view shows is what the sim does.
/// </summary>
public class BuildZoneViewTests
{
    public BuildZoneViewTests()
    {
        Ui.Init(TestContent.Db.Loc);
    }

    private static Rect Tile(Point p) => new(p.X, p.Y, 1, 1);

    private static ZoneDef Stockpile(World w) => w.Content.ZoneById("stockpile");

    private static void Drag(ZoneTool tool, Point from, Point to)
    {
        tool.Move(from);
        tool.Press(from);
        tool.Move(to);
    }

    private static void Gesture(BuildTool tool, Point from, Point to)
    {
        tool.Hover(from);
        tool.Press(from);
        tool.Hover(to);
    }

    [Fact]
    public void AStockpileDraftedOverADraftedWallLeavesTheWallsTiles()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        Point yard = BuildPlanTests.Yard(w), next = new(yard.X + 1, yard.Y);
        var box = new Rect(yard.X - 1, yard.Y, 3, 3);
        Assert.Equal(9, ZoneTool.Valid(w, Stockpile(w), box).Count); // open ground before anything is planned

        var build = new BuildTool();
        build.Place("wall");
        Gesture(build, yard, next);
        Assert.True(build.Release(w, drafts, c => drafts.Add(c))); // two walls, drafted while paused
        build.Stop();

        var zones = new ZoneTool();
        zones.Start("stockpile");
        Drag(zones, new Point(box.X, box.Y), new Point(box.X + 2, box.Y + 2));
        CampPreview seen = CampPreview.Of(w, drafts.All);
        Assert.Equal("zone_check.planned", seen.ZoneCheck(Stockpile(w), yard)); // the drag draws a cross where the wall will stand
        Assert.Null(seen.ZoneCheck(Stockpile(w), new Point(yard.X, yard.Y + 1)));
        Assert.StartsWith("Stockpile · 7 tiles", ZoneText.Cursor(w, zones, seen)); // the hint counts what the release marks
        MarkZone mark = Assert.IsType<MarkZone>(Assert.Single(zones.Release(w, seen, drafts.NewZoneId, w.Tick)));
        Assert.Equal(7, Zones.Count(mark.Tiles));
        Assert.False(Zones.Contains(mark.Tiles, yard) || Zones.Contains(mark.Tiles, next));
        drafts.Add(mark);
        Zone shown = Zones.Preview(w, drafts.All).Single(z => z.Id == mark.ZoneId);

        drafts.Flush(sim);
        sim.Step();
        Assert.Equal(shown.Tiles, w.Map.Zones.Last(z => z.Kind == "stockpile").Tiles);
        Assert.Equal(new[] { Tile(yard), Tile(next) }, w.Blueprints.Select(b => b.Area));
    }

    [Fact]
    public void AWallDraftedOverADraftedStockpileIsRefusedOnItsTiles()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        Point yard = BuildPlanTests.Yard(w);
        var zones = new ZoneTool();
        zones.Start("stockpile");
        Drag(zones, yard, new Point(yard.X + 1, yard.Y + 1));
        foreach (Command c in zones.Release(w, CampPreview.Of(w, drafts.All), drafts.NewZoneId, w.Tick)) drafts.Add(c);
        zones.Stop();

        // A wall across the drafted stockpile: the two tiles off it can go; the two on it cannot, as the sim will find.
        var build = new BuildTool();
        build.Place("wall");
        Gesture(build, new Point(yard.X - 2, yard.Y), new Point(yard.X + 1, yard.Y));
        CanPlaceState check = BuildTool.Check(w, CampPreview.Of(w, drafts.All), "wall", build.Pieces(w));
        Assert.Equal(new[] { true, true, false, false }, check.Pieces.Select(p => p.Ok));
        Assert.Equal(Building.ReasonStockpile, check.Pieces[2].Tiles[0].ReasonKey);
        Assert.Contains(build.Hint(w, drafts, paused: true), l => l.Contains(w.Content.Loc.Get(Building.ReasonStockpile), StringComparison.Ordinal));
        Assert.True(build.Release(w, drafts, c => drafts.Add(c)));

        // One wholly on it cannot go at all, and leaves no draft.
        Gesture(build, new Point(yard.X, yard.Y + 1), new Point(yard.X, yard.Y + 1));
        Assert.False(build.Release(w, drafts, c => drafts.Add(c)));
        Assert.Equal(2, drafts.Count);

        drafts.Flush(sim);
        sim.Step();
        Assert.Equal(new[] { Tile(new Point(yard.X - 2, yard.Y)), Tile(new Point(yard.X - 1, yard.Y)) }, w.Blueprints.Select(b => b.Area));
        Assert.Equal(4, Zones.Count(w.Map.Zones.Last(z => z.Kind == "stockpile").Tiles));
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.world.plan_failed", StringComparison.Ordinal));
    }

    [Fact]
    public void ChoicesQueuedThisTickSeeEachOtherBothWays()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var none = new OrderDrafts(); // time runs: choices go straight out as commands for this tick
        void Order(Command c) => sim.Enqueue(c with { Tick = w.Tick });
        Point yard = BuildPlanTests.Yard(w), left = new(yard.X - 1, yard.Y), right = new(yard.X + 1, yard.Y);
        var build = new BuildTool();
        build.Place("wall");
        Gesture(build, yard, yard);
        Assert.True(build.Release(w, none, Order));

        var zones = new ZoneTool();
        zones.Start("stockpile");
        Drag(zones, left, right);
        CampPreview seen = CampPreview.Of(w, none.All);
        Assert.StartsWith("Stockpile · 2 tiles", ZoneText.Cursor(w, zones, seen));
        MarkZone mark = Assert.IsType<MarkZone>(Assert.Single(zones.Release(w, seen, () => 0, w.Tick)));
        Assert.Equal(new[] { left, right }, Zones.Each(mark.Tiles));
        Order(mark);

        // And the other way: a wall on the stockpile queued this tick cannot go.
        Gesture(build, left, left);
        Assert.Equal(Building.ReasonStockpile, BuildTool.Check(w, CampPreview.Of(w), "wall", build.Pieces(w)).Pieces.Single().Tiles.Single().ReasonKey);
        Assert.False(build.Release(w, none, Order));
        Assert.Equal(2, w.PendingCommands.Count);

        sim.Step();
        Assert.Equal(new[] { Tile(yard) }, w.Blueprints.Select(b => b.Area));
        Assert.Equal(new[] { left, right }, Zones.Each(w.Map.Zones.Last(z => z.Kind == "stockpile").Tiles));
    }

    /// <summary>A roof is overhead, so it may go over a stockpile drafted while paused, and keep it dry (lead's call at the merge).</summary>
    [Fact]
    public void ARoofMayGoOverADraftedStockpile()
    {
        var sim = Simulation.Create(TestContent.Db, 1);
        World w = sim.World;
        var drafts = new OrderDrafts();
        // A ring of walls in the yard, drafted, holds up a roof over its inside (as in BuildPlanTests).
        Point c = BuildPlanTests.Yard(w, 2, 1);
        var ring = new List<Rect>();
        for (int x = c.X - 2; x <= c.X + 2; x++) { ring.Add(new Rect(x, c.Y - 1, 1, 1)); ring.Add(new Rect(x, c.Y + 2, 1, 1)); }
        foreach (int y in new[] { c.Y, c.Y + 1 }) { ring.Add(new Rect(c.X - 2, y, 1, 1)); ring.Add(new Rect(c.X + 2, y, 1, 1)); }
        drafts.Add(new PlaceBlueprint(w.Tick, "wall", ring));
        var inside = new Rect(c.X - 1, c.Y, 3, 2);
        var zones = new ZoneTool();
        zones.Start("stockpile");
        Drag(zones, new Point(inside.X, inside.Y), new Point(inside.X + 2, inside.Y + 1));
        foreach (Command m in zones.Release(w, CampPreview.Of(w, drafts.All), drafts.NewZoneId, w.Tick)) drafts.Add(m);
        Assert.Equal(6, Zones.Count(Zones.Preview(w, drafts.All).Last(z => z.Kind == "stockpile").Tiles));

        var build = new BuildTool();
        build.Place("roof");
        Gesture(build, new Point(inside.X, inside.Y), new Point(inside.X + 2, inside.Y + 1));
        Assert.True(BuildTool.Check(w, CampPreview.Of(w, drafts.All), "roof", build.Pieces(w)).Ok);
        Assert.True(build.Release(w, drafts, m => drafts.Add(m)));

        drafts.Flush(sim);
        sim.Step();
        Assert.Contains(w.Blueprints, b => b.DefId == "roof" && b.Area == inside);
        Assert.Equal(6, Zones.Count(w.Map.Zones.Last(z => z.Kind == "stockpile").Tiles));
    }
}
