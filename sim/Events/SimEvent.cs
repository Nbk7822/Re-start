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
/// <c>n:</c> number.
/// </summary>
public sealed class EventArg
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";

    public static EventArg Char(string name, int characterId) => new() { Name = name, Value = "c:" + characterId.ToString(CultureInfo.InvariantCulture) };
    public static EventArg Item(string name, string itemId) => new() { Name = name, Value = "i:" + itemId };
    public static EventArg Key(string name, string locKey) => new() { Name = name, Value = "k:" + locKey };
    public static EventArg Num(string name, long n) => new() { Name = name, Value = "n:" + n.ToString(CultureInfo.InvariantCulture) };
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
