using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Api;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game;

/// <summary>
/// The Build list (build design 3.2): the buildings the sim offers (StateApi.Buildings, so a mod's appear
/// too), grouped by category, each with its cost and, on hover, what it is (or, greyed out, why it cannot
/// be planned yet); then Take down and Call off plans, and what is in store. Picking anything closes the
/// list and puts that tool in hand; no panel stays open.
/// </summary>
public partial class BottomBar
{
    private ListPopup _build = null!;
    private readonly List<(string Id, Button Button)> _buildButtons = new();
    private readonly List<(string BuildingId, string ItemName, Label Label)> _buildStock = new();

    private void RefreshBuild()
    {
        World w = _host!.World;
        if (_buildButtons.Count == 0) MakeBuildRows(w);
        foreach (BuildingState b in StateApi.Buildings(w))
            foreach ((string id, Button button) in _buildButtons)
                if (id == b.Id)
                {
                    button.Disabled = !b.Available;
                    button.TooltipText = b.Available ? b.Description : b.Reason;
                    button.Modulate = b.Available ? Colors.White : new Color(1f, 1f, 1f, 0.45f);
                }
        foreach ((string buildingId, string item, Label label) in _buildStock)
            label.Text = Ui.F("ui.build.in_store", ("n", Ui.N(StateApi.CanPlace(w, buildingId, System.Array.Empty<SimRect>()).InStore)), ("item", item));
    }

    private void MakeBuildRows(World w)
    {
        IReadOnlyList<BuildingState> list = StateApi.Buildings(w);
        var categories = new List<string>();
        foreach (BuildingState b in list)
            if (!categories.Contains(b.Category)) categories.Add(b.Category);
        foreach (string category in categories)
        {
            _build.Rows.AddChild(Ui.MakeLabel(Ui.T("ui.build.category." + category), Ui.SmallFontSize, Ui.Dim));
            foreach (BuildingState b in list)
            {
                if (b.Category != category) continue;
                string id = b.Id;
                Button button = EntryButton(b.Name, b.Cost, b.Description);
                button.Pressed += () => PickBuilding(id);
                _build.Rows.AddChild(button);
                _buildButtons.Add((id, button));
            }
        }
        _build.Rows.AddChild(new HSeparator());
        var tools = new HBoxContainer();
        tools.AddThemeConstantOverride("separation", 6);
        Button takeDown = Ui.MakeButton(Ui.T("ui.build.take_down"), Ui.T("ui.build.take_down_hint"));
        takeDown.Pressed += () => PickTool(BuildMode.TakeDown);
        Button callOff = Ui.MakeButton(Ui.T("ui.build.call_off"), Ui.T("ui.build.call_off_hint"));
        callOff.Pressed += () => PickTool(BuildMode.CallOff);
        tools.AddChild(takeDown);
        tools.AddChild(callOff);
        _build.Rows.AddChild(tools);
        var items = new List<string>();
        foreach (BuildingState b in list)
        {
            string item = w.Content.BuildingById(b.Id).Material.Item;
            if (items.Contains(item)) continue;
            items.Add(item);
            Label label = Ui.MakeLabel("", Ui.SmallFontSize, Ui.Dim);
            _build.Rows.AddChild(label);
            _buildStock.Add((b.Id, w.Content.Loc.Get(w.Content.ItemById(item).NameKey), label));
        }
    }

    /// <summary>A flat button showing a building's name, with its cost at the right.</summary>
    private static Button EntryButton(string name, string cost, string tooltip)
    {
        Button b = Ui.MakeButton("", tooltip);
        b.Flat = true;
        b.CustomMinimumSize = new Vector2(0, 26);
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.SetAnchorsPreset(LayoutPreset.FullRect);
        row.OffsetLeft = 14;
        row.OffsetRight = -8;
        Label n = Ui.MakeLabel(name);
        n.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        n.VerticalAlignment = VerticalAlignment.Center;
        Label c = Ui.MakeLabel(cost, Ui.SmallFontSize, Ui.Dim);
        c.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(n);
        row.AddChild(c);
        b.AddChild(row);
        return b;
    }

    private void PickBuilding(string id)
    {
        _host?.StopZoneTool(); // one tool in hand at a time
        _host?.Build.Place(id);
        SetOpen(null);
    }

    private void PickTool(BuildMode mode)
    {
        _host?.StopZoneTool(); // one tool in hand at a time
        if (mode == BuildMode.TakeDown) _host?.Build.TakeDown();
        else _host?.Build.CallOff();
        SetOpen(null);
    }

    /// <summary>Picks a building in the Build list through its button's own signal (RETURN_PRESS checks); false if there is no such entry or it is greyed out.</summary>
    public bool PressBuilding(string id)
    {
        if (_host == null) return false;
        RefreshBuild();
        foreach ((string bid, Button button) in _buildButtons)
            if (bid == id && !button.Disabled)
            {
                button.EmitSignal(BaseButton.SignalName.Pressed);
                return true;
            }
        return false;
    }
}
