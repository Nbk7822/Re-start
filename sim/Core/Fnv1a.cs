using System.Text;

namespace Return.Sim.Core;

/// <summary>
/// FNV-1a 64-bit over UTF-8 bytes. The stable string hash for the sim: string.GetHashCode is
/// randomised per process and must never be used (ADR-0004).
/// </summary>
public static class Fnv1a
{
    public const ulong Offset = 0xcbf29ce484222325UL;
    public const ulong Prime = 0x100000001b3UL;

    public static ulong Hash64(string s)
    {
        ulong h = Offset;
        foreach (byte b in Encoding.UTF8.GetBytes(s))
        {
            h ^= b;
            h = unchecked(h * Prime);
        }
        return h;
    }
}
