using System.Text.Json.Serialization;

namespace Return.Sim.Core;

/// <summary>A stateful PCG32 substream owned by one system. Every draw names its purpose.</summary>
public sealed class DiceStream
{
    private const int RecentSize = 64;
    private readonly DiceId[] _recent = new DiceId[RecentSize];
    private int _recentPos;

    public DiceSystem System { get; set; }
    public Pcg32 Gen { get; set; } = new();
    public long Draws { get; set; }

    /// <summary>The last 64 draw tags, oldest first. Debug aid for diffing runs; not part of state.</summary>
    [JsonIgnore]
    public IReadOnlyList<DiceId> Recent
    {
        get
        {
            int n = (int)Math.Min(Draws, RecentSize);
            var list = new List<DiceId>(n);
            for (int i = n; i > 0; i--)
                list.Add(_recent[(_recentPos - i + RecentSize) % RecentSize]);
            return list;
        }
    }

    /// <summary>Uniform integer in [minInclusive, maxExclusive) using Lemire's unbiased method.</summary>
    public int Range(DiceId draw, int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), $"Empty range [{minInclusive}, {maxExclusive}) for {System}/{draw}.");
        Note(draw);
        uint range = (uint)((long)maxExclusive - minInclusive);
        ulong m = (ulong)Gen.NextUInt() * range;
        uint low = (uint)m;
        if (low < range)
        {
            uint threshold = unchecked(0u - range) % range;
            while (low < threshold)
            {
                m = (ulong)Gen.NextUInt() * range;
                low = (uint)m;
            }
        }
        return (int)(minInclusive + (long)(m >> 32));
    }

    public bool Chance(DiceId draw, int permille) => Range(draw, 0, 1000) < permille;

    public T Pick<T>(DiceId draw, IReadOnlyList<T> items)
    {
        if (items.Count == 0)
            throw new ArgumentException($"Cannot pick from an empty list for {System}/{draw}.", nameof(items));
        return items[Range(draw, 0, items.Count)];
    }

    private void Note(DiceId draw)
    {
        Draws++;
        _recent[_recentPos] = draw;
        _recentPos = (_recentPos + 1) % RecentSize;
    }
}

/// <summary>Per-system Dice substreams keyed by (DiceSeed, system). Reseeded on rewind (ADR-0004).</summary>
public sealed class DiceRng
{
    public ulong DiceSeed { get; set; }
    public List<DiceStream> Streams { get; set; } = new();

    public DiceStream this[DiceSystem system] => Streams[(int)system];

    public static DiceRng FromSeed(ulong diceSeed)
    {
        var dice = new DiceRng { DiceSeed = diceSeed };
        dice.SeedStreams();
        return dice;
    }

    /// <summary>Derives a new DiceSeed from the old one and a salt, then reseeds every substream.</summary>
    public void Reseed(ulong salt)
    {
        DiceSeed = Mix64.Combine(DiceSeed, salt);
        SeedStreams();
    }

    private void SeedStreams()
    {
        DiceSystem[] systems = Enum.GetValues<DiceSystem>();
        var streams = new List<DiceStream>(systems.Length);
        for (int i = 0; i < systems.Length; i++)
        {
            if ((int)systems[i] != i)
                throw new InvalidOperationException($"DiceSystem values must be contiguous from 0; {systems[i]} is {(int)systems[i]}.");
            ulong key = Mix64.Combine(DiceSeed, (ulong)systems[i]);
            streams.Add(new DiceStream { System = systems[i], Gen = new Pcg32(key, Mix64.Mix(key ^ 0x5851F42D4C957F2DUL)) });
        }
        Streams = streams;
    }
}
