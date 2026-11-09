using System;
using Return.Sim;
using SimPoint = Return.Sim.Core.Point;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game.Logic;

/// <summary>
/// How trodden each tile of open ground looks, 0 (untouched) to 1 (bare mud): packed earth and ash
/// around the hearth, mud at the shelter door and the ruins' mouth, the trampled stockpile and
/// watch post, the paths people wear between them, and the ground around the berry bushes. Derived
/// from the map's fixed camp features, so the same seed always shows the same paths. Walls, floors,
/// water and rock are 0 (they are not ground).
/// </summary>
public static class GroundWear
{
    public static float[] Compute(Map m)
    {
        var wear = new float[m.Width * m.Height];
        SimPoint door = DoorOf(m);
        SimPoint outside = new(door.X, door.Y + 1);
        SimRect ruins = m.CoveredRuins;
        SimPoint ruinsMouth = new(ruins.X + ruins.W / 2, ruins.Y + ruins.H);
        SimRect pile = m.Stockpile;
        SimPoint pileEdge = new(pile.X + pile.W, pile.Y + pile.H / 2);

        Spot(m, wear, m.Hearth, 2.9f, 0.95f);
        Spot(m, wear, outside, 1.9f, 0.85f);
        Spot(m, wear, ruinsMouth, 1.6f, 0.7f);
        Spot(m, wear, m.WatchPost, 1.6f, 0.65f);
        for (int y = pile.Y - 1; y <= pile.Y + pile.H; y++)
            for (int x = pile.X - 1; x <= pile.X + pile.W; x++)
            {
                bool inside = x >= pile.X && x < pile.X + pile.W && y >= pile.Y && y < pile.Y + pile.H;
                Raise(m, wear, new SimPoint(x, y), inside ? 0.7f : 0.4f);
            }
        foreach (Bush b in m.Bushes) Spot(m, wear, b.Pos, 1.5f, 0.35f);

        Path(m, wear, outside, m.Hearth, 0.75f, 1);
        Path(m, wear, m.Hearth, pileEdge, 0.7f, 2);
        Path(m, wear, m.Hearth, ruinsMouth, 0.6f, 3);
        Path(m, wear, m.Hearth, m.WatchPost, 0.55f, 4);
        return wear;
    }

    /// <summary>The shelter's door tile (its centre if the map has none).</summary>
    public static SimPoint DoorOf(Map m)
    {
        SimRect s = m.Shelter;
        for (int y = s.Y; y < s.Y + s.H; y++)
            for (int x = s.X; x < s.X + s.W; x++)
            {
                var p = new SimPoint(x, y);
                if (m.InBounds(p) && m.At(p) == TerrainKind.Door) return p;
            }
        return s.Center;
    }

    private static bool IsGround(Map m, SimPoint p) =>
        m.InBounds(p) && m.At(p) is TerrainKind.Grass or TerrainKind.Soil;

    private static void Raise(Map m, float[] wear, SimPoint p, float value)
    {
        if (!IsGround(m, p)) return;
        int i = m.Index(p);
        if (value > wear[i]) wear[i] = Math.Min(1f, value);
    }

    private static void Spot(Map m, float[] wear, SimPoint centre, float radius, float strength)
    {
        int r = (int)MathF.Ceiling(radius);
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > radius) continue;
                float falloff = 1f - d / (radius + 0.5f);
                var p = new SimPoint(centre.X + dx, centre.Y + dy);
                float jitter = 0.85f + 0.3f * ArtHash.Unit(p.X, p.Y, 11);
                Raise(m, wear, p, strength * falloff * jitter);
            }
    }

    /// <summary>A worn path from a to b: a line that wanders a little, its edges softer than its middle.</summary>
    private static void Path(Map m, float[] wear, SimPoint a, SimPoint b, float strength, int salt)
    {
        int steps = Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
        if (steps == 0) return;
        float nx = -(b.Y - a.Y) / (float)steps, ny = (b.X - a.X) / (float)steps;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float bend = MathF.Sin(t * MathF.PI) * (ArtHash.Signed(salt, 21) * 1.5f) + ArtHash.Signed(salt, i, 22) * 0.35f;
            int x = (int)MathF.Round(a.X + (b.X - a.X) * t + nx * bend);
            int y = (int)MathF.Round(a.Y + (b.Y - a.Y) * t + ny * bend);
            Raise(m, wear, new SimPoint(x, y), strength);
            Raise(m, wear, new SimPoint(x + 1, y), strength * 0.45f);
            Raise(m, wear, new SimPoint(x, y + 1), strength * 0.45f);
        }
    }
}
