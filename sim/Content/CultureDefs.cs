namespace Return.Sim.Content;

// Culture and faith templates (ADR-0023). Read-only after load; a world seeds its own peoples,
// faiths and gods from these once, with Truths, so the same seed always gives the same ones.

/// <summary>The four ethics axes of fixed decision 5, each −2..+2. Positive is the first-named pole (Mortal, Arcane, Wild, Oathbound).</summary>
public sealed class EthicsDef
{
    public int MortalImmortal { get; init; }
    public int ArcaneMundane { get; init; }
    public int WildCivil { get; init; }
    public int OathboundFree { get; init; }
}

public sealed class RangeDef
{
    public int Min { get; init; }
    public int Max { get; init; }
}

public sealed class PeopleWeight
{
    /// <summary>A species id, or "*" for any species not listed.</summary>
    public string Species { get; init; } = "";
    public int Weight { get; init; }
}

public sealed class FaithWeight
{
    public string Faith { get; init; } = "";
    public int Weight { get; init; }
}

/// <summary>A custom a culture or faith may hold: always, or as one of a weighted pool drawn per world.</summary>
public sealed class CustomRef
{
    public string Custom { get; init; } = "";
    public bool Always { get; init; }
    public int Weight { get; init; }
}

public sealed class AttitudeDef
{
    public string Species { get; init; } = "";
    /// <summary>revere, wary, fear or scorn.</summary>
    public string Stance { get; init; } = "";
}

public sealed class CultureDef
{
    public string Id { get; init; } = "";
    public List<string> NameKeys { get; init; } = new();
    public List<PeopleWeight> Peoples { get; init; } = new();
    public EthicsDef Ethics { get; init; } = new();
    public List<FaithWeight> Faiths { get; init; } = new();
    public int OtherFaithPermille { get; init; }
    public RangeDef Devotion { get; init; } = new();
    public List<CustomRef> Customs { get; init; } = new();
    public int CustomPicks { get; init; }
    public List<AttitudeDef> Attitudes { get; init; } = new();
}

public sealed class DomainWeight
{
    public string Domain { get; init; } = "";
    public int Weight { get; init; }
}

/// <summary>One god per slot is seeded in every world: a name, a domain, an epithet and a temperament.</summary>
public sealed class GodSlotDef
{
    public List<string> NameKeys { get; init; } = new();
    public List<DomainWeight> Domains { get; init; } = new();
    public RangeDef Wrath { get; init; } = new();
    public RangeDef Grace { get; init; } = new();
}

public sealed class FaithDef
{
    public string Id { get; init; } = "";
    public List<string> NameKeys { get; init; } = new();
    public EthicsDef Ethics { get; init; } = new();
    public List<GodSlotDef> Gods { get; init; } = new();
    public List<CustomRef> Customs { get; init; } = new();
    public int CustomPicks { get; init; }
    /// <summary>The least devotion a follower needs to read signs as this faith's gods speaking.</summary>
    public int OmenDevotion { get; init; }
}

/// <summary>One rule with one hook. The kind is behaviour in code; its params are integers named in RequiredContent.CustomKinds.</summary>
public sealed class CustomDef
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public Dictionary<string, int> Params { get; init; } = new();

    public int Arg(string name) =>
        Params.TryGetValue(name, out int v) ? v : throw new ContentException($"customs.json: '{Id}' has no param '{name}'.");

    /// <summary>The custom's short name for lists and the story log.</summary>
    public string NameKey => "custom." + Id + ".name";

    /// <summary>The custom as a person's rule, for the inspector.</summary>
    public string RuleKey => "custom." + Id + ".rule";

    /// <summary>The loc key of one of this custom's event moments ("breach", "shared"...).</summary>
    public string EventKey(string moment) => "event.culture." + Id + "." + moment;
}

public sealed class SignDef
{
    /// <summary>rain, windfall, food_spoils, work_accident or death.</summary>
    public string Sign { get; init; } = "";
    /// <summary>anger or blessing.</summary>
    public string Reads { get; init; } = "";
}

public sealed class DomainDef
{
    public string Id { get; init; } = "";
    public List<string> EpithetKeys { get; init; } = new();
    public string CaresKey { get; init; } = "";
    public List<SignDef> Signs { get; init; } = new();
    public List<string> PleasedBy { get; init; } = new();
    public List<string> AngeredBy { get; init; } = new();
}
