namespace Return.Sim.Core;

/// <summary>SplitMix64 finaliser and a two-input combiner. Pure functions; the basis of stateless Truths.</summary>
public static class Mix64
{
    public static ulong Mix(ulong z)
    {
        unchecked
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public static ulong Combine(ulong a, ulong b) => Mix(a ^ Mix(unchecked(b + 0x9E3779B97F4A7C15UL)));
}
