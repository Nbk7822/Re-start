namespace Return.Sim.Core;

/// <summary>
/// PCG-XSH-RR 64/32 (O'Neill, pcg-random.org) with the reference seeding of pcg32_srandom_r.
/// State and increment are public properties so snapshots round-trip them exactly.
/// </summary>
public sealed class Pcg32
{
    private const ulong Multiplier = 6364136223846793005UL;

    public ulong State { get; set; }
    public ulong Inc { get; set; }

    public Pcg32()
    {
    }

    public Pcg32(ulong seed, ulong sequence)
    {
        unchecked
        {
            State = 0;
            Inc = (sequence << 1) | 1UL;
            NextUInt();
            State += seed;
            NextUInt();
        }
    }

    public uint NextUInt()
    {
        unchecked
        {
            ulong old = State;
            State = old * Multiplier + Inc;
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }
    }
}
