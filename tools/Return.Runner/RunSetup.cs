using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Runner;

/// <summary>
/// How a world is run (ADR-0030): your people and gift chosen for testing (--people, --gift) or rolled, and whether
/// --fates forces one death on a day from 10 to 35, hashed from the seed outside the sim's streams, so every world
/// shows a return. `run` and `soak` share it, so a seed's log is the same either way.
/// </summary>
public sealed record RunSetup(RebirthChoice? Choice = null, bool Fates = false)
{
    public static readonly RunSetup Plain = new();

    /// <summary>A world may take at most this many times its ticks in steps (a return sends it back); past that it is a death loop and the world fails.</summary>
    public const int StepsPerTickCap = 4;

    private const ulong FateSalt = 0x66617465UL; // "fate"

    /// <summary>The tick --fates kills you at in timeline 0: a day from 10 to 35 (as the log numbers them) and an hour, from a hash of the seed.</summary>
    public static long FateTick(ulong seed, WorldDef def)
    {
        ulong h = Mix64.Combine(seed, FateSalt);
        int day = 10 + (int)(h % 26UL);
        int hour = (int)(Mix64.Mix(h) % (ulong)def.HoursPerDay);
        return (long)(day - 1) * def.TicksPerDay + (long)hour * def.TicksPerHour;
    }
}
