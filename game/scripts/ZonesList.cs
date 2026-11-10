using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Systems;

namespace Return.Game;

/// <summary>
/// The Zones list on the bottom bar (ADR-0028): the kinds of zone to mark, unmarking, and the camp's
/// zones, each with a ✕ and, for a stockpile, what it keeps. Picking a kind starts the marking tool and
/// closes the list. A ✕ or toggle that would leave an item with nowhere to be kept is greyed out with
/// the reason. It shows what will be once queued and drafted choices apply, and says so while any wait.
/// </summary>
public sealed class ZonesList
{
    private readonly SimHost _host;
    private readonly VBoxContainer _rows;
    private readonly Action _close;
    private string _signature = "";
    private Label? _pending;

    public ZonesList(SimHost host, VBoxContainer rows, Action close)
    {
        _host = host;
        _rows = rows;
        _close = close;
    }

    public void Refresh()
    {
        World w = _host.World;
        IReadOnlyList<ZoneState> zones = StateApi.Zones(w, _host.Drafts.All);
        string signature = Signature(zones);
        if (signature != _signature)
        {
            _signature = signature;
            Rebuild(w, zones);
        }
        bool waiting = _host.Drafts.HasZoneDrafts;
        foreach (ZoneState z in zones) waiting |= z.Pending;
        if (_pending != null) _pending.Visible = waiting;
    }

    private static string Signature(IReadOnlyList<ZoneState> zones)
    {
        var s = new StringBuilder();
        foreach (ZoneState z in zones)
            s.Append(z.Id).Append(':').Append(z.Kind).Append(':').Append(z.Tiles).Append(':').Append(z.Place).Append(':')
                .Append(string.Join(",", z.ItemIds)).Append(z.Pending ? "*" : "").Append(';');
        return s.ToString();
    }

    private void Rebuild(World w, IReadOnlyList<ZoneState> zones)
    {
        foreach (Node n in _rows.GetChildren())
        {
            _rows.RemoveChild(n);
            n.QueueFree();
        }
        _rows.AddChild(Ui.MakeLabel(Ui.T("ui.zones.mark"), Ui.SmallFontSize, Ui.Dim));
        foreach (ZoneKindState k in StateApi.ZoneKinds(w))
        {
            string kind = k.Id;
            _rows.AddChild(Choice(k.Name, k.Hint, () => { _host.StartZoneTool(kind); _close(); }));
        }
        _rows.AddChild(Choice(Ui.T("ui.zones.unmark"), Ui.T("ui.zones.unmark_hint"), () => { _host.StartUnmark(); _close(); }));
        _rows.AddChild(new HSeparator());

        _rows.AddChild(Ui.MakeLabel(Ui.T("ui.zones.yours"), Ui.SmallFontSize, Ui.Dim));
        List<Zone> shown = Zones.Preview(w, _host.Drafts.All);
        foreach (ZoneState z in zones)
        {
            int id = z.Id;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);
            Button name = Choice(ZoneText.Row(z), Ui.T("ui.zones.row_hint"), () => _host.FocusCamera(z.Bounds.Center));
            name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(name);
            bool removable = Zones.CanRemove(w, shown, id);
            Button remove = Ui.MakeButton(Ui.T("ui.zones.remove"), ZoneText.RemoveHint(w, z, removable));
            remove.Disabled = !removable;
            remove.Pressed += () => _host.Order(new RemoveZone(_host.World.Tick, id));
            row.AddChild(remove);
            _rows.AddChild(row);
            if (w.Content.TryZone(z.Kind) is { Behaviour: Zones.Stores }) _rows.AddChild(Keeps(w, shown, z));
        }

        Label hint = Ui.MakeLabel(Ui.T("ui.zones.stop_hint"), Ui.SmallFontSize, Ui.Dim);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _rows.AddChild(hint);
        _pending = Ui.MakeLabel(Ui.T("ui.zones.pending"), Ui.SmallFontSize, Ui.Accent);
        _rows.AddChild(_pending);
    }

    /// <summary>"Keeps: [berries] [timber]": one toggle per item, greyed out where unticking would leave the item nowhere to go.</summary>
    private HBoxContainer Keeps(World w, IReadOnlyList<Zone> shown, ZoneState z)
    {
        var keeps = new HBoxContainer();
        keeps.AddThemeConstantOverride("separation", 4);
        keeps.AddChild(new Control { CustomMinimumSize = new Vector2(14, 0), MouseFilter = Control.MouseFilterEnum.Ignore });
        keeps.AddChild(Ui.MakeLabel(Ui.T("ui.zones.keeps"), Ui.SmallFontSize, Ui.Dim));
        int id = z.Id;
        foreach (ItemDef item in w.Content.Items)
        {
            bool kept = z.ItemIds.Contains(item.Id);
            var next = new List<string>(z.ItemIds);
            if (kept) next.Remove(item.Id);
            else next.Add(item.Id);
            next.Sort(StringComparer.Ordinal);
            bool allowed = Zones.CanKeep(w, shown, id, next);
            string name = Ui.T(item.NameKey);
            Button toggle = Ui.MakeButton(kept ? Ui.F("ui.zones.keeps_item", ("item", name)) : name, allowed ? "" : ZoneText.NeedsAnother(w, item.Id));
            toggle.ToggleMode = true;
            toggle.SetPressedNoSignal(kept);
            toggle.Disabled = !allowed;
            toggle.Pressed += () => _host.Order(new SetZoneItems(_host.World.Tick, id, next));
            keeps.AddChild(toggle);
        }
        return keeps;
    }

    private static Button Choice(string text, string hint, Action pressed)
    {
        Button b = Ui.MakeButton(text, hint);
        b.Flat = true;
        b.Alignment = HorizontalAlignment.Left;
        b.Pressed += pressed;
        return b;
    }

    /// <summary>Presses the "Mark a place" entry for a kind through its signal (RETURN_PRESS checks); false if there is none.</summary>
    public bool PressKind(string kind)
    {
        Refresh();
        string? name = null;
        foreach (ZoneKindState k in StateApi.ZoneKinds(_host.World))
            if (k.Id == kind) name = k.Name;
        if (name == null) return false;
        foreach (Node n in _rows.GetChildren())
            if (n is Button b && b.Text == name)
            {
                b.EmitSignal(BaseButton.SignalName.Pressed);
                return true;
            }
        return false;
    }

    /// <summary>Presses "Unmark" through its signal (RETURN_PRESS checks).</summary>
    public bool PressUnmark()
    {
        Refresh();
        foreach (Node n in _rows.GetChildren())
            if (n is Button b && b.Text == Ui.T("ui.zones.unmark"))
            {
                b.EmitSignal(BaseButton.SignalName.Pressed);
                return true;
            }
        return false;
    }
}
