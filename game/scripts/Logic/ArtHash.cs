namespace Return.Game.Logic;

/// <summary>
/// Stable, stateless hashing for view-only variety: where a tuft of grass grows, how a blood
/// splatter is turned, which stains a wounded cloak shows. Keyed by things the sim already fixes
/// (tile, event id, character id, tick), so the same world always looks the same. It is never one
/// of the sim's RNG streams and never advances them (ADR-0004); the view reads sim state only.
/// </summary>
public static class ArtHash
{
    /// <summary>SplitMix64 finaliser.</summary>
    public static ulong Mix(ulong z)
    {
        unchecked
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>A hash of up to three keys; the order of the keys matters.</summary>
    public static ulong Of(long a, long b = 0, long c = 0)
    {
        unchecked
        {
            ulong h = Mix((ulong)a + 0x9E3779B97F4A7C15UL);
            h = Mix(h ^ ((ulong)b + 0xD1B54A32D192ED03UL));
            return Mix(h ^ ((ulong)c + 0x8CB92BA72F3D8DD7UL));
        }
    }

    /// <summary>A number in [0, 1) from a hash.</summary>
    public static float Unit(ulong h) => (h >> 40) / 16777216f;

    /// <summary>A number in [0, 1) from up to three keys.</summary>
    public static float Unit(long a, long b = 0, long c = 0) => Unit(Of(a, b, c));

    /// <summary>A number in [-1, 1) from up to three keys.</summary>
    public static float Signed(long a, long b = 0, long c = 0) => Unit(Of(a, b, c)) * 2f - 1f;

    /// <summary>A whole number in [0, n) from up to three keys (0 when n &lt;= 1).</summary>
    public static int Pick(int n, long a, long b = 0, long c = 0) => n <= 1 ? 0 : (int)(Of(a, b, c) % (ulong)n);
}
