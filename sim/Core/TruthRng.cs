namespace Return.Sim.Core;

/// <summary>
/// Stateless, hash-derived randomness for world facts (ADR-0004).
/// <c>Value = Combine(Combine(Combine(WorldSeed, id), entity), index)</c> with <see cref="Mix64.Combine"/>.
/// No sequence and no locking: any truth can be asked for at any time, in any order, and the
/// answer never changes for a given seed, so it survives rewinds by construction.
/// </summary>
public sealed class TruthRng
{
    public ulong WorldSeed { get; set; }

    public ulong Value(TruthId id, ulong entity, uint index = 0) =>
        Mix64.Combine(Mix64.Combine(Mix64.Combine(WorldSeed, (ulong)id), entity), index);

    /// <summary>Uniform integer in [minInclusive, maxExclusive) via the high half of a 64x64 multiply.</summary>
    public int Range(TruthId id, ulong entity, uint index, int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), $"Empty range [{minInclusive}, {maxExclusive}) for truth {id}.");
        ulong span = (ulong)((long)maxExclusive - minInclusive);
        ulong high = Math.BigMul(Value(id, entity, index), span, out _);
        return (int)(minInclusive + (long)high);
    }

    public bool Chance(TruthId id, ulong entity, uint index, int permille) =>
        Range(id, entity, index, 0, 1000) < permille;
}
