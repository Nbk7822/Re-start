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
    public int SchemaVersion { get; set; } = 4; // 3: run memory, gift state, wounds and the Fate stream (ADR-0030); 4: plans, structures and the built camp (ADR-0027)
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
    /// <summary>Plans on the grid, in id order (ADR-0027).</summary>
    public List<Blueprint> Blueprints { get; set; } = new();
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

    /// <summary>Run memory: who you are, your timelines and what you carry across a death. A return does not restore it (ADR-0030).</summary>
    public FateBook Fate { get; set; } = new();

    /// <summary>Your gift's state in this timeline; it goes back with the world (ADR-0030).</summary>
    public GiftState Gift { get; set; } = new();

    /// <summary>Memories from lost futures applied in this world's history (ADR-0030).</summary>
    public List<AppliedMemory> Remembered { get; set; } = new();

    /// <summary>Standing orders you have given, ordinal by order id (ADR-0030).</summary>
    public List<StandingOrder> StandingOrders { get; set; } = new();

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
