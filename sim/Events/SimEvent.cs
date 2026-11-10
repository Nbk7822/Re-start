using System.Globalization;

namespace Return.Sim.Events;

/// <summary>Which system produced an event. Stories need two or more distinct systems.</summary>
public enum SystemTag
{
    Needs,
    Mood,
    Social,
    Work,
    Travel,
    Incident,
    Combat,
    World,
    /// <summary>Character growth: wants forming, arc stages, trait changes (fixed decision 12, ADR-0021).</summary>
    Growth,
    /// <summary>Customs, faith and omens: a culture event sits between its cause and its consequence (ADR-0023).</summary>
    Culture,
    /// <summary>Who you are and Return by Death: waking here, coming back, the end of a lifetime (ADR-0030).</summary>
    Fate,
    /// <summary>Your one gift acting in the world: awakening, turning harm, a hidden store, a familiar face (ADR-0030).</summary>
    Gift,
}

/// <summary>Persistent state changes. An event that carries one can end a story (ADR-0006).</summary>
public enum EffectKind
{
    GrudgeFormed,
    GrudgeEnded,
    DebtFormed,
    DebtRepaid,
    DebtBroken,
    Death,
    Departure,
    Recruitment,

    // The four peoples (ADR-0022).
    /// <summary>What bound someone's nature is gone for good: a dryad's tree felled. A is bound, B whoever severed it, RefId the tree.</summary>
    BondSevered,
    /// <summary>A bargain struck, kept to its end, or broken. A is the bound party (the djinn), B the patron, RefId the contract.</summary>
    ContractFormed,
    ContractKept,
    ContractBroken,

    // The player (ADR-0030).
    /// <summary>Your gift reached a new stage through what you did with it. A is you, RefId the stage reached.</summary>
    GiftGrew,
}

/// <summary>A persistent effect. A is the primary character (holder, debtor, the dead), B the other, RefId the grudge or debt id.</summary>
public sealed class Effect
{
    public EffectKind Kind { get; set; }
    public int A { get; set; }
    public int B { get; set; }
    public int RefId { get; set; }
}

/// <summary>
/// A named argument for an event's text. Values are typed by prefix so text is resolved at display
/// time through the localisation table: <c>c:</c> character id, <c>i:</c> item id, <c>k:</c> loc key,
/// <c>n:</c> number, <c>h:</c> hour of the day.
/// </summary>
public sealed class EventArg
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";

    public static EventArg Char(string name, int characterId) => new() { Name = name, Value = "c:" + characterId.ToString(CultureInfo.InvariantCulture) };
    public static EventArg Item(string name, string itemId) => new() { Name = name, Value = "i:" + itemId };
    public static EventArg Key(string name, string locKey) => new() { Name = name, Value = "k:" + locKey };
    public static EventArg Num(string name, long n) => new() { Name = name, Value = "n:" + n.ToString(CultureInfo.InvariantCulture) };

    /// <summary>An hour of the day (0..23), written through the loc key time.hour so the format is the loc file's (ADR-0029).</summary>
    public static EventArg Hour(string name, int hour) => new() { Name = name, Value = "h:" + hour.ToString(CultureInfo.InvariantCulture) };
}

/// <summary>One thing that happened: when, from which system, to whom, because of what, changing what.</summary>
public sealed class SimEvent
{
    public long Id { get; set; }
    public long Tick { get; set; }
    public SystemTag System { get; set; }
    public string Key { get; set; } = "";
    public List<EventArg> Args { get; set; } = new();
    public List<int> Actors { get; set; } = new();
    public List<long> Causes { get; set; } = new();
    public List<Effect> Effects { get; set; } = new();
}
