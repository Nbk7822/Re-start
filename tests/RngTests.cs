using System.Text.Json;
using Return.Sim.Core;

namespace Return.Tests;

public class RngTests
{
    [Fact]
    public void Pcg32MatchesReference()
    {
        var g = new Pcg32(42, 54);
        uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
        foreach (uint e in expected) Assert.Equal(e, g.NextUInt());
    }

    [Fact]
    public void Fnv1aMatchesReference()
    {
        Assert.Equal(0xcbf29ce484222325UL, Fnv1a.Hash64(""));
        Assert.Equal(0xaf63dc4c8601ec8cUL, Fnv1a.Hash64("a"));
        Assert.Equal(0x85944171f73967e8UL, Fnv1a.Hash64("foobar"));
    }

    [Fact]
    public void TruthsAreOrderIndependent()
    {
        var a = new TruthRng { WorldSeed = 11 };
        var b = new TruthRng { WorldSeed = 11 };
        ulong a1 = a.Value(TruthId.Terrain, 1), a2 = a.Value(TruthId.TreePlacement, 2), a3 = a.Value(TruthId.Terrain, 3, 7);
        for (uint i = 0; i < 100; i++) b.Value(TruthId.Terrain, 1, i);
        ulong b3 = b.Value(TruthId.Terrain, 3, 7), b1 = b.Value(TruthId.Terrain, 1), b2 = b.Value(TruthId.TreePlacement, 2);
        Assert.Equal(a1, b1);
        Assert.Equal(a2, b2);
        Assert.Equal(a3, b3);
    }

    [Fact]
    public void TruthsDifferByInput()
    {
        var t = new TruthRng { WorldSeed = 11 };
        ulong v = t.Value(TruthId.Terrain, 5, 0);
        Assert.NotEqual(v, new TruthRng { WorldSeed = 12 }.Value(TruthId.Terrain, 5, 0));
        Assert.NotEqual(v, t.Value(TruthId.TreePlacement, 5, 0));
        Assert.NotEqual(v, t.Value(TruthId.Terrain, 6, 0));
        Assert.NotEqual(v, t.Value(TruthId.Terrain, 5, 1));
    }

    [Fact]
    public void TruthRangeStaysInBounds()
    {
        var t = new TruthRng { WorldSeed = 3 };
        var seen = new bool[7];
        for (uint i = 0; i < 10_000; i++)
        {
            int r = t.Range(TruthId.Terrain, 9, i, -3, 4);
            Assert.InRange(r, -3, 3);
            seen[r + 3] = true;
        }
        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void DiceSubstreamsAreIndependent()
    {
        var r = Rng.FromSeed(5);
        for (int i = 0; i < 50; i++) r.Dice[DiceSystem.Social].Range(DiceId.ArrivalDay, 0, 1000);
        int combat = r.Dice[DiceSystem.Combat].Range(DiceId.ArrivalDay, 0, 1000);
        Assert.Equal(Rng.FromSeed(5).Dice[DiceSystem.Combat].Range(DiceId.ArrivalDay, 0, 1000), combat);
    }

    [Fact]
    public void DiceSameSeedSameSequence()
    {
        var a = Rng.FromSeed(7);
        var b = Rng.FromSeed(7);
        for (int i = 0; i < 1000; i++)
            Assert.Equal(a.Dice[DiceSystem.Work].Range(DiceId.ArrivalDay, 0, 1000), b.Dice[DiceSystem.Work].Range(DiceId.ArrivalDay, 0, 1000));
    }

    [Fact]
    public void DiceRangeStaysInBounds()
    {
        var d = Rng.FromSeed(8).Dice[DiceSystem.Work];
        var seen = new bool[7];
        for (int i = 0; i < 10_000; i++)
        {
            int r = d.Range(DiceId.ArrivalDay, -3, 4);
            Assert.InRange(r, -3, 3);
            seen[r + 3] = true;
        }
        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void ReseedChangesEverySubstreamDeterministically()
    {
        var a = Rng.FromSeed(7);
        var b = Rng.FromSeed(7);
        var plain = Rng.FromSeed(7);
        a.Dice.Reseed(99);
        b.Dice.Reseed(99);
        foreach (DiceSystem s in Enum.GetValues<DiceSystem>())
        {
            int va = a.Dice[s].Range(DiceId.ArrivalDay, 0, int.MaxValue);
            Assert.Equal(va, b.Dice[s].Range(DiceId.ArrivalDay, 0, int.MaxValue));
            Assert.NotEqual(va, plain.Dice[s].Range(DiceId.ArrivalDay, 0, int.MaxValue));
        }
        Assert.Equal(plain.Truths.Value(TruthId.Terrain, 1), a.Truths.Value(TruthId.Terrain, 1));
    }

    [Fact]
    public void DiceStateRoundTripsThroughJson()
    {
        var r = Rng.FromSeed(21);
        for (int i = 0; i < 10; i++) r.Dice[DiceSystem.Needs].Range(DiceId.ArrivalDay, 0, 1000);
        Rng copy = JsonSerializer.Deserialize<Rng>(JsonSerializer.Serialize(r))!;
        Assert.Equal(r.Dice[DiceSystem.Needs].Draws, copy.Dice[DiceSystem.Needs].Draws);
        for (int i = 0; i < 10; i++)
            Assert.Equal(r.Dice[DiceSystem.Needs].Range(DiceId.ArrivalDay, 0, 1000), copy.Dice[DiceSystem.Needs].Range(DiceId.ArrivalDay, 0, 1000));
        Assert.Equal(r.Truths.Value(TruthId.Terrain, 4), copy.Truths.Value(TruthId.Terrain, 4));
    }

    [Fact]
    public void DrawsAreCounted()
    {
        var r = Rng.FromSeed(1);
        for (int i = 0; i < 3; i++) r.Dice[DiceSystem.Needs].Chance(DiceId.ArrivalDay, 500);
        Assert.Equal(3, r.Dice[DiceSystem.Needs].Draws);
        Assert.Equal(0, r.Dice[DiceSystem.Social].Draws);
    }
}
