using System;
using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Game.Logic;

/// <summary>
/// The marking tool for zones (ADR-0028), Godot-free so the tests compile it: a kind to mark (or
/// unmarking), the rectangle being dragged, the one change a release makes, and the facts the hint by
/// the cursor gives. MapView feeds it tiles and draws it; SimHost sends what it returns through Order,
/// so a choice made while paused is a draft like any other. The same checks as the commands
/// (Zones.Check) decide which tiles a zone can take.
/// </summary>
public sealed class ZoneTool
{
    /// <summary>The kind being marked, or null when unmarking or off.</summary>
    public string? Kind { get; private set; }

    public bool Unmarking { get; private set; }

    public bool Active => Kind != null || Unmarking;

    public bool Dragging { get; private set; }

    public Point DragStart { get; private set; } = Point.None;

    public Point DragEnd { get; private set; } = Point.None;

    /// <summary>The tile under the cursor, for the hint before a drag begins.</summary>
    public Point Hover { get; private set; } = Point.None;

    public void Start(string kind)
    {
        Kind = kind;
        Unmarking = false;
        Dragging = false;
    }

    public void StartUnmark()
    {
        Kind = null;
        Unmarking = true;
        Dragging = false;
    }

    public void Stop()
    {
        Kind = null;
        Unmarking = false;
        Dragging = false;
    }

    public void Press(Point tile)
    {
        if (!Active) return;
        Dragging = true;
        DragStart = tile;
        DragEnd = tile;
    }

    public void Move(Point tile)
    {
        Hover = tile;
        if (Dragging) DragEnd = tile;
    }

    /// <summary>The rectangle dragged so far (one tile before the mouse moves).</summary>
    public Rect Box => Span(DragStart, DragEnd);

    /// <summary>
    /// Ends the drag and returns the change it makes, as commands for <paramref name="tick"/>: one
    /// marking, or for unmarking one command per zone it touches. <paramref name="zones"/> is what the
    /// player sees (applied, queued and drafted); <paramref name="newZoneId"/> names a zone made now (0
    /// while time runs, a negative draft id while paused). Nothing when no tile could change.
    /// </summary>
    public IReadOnlyList<Command> Release(World w, IReadOnlyList<Zone> zones, Func<int> newZoneId, long tick)
    {
        if (!Dragging) return Array.Empty<Command>();
        Dragging = false;
        if (Unmarking) return UnmarkCommands(w, zones, Box, tick);
        return Kind == null ? Array.Empty<Command>() : MarkCommands(w, zones, Kind, Box, DragStart, newZoneId, tick);
    }

    public static Rect Span(Point a, Point b)
    {
        int x0 = Math.Min(a.X, b.X), y0 = Math.Min(a.Y, b.Y);
        return new Rect(x0, y0, Math.Abs(a.X - b.X) + 1, Math.Abs(a.Y - b.Y) + 1);
    }

    public static IEnumerable<Point> Tiles(Rect r)
    {
        for (int y = r.Y; y < r.Y + r.H; y++)
            for (int x = r.X; x < r.X + r.W; x++) yield return new Point(x, y);
    }

    /// <summary>The tiles of the box a zone of this kind may lie on.</summary>
    public static List<Point> Valid(World w, ZoneDef def, Rect box)
    {
        var ok = new List<Point>();
        foreach (Point p in Tiles(box))
            if (Zones.Check(w, def, p) == null) ok.Add(p);
        return ok;
    }

    /// <summary>
    /// Marking a box: if the drag started on or next to a zone of the same kind, the tiles join that
    /// zone; otherwise they make a new one (a single kind always joins its one zone).
    /// </summary>
    public static IReadOnlyList<Command> MarkCommands(World w, IReadOnlyList<Zone> zones, string kind, Rect box, Point start, Func<int> newZoneId, long tick)
    {
        ZoneDef? def = w.Content.TryZone(kind);
        if (def == null) return Array.Empty<Command>();
        List<Point> tiles = Valid(w, def, box);
        if (tiles.Count == 0) return Array.Empty<Command>();
        Zone? join = JoinTarget(zones, def, start);
        if (join == null) return new Command[] { new MarkZone(tick, newZoneId(), kind, Zones.Canonical(tiles)) };
        var all = new List<Point>(Zones.Each(join.Tiles));
        all.AddRange(tiles);
        return new Command[] { new MarkZone(tick, join.Id, kind, Zones.Canonical(all)) };
    }

    /// <summary>The zone of this kind the drag joins: one under or next to the tile it started on (lowest id first), or the single one of a single kind.</summary>
    public static Zone? JoinTarget(IReadOnlyList<Zone> zones, ZoneDef def, Point start)
    {
        foreach (Zone z in zones)
        {
            if (z.Kind != def.Id) continue;
            if (def.Single) return z;
            foreach (Point p in Zones.Each(z.Tiles))
                if (p.ChebyshevTo(start) <= 1) return z;
        }
        return null;
    }

    /// <summary>Unmarking a box: each zone it touches keeps the rest of its tiles, or goes when none are left.</summary>
    public static IReadOnlyList<Command> UnmarkCommands(World w, IReadOnlyList<Zone> zones, Rect box, long tick)
    {
        var commands = new List<Command>();
        foreach (Zone z in zones)
        {
            var keep = new List<Point>();
            bool touched = false;
            foreach (Point p in Zones.Each(z.Tiles))
            {
                if (box.Contains(p)) touched = true;
                else keep.Add(p);
            }
            if (!touched) continue;
            commands.Add(keep.Count == 0 ? new RemoveZone(tick, z.Id) : new MarkZone(tick, z.Id, z.Kind, Zones.Canonical(keep)));
        }
        return commands;
    }

    /// <summary>What the hint by the cursor says about the box: how many tiles the zone would take, where, and the facts that matter for its kind.</summary>
    public readonly record struct Facts(int Tiles, string PlaceKey, bool Dry, bool OutOfSight);

    /// <summary>
    /// The facts for marking this box: the tiles a zone could take, the place they would lie, and for a
    /// stockpile whether it is dry (every tile under cover) and whether it lies out of the watch's sight
    /// (every tile beyond what the post sees).
    /// </summary>
    public static Facts FactsFor(World w, string kind, Rect box)
    {
        ZoneDef? def = w.Content.TryZone(kind);
        if (def == null) return new Facts(0, "place_phrase.open", false, false);
        List<Point> tiles = Valid(w, def, box);
        bool dry = tiles.Count > 0, unseen = tiles.Count > 0;
        foreach (Point p in tiles)
        {
            if (!w.Map.Covered(p)) dry = false;
            if (p.ChebyshevTo(w.Map.WatchPost) <= CombatSystem.SightRange) unseen = false;
        }
        return new Facts(tiles.Count, Zones.PlacePhraseKey(w, Zones.Canonical(tiles)), dry, unseen);
    }
}
