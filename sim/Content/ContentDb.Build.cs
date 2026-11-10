namespace Return.Sim.Content;

/// <summary>The Build menu's content (ADR-0027): buildings.json, merged by id from mod folders like every other file.</summary>
public sealed partial class ContentDb
{
    private Dictionary<string, BuildingDef> _buildings = new();

    /// <summary>Every building, in file order (mods merged by id): the order the Build list shows them.</summary>
    public IReadOnlyList<BuildingDef> Buildings { get; private set; } = Array.Empty<BuildingDef>();

    public BuildingDef BuildingById(string id) => Find(_buildings, id, "buildings.json");
    public BuildingDef? TryBuilding(string id) => _buildings.TryGetValue(id, out BuildingDef? def) ? def : null;

    private void LoadBuildings(List<string> dirs)
    {
        Buildings = ReadMerged<BuildingDef>(dirs, "buildings.json", d => d.Id);
        _buildings = Index(Buildings, d => d.Id);
    }

    /// <summary>
    /// Every building names real items and terrain, has the fields its kind needs and its four loc forms
    /// (name, description, "a bed", "beds"), and has a kind the simulation has behaviour for; the ids the
    /// code relies on exist with the kind it expects.
    /// </summary>
    private void ValidateBuildings()
    {
        foreach (BuildingDef b in Buildings)
        {
            RequireLoc("buildings.json", b.Id, b.NameKey, b.DescKey, b.AKey, b.PluralKey);
            if (!RequiredContent.BuildingKinds.Contains(b.Kind))
                Fail("buildings.json", $"'{b.Id}' is of kind '{b.Kind}', which has no behaviour in this version of the simulation");
            if (!RequiredContent.BuildingCategories.Contains(b.Category))
                Fail("buildings.json", $"'{b.Id}' has category '{b.Category}'; use one of {string.Join(", ", RequiredContent.BuildingCategories)}");
            // One material per building until the items milestone (ADR-0027), as food is berries-only (ADR-0018).
            if (b.Materials.Count != 1)
                Fail("buildings.json", $"'{b.Id}' needs exactly one material in this version of the simulation (ADR-0027), not {b.Materials.Count}");
            foreach (MaterialDef m in b.Materials)
            {
                if (!_items.ContainsKey(m.Item)) Fail("buildings.json", $"'{b.Id}' is built from unknown item '{m.Item}'");
                if (m.Count < 1 || m.PerTiles < 0) Fail("buildings.json", $"'{b.Id}' needs a material count of at least 1 and perTiles of 0 or more");
            }
            if (b.WorkTicks < 1) Fail("buildings.json", $"'{b.Id}' needs workTicks of at least 1");
            if (b.TakeDownPct is < 0 or > 100) Fail("buildings.json", $"'{b.Id}' needs takeDownPct from 0 to 100");
            if (b.PlaceOn.Count == 0) Fail("buildings.json", $"'{b.Id}' needs at least one terrain in placeOn");
            foreach (string t in b.PlaceOn)
                if (!Systems.Building.PlaceableTerrain.Contains(t))
                    Fail("buildings.json", $"'{b.Id}' is placed on '{t}'; use {string.Join(", ", Systems.Building.PlaceableTerrain)}");
            if (b.Kind == Systems.Building.Roof && (b.MaxSide < 1 || b.SupportRange < 0))
                Fail("buildings.json", $"'{b.Id}' is a roof, so it needs maxSide of at least 1 and a supportRange");
            if (b.RestBonusPct is < 0 or > 100) Fail("buildings.json", $"'{b.Id}' needs restBonusPct from 0 to 100");
        }
        foreach ((string id, string kind) in RequiredContent.Buildings)
        {
            if (!_buildings.TryGetValue(id, out BuildingDef? b)) { Fail("buildings.json", $"the simulation needs a building '{id}'"); continue; }
            if (b.Kind != kind) Fail("buildings.json", $"the simulation needs '{id}' to be of kind '{kind}', not '{b.Kind}'");
        }
    }
}
