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
/// flies by day, smoke from the sun-burnt. A dryad's wounds run amber sap that dries dark; the
/// undead and the djinn leave soft drifts of grave dust or ash instead of blood. Stylised, never
/// dismemberment.
/// </summary>
public static class Gore
{
    /// <summary>Blood darkens from fresh to dried over this many in-game hours.</summary>
    public const float DryHours = 16f;

    /// <summary>Ground decals are flattened this much for the three-quarter view.</summary>
    private const float Squash = 0.78f;

    private static readonly Color SapFresh = new(0.55f, 0.35f, 0.08f);
    private static readonly Color SapDry = new(0.27f, 0.17f, 0.06f);
    private static readonly Color GraveDust = new(0.60f, 0.58f, 0.52f);
    private static readonly Color DjinnAsh = new(0.15f, 0.14f, 0.13f);

    /// <summary>
    /// Every mark on the ground, drawn texture by texture (pools, then each splatter, the drops, the
    /// scorch) so consecutive quads share a texture and batch into a few draw calls.
    /// </summary>
    public static void DrawDecals(Brush b, ArtTextures tex, IReadOnlyList<Decal> decals, long tick, int ticksPerHour)
    {
        if (decals.Count == 0) return;
        Pass(b, tex, decals, tick, ticksPerHour, DecalKind.Pool, -1);
        Pass(b, tex, decals, tick, ticksPerHour, DecalKind.Scorch, -1);
        for (int v = 0; v < GoreLog.DustVariants; v++) Pass(b, tex, decals, tick, ticksPerHour, DecalKind.Dust, v);
        for (int v = 0; v < GoreLog.SplatterVariants; v++) Pass(b, tex, decals, tick, ticksPerHour, DecalKind.Splatter, v);
        for (int v = 0; v < GoreLog.DropVariants; v++) Pass(b, tex, decals, tick, ticksPerHour, DecalKind.Drops, v);
    }

    private static void Pass(Brush b, ArtTextures tex, IReadOnlyList<Decal> decals, long tick, int ticksPerHour, DecalKind kind, int variant)
    {
        const float T = MapView.TileSize;
        for (int i = 0; i < decals.Count; i++)
        {
            Decal d = decals[i];
            if (d.Kind != kind || (variant >= 0 && d.Variant != variant)) continue;
            float hours = Math.Max(0, tick - d.Tick) / (float)ticksPerHour;
            float fade = MathF.Pow(Math.Clamp(1f - d.Wear, 0f, 1f), 0.7f);
            float dried = Math.Clamp(hours / DryHours, 0f, 1f);
            Color blood = d.Ichor == Ichor.Sap ? SapFresh.Lerp(SapDry, dried) : Palette.BloodFresh.Lerp(Palette.BloodDry, dried);
            float x = d.X * T, y = d.Y * T;
            switch (kind)
            {
                case DecalKind.Splatter:
                    Draw(b, tex.Splats[d.Variant % tex.Splats.Length], x, y, T * 1.35f * d.Scale, d.Rotation, blood.WithAlpha(0.92f * fade), 0.22f);
                    break;
                case DecalKind.Drops:
                    Draw(b, tex.Drops[d.Variant % tex.Drops.Length], x, y, T * 0.9f * d.Scale, d.Rotation, blood.WithAlpha(0.9f * fade), 0.12f);
                    break;
                case DecalKind.Pool:
                {
                    float spread = Math.Min(1f, 0.3f + 0.7f * Math.Max(0, tick - d.Tick) / (float)GoreLog.PoolSpreadTicks);
                    Color pool = blood.Darkened(0.12f).WithAlpha(0.95f * (d.Lasting ? 1f : fade));
                    Draw(b, tex.Pool, x, y, T * 1.55f * d.Scale * spread, d.Rotation, pool, 0.36f);
                    break;
                }
                case DecalKind.Dust:
                {
                    // A soft drift that settles and greys a little as it ages; a heap under a body stays.
                    Color dust = d.Ichor == Ichor.Ash ? DjinnAsh : GraveDust.Darkened(0.12f * dried);
                    float a = (d.Ichor == Ichor.Ash ? 0.8f : 0.72f) * (d.Lasting ? 1f : fade);
                    Draw(b, tex.Blobs[d.Variant % tex.Blobs.Length], x, y, T * 1.05f * d.Scale, d.Rotation, dust.WithAlpha(a), 0.3f);
                    break;
                }
                case DecalKind.Scorch:
                {
                    float a = 0.9f * (d.Lasting ? 1f : fade);
                    if (tex.Scorch != null) Draw(b, tex.Scorch, x, y, T * 1.3f * d.Scale, d.Rotation, new Color(1f, 1f, 1f, a), 0.22f);
                    else Draw(b, null, x, y, T * 1.3f * d.Scale, d.Rotation, new Color(0.06f, 0.05f, 0.04f, a), 0.22f);
                    break;
                }
            }
        }
    }

    /// <summary>A ground decal; without its texture, a flat blot of the same tint.</summary>
    private static void Draw(Brush b, Texture2D? tex, float x, float y, float size, float rotation, Color tint, float fallbackRadius)
    {
        if (tint.A <= 0.01f) return;
        if (tex != null)
        {
            b.Decal(tex, x, y, size, rotation, Squash, tint);
            return;
        }
        float r = fallbackRadius * size;
        b.Clear().Ellipse(x, y, r, r * Squash, 14).Fill(tint);
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
