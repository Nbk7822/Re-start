using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Decal = Return.Game.Logic.Decal;

namespace Return.Game.Art;

/// <summary>
/// Draws what the gore log remembers (ADR-0009 update): blood thrown where people were hurt, pools
/// that spread under the downed and the dead and then dry from wet red to brown-black, drops along
/// a wounded walker's path, scorch where a vampire burned; and the living details of a corpse:
/// flies by day, smoke from the sun-burnt. Stylised, never dismemberment.
/// </summary>
public static class Gore
{
    /// <summary>Blood darkens from fresh to dried over this many in-game hours.</summary>
    public const float DryHours = 16f;

    public static void DrawDecals(Brush b, ArtTextures tex, IReadOnlyList<Decal> decals, long tick, int ticksPerHour)
    {
        const float T = MapView.TileSize;
        for (int i = 0; i < decals.Count; i++)
        {
            Decal d = decals[i];
            float hours = Math.Max(0, tick - d.Tick) / (float)ticksPerHour;
            float fade = MathF.Pow(Math.Clamp(1f - d.Wear, 0f, 1f), 0.7f);
            Color blood = Palette.BloodFresh.Lerp(Palette.BloodDry, Math.Clamp(hours / DryHours, 0f, 1f));
            float x = d.X * T, y = d.Y * T;
            switch (d.Kind)
            {
                case DecalKind.Splatter:
                    Draw(b, tex.Splats[d.Variant % tex.Splats.Length], x, y, T * 1.35f * d.Scale, d.Rotation, blood.WithAlpha(0.92f * fade), FallbackSplat);
                    break;
                case DecalKind.Drops:
                    Draw(b, tex.Drops[d.Variant % tex.Drops.Length], x, y, T * 0.9f * d.Scale, d.Rotation, blood.WithAlpha(0.9f * fade), FallbackDrops);
                    break;
                case DecalKind.Pool:
                {
                    float spread = Math.Min(1f, 0.3f + 0.7f * Math.Max(0, tick - d.Tick) / (float)GoreLog.PoolSpreadTicks);
                    Color pool = blood.Darkened(0.12f).WithAlpha(0.95f * (d.Lasting ? 1f : fade));
                    Draw(b, tex.Pool, x, y, T * 1.55f * d.Scale * spread, d.Rotation, pool, FallbackPool);
                    break;
                }
                case DecalKind.Scorch:
                    Draw(b, tex.Scorch, x, y, T * 1.3f * d.Scale, d.Rotation, new Color(1f, 1f, 1f, 0.9f * (d.Lasting ? 1f : fade)), FallbackScorch);
                    break;
            }
        }
    }

    private enum Fallback
    {
        Splat,
        Drops,
        Pool,
        Scorch,
    }

    private const Fallback FallbackSplat = Fallback.Splat;
    private const Fallback FallbackDrops = Fallback.Drops;
    private const Fallback FallbackPool = Fallback.Pool;
    private const Fallback FallbackScorch = Fallback.Scorch;

    /// <summary>A ground decal, flattened a little for the three-quarter view.</summary>
    private static void Draw(Brush b, Texture2D? tex, float x, float y, float size, float rotation, Color tint, Fallback fallback)
    {
        if (tint.A <= 0.01f) return;
        b.Push();
        b.Translate(x, y);
        b.Scale(1f, 0.78f);
        b.Rotate(rotation);
        if (tex != null) b.Target.DrawTextureRect(tex, new Rect2(-size / 2f, -size / 2f, size, size), false, tint);
        else
        {
            float r = fallback switch { Fallback.Pool => 0.36f, Fallback.Drops => 0.12f, _ => 0.22f } * size;
            b.Clear().Ellipse(0f, 0f, r, r * 0.8f, 14).Fill(fallback == Fallback.Scorch ? new Color(0.06f, 0.05f, 0.04f, tint.A) : tint);
        }
        b.Pop();
    }

    /// <summary>Flies wheeling over a body (by day).</summary>
    public static void Flies(Brush b, float x, float y, float time, int seed, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float s = seed * 0.37f + i * 1.9f;
            float fx = x + MathF.Sin(time * (2.7f + i * 0.31f) + s) * 7f + MathF.Sin(time * 5.3f + s * 2f) * 2f;
            float fy = y - 5f + MathF.Sin(time * (3.9f + i * 0.23f) + s * 1.3f) * 3.5f;
            b.Dot(fx, fy, 0.75f, Palette.Fly);
            b.Dot(fx + 0.6f, fy - 0.5f, 0.45f, new Color(0.7f, 0.7f, 0.7f, 0.35f));
        }
    }

    /// <summary>Sparks lifting off someone burning in the sun.</summary>
    public static void Embers(Brush b, float x, float y, float time, int seed)
    {
        for (int i = 0; i < 5; i++)
        {
            float k = (time * 0.9f + i / 5f + seed * 0.11f) % 1f;
            float ex = x + MathF.Sin(i * 3.1f + time * 2f + seed) * 5f;
            float ey = y - k * 18f;
            b.Dot(ex, ey, 0.8f * (1f - k) + 0.3f, Palette.Ember.WithAlpha(0.9f * (1f - k)));
        }
    }
}
