using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Game.Logic;

/// <summary>
/// The Zones list's and the marking tool's sentences (ADR-0028), from StateApi and loc templates.
/// Godot-free so the tests compile it.
/// </summary>
public static class ZoneText
{
    /// <summary>"Stockpile by the hearth · 12 tiles".</summary>
    public static string Row(ZoneState z) =>
        Ui.F(z.Tiles == 1 ? "ui.zones.row_one" : "ui.zones.row", ("name", z.Name), ("place", z.Place), ("n", Ui.N(z.Tiles)));

    /// <summary>The ✕'s tooltip: what it does, or why it is greyed out (the first item no other stockpile keeps).</summary>
    public static string RemoveHint(World w, ZoneState z, bool allowed)
    {
        if (allowed) return Ui.T("ui.zones.remove_hint");
        foreach (string id in z.ItemIds)
            if (!OtherKeeps(w, z.Id, id)) return Ui.F("ui.zones.needs_another", ("item", Ui.T(w.Content.ItemById(id).NameKey)));
        return Ui.T("ui.zones.remove_hint");
    }

    /// <summary>An item toggle's tooltip when it cannot be unticked.</summary>
    public static string NeedsAnother(World w, string itemId) => Ui.F("ui.zones.needs_another", ("item", Ui.T(w.Content.ItemById(itemId).NameKey)));

    /// <summary>The hint by the cursor while marking or unmarking: the zone, its tiles and place, and the facts that matter for its kind.</summary>
    public static string Cursor(World w, ZoneTool tool)
    {
        if (!tool.Active || (!tool.Dragging && tool.Hover == Point.None)) return "";
        Rect box = tool.Dragging ? tool.Box : new Rect(tool.Hover.X, tool.Hover.Y, 1, 1);
        if (tool.Unmarking)
        {
            int n = 0;
            foreach (Point p in ZoneTool.Tiles(box))
                if (Zones.UseZoneAt(w, p) != null) n++;
            return Ui.F(n == 1 ? "ui.zones.cursor.unmark_one" : "ui.zones.cursor.unmark", ("n", Ui.N(n)));
        }
        ZoneDef def = w.Content.ZoneById(tool.Kind!);
        ZoneTool.Facts facts = ZoneTool.FactsFor(w, def.Id, box);
        if (facts.Tiles == 0) return Ui.T("ui.zones.cursor.none");
        var parts = new List<string>
        {
            Ui.F(facts.Tiles == 1 ? "ui.zones.cursor.mark_one" : "ui.zones.cursor.mark",
                ("zone", Ui.T(def.NameKey)), ("n", Ui.N(facts.Tiles)), ("place", Ui.T(facts.PlaceKey))),
        };
        if (def.Behaviour == Zones.Stores)
        {
            parts.Add(Ui.T(facts.Dry ? "ui.zones.cursor.dry" : "ui.zones.cursor.open"));
            if (facts.OutOfSight) parts.Add(Ui.T("ui.zones.cursor.out_of_sight"));
        }
        return string.Join(Ui.T("ui.zones.cursor.sep"), parts);
    }

    private static bool OtherKeeps(World w, int zoneId, string itemId)
    {
        foreach (Zone z in Zones.Preview(w))
            if (z.Id != zoneId && Zones.Keeps(w, z, itemId)) return true;
        return false;
    }
}
