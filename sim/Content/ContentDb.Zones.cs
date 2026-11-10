namespace Return.Sim.Content;

/// <summary>Zone kinds (ADR-0028): loading, lookup and the checks made at load.</summary>
public sealed partial class ContentDb
{
    private Dictionary<string, ZoneDef> _zones = new();

    public IReadOnlyList<ZoneDef> Zones { get; private set; } = Array.Empty<ZoneDef>();

    public ZoneDef ZoneById(string id) => Find(_zones, id, "zones.json");
    public ZoneDef? TryZone(string id) => _zones.TryGetValue(id, out ZoneDef? def) ? def : null;

    private void LoadZones(List<string> dirs)
    {
        Zones = ReadMerged<ZoneDef>(dirs, "zones.json", d => d.Id);
        _zones = Index(Zones, d => d.Id);
    }

    /// <summary>Every kind's text exists, its behaviour is one this build implements on the layer that behaviour needs, and the kinds WorldGen lays out exist.</summary>
    private void ValidateZones()
    {
        foreach (ZoneDef z in Zones)
        {
            // The Chronicle names a zone mid-sentence ("You marked a stockpile …", "You moved the stockpile …").
            RequireLoc("zones.json", z.Id, z.NameKey, z.HintKey, Systems.Zones.ArticleKey(z.Id, indefinite: true), Systems.Zones.ArticleKey(z.Id, indefinite: false));
            string? layer = null;
            foreach ((string behaviour, string onLayer) in RequiredContent.ZoneBehaviours)
                if (behaviour == z.Behaviour) layer = onLayer;
            if (layer == null) Fail("zones.json", $"'{z.Id}' has behaviour '{z.Behaviour}', which has no behaviour in this version of the simulation");
            else if (z.Layer != layer) Fail("zones.json", $"'{z.Id}' has behaviour '{z.Behaviour}', which belongs on the '{layer}' layer, not '{z.Layer}'");
        }
        foreach (string id in RequiredContent.Zones)
            if (!_zones.ContainsKey(id)) Fail("zones.json", $"the simulation needs a zone kind '{id}'");
        foreach (string id in RequiredContent.ZoneThoughts)
            if (!_thoughts.ContainsKey(id)) Fail("thoughts.json", $"the simulation needs a thought '{id}'");
    }
}
