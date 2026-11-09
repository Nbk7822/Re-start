using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Content;
using SimPoint = Return.Sim.Core.Point;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game;

/// <summary>
/// Draws the world as coloured rectangles and text (fixed decision 11, ADR-0008): terrain, faint
/// grid lines, trees, berry bushes, the shelter's three roof sections, beds, hearth, stockpile
/// zone, watch post, item stacks, and the active people in their cast colours with their names
/// under them and a one-character mood marker over their heads. Labels keep a constant on-screen
/// size at any zoom. Read-only over World; the only state here is view smoothing.
/// </summary>
public partial class MapView : Node2D
{
    public const int TileSize = 24;

    private const int NameFontSize = 12;
    private const int MarkerFontSize = 16;
    private const int CountFontSize = 10;

    // Calm terrain palette, indexed by TerrainKind.
    private static readonly Color[] TerrainColors =
    {
        new(0.49f, 0.58f, 0.38f), // Grass
        new(0.61f, 0.52f, 0.38f), // Soil
        new(0.37f, 0.50f, 0.59f), // Water
        new(0.55f, 0.54f, 0.52f), // Rock
        new(0.73f, 0.66f, 0.54f), // Floor
        new(0.35f, 0.31f, 0.27f), // Wall
        new(0.56f, 0.43f, 0.29f), // Door
    };

    private static readonly Color UnknownTerrain = new(0.8f, 0.3f, 0.8f);
    private static readonly Color GridColor = new(0f, 0f, 0f, 0.08f);
    private static readonly Color TreeColor = new(0.25f, 0.42f, 0.24f);
    private static readonly Color TreeInner = new(0.30f, 0.49f, 0.28f);
    private static readonly Color TreeEdge = new(0.16f, 0.27f, 0.15f);
    private static readonly Color BushColor = new(0.38f, 0.54f, 0.30f);
    private static readonly Color BushEdge = new(0.24f, 0.36f, 0.19f);
    private static readonly Color BerryColor = new(0.72f, 0.22f, 0.29f);
    private static readonly Color BedColor = new(0.85f, 0.80f, 0.71f);
    private static readonly Color PillowColor = new(0.95f, 0.93f, 0.88f);
    private static readonly Color BedEdge = new(0.42f, 0.35f, 0.28f);
    private static readonly Color HearthColor = new(0.77f, 0.39f, 0.23f);
    private static readonly Color HearthGlow = new(0.93f, 0.66f, 0.36f);
    private static readonly Color HearthEdge = new(0.45f, 0.22f, 0.13f);
    private static readonly Color PostColor = new(0.42f, 0.33f, 0.25f);
    private static readonly Color PostInner = new(0.66f, 0.55f, 0.42f);
    private static readonly Color PostEdge = new(0.24f, 0.19f, 0.14f);
    private static readonly Color StockpileFill = new(0.89f, 0.81f, 0.48f, 0.12f);
    private static readonly Color StockpileEdge = new(0.89f, 0.81f, 0.48f, 0.9f);
    private static readonly Color CoveredFill = new(0.25f, 0.22f, 0.20f, 0.28f);
    private static readonly Color CoveredEdge = new(0.30f, 0.26f, 0.22f, 0.9f);
    private static readonly Color RoofSoundFill = new(0.45f, 0.32f, 0.21f, 0.38f);
    private static readonly Color RoofSoundEdge = new(0.36f, 0.25f, 0.16f);
    private static readonly Color RoofBrokenFill = new(0.75f, 0.36f, 0.30f, 0.12f);
    private static readonly Color RoofBrokenEdge = new(0.76f, 0.38f, 0.31f);
    private static readonly Color RainTint = new(0.30f, 0.38f, 0.48f, 0.16f);
    private static readonly Color ItemEdge = new(0.15f, 0.13f, 0.11f, 0.8f);
    private static readonly Color BodyEdge = new(0.12f, 0.11f, 0.10f);
    private static readonly Color HostileEdge = new(0.80f, 0.20f, 0.16f);
    private static readonly Color HostileColor = new(0.55f, 0.27f, 0.24f);
    private static readonly Color StrangerColor = new(0.70f, 0.70f, 0.70f);
    private static readonly Color SelectColor = new(0.99f, 0.96f, 0.86f);
    private static readonly Color NameColor = new(0.97f, 0.95f, 0.90f);
    private static readonly Color LabelOutline = new(0.08f, 0.07f, 0.06f, 0.9f);

    private SimHost? _host;
    private Font _font = null!;
    private Transform2D _toScreen = Transform2D.Identity;
    private float _zoom = 1f;
    private readonly Dictionary<int, Vector2> _shown = new();
    private readonly Dictionary<string, Color> _castColors = new(StringComparer.Ordinal);
    private readonly List<(int Id, Rect2 Box)> _hits = new();

    public void Bind(SimHost host)
    {
        _host = host;
        _font = ThemeDB.FallbackFont;
    }

    /// <summary>Centre of a tile in map pixels.</summary>
    public static Vector2 TileCenter(SimPoint p) => new((p.X + 0.5f) * TileSize, (p.Y + 0.5f) * TileSize);

    public Vector2I TileUnderMouse()
    {
        Vector2 m = GetLocalMousePosition();
        return new Vector2I(Mathf.FloorToInt(m.X / TileSize), Mathf.FloorToInt(m.Y / TileSize));
    }

    public bool InBounds(Vector2I tile) =>
        _host != null && tile.X >= 0 && tile.Y >= 0 && tile.X < _host.World.Map.Width && tile.Y < _host.World.Map.Height;

    /// <summary>The person drawn under the mouse (topmost first), or 0.</summary>
    public int CharacterUnderMouse()
    {
        Vector2 m = GetLocalMousePosition();
        for (int i = _hits.Count - 1; i >= 0; i--)
            if (_hits[i].Box.HasPoint(m)) return _hits[i].Id;
        return 0;
    }

    public override void _Process(double delta)
    {
        if (_host == null) return;
        Smooth(delta);
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_host == null || e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
        _host.Select(CharacterUnderMouse());
        GetViewport().SetInputAsHandled();
    }

    public override void _Draw()
    {
        if (_host == null) return;
        World w = _host.World;
        Map m = w.Map;
        _toScreen = GetGlobalTransformWithCanvas();
        _zoom = Mathf.Max(0.01f, _toScreen.X.Length());
        _hits.Clear();

        DrawTerrain(m);
        DrawGrid(m);
        DrawCamp(m);
        DrawItems(w);
        DrawPlants(m);
        DrawRoofs(m);
        if (w.Weather.Raining) DrawRect(new Rect2(0, 0, m.Width * TileSize, m.Height * TileSize), RainTint);
        DrawPeople(w);
    }

    /// <summary>
    /// Eases each person's drawn position toward their tile so steps read as walking; faster at
    /// higher speeds, snapping on big jumps. View-only: the sim position is always the tile.
    /// </summary>
    private void Smooth(double delta)
    {
        World w = _host!.World;
        float speed = Mathf.Max(1, _host.Clock.MultiplierOf(_host.Clock.Speed));
        float alpha = 1f - Mathf.Exp(-(float)delta * 14f * speed);
        foreach (Character c in w.Characters)
        {
            if (!c.IsActive)
            {
                _shown.Remove(c.Id);
                continue;
            }
            Vector2 target = TileCenter(c.Pos);
            if (!_shown.TryGetValue(c.Id, out Vector2 at) || at.DistanceTo(target) > TileSize * 3) at = target;
            else at = at.Lerp(target, alpha);
            _shown[c.Id] = at;
        }
    }

    private void DrawTerrain(Map m)
    {
        for (int y = 0; y < m.Height; y++)
        {
            int x = 0;
            while (x < m.Width)
            {
                byte kind = m.Terrain[y * m.Width + x];
                int run = 1;
                while (x + run < m.Width && m.Terrain[y * m.Width + x + run] == kind) run++;
                Color color = kind < TerrainColors.Length ? TerrainColors[kind] : UnknownTerrain;
                DrawRect(new Rect2(x * TileSize, y * TileSize, run * TileSize, TileSize), color);
                x += run;
            }
        }
    }

    private void DrawGrid(Map m)
    {
        float width = m.Width * TileSize, height = m.Height * TileSize;
        for (int x = 0; x <= m.Width; x++) DrawLine(new Vector2(x * TileSize, 0), new Vector2(x * TileSize, height), GridColor);
        for (int y = 0; y <= m.Height; y++) DrawLine(new Vector2(0, y * TileSize), new Vector2(width, y * TileSize), GridColor);
    }

    private void DrawCamp(Map m)
    {
        Rect2 zone = TilesRect(m.Stockpile);
        DrawRect(zone, StockpileFill);
        DrawDashedRect(zone.Grow(-1.5f), StockpileEdge, 1.5f, 5f);

        foreach (Bed b in m.Beds)
        {
            Rect2 tile = TileRect(b.Pos);
            var bed = new Rect2(tile.Position + new Vector2(4, 2), new Vector2(TileSize - 8, TileSize - 4));
            DrawRect(bed, BedColor);
            DrawRect(new Rect2(bed.Position + new Vector2(2, 2), new Vector2(bed.Size.X - 4, 5)), PillowColor);
            DrawRect(bed, BedEdge, false, 1f);
        }

        Rect2 hearth = TileRect(m.Hearth).Grow(-3);
        DrawRect(hearth, HearthColor);
        DrawRect(hearth.Grow(-5), HearthGlow);
        DrawRect(hearth, HearthEdge, false, 1.5f);

        Rect2 post = TileRect(m.WatchPost).Grow(-3);
        DrawRect(post, PostColor);
        DrawRect(post.Grow(-5), PostInner);
        DrawRect(post, PostEdge, false, 1.5f);
    }

    private void DrawItems(World w)
    {
        foreach (ItemStack s in w.Items)
        {
            if (s.Count <= 0) continue;
            Rect2 tile = TileRect(s.Pos);
            var box = new Rect2(tile.Position + new Vector2(5, 5), new Vector2(14, 14));
            DrawRect(box, ItemColor(s.DefId));
            DrawRect(box, ItemEdge, false, 1f);
            DrawScreenText(tile.End - new Vector2(1, 1), Ui.N(s.Count), CountFontSize, NameColor, new Vector2(1f, 1f), 3);
        }
    }

    private void DrawPlants(Map m)
    {
        foreach (Bush b in m.Bushes)
        {
            Rect2 r = TileRect(b.Pos).Grow(-4);
            DrawRect(r, BushColor);
            DrawRect(r, BushEdge, false, 1f);
            int dots = Math.Min(b.Berries, 6);
            for (int i = 0; i < dots; i++)
                DrawRect(new Rect2(r.Position.X + 3 + i % 3 * 5, r.Position.Y + 3 + i / 3 * 6, 3, 3), BerryColor);
        }
        foreach (var t in m.Trees)
        {
            if (t.Felled) continue;
            Rect2 r = TileRect(t.Pos).Grow(-2);
            DrawRect(r, TreeColor);
            DrawRect(r.Grow(-6), TreeInner);
            DrawRect(r, TreeEdge, false, 1f);
        }
    }

    private void DrawRoofs(Map m)
    {
        Rect2 ruins = TilesRect(m.CoveredRuins);
        DrawRect(ruins, CoveredFill);
        DrawRect(ruins.Grow(-1), CoveredEdge, false, 1f);

        foreach (RoofSection s in m.Roof)
        {
            Rect2 r = TilesRect(s.Area).Grow(-1.5f);
            if (s.Sound)
            {
                DrawRect(r, RoofSoundFill);
                DrawRect(r, RoofSoundEdge, false, 1.5f);
            }
            else
            {
                DrawRect(r, RoofBrokenFill);
                DrawDashedRect(r, RoofBrokenEdge, 1.5f, 4f);
            }
        }
    }

    private void DrawPeople(World w)
    {
        int selected = _host!.SelectedId;
        var labels = new List<(int Id, Vector2 Top, Vector2 Bottom, string Name, string? Emote)>();
        foreach (Character c in w.Characters)
        {
            if (!c.IsActive || !w.Map.InBounds(c.Pos)) continue;
            Vector2 at = _shown.TryGetValue(c.Id, out Vector2 shown) ? shown : TileCenter(c.Pos);
            bool lying = c.Downed || c.Asleep;
            Vector2 size = lying ? new Vector2(18, 10) : new Vector2(13, 18);
            var body = new Rect2(at - size / 2 + new Vector2(0, lying ? 3 : 0), size);
            Color color = CastColor(c);
            if (c.Downed) color = color.Darkened(0.35f);
            if (c.Id == selected) DrawRect(body.Grow(3), SelectColor, false, 2f);
            DrawRect(body, color);
            DrawRect(body, c.Status == CharStatus.Hostile ? HostileEdge : BodyEdge, false, 1.5f);
            _hits.Add((c.Id, new Rect2(at - new Vector2(TileSize, TileSize) / 2, new Vector2(TileSize, TileSize))));
            labels.Add((c.Id, new Vector2(at.X, body.Position.Y - 1), new Vector2(at.X, body.End.Y + 1), Ui.T(c.NameKey), ViewText.MoodEmote(w, c)));
        }

        // Names go under bodies, left to right; a name that would overlap one already placed drops a line.
        labels.Sort((a, b) => a.Bottom.X != b.Bottom.X ? a.Bottom.X.CompareTo(b.Bottom.X) : a.Id.CompareTo(b.Id));
        var placed = new List<Rect2>();
        foreach (var label in labels)
        {
            Vector2 size = _font.GetStringSize(label.Name, HorizontalAlignment.Left, -1, NameFontSize);
            Vector2 at = (_toScreen * label.Bottom).Round();
            var box = new Rect2(at.X - size.X / 2 - 3, at.Y, size.X + 6, size.Y - 2);
            for (int tries = 0; tries < 4 && Overlaps(box, placed); tries++) box.Position += new Vector2(0, size.Y - 2);
            placed.Add(box);
            DrawScreenText(label.Bottom, label.Name, NameFontSize, NameColor, new Vector2(0.5f, 0f), 4, box.Position.Y - at.Y);
            if (label.Emote == null) continue;
            string? glyph = ViewText.EmoteGlyph(label.Emote);
            if (glyph != null) DrawScreenText(label.Top, glyph, MarkerFontSize, EmoteColor(label.Emote), new Vector2(0.5f, 1f), 4);
        }
    }

    private static bool Overlaps(Rect2 box, List<Rect2> placed)
    {
        foreach (Rect2 r in placed)
            if (r.Intersects(box)) return true;
        return false;
    }

    /// <summary>
    /// Draws text at a constant on-screen size whatever the camera zoom, snapped to whole pixels.
    /// <paramref name="anchor"/> is in map pixels; <paramref name="align"/> chooses which point of
    /// the text box sits on it ((0.5, 0) top centre, (0.5, 1) bottom centre, (1, 1) bottom right);
    /// <paramref name="screenDy"/> shifts it down by that many screen pixels.
    /// </summary>
    private void DrawScreenText(Vector2 anchor, string text, int fontSize, Color color, Vector2 align, int outline, float screenDy = 0f)
    {
        Vector2 size = _font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        float ascent = _font.GetAscent(fontSize);
        Vector2 screen = (_toScreen * anchor).Round() + new Vector2(0, screenDy);
        Vector2 local = _toScreen.AffineInverse() * screen;
        DrawSetTransform(local, 0f, new Vector2(1f / _zoom, 1f / _zoom));
        var pos = new Vector2(Mathf.Round(-size.X * align.X), Mathf.Round(-size.Y * align.Y + ascent));
        if (outline > 0) DrawStringOutline(_font, pos, text, HorizontalAlignment.Left, -1, fontSize, outline, LabelOutline);
        DrawString(_font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }

    private void DrawDashedRect(Rect2 r, Color color, float width, float dash)
    {
        var a = r.Position;
        var b = new Vector2(r.End.X, r.Position.Y);
        var c = r.End;
        var d = new Vector2(r.Position.X, r.End.Y);
        DrawDashedLine(a, b, color, width, dash);
        DrawDashedLine(b, c, color, width, dash);
        DrawDashedLine(c, d, color, width, dash);
        DrawDashedLine(d, a, color, width, dash);
    }

    private Color CastColor(Character c)
    {
        if (_castColors.TryGetValue(c.CastKey, out Color color)) return color;
        color = c.Status == CharStatus.Hostile ? HostileColor : StrangerColor;
        foreach (CastDef def in _host!.Content.Cast)
            if (def.Key == c.CastKey && Color.HtmlIsValid(def.Color))
            {
                color = Color.FromHtml(def.Color);
                break;
            }
        _castColors[c.CastKey] = color;
        return color;
    }

    private static Color ItemColor(string defId) => defId switch
    {
        "berries" => new Color(0.71f, 0.22f, 0.30f),
        "timber" => new Color(0.55f, 0.36f, 0.21f),
        _ => new Color(0.76f, 0.76f, 0.74f),
    };

    private static Color EmoteColor(string emote) => emote switch
    {
        "anger" => new Color(0.93f, 0.36f, 0.27f),
        "sweat" => new Color(0.58f, 0.78f, 0.93f),
        "hearts" => new Color(0.94f, 0.52f, 0.66f),
        "sparkle" => new Color(0.98f, 0.87f, 0.47f),
        "alert" => new Color(0.98f, 0.60f, 0.28f),
        "question" => new Color(0.82f, 0.82f, 0.97f),
        "zzz" => new Color(0.76f, 0.80f, 0.90f),
        "gloom" => new Color(0.66f, 0.68f, 0.74f),
        "music" => new Color(0.66f, 0.87f, 0.63f),
        _ => NameColor,
    };

    private static Rect2 TileRect(SimPoint p) => new(p.X * TileSize, p.Y * TileSize, TileSize, TileSize);

    private static Rect2 TilesRect(SimRect r) => new(r.X * TileSize, r.Y * TileSize, r.W * TileSize, r.H * TileSize);
}
