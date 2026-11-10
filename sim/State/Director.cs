namespace Return.Sim;

public enum RationPolicy
{
    Hungriest,
    PromisesFirst,
    SelfFirst,
}

public sealed class DirectorState
{
    public int Pressure { get; set; }
    public long LastMajorTick { get; set; } = -1_000_000;
}

public sealed class WeatherState
{
    public bool Raining { get; set; }
    public long RainEndsTick { get; set; }
    public long RainEvent { get; set; }
}

/// <summary>A wanderer due to walk in at a given tick (scheduled from the Dice stream).</summary>
public sealed class Arrival
{
    public int CharacterId { get; set; }
    public long Tick { get; set; }
    public bool Done { get; set; }

    /// <summary>The days (0-based) this arrival's slot may fall on: a return re-draws who comes and when within them (ADR-0030).</summary>
    public int FirstDay { get; set; }
    public int LastDay { get; set; }

    /// <summary>A shuffled wanderer's slot (another wanderer may take it on a return), not someone's own season.</summary>
    public bool Shuffled { get; set; }
}
