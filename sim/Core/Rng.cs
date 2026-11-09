namespace Return.Sim.Core;

/// <summary>
/// The two randomness sources (ADR-0004): stateless <see cref="Truths"/> for world facts and
/// stateful per-system <see cref="Dice"/> for events. Every draw names its source.
/// </summary>
public sealed class Rng
{
    private const ulong DiceSalt = 0x64696365UL; // "dice"

    public TruthRng Truths { get; set; } = new();
    public DiceRng Dice { get; set; } = new();

    public static Rng FromSeed(ulong worldSeed) => new()
    {
        Truths = new TruthRng { WorldSeed = worldSeed },
        Dice = DiceRng.FromSeed(Mix64.Combine(worldSeed, DiceSalt)),
    };
}
