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
    public int SchemaVersion { get; set; } = 1;
    public long Tick { get; set; }
    public ulong Seed { get; set; }
    public int NextId { get; set; } = 1;
    public Rng Rng { get; set; } = new();
    public Map Map { get; set; } = new();
    public List<Character> Characters { get; set; } = new();
    public List<ItemStack> Items { get; set; } = new();
    public List<Grudge> Grudges { get; set; } = new();
    public List<Debt> Debts { get; set; } = new();
    public RationPolicy Policy { get; set; } = RationPolicy.Hungriest;
    public DirectorState Director { get; set; } = new();
    public WeatherState Weather { get; set; } = new();
    public List<Arrival> Arrivals { get; set; } = new();
    public EventLog Log { get; set; } = new();
    public List<Command> PendingCommands { get; set; } = new();
    public List<Command> CommandLog { get; set; } = new();
    public long NextCommandSeq { get; set; } = 1;
    public long PolicyEvent { get; set; }

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
