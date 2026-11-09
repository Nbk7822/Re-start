using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using SimPoint = Return.Sim.Core.Point;
using SimRect = Return.Sim.Core.Rect;
using Tree = Return.Sim.Tree;

namespace Return.Game.Art;

/// <summary>
/// The camp and the land in the grounded three-quarter view, ported from the art direction
/// mockup (terrain, river with flowing marks, treeB, bushes with berries, roomB walls with a top
/// and a front face, roof sections and the broken one, hearth fire with smoke, stockpile goods)
/// and made grimier: grunge and grain over everything, mud where people walk, ash around the
/// fire, frost in winter. Ground is drawn once into its own layer (redrawn when the season turns);
/// water, grass, trees and fire move every frame. Shadows are drawn in a pass of their own before
/// anything that stands up. All positions are map pixels; everything goes through the Brush.
/// </summary>
public sealed class WorldArt
{
    public const float T = MapView.TileSize;

    /// <summary>Height of a wall in pixels (the front face).</summary>
    public const float WallH = T * 0.52f;

    /// <summary>Thickness of a wall band in pixels (walls run through the middle of their tiles).</summary>
    public const float WallThick = T * 0.38f;

    private readonly Map _m;
    private readonly ArtTextures _tex;
    private readonly float[] _wear;
    private readonly bool[] _wall;
    private readonly List<(int Y, int X0, int X1)> _waterRows = new();
    private readonly Vector2[] _tuftBase;
    private readonly float[] _tuftSize;
    private readonly Vector2[] _flowLines;
    private readonly Vector2[] _flowSeeds;
    private readonly Color[] _flowColors;

    // The river as one smooth-banked polygon (the sim's water tiles step; the drawn bank curves).
    private readonly Vector2[] _river = Array.Empty<Vector2>();
    private readonly Vector2[] _riverUv = Array.Empty<Vector2>();
    private readonly Color[] _riverTint = Array.Empty<Color>();
    private readonly Vector2[] _bank = Array.Empty<Vector2>();

    public WorldArt(Map m, ArtTextures tex)
    {
        _m = m;
        _tex = tex;
        _wear = GroundWear.Compute(m);
        _wall = new bool[m.Width * m.Height];
        for (int i = 0; i < _wall.Length; i++) _wall[i] = (TerrainKind)m.Terrain[i] == TerrainKind.Wall;

        for (int y = 0; y < m.Height; y++)
        {
            int x = 0;
            while (x < m.Width)
            {
                if (m.At(new SimPoint(x, y)) != TerrainKind.Water) { x++; continue; }
                int x0 = x;
                while (x < m.Width && m.At(new SimPoint(x, y)) == TerrainKind.Water) x++;
                _waterRows.Add((y, x0, x));
            }
        }

        // Bank x per row (the run of water that reaches the east edge), smoothed with Catmull-Rom.
        var bankX = new float[m.Height];
        bool river = m.Height > 1;
        for (int y = 0; y < m.Height && river; y++)
        {
            int x0 = -1;
            foreach ((int ry, int rx0, int rx1) in _waterRows)
                if (ry == y && rx1 == m.Width) x0 = rx0;
            if (x0 < 0) river = false;
            else bankX[y] = x0 * T + ArtHash.Signed(y, 12) * 2.5f;
        }
        if (river)
        {
            const int sub = 3;
            var pts = new List<Vector2>();
            Vector2 P(int y) => new(bankX[Math.Clamp(y, 0, m.Height - 1)], (y + 0.5f) * T);
            for (int y = -1; y < m.Height; y++)
                for (int s = 0; s < sub; s++)
                {
                    float t = s / (float)sub;
                    Vector2 p0 = P(y - 1), p1 = P(y), p2 = P(y + 1), p3 = P(y + 2);
                    float t2 = t * t, t3 = t2 * t;
                    pts.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            pts.Add(P(m.Height));
            _bank = pts.ToArray();
            pts.Add(new Vector2(m.Width * T + T, m.Height * T + T));
            pts.Add(new Vector2(m.Width * T + T, -T));
            _river = pts.ToArray();
            _riverUv = new Vector2[_river.Length];
            _riverTint = new Color[_river.Length];
        }

        var tufts = new List<(Vector2 At, float Size)>();
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                var p = new SimPoint(x, y);
                TerrainKind k = m.At(p);
                if (k is not (TerrainKind.Grass or TerrainKind.Soil)) continue;
                if (_wear[m.Index(p)] > 0.3f || m.TreeAt(p) != null || m.BushAt(p) != null || p.ChebyshevTo(m.Hearth) <= 1 || m.Stockpile.Contains(p)) continue;
                int n = k == TerrainKind.Grass ? 1 + ArtHash.Pick(3, x, y, 31) : ArtHash.Unit(x, y, 32) < 0.3f ? 1 : 0;
                for (int i = 0; i < n; i++)
                    tufts.Add((new Vector2((x + 0.15f + 0.7f * ArtHash.Unit(x, y, 40 + i)) * T, (y + 0.2f + 0.75f * ArtHash.Unit(x, y, 50 + i)) * T),
                        0.7f + 0.6f * ArtHash.Unit(x, y, 60 + i)));
            }
        _tuftBase = new Vector2[tufts.Count];
        _tuftSize = new float[tufts.Count];
        for (int i = 0; i < tufts.Count; i++) { _tuftBase[i] = tufts[i].At; _tuftSize[i] = tufts[i].Size; }

        int marks = _waterRows.Count == 0 ? 0 : Math.Min(140, _waterRows.Count * 3);
        _flowSeeds = new Vector2[marks];
        for (int i = 0; i < marks; i++)
        {
            var row = _waterRows[ArtHash.Pick(_waterRows.Count, i, 71)];
            _flowSeeds[i] = new Vector2(row.X0 + 0.6f + ArtHash.Unit(i, 72) * Math.Max(0.2f, row.X1 - row.X0 - 1f), ArtHash.Unit(i, 73) * m.Height);
        }
        _flowLines = new Vector2[marks * 4];
        _flowColors = new Color[marks * 2];
    }

    public float WearAt(SimPoint p) => _m.InBounds(p) ? _wear[_m.Index(p)] : 0f;

    private bool IsWall(int x, int y) => x >= 0 && y >= 0 && x < _m.Width && y < _m.Height && _wall[y * _m.Width + x];

    private TerrainKind KindAt(int x, int y) => _m.At(new SimPoint(Math.Clamp(x, 0, _m.Width - 1), Math.Clamp(y, 0, _m.Height - 1)));

    // ================= ground (static layer) =================

    public void DrawGround(Brush b, bool winter)
    {
        float mapW = _m.Width * T, mapH = _m.Height * T;
        SimRect shelter = _m.Shelter, ruins = _m.CoveredRuins;

        // Grass under everything open (soil, rock and the river are laid over it), floors indoors.
        for (int y = 0; y < _m.Height; y++)
            for (int x = 0; x < _m.Width; x++)
            {
                var p = new SimPoint(x, y);
                Color c = _m.At(p) == TerrainKind.Floor
                    ? (ruins.Contains(p) ? Palette.RuinFloor : Palette.Floor)
                    : Palette.Grass3.Lerp(Palette.Grass2, ArtHash.Unit(x, y, 1)).Lerp(Palette.GrassDry, ArtHash.Unit(x / 3, y / 3, 2) * 0.4f);
                if (winter && _m.At(p) != TerrainKind.Floor) c = c.Lerp(Palette.GrassWinter, 0.55f);
                b.Rect(x * T, y * T, T, T, c);
            }

        // Soft blobs, grouped by texture so they batch: soil (ragged edges), the wet bank, mud where people walk.
        for (int k = 0; k < 3; k++)
        {
            for (int y = 0; y < _m.Height; y++)
                for (int x = 0; x < _m.Width; x++)
                {
                    TerrainKind kind = KindAt(x, y);
                    float cx = (x + 0.5f) * T, cy = (y + 0.5f) * T;
                    if (kind is TerrainKind.Soil or TerrainKind.Rock)
                        Blob(b, k, cx + ArtHash.Signed(x, y, 3) * 4f, cy + ArtHash.Signed(x, y, 4) * 4f, T * 2.25f, ArtHash.Of(x, y, 3),
                            winter ? Palette.Soil.Lerp(Palette.GrassWinter, 0.25f) : Palette.Soil.Lerp(Palette.Soil2, ArtHash.Unit(x, y, 4)));
                }
            for (int y = 0; y < _m.Height; y++)
                for (int x = 0; x < _m.Width; x++)
                {
                    TerrainKind kind = KindAt(x, y);
                    float cx = (x + 0.5f) * T, cy = (y + 0.5f) * T;
                    if (kind is TerrainKind.Grass or TerrainKind.Soil && KindAt(x + 1, y) == TerrainKind.Water)
                        Blob(b, k, cx + T * 0.55f, cy, T * 1.9f, ArtHash.Of(x, y, 5), Palette.Bank.WithAlpha(0.85f));
                    float wear = _wear[y * _m.Width + x];
                    if (wear > 0.08f) Blob(b, k, cx, cy, T * (1.3f + wear * 0.8f), ArtHash.Of(x, y, 6), Palette.Mud.WithAlpha(Mathf.Clamp(wear * 0.95f, 0f, 0.9f)));
                    if (wear > 0.5f) Blob(b, k, cx + ArtHash.Signed(x, y, 7) * 4f, cy + ArtHash.Signed(x, y, 8) * 4f, T * 0.9f, ArtHash.Of(x, y, 9), Palette.MudWet.WithAlpha(0.45f));
                }
        }

        // Ash and soot around the hearth.
        Vector2 hearth = Center(_m.Hearth);
        Blob(b, -1, hearth.X, hearth.Y + 2f, T * 2.6f, 77, Palette.Ash.WithAlpha(0.75f));
        b.Texture(_tex.Stains[0], hearth.X, hearth.Y + 3f, T * 2.2f, T * 1.8f, 0.4f, new Color(0.05f, 0.04f, 0.03f, 0.7f));

        // Floors reach the middle of the wall tiles, where the walls stand: planks in the shelter, cracked flags in the ruins.
        Rect2 floor = TilesRect(shelter).Grow(-T * 0.5f);
        b.Rect(floor, Palette.Floor);
        b.TextureRegion(_tex.Planks, floor, new Rect2(0, 0, floor.Size.X * 2.2f, floor.Size.Y * 2.2f), new Color(0.78f, 0.74f, 0.7f));
        b.Rect(floor, new Color(0f, 0f, 0f, 0.12f));
        Rect2 ruinFloor = TilesRect(ruins);
        ruinFloor = new Rect2(ruinFloor.Position.X, ruinFloor.Position.Y - T * 0.5f, ruinFloor.Size.X, ruinFloor.Size.Y + T * 0.5f);
        b.TextureRegion(_tex.Stone, ruinFloor, new Rect2(0, 0, ruinFloor.Size.X * 2.6f, ruinFloor.Size.Y * 2.6f), new Color(0.52f, 0.54f, 0.5f));
        b.Rect(ruinFloor, new Color(0f, 0f, 0f, 0.28f));
        Vector2 seal = ruinFloor.GetCenter();
        var sealInk = new Color(0.2f, 0.27f, 0.23f, 0.8f);
        b.Clear().Add(seal.X, seal.Y - T * 0.8f).Add(seal.X + T * 0.8f, seal.Y).Add(seal.X, seal.Y + T * 0.8f).Add(seal.X - T * 0.8f, seal.Y).Stroke(sealInk, 1.2f);
        b.Clear().Ellipse(seal.X, seal.Y, T * 0.42f, T * 0.42f, 16).Stroke(sealInk, 1f);

        // Grime over the whole map: damp mottling, sun-bleached patches, grit and a faint grain.
        Overlay(b, _tex.GrungeDark, mapW, mapH, 1.0f, new Color(1f, 1f, 1f, 0.4f));
        Overlay(b, _tex.GrungeLight, mapW, mapH, 0.8f, new Color(1f, 1f, 1f, winter ? 0.2f : 0.1f));
        Overlay(b, _tex.Dirt, mapW, mapH, 2.2f, new Color(1f, 1f, 1f, 0.2f));
        Overlay(b, _tex.Grain, mapW, mapH, 1.6f, new Color(1f, 1f, 1f, 0.12f));
        if (winter) Overlay(b, _tex.Frost, mapW, mapH, 0.9f, new Color(1f, 1f, 1f, 0.26f));

        // The river over the grime: deep water with a smooth, dark, muddy bank.
        if (_river.Length > 0)
        {
            b.Polygon(_river, Palette.River);
            b.Polyline(_bank, Palette.RiverShallow.WithAlpha(0.8f), T * 0.9f);
            b.Polyline(_bank, Palette.Bank, 3f);
        }
        else
            foreach ((int y, int x0, int x1) in _waterRows) b.Rect(x0 * T, y * T, (x1 - x0) * T, T, Palette.River);

        // Rock outcrops, the stockpile's marked ground, bedrolls.
        for (int y = 0; y < _m.Height; y++)
            for (int x = 0; x < _m.Width; x++)
                if (KindAt(x, y) == TerrainKind.Rock) Boulder(b, x, y);
        DashedRect(b, TilesRect(_m.Stockpile).Grow(-2f), new Color(0.78f, 0.7f, 0.5f, 0.45f), 1.2f, 5f);
        foreach (Bed bed in _m.Beds) Bedroll(b, bed.Pos);
    }

    /// <summary>A faint square grid (ADR-0008), drawn over the ground every frame so it stays crisp at any zoom.</summary>
    public void DrawGrid(Brush b)
    {
        float mapW = _m.Width * T, mapH = _m.Height * T;
        var grid = new Color(0f, 0f, 0f, 0.13f);
        for (int x = 0; x <= _m.Width; x++) b.Line(x * T, 0f, x * T, mapH, grid, 1f);
        for (int y = 0; y <= _m.Height; y++) b.Line(0f, y * T, mapW, y * T, grid, 1f);
    }

    private static void Overlay(Brush b, Texture2D? tex, float w, float h, float texelsPerPixel, Color modulate) =>
        b.TextureRegion(tex, new Rect2(0, 0, w, h), new Rect2(0, 0, w * texelsPerPixel, h * texelsPerPixel), modulate);

    /// <summary>A soft blob tinted <paramref name="tint"/>, if its hash picks texture <paramref name="pass"/> (any texture when -1).</summary>
    private void Blob(Brush b, int pass, float x, float y, float size, ulong h, Color tint)
    {
        int k = (int)(h % 3);
        if (pass >= 0 && k != pass) return;
        Texture2D? tex = _tex.Blobs[k];
        if (tex == null)
        {
            b.Clear().Ellipse(x, y, size * 0.36f, size * 0.32f, 12).Fill(tint);
            return;
        }
        b.Texture(tex, x, y, size, size * (0.85f + 0.3f * ArtHash.Unit(h)), ArtHash.Unit(ArtHash.Mix(h)) * MathF.Tau, tint);
    }

    /// <summary>
    /// A rock tile: on a bed of grey scree, one boulder of its own size, shape, tilt and colour, and
    /// often a smaller stone beside it, so an outcrop reads as a jumble rather than a grid of eggs.
    /// </summary>
    private static void Boulder(Brush b, int x, int y)
    {
        float cx = (x + 0.5f) * T, cy = (y + 0.5f) * T;
        b.Clear().Ellipse(cx, cy + 2f, T * 0.78f, T * 0.64f, 14).Fill(Palette.StoneLo.Darkened(0.35f).WithAlpha(0.38f));
        Stone(b, cx + ArtHash.Signed(x, y, 15) * T * 0.14f, cy + ArtHash.Signed(x, y, 16) * T * 0.12f, T * (0.3f + 0.24f * ArtHash.Unit(x, y, 13)), x * 31 + y);
        if (ArtHash.Unit(x, y, 17) < 0.55f)
            Stone(b, cx + ArtHash.Signed(x, y, 18) * T * 0.36f, cy + T * (0.22f + 0.1f * ArtHash.Unit(x, y, 19)), T * (0.12f + 0.08f * ArtHash.Unit(x, y, 21)), x * 31 + y + 7);
    }

    private static void Stone(Brush b, float cx, float cy, float r, int salt)
    {
        float hgt = r * 0.65f;
        float tilt = ArtHash.Signed(salt, 22) * 0.4f;
        Color tone = Palette.Stone.Lerp(Palette.Hex(0x6a6354), ArtHash.Unit(salt, 23)).Darkened(ArtHash.Unit(salt, 24) * 0.15f);
        // Front face, then the lit top.
        b.Clear();
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + tilt;
            float k = 0.78f + 0.3f * ArtHash.Unit(salt, 30 + i);
            b.Add(cx + MathF.Cos(a) * r * k, cy + MathF.Sin(a) * r * 0.76f * k);
        }
        b.Offset(0f, 1.5f).Fill(tone.Darkened(0.45f));
        b.Offset(0f, -1.5f - hgt * 0.5f).Shade(tone.Lightened(0.18f), tone.Darkened(0.3f), Brush.LightDir).Stroke(Palette.Ink, 0.9f);
        b.Line(cx - r * 0.45f, cy - hgt * 0.45f, cx + r * 0.05f, cy - hgt * 0.15f + r * 0.2f, tone.Darkened(0.45f), 0.7f);
        if (ArtHash.Unit(salt, 25) < 0.5f) b.Clear().Ellipse(cx - r * 0.25f, cy - hgt * 0.9f, r * 0.32f, r * 0.16f, 10).Fill(Palette.Moss.WithAlpha(0.7f));
    }

    private static void Bedroll(Brush b, SimPoint p)
    {
        float x = p.X * T, y = p.Y * T;
        b.Clear().RoundRect(x + 4f, y + 2f, T - 8f, T - 3f, 3f).Lit(Palette.Straw, 0.2f).Stroke(Palette.Ink, 0.8f);
        b.Clear().RoundRect(x + 4.5f, y + 9f, T - 9f, T - 11f, 2.5f).Lit(Palette.Cloth, 0.2f).Stroke(Palette.InkSoft, 0.7f);
        b.Clear().RoundRect(x + 7f, y + 3.5f, T - 14f, 5f, 2f).Fill(Palette.Hex(0x9a8f78));
    }

    // ================= water and grass (every frame) =================

    public void DrawWater(Brush b, float time, bool raining)
    {
        float flow = time * T * (raining ? 1.3f : 0.8f);
        if (_tex.Water != null && _river.Length > 0)
        {
            // The surface scrolls downstream: two layers at different scales and speeds.
            float size = _tex.Water.GetWidth();
            Surface(b, 0.7f / size, flow, 0f, new Color(1f, 1f, 1f, 0.7f));
            Surface(b, 0.4f / size, flow * 0.6f, 0.37f, new Color(1f, 1f, 1f, 0.35f));
        }

        // Flowing marks drift downstream and fade in and out.
        int n = _flowSeeds.Length;
        float h = _m.Height;
        for (int i = 0; i < n; i++)
        {
            Vector2 s = _flowSeeds[i];
            float y = (s.Y + time * (0.9f + 0.3f * ArtHash.Unit(i, 74))) % h;
            float life = MathF.Sin((y / h) * MathF.PI * 7f + i) * 0.5f + 0.5f;
            float px = s.X * T, py = y * T, len = 5f + 4f * ArtHash.Unit(i, 75);
            _flowLines[i * 4] = new Vector2(px, py);
            _flowLines[i * 4 + 1] = new Vector2(px + len * 0.5f, py - 2f);
            _flowLines[i * 4 + 2] = new Vector2(px + len * 0.5f, py - 2f);
            _flowLines[i * 4 + 3] = new Vector2(px + len, py);
            Color c = (i % 4 == 0 ? Palette.Foam : new Color(0.33f, 0.45f, 0.48f, 0.7f)).WithAlpha(0.15f + 0.45f * life);
            _flowColors[i * 2] = c;
            _flowColors[i * 2 + 1] = c;
        }
        if (n > 0) b.Segments(_flowLines, _flowColors, Colors.White, 1f);
    }

    private void Surface(Brush b, float uvPerPixel, float flow, float offset, Color tint)
    {
        for (int i = 0; i < _river.Length; i++)
        {
            Vector2 p = _river[i];
            _riverUv[i] = new Vector2(p.X * uvPerPixel + offset, (p.Y - flow) * uvPerPixel + offset);
            _riverTint[i] = tint;
        }
        b.TexturedPolygon(_river, _riverTint, _riverUv, _tex.Water);
    }

    /// <summary>Tufts of three blades, stirring in the wind (thin triangles, batched).</summary>
    public void DrawGrass(Brush b, float time, bool winter, float wind)
    {
        Color dark = winter ? Palette.Hex(0x4b463a) : Palette.Hex(0x27331f);
        Color light = winter ? Palette.Hex(0x6d6553) : Palette.Hex(0x51613f);
        for (int i = 0; i < _tuftBase.Length; i++)
        {
            Vector2 p = _tuftBase[i];
            float s = _tuftSize[i];
            float sway = MathF.Sin(time * 1.6f + p.X * 0.03f + p.Y * 0.05f) * (0.9f + wind) * s;
            float hgt = 3.2f + 1.8f * s;
            Color c = (i % 3 == 0) ? light : dark;
            Color tip = c.Lerp(light, 0.45f);
            b.Blade(p + new Vector2(-0.5f, 0f), p + new Vector2(0.5f, 0f), p + new Vector2(sway - 1.6f, -hgt), c, tip);
            b.Blade(p + new Vector2(0.7f, 0f), p + new Vector2(1.7f, 0f), p + new Vector2(1.2f + sway * 1.2f, -hgt - 1.2f), c, tip);
            b.Blade(p + new Vector2(1.9f, 0f), p + new Vector2(2.9f, 0f), p + new Vector2(2.6f + sway + 1.2f, -hgt * 0.8f), c, tip);
        }
    }

    // ================= shadows (one pass, before anything that stands up) =================

    public void TreeShadow(Brush b, Tree t, Vector2 shadow, float alpha)
    {
        if (t.Felled) return;
        (float px, float py, float rad, _) = TreeShape(t);
        Figures.Shadow(b, _tex, px + 4f + shadow.X * 2.2f, py + 1f, rad * 1.05f + MathF.Abs(shadow.X) * 3f, rad * 0.4f, alpha * 1.1f);
    }

    public void BushShadow(Brush b, Bush bush, Vector2 shadow, float alpha)
    {
        float px = (bush.Pos.X + 0.5f) * T, py = (bush.Pos.Y + 0.5f) * T;
        Figures.Shadow(b, _tex, px + 2f + shadow.X, py + 5f, 11f, 4.5f, alpha);
    }

    public void StackShadow(Brush b, ItemStack s, float alpha)
    {
        float x = (s.Pos.X + 0.5f) * T, y = (s.Pos.Y + 0.62f) * T;
        Figures.Shadow(b, _tex, x + 1.5f, y + 2f, 10f, 4f, alpha);
    }

    public void HearthShadow(Brush b)
    {
        Vector2 c = HearthCenter;
        Figures.Shadow(b, _tex, c.X + 1f, c.Y + 3f, 12f, 6f, 0.3f);
    }

    public void PostShadow(Brush b, float alpha)
    {
        Vector2 c = Center(_m.WatchPost);
        Figures.Shadow(b, _tex, c.X + 1f, c.Y + T * 0.3f, 6f, 2.5f, alpha);
    }

    // ================= things that stand up (y-sorted) =================

    public static float TreeBaseY(Tree t) => (t.Pos.Y + 0.84f) * T;

    private static (float X, float Y, float Rad, bool Pine) TreeShape(Tree t)
    {
        int id = t.Id;
        float r = 1.15f + 0.45f * ArtHash.Unit(id, 81);
        return ((t.Pos.X + 0.5f) * T + ArtHash.Signed(id, 82) * 2f, TreeBaseY(t), r * T * 0.55f, ArtHash.Unit(id, 83) < 0.3f);
    }

    public void DrawTree(Brush b, Tree t, float time, bool winter)
    {
        int id = t.Id;
        (float px, float py, float rad, bool pine) = TreeShape(t);
        float sway = MathF.Sin(time * 0.9f + id * 1.3f) * 1.6f;

        // Trunk, flared at the root, lit from the left.
        float trunkTop = pine ? py - rad * 0.7f : py - rad * 1.05f;
        b.Clear().Add(px - 3.6f, py + 0.5f).Add(px - 2.4f, py - 2f).Add(px - 2.1f, trunkTop).Add(px + 2.1f, trunkTop).Add(px + 2.4f, py - 2f).Add(px + 3.8f, py + 0.5f)
            .Shade(Palette.Trunk.Lightened(0.08f), Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(Palette.Ink, 0.9f);

        if (pine)
        {
            Color needles = winter ? Palette.Pine.Lerp(Palette.Hex(0x51594f), 0.25f) : Palette.Pine;
            for (int tier = 0; tier < 3; tier++)
            {
                float w = rad * (1.0f - tier * 0.24f), top = py - rad * (1.15f + tier * 0.55f), baseY = top + rad * 0.95f;
                float sx = sway * (0.4f + tier * 0.3f);
                b.Clear().Add(px + sx, top - rad * 0.25f).Add(px + w + sx * 0.5f, baseY).Add(px + w * 0.3f, baseY - 2f).Add(px - w * 0.3f, baseY - 1f).Add(px - w + sx * 0.5f, baseY)
                    .Shade(needles.Lightened(0.12f), needles.Darkened(0.35f), Brush.LightDir).Stroke(Palette.Ink, 0.9f);
                if (winter) b.Line(px - w * 0.6f + sx, baseY - 2.5f, px - w * 0.1f + sx, baseY - 3.6f, new Color(0.8f, 0.84f, 0.86f, 0.55f), 1.2f);
            }
            return;
        }

        float cy = py - rad * 1.55f;
        if (winter)
        {
            // Bare branches: the broadleaves lose everything to the frost.
            for (int k = 0; k < 5; k++)
            {
                float a = -MathF.PI / 2f + (k - 2) * 0.45f + ArtHash.Signed(id, 90 + k) * 0.12f;
                float len = rad * (0.9f + 0.35f * ArtHash.Unit(id, 95 + k));
                float ex = px + MathF.Cos(a) * len + sway * 0.6f, ey = trunkTop + MathF.Sin(a) * len;
                b.Line(px, trunkTop + 2f, ex, ey, Palette.Branch, 1.6f);
                b.Line(ex, ey, ex + MathF.Cos(a - 0.6f) * len * 0.35f, ey + MathF.Sin(a - 0.6f) * len * 0.35f, Palette.Branch, 1f);
                b.Line((px + ex) / 2f, (trunkTop + ey) / 2f, (px + ex) / 2f + MathF.Cos(a + 0.7f) * len * 0.3f, (trunkTop + ey) / 2f + MathF.Sin(a + 0.7f) * len * 0.3f, Palette.Branch, 0.9f);
            }
            return;
        }

        Color dark = Palette.Canopy[0], mid = Palette.Canopy[1], hi = Palette.Canopy[2];
        CanopyBlob(b, px - rad * 0.45f + sway * 0.55f, cy + rad * 0.25f, rad * 0.62f, dark, true);
        CanopyBlob(b, px + rad * 0.45f + sway * 0.55f, cy + rad * 0.25f, rad * 0.6f, dark, true);
        CanopyBlob(b, px + sway * 0.8f, cy, rad * 0.78f, mid, true);
        CanopyBlob(b, px - rad * 0.2f + sway, cy - rad * 0.25f, rad * 0.55f, hi, false);
        b.Clear().Ellipse(px - rad * 0.3f + sway, cy - rad * 0.42f, rad * 0.3f, rad * 0.27f, 12).Fill(Palette.CanopyHi.WithAlpha(0.8f));
        // Dark gaps between leaf clusters.
        for (int k = 0; k < 3; k++)
            b.Dot(px + ArtHash.Signed(id, 100 + k) * rad * 0.5f + sway * 0.7f, cy + ArtHash.Signed(id, 110 + k) * rad * 0.35f, 1.2f, new Color(0.05f, 0.08f, 0.05f, 0.6f));
    }

    private static void CanopyBlob(Brush b, float x, float y, float r, Color c, bool outline)
    {
        b.Clear().Ellipse(x, y, r, r * 0.9f, 16).Shade(c.Lightened(0.1f), c.Darkened(0.32f), Brush.LightDir);
        if (outline) b.Stroke(Palette.Ink, 0.9f);
    }

    public static void DrawStump(Brush b, Tree t)
    {
        float px = (t.Pos.X + 0.5f) * T, py = (t.Pos.Y + 0.6f) * T;
        b.Clear().RoundRect(px - 4.5f, py - 4f, 9f, 5f, 2f).Shade(Palette.Trunk.Lightened(0.05f), Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(Palette.Ink, 0.8f);
        b.Clear().Ellipse(px, py - 4f, 4.5f, 2.2f, 12).Fill(Palette.LogEnd).Stroke(Palette.Ink, 0.7f);
        b.Clear().Ellipse(px, py - 4f, 2.2f, 1f, 10).Stroke(Palette.Log.Darkened(0.2f), 0.5f);
        b.Dot(px + 6f, py, 1.2f, Palette.LogEnd.Darkened(0.2f));
        b.Dot(px - 5f, py + 1.5f, 0.9f, Palette.LogEnd.Darkened(0.2f));
    }

    public static float BushBaseY(Bush bush) => (bush.Pos.Y + 0.72f) * T;

    public static void DrawBush(Brush b, Bush bush, float time, bool winter)
    {
        float px = (bush.Pos.X + 0.5f) * T, py = (bush.Pos.Y + 0.5f) * T;
        float sw = MathF.Sin(time * 1.4f + bush.Pos.X) * 0.7f;
        if (winter && bush.Berries == 0)
        {
            for (int k = 0; k < 6; k++)
            {
                float a = -MathF.PI / 2f + (k - 2.5f) * 0.42f;
                b.Line(px, py + 4f, px + MathF.Cos(a) * 9f + sw, py + 4f + MathF.Sin(a) * 9f, Palette.Branch.Lightened(0.1f), 1f);
            }
            return;
        }
        b.Clear().Ellipse(px + sw, py, 10.5f, 8.2f, 16).Shade(Palette.BushHi, Palette.Bush.Darkened(0.3f), Brush.LightDir).Stroke(Palette.Ink, 0.9f);
        b.Clear().Ellipse(px - 3f + sw, py - 2.5f, 5f, 3.6f, 12).Fill(Palette.BushHi.WithAlpha(0.7f));
        ReadOnlySpan<float> spots = stackalloc float[] { -4.5f, 1.5f, 3f, -2.2f, 5.2f, 3f, -0.8f, 4f, 0.2f, -4.6f, 6f, -1f };
        int berries = Math.Min(bush.Berries, 6);
        for (int i = 0; i < berries; i++)
        {
            float bx = px + spots[i * 2] + sw, by = py + spots[i * 2 + 1];
            b.Dot(bx, by, 1.6f, Palette.Ink);
            b.Dot(bx, by, 1.25f, Palette.Berry);
            b.Dot(bx - 0.4f, by - 0.4f, 0.45f, Palette.BerryHi);
        }
    }

    /// <summary>A wall or doorway at (x, y): walls run up to door frames.</summary>
    private bool Joins(int x, int y) =>
        x >= 0 && y >= 0 && x < _m.Width && y < _m.Height && (_wall[y * _m.Width + x] || (TerrainKind)_m.Terrain[y * _m.Width + x] == TerrainKind.Door);

    /// <summary>Where a wall tile's band meets the ground (its sort key): the band runs through the tile's centre.</summary>
    public static float WallSortY(SimPoint p) => (p.Y + 0.5f) * T + WallThick / 2f;

    public static float DoorSortY(SimPoint p) => WallSortY(p);

    /// <summary>
    /// A wall tile as a thin band through the tile's centre with arms to its wall neighbours (as the
    /// mockup's roomB): front faces of rough masonry where the band's south side shows, then the top
    /// raised by the wall height, inked on its outer edges only so a run of wall reads as one wall.
    /// </summary>
    public void DrawWall(Brush b, SimPoint p)
    {
        float x0 = p.X * T, cx = (p.X + 0.5f) * T, cy = (p.Y + 0.5f) * T, h = WallThick / 2f;
        bool n = Joins(p.X, p.Y - 1), s = Joins(p.X, p.Y + 1), w = Joins(p.X - 1, p.Y), e = Joins(p.X + 1, p.Y);
        float ground = cy + h, faceTop = ground - WallH;
        Color ink = Palette.Ink;

        // Front faces: the arms east and west, and the core unless the wall carries on south.
        float fl = w ? x0 : cx - h, fr = e ? x0 + T : cx + h;
        if (s && !w && !e) fl = fr = cx;
        if (fr > fl)
        {
            b.Rect(fl, faceTop, fr - fl, WallH, Palette.WallFace);
            Masonry(b, p, fl, faceTop, fr, ground);
            b.Rect(fl, ground - 3.5f, fr - fl, 3.5f, Palette.WallGrime.WithAlpha(0.7f));
            b.Rect(fl, faceTop, fr - fl, 2f, new Color(0f, 0f, 0f, 0.4f));
            b.Line(fl, ground, fr, ground, ink, 1f);
            if (!w) b.Line(fl, faceTop, fl, ground, ink, 1f);
            if (!e) b.Line(fr, faceTop, fr, ground, ink, 1f);
            if (s && w) b.Line(cx - h, faceTop, cx - h, ground, ink, 0.8f);
            if (s && e) b.Line(cx + h, faceTop, cx + h, ground, ink, 0.8f);
        }

        // The top of the band, raised by the wall height.
        float top = -WallH;
        Color topColor = Palette.WallTop.Darkened(ArtHash.Unit(p.X, p.Y, 120) * 0.12f);
        TopPiece(b, cx - h, cy - h + top, cx + h, cy + h + top, topColor);
        if (w) TopPiece(b, x0, cy - h + top, cx - h, cy + h + top, topColor);
        if (e) TopPiece(b, cx + h, cy - h + top, x0 + T, cy + h + top, topColor);
        if (n) TopPiece(b, cx - h, p.Y * T + top, cx + h, cy - h + top, topColor);
        if (s) TopPiece(b, cx - h, cy + h + top, cx + h, (p.Y + 1) * T + top, topColor);
        float yt = cy - h + top, yb = cy + h + top;
        // Long edges of the east-west arms and the north-south arms, then the core's open sides.
        if (w) { b.Line(x0, yt, cx - h, yt, ink, 1f); b.Line(x0, yb, cx - h, yb, ink, 0.8f); }
        if (e) { b.Line(cx + h, yt, x0 + T, yt, ink, 1f); b.Line(cx + h, yb, x0 + T, yb, ink, 0.8f); }
        if (n) { b.Line(cx - h, p.Y * T + top, cx - h, yt, ink, 1f); b.Line(cx + h, p.Y * T + top, cx + h, yt, ink, 1f); }
        if (s) { b.Line(cx - h, yb, cx - h, (p.Y + 1) * T + top, ink, 1f); b.Line(cx + h, yb, cx + h, (p.Y + 1) * T + top, ink, 1f); }
        if (!n) b.Line(cx - h, yt, cx + h, yt, ink, 1f);
        if (!s) b.Line(cx - h, yb, cx + h, yb, ink, 0.8f);
        if (!w) b.Line(cx - h, yt, cx - h, yb, ink, 1f);
        if (!e) b.Line(cx + h, yt, cx + h, yb, ink, 1f);
    }

    /// <summary>Rough blocks in two courses with dark mortar, each block a slightly different stone, a few cracks and stains.</summary>
    private static void Masonry(Brush b, SimPoint p, float l, float t, float r, float bottom)
    {
        float mid = (t + bottom) * 0.5f;
        Color mortar = Palette.WallGrime.WithAlpha(0.85f);
        for (int course = 0; course < 2; course++)
        {
            float y0 = course == 0 ? t + 1.5f : mid + 0.4f, y1 = course == 0 ? mid - 0.4f : bottom - 0.5f;
            float x = l - (course == 1 ? 4f : 0f) - ArtHash.Unit(p.X, p.Y, 123 + course) * 5f;
            int k = 0;
            while (x < r)
            {
                float wdt = 6f + 5f * ArtHash.Unit(p.X * 7 + k, p.Y, 124 + course);
                float bl = MathF.Max(l, x), br = MathF.Min(r, x + wdt);
                if (br > bl + 0.5f)
                {
                    Color stone = Palette.WallFace.Lerp(Palette.WallTop, 0.12f + 0.22f * ArtHash.Unit(p.X * 7 + k, p.Y, 125 + course)).Darkened(0.05f);
                    b.Rect(bl + 0.5f, y0, br - bl - 1f, y1 - y0, stone);
                    b.Rect(bl + 0.5f, y0, br - bl - 1f, 0.8f, new Color(1f, 0.95f, 0.85f, 0.08f));
                }
                if (x + wdt < r && x + wdt > l) b.Line(x + wdt, y0, x + wdt, y1, mortar, 0.8f);
                x += wdt;
                k++;
            }
        }
        b.Line(l, mid, r, mid, mortar, 0.8f);
        if (ArtHash.Unit(p.X, p.Y, 121) < 0.35f)
            b.Line(l + (r - l) * (0.3f + 0.4f * ArtHash.Unit(p.X, p.Y, 122)), t + 2.5f, l + (r - l) * 0.5f, bottom - 2f, Palette.WallGrime.WithAlpha(0.6f), 0.7f);
        if (ArtHash.Unit(p.X, p.Y, 126) < 0.5f)
            b.Clear().Ellipse(l + (r - l) * ArtHash.Unit(p.X, p.Y, 127), bottom - 2.5f, (r - l) * 0.25f, 2.2f, 10).Fill(Palette.Moss.WithAlpha(0.45f));
    }

    private static void TopPiece(Brush b, float l, float t, float r, float bt, Color c)
    {
        b.Rect(l, t, r - l, bt - t, c);
        b.Rect(l, t, r - l, 1.2f, new Color(1f, 0.95f, 0.85f, 0.12f));
        b.Rect(l, bt - 1f, r - l, 1f, new Color(0f, 0f, 0f, 0.12f));
    }

    /// <summary>The shelter doorway: worn posts where the wall ends, a threshold, and a broken door leaf hanging open.</summary>
    public static void DrawDoor(Brush b, SimPoint p)
    {
        float x = p.X * T, cy = (p.Y + 0.5f) * T, ground = cy + WallThick / 2f;
        Color post = Palette.Trunk.Lightened(0.1f);
        b.Clear().RoundRect(x - 0.5f, cy - WallThick / 2f, T + 1f, WallThick, 1.5f).Fill(Palette.Floor.Darkened(0.35f));
        b.Clear().RoundRect(x + 2f, ground - 3f, T - 4f, 3f, 1f).Shade(Palette.Log, Palette.Log.Darkened(0.4f), new Vector2(0f, 1f)).Stroke(Palette.Ink, 0.7f);
        b.Clear().RoundRect(x - 1.5f, ground - WallH - 4f, 3.6f, WallH + 4f, 1f).Shade(post, Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(Palette.Ink, 0.8f);
        b.Clear().RoundRect(x + T - 2.1f, ground - WallH - 4f, 3.6f, WallH + 4f, 1f).Shade(post, Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(Palette.Ink, 0.8f);
        // The leaf hangs off the east post, swung out and down.
        b.Clear().Add(x + T - 1f, ground - WallH + 1f).Add(x + T + 6f, ground - WallH + 5f).Add(x + T + 6f, ground + 2.5f).Add(x + T - 1f, ground)
            .Shade(Palette.Log.Darkened(0.05f), Palette.Log.Darkened(0.45f), new Vector2(1f, 0f)).Stroke(Palette.Ink, 0.8f);
        b.Line(x + T + 0.5f, ground - WallH + 5f, x + T + 5f, ground - WallH + 8f, Palette.Ink.WithAlpha(0.7f), 0.6f);
        b.Line(x + T + 0.5f, ground - 3f, x + T + 5f, ground - 1f, Palette.Ink.WithAlpha(0.7f), 0.6f);
    }

    /// <summary>Roof sections rest on the walls: their sort key is just in front of the shelter's south wall.</summary>
    public float RoofSortY => (_m.Shelter.Y + _m.Shelter.H - 0.5f) * T + WallThick / 2f + 0.01f;

    /// <summary>The roof section's top face in map pixels, stretched to the wall centres on the outer sides.</summary>
    private Rect2 RoofRect(RoofSection s)
    {
        SimRect sh = _m.Shelter;
        float x0 = s.Area.X * T, x1 = (s.Area.X + s.Area.W) * T;
        if (s.Area.X == sh.X + 1) x0 -= T * 0.5f;
        if (s.Area.X + s.Area.W == sh.X + sh.W - 1) x1 += T * 0.5f;
        float y0 = (sh.Y + 0.5f) * T, y1 = (sh.Y + sh.H - 0.5f) * T;
        return new Rect2(x0, y0 - WallH - 2f, x1 - x0, y1 - y0);
    }

    /// <summary>A roof section on the walls: weathered boards when sound; bare rafters, loose boards and a red dashed outline when broken.</summary>
    public void DrawRoof(Brush b, RoofSection s, float alpha)
    {
        Rect2 top = RoofRect(s).Grow(-0.5f);
        // Someone underneath: the boards fade so they can be seen, the frame stays.
        float frame = Math.Max(alpha, 0.8f);
        Color ink = Palette.Ink.WithAlpha(frame);
        if (s.Sound)
        {
            // Weathered boards laid across, each a little different, with gaps, moss and grime.
            const float board = 4f;
            int rows = (int)(top.Size.Y / board);
            for (int i = 0; i < rows; i++)
            {
                float y = top.Position.Y + i * board;
                Color c = Palette.Roof.Lerp(Palette.RoofHi, ArtHash.Unit(s.Index, i, 150) * 0.55f).Darkened(i * 0.012f);
                b.Rect(top.Position.X, y, top.Size.X, board - 0.6f, c.WithAlpha(alpha));
                b.Rect(top.Position.X, y + board - 0.6f, top.Size.X, 0.6f, Palette.RoofLine.WithAlpha(0.9f * alpha));
                float joint = top.Position.X + top.Size.X * (0.2f + 0.6f * ArtHash.Unit(s.Index, i, 151));
                b.Rect(joint, y, 0.8f, board - 0.6f, Palette.RoofLine.WithAlpha(0.8f * alpha));
            }
            b.TextureRegion(_tex.GrungeDark, top, new Rect2(top.Position.X * 1.5f, top.Position.Y * 1.5f, top.Size.X * 1.5f, top.Size.Y * 1.5f), new Color(1f, 1f, 1f, 0.45f * alpha));
            if (ArtHash.Unit(s.Index, 152) < 0.7f)
                b.Clear().Ellipse(top.Position.X + top.Size.X * (0.25f + 0.5f * ArtHash.Unit(s.Index, 153)), top.Position.Y + top.Size.Y * 0.3f, top.Size.X * 0.16f, 2.5f, 10).Fill(Palette.Moss.WithAlpha(0.6f * alpha));
            b.Rect(top.Position.X, top.Position.Y, top.Size.X, top.Size.Y * 0.35f, new Color(1f, 0.92f, 0.75f, 0.06f * alpha));
            b.Rect(top.Position.X, top.End.Y - top.Size.Y * 0.3f, top.Size.X, top.Size.Y * 0.3f, new Color(0f, 0f, 0f, 0.12f * alpha));
            b.Rect(top.Position.X, top.End.Y, top.Size.X, 4f, Palette.Roof.Darkened(0.45f).WithAlpha(frame));
            b.Rect(top.Position.X, top.End.Y + 4f, top.Size.X, 1.5f, new Color(0f, 0f, 0f, 0.35f * frame));
            b.RectOutline(new Rect2(top.Position.X, top.Position.Y, top.Size.X, top.Size.Y + 4f), ink, 1f);
            return;
        }
        // Broken: rafters across the gap, a few boards hanging on, the hole showing the floor.
        Color beam = Palette.Log.Darkened(0.15f).WithAlpha(frame);
        for (int k = 0; k < 3; k++)
        {
            float bx = top.Position.X + 3f + k * (top.Size.X - 6f) / 2f;
            b.Clear().RoundRect(bx - 1.6f, top.Position.Y, 3.2f, top.Size.Y + 3f, 1f).Shade(beam.Lightened(0.1f), beam.Darkened(0.3f), new Vector2(1f, 0f)).Stroke(ink, 0.7f);
        }
        float bwid = top.Size.X - 6f;
        b.Clear().RoundRect(top.Position.X + 2f, top.Position.Y + 3f, bwid * 0.75f, 4f, 1f).Fill(Palette.Roof.WithAlpha(frame)).Stroke(ink, 0.7f);
        b.Clear().RoundRect(top.Position.X + bwid * 0.35f, top.Position.Y + 8f, bwid * 0.6f, 3.5f, 1f).Fill(Palette.Roof.Darkened(0.1f).WithAlpha(frame)).Stroke(ink, 0.7f);
        b.Push();
        b.Translate(top.Position.X + top.Size.X * 0.55f, top.Position.Y + top.Size.Y * 0.66f);
        b.Rotate(0.4f);
        b.Clear().RoundRect(-bwid * 0.3f, -1.8f, bwid * 0.6f, 3.6f, 1f).Fill(Palette.Roof.Darkened(0.2f).WithAlpha(frame)).Stroke(ink, 0.7f);
        b.Pop();
        DashedRect(b, top, Palette.Broken.WithAlpha(Palette.Broken.A * frame), 1.1f, 4f);
    }

    public float RuinRoofSortY => (_m.CoveredRuins.Y + _m.CoveredRuins.H) * T - 0.5f;

    /// <summary>The ruins' heavy old slab, cracked and partly fallen, resting on the north wall and two stone posts.</summary>
    public void DrawRuinRoof(Brush b, float alpha)
    {
        Rect2 r = TilesRect(_m.CoveredRuins);
        float x0 = r.Position.X, w = r.Size.X;
        float y0 = r.Position.Y - T * 0.5f - WallH, h = r.Size.Y + T * 0.5f;
        RuinPost(b, x0 + 2f, r.End.Y, alpha);
        RuinPost(b, x0 + w - 6f, r.End.Y, alpha);
        b.Clear().Add(x0 - 2f, y0).Add(x0 + w + 2f, y0).Add(x0 + w + 2f, y0 + h * 0.74f).Add(x0 + w * 0.72f, y0 + h * 0.88f).Add(x0 + w * 0.45f, y0 + h * 0.76f).Add(x0 + w * 0.2f, y0 + h - 2f).Add(x0 - 2f, y0 + h - 2f);
        b.Shade(Palette.Stone.WithAlpha(alpha), Palette.StoneLo.Darkened(0.25f).WithAlpha(alpha), Brush.LightDir).Stroke(Palette.Ink.WithAlpha(Math.Max(alpha, 0.8f)), 1f);
        b.TextureRegion(_tex.Stone, new Rect2(x0, y0, w, h * 0.72f), new Rect2(x0 * 1.6f, y0 * 1.6f, w * 1.6f, h * 1.15f), new Color(0.62f, 0.62f, 0.6f, 0.45f * alpha));
        b.Line(x0 + w * 0.3f, y0 + 3f, x0 + w * 0.42f, y0 + h * 0.5f, Palette.Ink.WithAlpha(0.7f * alpha), 0.8f);
        b.Line(x0 + w * 0.42f, y0 + h * 0.5f, x0 + w * 0.6f, y0 + h * 0.62f, Palette.Ink.WithAlpha(0.7f * alpha), 0.8f);
        b.Clear().Ellipse(x0 + w * 0.25f, y0 + h * 0.18f, w * 0.12f, h * 0.06f, 10).Fill(Palette.Moss.WithAlpha(0.7f * alpha));
    }

    private static void RuinPost(Brush b, float x, float bottom, float alpha) =>
        b.Clear().RoundRect(x, bottom - WallH - 2f, 4f, WallH + 2f, 1f)
            .Shade(Palette.StoneHi.WithAlpha(alpha), Palette.StoneLo.WithAlpha(alpha), new Vector2(1f, 0f)).Stroke(Palette.Ink.WithAlpha(Math.Max(alpha, 0.8f)), 0.8f);

    public float HearthSortY => (_m.Hearth.Y + 0.78f) * T;

    public Vector2 HearthCenter => Center(_m.Hearth) + new Vector2(0f, 1f);

    /// <summary>A ring of stones, ash and crossed logs; three tongues of flame that never hold still.</summary>
    public void DrawHearth(Brush b, float time)
    {
        Vector2 c = HearthCenter;
        b.Clear().Ellipse(c.X, c.Y + 1f, 8.6f, 5.4f, 16).Fill(Palette.Hex(0x2a221a)).Stroke(Palette.Ink, 0.8f);
        for (int i = 0; i < 9; i++)
        {
            float a = i / 9f * MathF.Tau;
            float sx = c.X + MathF.Cos(a) * 9.5f, sy = c.Y + 1f + MathF.Sin(a) * 6f;
            float r = 2.6f + 0.6f * ArtHash.Unit(i, 130);
            b.Clear().Add(sx - r, sy).Add(sx - r * 0.5f, sy - r * 0.8f).Add(sx + r * 0.6f, sy - r * 0.7f).Add(sx + r, sy + r * 0.1f).Add(sx + r * 0.4f, sy + r * 0.8f).Add(sx - r * 0.6f, sy + r * 0.7f)
                .Shade(Palette.StoneHi, Palette.StoneLo.Darkened(0.2f), Brush.LightDir).Stroke(Palette.Ink, 0.7f);
        }
        b.Push();
        b.Translate(c.X, c.Y);
        b.Rotate(0.55f);
        b.Clear().RoundRect(-6f, -1.5f, 12f, 3f, 1.2f).Fill(Palette.Log.Darkened(0.35f)).Stroke(Palette.Ink, 0.6f);
        b.Rotate(-1.1f);
        b.Clear().RoundRect(-6f, -1.5f, 12f, 3f, 1.2f).Fill(Palette.Log.Darkened(0.3f)).Stroke(Palette.Ink, 0.6f);
        b.Pop();
        // Embers glow at the base.
        for (int i = 0; i < 4; i++)
        {
            float g = 0.6f + 0.4f * MathF.Sin(time * (6f + i) + i);
            b.Dot(c.X - 3f + i * 2f, c.Y + 0.5f + (i % 2) * 1.2f, 0.9f, Palette.Ember.WithAlpha(g));
        }
        Flames(b, c.X, c.Y - 0.5f, time, 1f);
    }

    public static void Flames(Brush b, float x, float y, float time, float scale)
    {
        ReadOnlySpan<float> layers = stackalloc float[] { 5.2f, 10f, 7f, 4.1f, 7.6f, 9f, 2.6f, 4.8f, 11f };
        Color c1 = Palette.Flame1, c2 = Palette.Flame2, c3 = Palette.Flame3;
        for (int k = 0; k < 3; k++)
        {
            float w = layers[k * 3] * scale, h = layers[k * 3 + 1] * scale, sp = layers[k * 3 + 2];
            float fl = 1f + MathF.Sin(time * sp + k) * 0.14f + MathF.Sin(time * sp * 1.7f + k * 2f) * 0.08f;
            float tip = MathF.Sin(time * 5f + k) * 1.2f * scale;
            b.Clear();
            Figures.Quad(b, new Vector2(x - w, y), new Vector2(x - w * 0.9f, y - h * 0.6f * fl), new Vector2(x + tip, y - h * fl), 5, true);
            Figures.Quad(b, new Vector2(x + tip, y - h * fl), new Vector2(x + w * 0.9f, y - h * 0.6f * fl), new Vector2(x + w, y), 5, false);
            b.Fill(k == 0 ? c1 : k == 1 ? c2 : c3);
        }
    }

    /// <summary>
    /// Puffs rising and spreading on the wind: pale wood smoke from the hearth, thick dark smoke
    /// (<paramref name="dark"/>) from someone burning in the sun.
    /// </summary>
    public void DrawSmoke(Brush b, Vector2 at, float time, float strength, float wind, int salt, bool dark = false)
    {
        if (strength <= 0.01f) return;
        Color tint = dark ? new Color(0.33f, 0.31f, 0.29f) : new Color(0.74f, 0.72f, 0.68f);
        int puffs = dark ? 7 : 6;
        for (int i = 0; i < puffs; i++)
        {
            float k = (time * (dark ? 0.45f : 0.28f) + i / (float)puffs + salt * 0.13f) % 1f;
            float x = at.X + MathF.Sin(i * 5f + time * 0.7f + salt) * 3f + k * 16f * wind;
            float y = at.Y - 4f - k * (dark ? 30f : 38f);
            float size = (dark ? 8f : 7f) + k * (dark ? 20f : 22f);
            float a = (dark ? 0.85f : 0.6f) * (1f - k) * MathF.Min(1f, k * 5f) * strength;
            if (_tex.Smoke != null) b.Texture(_tex.Smoke, x, y, size, size, k * 2f + i, tint.WithAlpha(Math.Min(1f, a)));
            else b.Clear().Ellipse(x, y, size * 0.3f, size * 0.25f, 12).Fill(tint.WithAlpha(a * 0.7f));
        }
    }

    public static float StackSortY(ItemStack s) => (s.Pos.Y + 0.7f) * T;

    /// <summary>Goods on the ground: a log pile for timber, baskets heaped with berries, a sack for anything else.</summary>
    public static void DrawStack(Brush b, ItemStack s)
    {
        float x = (s.Pos.X + 0.5f) * T, y = (s.Pos.Y + 0.62f) * T;
        switch (s.DefId)
        {
            case "timber":
            {
                int logs = Math.Clamp(s.Count, 1, 6);
                for (int i = 0; i < logs; i++)
                {
                    int row = i < 3 ? 0 : i < 5 ? 1 : 2;
                    int inRow = row == 0 ? i : row == 1 ? i - 3 : 0;
                    int perRow = row == 0 ? Math.Min(3, logs) : row == 1 ? Math.Min(2, logs - 3) : 1;
                    float ly = y - row * 4.2f - inRow * 0.3f;
                    float lx = x - (perRow - 1) * 0.5f + (inRow - (perRow - 1) / 2f) * 0.6f;
                    b.Push();
                    b.Translate(lx, ly - inRow * 3.6f + row * 1.8f);
                    b.Rotate(ArtHash.Signed(s.Id, i, 140) * 0.08f);
                    b.Clear().RoundRect(-9f, -2.1f, 18f, 4.2f, 1.8f).Lit(Palette.Log, 0.2f).Stroke(Palette.Ink, 0.75f);
                    b.Clear().Ellipse(8.6f, 0f, 1.6f, 2.1f, 10).Fill(Palette.LogEnd).Stroke(Palette.Ink, 0.6f);
                    b.Pop();
                }
                break;
            }
            case "berries":
            {
                int baskets = Math.Clamp((s.Count + 5) / 6, 1, 3);
                for (int k = 0; k < baskets; k++)
                {
                    float bx = x - (baskets - 1) * 5f + k * 10f, by = y - (k % 2) * 1.5f;
                    b.Clear().Ellipse(bx, by, 6f, 4.6f, 14).Lit(Palette.Basket, 0.25f).Stroke(Palette.Ink, 0.8f);
                    b.Clear().Ellipse(bx, by - 1.2f, 4.6f, 2.6f, 12).Fill(Palette.Basket.Darkened(0.4f));
                    int shown = Math.Min(6, s.Count - k * 6);
                    for (int i = 0; i < shown; i++)
                    {
                        float ox = bx - 3f + (i % 3) * 3f, oy = by - 2.2f + (i / 3) * 1.6f;
                        b.Dot(ox, oy, 1.3f, Palette.Berry);
                        b.Dot(ox - 0.35f, oy - 0.35f, 0.4f, Palette.BerryHi);
                    }
                }
                break;
            }
            default:
                b.Clear().RoundRect(x - 5f, y - 7f, 10f, 9f, 3f).Lit(Palette.Straw.Darkened(0.2f), 0.25f).Stroke(Palette.Ink, 0.8f);
                b.Line(x - 2f, y - 7f, x + 2f, y - 8f, Palette.Ink, 0.7f);
                break;
        }
    }

    public float WatchPostSortY => (_m.WatchPost.Y + 0.8f) * T;

    public Vector2 LanternAt => Center(_m.WatchPost) + new Vector2(5f, -T * 0.95f);

    /// <summary>A lookout stake with a crossbar and a lantern (lit at night while someone keeps watch).</summary>
    public void DrawWatchPost(Brush b, bool lit, float time)
    {
        Vector2 c = Center(_m.WatchPost);
        float x = c.X - 2f, foot = c.Y + T * 0.3f, top = c.Y - T * 1.15f;
        b.Clear().RoundRect(x - 1.8f, top, 3.6f, foot - top, 1f).Shade(Palette.Trunk.Lightened(0.1f), Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(Palette.Ink, 0.8f);
        b.Clear().RoundRect(x - 1f, top + 3f, 9f, 2.2f, 0.8f).Fill(Palette.Trunk).Stroke(Palette.Ink, 0.7f);
        Vector2 l = LanternAt;
        b.Line(l.X, top + 5f, l.X, l.Y - 3f, Palette.Ink, 0.6f);
        b.Clear().RoundRect(l.X - 2.2f, l.Y - 3f, 4.4f, 5.5f, 1f).Fill(lit ? Palette.Flame3.Lerp(Palette.Flame2, 0.3f + 0.2f * MathF.Sin(time * 9f)) : Palette.Hex(0x3a3328)).Stroke(Palette.Ink, 0.7f);
        b.Line(l.X - 2.2f, l.Y - 0.3f, l.X + 2.2f, l.Y - 0.3f, Palette.Ink, 0.5f);
    }

    // ================= helpers =================

    public static Vector2 Center(SimPoint p) => new((p.X + 0.5f) * T, (p.Y + 0.5f) * T);

    public static Rect2 TilesRect(SimRect r) => new(r.X * T, r.Y * T, r.W * T, r.H * T);

    public static void DashedRect(Brush b, Rect2 r, Color color, float width, float dash)
    {
        var a = r.Position;
        var b2 = new Vector2(r.End.X, r.Position.Y);
        var c = r.End;
        var d = new Vector2(r.Position.X, r.End.Y);
        b.Dashed(a, b2, color, width, dash);
        b.Dashed(b2, c, color, width, dash);
        b.Dashed(c, d, color, width, dash);
        b.Dashed(d, a, color, width, dash);
    }
}
