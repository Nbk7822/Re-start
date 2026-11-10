using System.Text.Json.Serialization;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Modding;

namespace Return.Sim;

/// <summary>
/// The whole simulation state. Everything here is serialised into snapshots; lists are kept in id
/// order so iteration is deterministic. Content is re-attached on load, never serialised.
/// </summary>
public sealed class World
{
    /// <summary>Bump when the snapshot shape changes incompatibly.</summary>
    public int SchemaVersion { get; set; } = 3;
    public long Tick { get; set; }
    public ulong Seed { get; set; }
    public int NextId { get; set; } = 1;
    public Rng Rng { get; set; } = new();
    public Map Map { get; set; } = new();
    public List<Character> Characters { get; set; } = new();
    public List<ItemStack> Items { get; set; } = new();
    public List<Reservation> Reservations { get; set; } = new();
    public List<Grudge> Grudges { get; set; } = new();
    public List<Debt> Debts { get; set; } = new();
    /// <summary>Bargains struck with the camp (a djinn's), first-class like debts (ADR-0022).</summary>
    public List<Contract> Contracts { get; set; } = new();
    public RationPolicy Policy { get; set; } = RationPolicy.Hungriest;
    public DirectorState Director { get; set; } = new();
    public WeatherState Weather { get; set; } = new();
    public List<Arrival> Arrivals { get; set; } = new();
    public EventLog Log { get; set; } = new();
    public List<Command> PendingCommands { get; set; } = new();
    public List<Command> CommandLog { get; set; } = new();
    public long NextCommandSeq { get; set; } = 1;
    public long PolicyEvent { get; set; }
    public long WinterEvent { get; set; }

    /// <summary>The person from Earth the player is. Other otherworlders (rivals, later) are not the player.</summary>
    public int PlayerId { get; set; }

    /// <summary>This world's peoples, faiths and gods, and the running state of their customs (ADR-0023).</summary>
    public CultureBook Culture { get; set; } = new();

    // The Schedule (ADR-0029): one camp day per kind of day, the watch hours and the decision that set them, and the watch as kept now.
    public List<CampDay> CampDays { get; set; } = new();
    /// <summary>The watch hours: from inclusive, to exclusive, wrapping past midnight; equal means the camp keeps no watch.</summary>
    public int WatchFrom { get; set; }
    public int WatchTo { get; set; }
    public long WatchEvent { get; set; }
    public WatchState Watch { get; set; } = new();

    /// <summary>The night "would not keep watch outside the home area" was last said (once a night for the camp; zones step 3, ADR-0028); int.MinValue for never, since the small hours of day 0 are night −1.</summary>
    public int HomeWatchNight { get; set; } = int.MinValue;

    /// <summary>That line's event, which tonight's lines about refusals and an empty post cite with them.</summary>
    public long HomeWatchEvent { get; set; }

    [JsonIgnore]
    public ContentDb Content { get; set; } = null!;

    [JsonIgnore]
    public List<IModHook> Hooks { get; } = new();

    public int NewId() => NextId++;

    public Character Get(int id)
    {
        foreach (Character c in Characters)
            if (c.Id == id) return c;
        throw new KeyNotFoundException("No character with id " + id);
    }

    public Character? Find(int id)
    {
        foreach (Character c in Characters)
            if (c.Id == id) return c;
        return null;
    }

    public IEnumerable<Character> Members()
    {
        foreach (Character c in Characters)
            if (c.Status == CharStatus.Member) yield return c;
    }
}
