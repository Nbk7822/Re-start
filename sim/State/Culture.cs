namespace Return.Sim;

/// <summary>
/// A world's peoples, faiths and gods as seeded from the templates (Truths, so the same seed gives
/// the same ones in every timeline), plus the little running state culture keeps: deeds the gods
/// care about, omens read, guest-bread offers decided, festivals under way (ADR-0023). Lists keep a
/// stable order (template, seeding or event order); there are no dictionaries in state.
/// </summary>
public sealed class CultureBook
{
    public List<SeededCulture> Cultures { get; set; } = new();
    public List<SeededFaith> Faiths { get; set; } = new();
    public List<FestivalDate> FestivalDates { get; set; } = new();
    public List<SacredPlace> SacredPlaces { get; set; } = new();
    public List<Deed> Deeds { get; set; } = new();
    public List<OmenRead> OmenReads { get; set; } = new();
    public List<GuestOffer> GuestOffers { get; set; } = new();
    public List<FestivalRun> Festivals { get; set; } = new();
}

/// <summary>A culture as this world has it: its name, its main faith and the customs it holds.</summary>
public sealed class SeededCulture
{
    public string TemplateId { get; set; } = "";
    public string NameKey { get; set; } = "";
    public string FaithId { get; set; } = "";
    public List<string> CustomIds { get; set; } = new();
}

public sealed class SeededFaith
{
    public string TemplateId { get; set; } = "";
    public string NameKey { get; set; } = "";
    public List<string> CustomIds { get; set; } = new();
    public List<SeededGod> Gods { get; set; } = new();
}

/// <summary>A god of a faith: name, domain, epithet, and a temperament from wrath and grace (0..100).</summary>
public sealed class SeededGod
{
    public int Slot { get; set; }
    public string NameKey { get; set; } = "";
    public string DomainId { get; set; } = "";
    public string EpithetKey { get; set; } = "";
    public int Wrath { get; set; }
    public int Grace { get; set; }
}

/// <summary>The day (0-based, of the custom's season) a festival held by a culture or a faith starts, at dusk.</summary>
public sealed class FestivalDate
{
    public string CustomId { get; set; } = "";
    public string HolderId { get; set; } = "";
    public bool HolderIsFaith { get; set; }
    public int Season { get; set; }
    public int Day { get; set; }
}

/// <summary>A tree held sacred by a culture or a faith; once felled it is desecrated for good.</summary>
public sealed class SacredPlace
{
    public int TreeId { get; set; }
    public string HolderId { get; set; } = "";
    public bool HolderIsFaith { get; set; }
    public string CustomId { get; set; } = "";
    public bool Desecrated { get; set; }
    public long DesecratedEvent { get; set; }
}

/// <summary>The closed set of deeds gods care about (domains.json names them in snake_case).</summary>
public enum DeedKind
{
    OathKept,
    OathBroken,
    DebtRepaid,
    MealTaken,
    MealShared,
    GuestHonoured,
    GuestWronged,
    BloodGiven,
    BloodRefused,
    TreeFelled,
    SacredHarmed,
}

/// <summary>Something someone did that a god cares about, remembered for a while so a later sign can be read as a verdict on it.</summary>
public sealed class Deed
{
    public DeedKind Kind { get; set; }
    public long EventId { get; set; }
    public int DoerId { get; set; }
    public long Tick { get; set; }
}

/// <summary>A follower read a sign as a verdict on a deed: they will not read that deed again, and wait a while before reading any.</summary>
public sealed class OmenRead
{
    public int ReaderId { get; set; }
    public long DeedEvent { get; set; }
    public long Tick { get; set; }
}

/// <summary>Whether a keeper of guest-right decided to give their own meal to a guest the camp promised one; decided once per host and promise.</summary>
public sealed class GuestOffer
{
    public int HostId { get; set; }
    public int DebtId { get; set; }
    public bool Accepted { get; set; }
}

/// <summary>A festival under way: who holds it, the event that began it, when it ends and who came.</summary>
public sealed class FestivalRun
{
    public string CustomId { get; set; } = "";
    public string HolderId { get; set; } = "";
    public bool HolderIsFaith { get; set; }
    public long StartEvent { get; set; }
    public long EndsTick { get; set; }
    public bool Feasted { get; set; }
    public List<int> Attendees { get; set; } = new();
}
