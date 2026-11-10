using Return.Game;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>The Godot-free half of the Zones view (ADR-0028): the marking tool, the hint by the cursor, and the list's rows.</summary>
public class ZoneViewTests
{
    private static readonly ContentDb Content = TestContent.Db;

    public ZoneViewTests()
    {
        Ui.Init(Content.Loc);
    }

    private static Zone Start(World w) => w.Map.Zones.First(z => z.Kind == "stockpile");

    private static IReadOnlyList<Command> Drag(World w, ZoneTool tool, Point from, Point to, int newId = 0)
    {
        tool.Move(from);
        tool.Press(from);
        tool.Move(to);
        return tool.Release(w, Zones.Preview(w), () => newId, w.Tick);
    }

    [Fact]
    public void ADragMarksTheBoxItSpansAsANewZone()
    {
        World w = WorldGen.Create(Content, 1);
        var tool = new ZoneTool();
        tool.Start("stockpile");
        Rect ruins = w.Map.CoveredRuins;
        IReadOnlyList<Command> sent = Drag(w, tool, new Point(ruins.X + ruins.W - 1, ruins.Y + ruins.H - 1), new Point(ruins.X, ruins.Y), newId: -1);
        MarkZone mark = Assert.IsType<MarkZone>(Assert.Single(sent));
        Assert.Equal(-1, mark.ZoneId);
        Assert.Equal("stockpile", mark.Kind);
        Assert.Equal(Zones.FromRect(ruins), mark.Tiles);
        Assert.True(tool.Active);
        Assert.False(tool.Dragging);
        Assert.Empty(tool.Release(w, Zones.Preview(w), () => 0, w.Tick)); // nothing more until the next drag
    }

    [Fact]
    public void ADragStartingOnOrBesideAZoneOfTheKindJoinsIt()
    {
        World w = WorldGen.Create(Content, 1);
        var tool = new ZoneTool();
        tool.Start("stockpile");
        Rect r = w.Map.StartStockpile;
        IReadOnlyList<Command> sent = Drag(w, tool, new Point(r.X - 1, r.Y), new Point(r.X - 2, r.Y + 1));
        MarkZone mark = Assert.IsType<MarkZone>(Assert.Single(sent));
        Assert.Equal(Start(w).Id, mark.ZoneId);
        Assert.Equal(16, Zones.Count(mark.Tiles));

        IReadOnlyList<Command> apart = Drag(w, tool, new Point(r.X - 3, r.Y), new Point(r.X - 4, r.Y));
        Assert.Equal(0, Assert.IsType<MarkZone>(Assert.Single(apart)).ZoneId); // two tiles away: a new stockpile
    }

    [Fact]
    public void TilesAZoneCannotHoldAreLeftOutAndABoxOfNothingSendsNothing()
    {
        World w = WorldGen.Create(Content, 2);
        var tool = new ZoneTool();
        tool.Start("stockpile");
        Point h = w.Map.Hearth;
        MarkZone mark = Assert.IsType<MarkZone>(Assert.Single(Drag(w, tool, new Point(h.X - 1, h.Y), new Point(h.X + 1, h.Y))));
        Assert.False(Zones.Contains(mark.Tiles, h));
        Assert.Equal(2, Zones.Count(mark.Tiles));
        Assert.Empty(Drag(w, tool, h, h));
    }

    [Fact]
    public void UnmarkingClearsTheBoxFromEveryZoneItTouches()
    {
        World w = WorldGen.Create(Content, 3);
        var tool = new ZoneTool();
        tool.StartUnmark();
        Zone meeting = w.Map.Zones.First(z => z.Kind == "meeting_place");
        Rect bounds = Zones.Bounds(meeting.Tiles);
        IReadOnlyList<Command> sent = Drag(w, tool, new Point(bounds.X, bounds.Y), new Point(bounds.X + bounds.W - 1, bounds.Y));
        MarkZone mark = Assert.IsType<MarkZone>(Assert.Single(sent));
        Assert.Equal(meeting.Id, mark.ZoneId);
        Assert.Equal(Zones.Count(meeting.Tiles) - bounds.W, Zones.Count(mark.Tiles));

        Rect r = w.Map.StartStockpile;
        Assert.IsType<RemoveZone>(Assert.Single(Drag(w, tool, new Point(r.X, r.Y), new Point(r.X + r.W - 1, r.Y + r.H - 1))));
        tool.Stop();
        Assert.False(tool.Active);
    }

    [Fact]
    public void TheHintByTheCursorSaysWhatMattersForTheKind()
    {
        World w = WorldGen.Create(Content, 1);
        var tool = new ZoneTool();
        Assert.Equal("", ZoneText.Cursor(w, tool));
        tool.Start("stockpile");
        Rect ruins = w.Map.CoveredRuins;
        tool.Move(new Point(ruins.X, ruins.Y));
        tool.Press(new Point(ruins.X, ruins.Y));
        tool.Move(new Point(ruins.X + 2, ruins.Y + 2));
        Assert.Equal("Stockpile · 9 tiles · in the covered ruins · dry · out of the watch's sight", ZoneText.Cursor(w, tool));
        tool.Release(w, Zones.Preview(w), () => 0, w.Tick);
        tool.Move(new Point(w.Map.WatchPost.X + 1, w.Map.WatchPost.Y));
        Assert.Equal("Stockpile · 1 tile · by the watch post · open to the damp", ZoneText.Cursor(w, tool));
        tool.Move(w.Map.Hearth);
        Assert.Equal("Nothing can be marked here", ZoneText.Cursor(w, tool));
        tool.Start("meeting_place");
        tool.Move(new Point(w.Map.Hearth.X, w.Map.Hearth.Y + 2));
        Assert.Equal("Meeting place · 1 tile · by the hearth", ZoneText.Cursor(w, tool));
        tool.StartUnmark();
        Rect r = w.Map.StartStockpile;
        tool.Move(new Point(r.X, r.Y));
        tool.Press(new Point(r.X, r.Y));
        tool.Move(new Point(r.X + 1, r.Y));
        Assert.Equal("Unmark · 2 tiles", ZoneText.Cursor(w, tool));
    }

    [Fact]
    public void TheListSaysWhereEachZoneLiesAndWhyTheLastStockpileStays()
    {
        World w = WorldGen.Create(Content, 1);
        IReadOnlyList<ZoneState> zones = StateApi.Zones(w);
        Assert.Equal(new[] { "Stockpile by the hearth · 12 tiles", "Meeting place by the hearth · 24 tiles" }, zones.Select(ZoneText.Row));
        Assert.Equal("Another stockpile must keep berries first.", ZoneText.RemoveHint(w, zones[0], allowed: false));
        Assert.Equal("Take this zone away", ZoneText.RemoveHint(w, zones[1], allowed: true));
        Assert.False(Zones.CanRemove(w, w.Map.Zones, zones[0].Id));
        Assert.True(Zones.CanRemove(w, w.Map.Zones, zones[1].Id));
        Assert.False(Zones.CanKeep(w, w.Map.Zones, zones[0].Id, new[] { "berries" }));
    }

    [Fact]
    public void TheTileTooltipNamesTheZoneAndWhatItKeeps()
    {
        World w = WorldGen.Create(Content, 1);
        Point pile = w.Map.StartStockpile.Center;
        IReadOnlyList<string> lines = ViewText.TileLines(w, StateApi.Location(w, pile.X, pile.Y));
        Assert.Contains("Stockpile (berries, timber)", lines);
    }
}
