using System;
using Godot;
using Return.Game.Logic;
using Return.Sim;
using SimPoint = Return.Sim.Core.Point;
using SimRect = Return.Sim.Core.Rect;

namespace Return.Game.Art;

/// <summary>
/// What the Build menu puts on the map (build design 3.4 and 3.8; ADR-0027), in the grounded
/// three-quarter style of the rest of the camp (ADR-0019): walls of split logs that read apart from the
/// old masonry, plank doors, beds whose blanket is their owner's cloth, roof sections on whatever walls
/// they touch, and plans under way (logs rising course by course, a door's posts, a bed's frame, rafters
/// then boards). Plans not yet begun are marks, drawn by the map view on the overlay.
/// </summary>
public sealed partial class WorldArt
{
    /// <summary>Courses of logs in a full-height timber wall.</summary>
    private const int Courses = 3;

    /// <summary>Tiles where a wall or door built in this playthrough stands (the old camp's have no structure).</summary>
    private static bool[] TimberTiles(Map m)
    {
        var timber = new bool[m.Width * m.Height];
        foreach (Structure s in m.Structures)
            if (!s.Removed && m.InBounds(s.Pos) && m.At(s.Pos) is TerrainKind.Wall or TerrainKind.Door) timber[m.Index(s.Pos)] = true;
        return timber;
    }

    private static bool BedOn(Map m, SimPoint p)
    {
        foreach (Bed b in m.Beds)
            if (b.Pos == p) return true;
        return false;
    }

    /// <summary>A wall or door stands somewhere along n tiles from (x, y) in steps of (dx, dy).</summary>
    private bool WallAlong(int x, int y, int dx, int dy, int n)
    {
        for (int i = 0; i < n; i++)
            if (Joins(x + i * dx, y + i * dy)) return true;
        return false;
    }

    /// <summary>
    /// A roof section's top face in map pixels, raised to rest on the walls: each side that has a wall or door
    /// beside it is stretched half a tile to that wall's centre (the old shelter's sections come out as before).
    /// </summary>
    public Rect2 RoofTop(SimRect a)
    {
        float x0 = a.X * T, x1 = (a.X + a.W) * T, y0 = a.Y * T, y1 = (a.Y + a.H) * T;
        if (WallAlong(a.X - 1, a.Y, 0, 1, a.H)) x0 -= T * 0.5f;
        if (WallAlong(a.X + a.W, a.Y, 0, 1, a.H)) x1 += T * 0.5f;
        if (WallAlong(a.X, a.Y - 1, 1, 0, a.W)) y0 -= T * 0.5f;
        if (WallAlong(a.X, a.Y + a.H, 1, 0, a.W)) y1 += T * 0.5f;
        return new Rect2(x0, y0 - WallH - 2f, x1 - x0, y1 - y0);
    }

    /// <summary>A roof section sorts just in front of the wall below it (else at its own south edge), so whoever stands outside is in front of it.</summary>
    public float RoofSortY(SimRect a) =>
        WallAlong(a.X, a.Y + a.H, 1, 0, a.W) ? WallSortY(new SimPoint(a.X, a.Y + a.H)) + 0.01f : (a.Y + a.H) * T + 0.01f;

    // ================= timber walls and doors =================

    private static Color TimberTone(SimPoint p, int k) =>
        Palette.Log.Lerp(Palette.Hex(0x7d6446), ArtHash.Unit(p.X * 3 + k, p.Y, 160) * 0.7f).Darkened(0.06f + ArtHash.Unit(p.X, p.Y * 3 + k, 161) * 0.14f);

    /// <summary>
    /// A wall of split logs: the same band through the tile's centre as the old walls, its front face laid in
    /// courses of logs whose ends show where the wall stops, its top the flat of the top log. <paramref name="rise"/>
    /// below 1 draws it part built, course by course; <paramref name="joins"/> says which neighbours it runs into.
    /// </summary>
    public void DrawTimberWall(Brush b, SimPoint p, float rise, Func<int, int, bool> joins)
    {
        int shown = Math.Clamp((int)MathF.Ceiling(rise * Courses - 0.001f), 1, Courses);
        float ch = WallH / Courses, height = ch * shown;
        float x0 = p.X * T, cx = (p.X + 0.5f) * T, cy = (p.Y + 0.5f) * T, h = WallThick / 2f;
        bool n = joins(p.X, p.Y - 1), s = joins(p.X, p.Y + 1), w = joins(p.X - 1, p.Y), e = joins(p.X + 1, p.Y);
        float ground = cy + h, faceTop = ground - height;
        Color ink = Palette.Ink;

        // The front face: logs along the arms east and west; where a north-south run ends, the logs' ends.
        float fl = w ? x0 : cx - h, fr = e ? x0 + T : cx + h;
        if (s && !w && !e) fl = fr = cx;
        if (fr > fl)
        {
            bool endGrain = !w && !e;
            for (int k = 0; k < shown; k++)
            {
                float y1 = ground - k * ch, y0 = y1 - ch, mid = (y0 + y1) * 0.5f;
                Color wood = TimberTone(p, k);
                if (endGrain)
                {
                    LogEnd(b, cx, mid, h, ch * 0.5f, wood);
                    continue;
                }
                b.Clear().RoundRect(fl, y0 + 0.2f, fr - fl, ch - 0.4f, ch * 0.45f).Shade(wood.Lightened(0.14f), wood.Darkened(0.34f), new Vector2(0f, 1f));
                b.Line(fl, y0 + 0.3f, fr, y0 + 0.3f, Palette.TrunkLo.WithAlpha(0.9f), 0.6f);
                float gx = fl + (fr - fl) * (0.15f + 0.55f * ArtHash.Unit(p.X * 5 + k, p.Y, 162));
                b.Line(gx, mid + 0.2f, gx + 3.5f + 2f * ArtHash.Unit(p.X, p.Y, 163 + k), mid + 0.2f, wood.Darkened(0.45f).WithAlpha(0.7f), 0.5f);
                if (!w) LogEnd(b, fl, mid, ch * 0.42f, ch * 0.5f, wood);
                if (!e) LogEnd(b, fr, mid, ch * 0.42f, ch * 0.5f, wood);
            }
            b.Rect(fl, ground - 2f, fr - fl, 2f, Palette.WallGrime.WithAlpha(0.4f)); // damp where it meets the ground
            b.Line(fl, ground, fr, ground, ink, 1f);
            if (s && w) b.Line(cx - h, faceTop, cx - h, ground, ink, 0.8f);
            if (s && e) b.Line(cx + h, faceTop, cx + h, ground, ink, 0.8f);
        }

        // The top: the flat of the top log, along the arms, raised by the wall's height so far.
        float top = -height;
        Color topColor = TimberTone(p, 7).Lightened(0.16f);
        TopPiece(b, cx - h, cy - h + top, cx + h, cy + h + top, topColor);
        if (w) TopPiece(b, x0, cy - h + top, cx - h, cy + h + top, topColor);
        if (e) TopPiece(b, cx + h, cy - h + top, x0 + T, cy + h + top, topColor);
        if (n) TopPiece(b, cx - h, p.Y * T + top, cx + h, cy - h + top, topColor);
        if (s) TopPiece(b, cx - h, cy + h + top, cx + h, (p.Y + 1) * T + top, topColor);
        Color split = topColor.Darkened(0.4f).WithAlpha(0.8f);
        if (w || e) b.Line(w ? x0 : cx - h, cy + top, e ? x0 + T : cx + h, cy + top, split, 0.6f);
        if (n || s) b.Line(cx, n ? p.Y * T + top : cy - h + top, cx, s ? (p.Y + 1) * T + top : cy + h + top, split, 0.6f);
        float yt = cy - h + top, yb = cy + h + top;
        if (w) { b.Line(x0, yt, cx - h, yt, ink, 1f); b.Line(x0, yb, cx - h, yb, ink, 0.8f); }
        if (e) { b.Line(cx + h, yt, x0 + T, yt, ink, 1f); b.Line(cx + h, yb, x0 + T, yb, ink, 0.8f); }
        if (n) { b.Line(cx - h, p.Y * T + top, cx - h, yt, ink, 1f); b.Line(cx + h, p.Y * T + top, cx + h, yt, ink, 1f); }
        if (s) { b.Line(cx - h, yb, cx - h, (p.Y + 1) * T + top, ink, 1f); b.Line(cx + h, yb, cx + h, (p.Y + 1) * T + top, ink, 1f); }
        if (!n) b.Line(cx - h, yt, cx + h, yt, ink, 1f);
        if (!s) b.Line(cx - h, yb, cx + h, yb, ink, 0.8f);
        if (!w) b.Line(cx - h, yt, cx - h, yb, ink, 1f);
        if (!e) b.Line(cx + h, yt, cx + h, yb, ink, 1f);
    }

    /// <summary>A log's sawn end: pale wood with a ring and a dark heart.</summary>
    private static void LogEnd(Brush b, float x, float y, float rx, float ry, Color wood)
    {
        b.Clear().Ellipse(x, y, rx, ry, 10).Fill(Palette.LogEnd.Lerp(wood, 0.25f)).Stroke(Palette.Ink, 0.6f);
        b.Clear().Ellipse(x, y, rx * 0.55f, ry * 0.55f, 8).Stroke(wood.Darkened(0.3f).WithAlpha(0.8f), 0.45f);
        b.Dot(x, y, MathF.Min(rx, ry) * 0.18f, wood.Darkened(0.45f));
    }

    /// <summary>
    /// A door built in this playthrough: a threshold log, two fresh posts where the walls end, a lintel across,
    /// and a plank door hung open on the east post. <paramref name="rise"/> below 1: the posts going up, the
    /// lintel and the door last.
    /// </summary>
    public void DrawTimberDoor(Brush b, SimPoint p, float rise)
    {
        if (Joins(p.X, p.Y - 1) && Joins(p.X, p.Y + 1) && !(Joins(p.X - 1, p.Y) && Joins(p.X + 1, p.Y)))
        {
            DrawTimberDoorAcross(b, p, rise);
            return;
        }
        float x = p.X * T, cy = (p.Y + 0.5f) * T, ground = cy + WallThick / 2f;
        float tall = (WallH + 4f) * Math.Clamp(rise, 0.2f, 1f);
        Color post = Palette.Log.Lightened(0.06f), ink = Palette.Ink;
        b.Clear().RoundRect(x - 0.5f, cy - WallThick / 2f, T + 1f, WallThick, 1.5f).Fill(Palette.Mud.WithAlpha(0.85f));
        b.Clear().RoundRect(x + 2f, ground - 3f, T - 4f, 3f, 1f).Shade(Palette.Log, Palette.Log.Darkened(0.4f), new Vector2(0f, 1f)).Stroke(ink, 0.7f);
        b.Clear().RoundRect(x - 1.5f, ground - tall, 3.6f, tall, 1f).Shade(post, Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(ink, 0.8f);
        b.Clear().RoundRect(x + T - 2.1f, ground - tall, 3.6f, tall, 1f).Shade(post, Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(ink, 0.8f);
        if (rise < 0.75f) return;
        // The door, planks with a brace, swung open against the east post.
        Vector2 a = new(x + T - 1f, ground - WallH + 0.5f), bb = new(x + T + 5.5f, ground - WallH + 3.5f), c = new(x + T + 5.5f, ground + 2f), d = new(x + T - 1f, ground);
        b.Clear().Add(a).Add(bb).Add(c).Add(d).Shade(Palette.Log.Lightened(0.04f), Palette.Log.Darkened(0.4f), new Vector2(1f, 0f)).Stroke(ink, 0.8f);
        for (int k = 1; k < 3; k++)
        {
            float t = k / 3f;
            b.Line(a.Lerp(bb, t), d.Lerp(c, t), Palette.TrunkLo.WithAlpha(0.85f), 0.55f);
        }
        b.Line(a.Lerp(d, 0.22f), bb.Lerp(c, 0.78f), Palette.TrunkLo.WithAlpha(0.9f), 0.8f);
        if (rise < 1f) return;
        b.Clear().RoundRect(x - 2.2f, ground - tall - 2.6f, T + 4.4f, 3f, 1f).Shade(post, Palette.TrunkLo, new Vector2(0f, 1f)).Stroke(ink, 0.8f);
    }

    /// <summary>
    /// A door in a wall that runs north and south: a break in the wall's top where the trodden way crosses it, a
    /// threshold log, a post at each end as tall as the wall, and the door swung open into the room off the north
    /// post. Seen from the south, the near post hides most of the opening, as it would.
    /// </summary>
    private static void DrawTimberDoorAcross(Brush b, SimPoint p, float rise)
    {
        float cx = (p.X + 0.5f) * T, y0 = p.Y * T, h = WallThick / 2f;
        float tall = (WallH + 4f) * Math.Clamp(rise, 0.2f, 1f);
        Color post = Palette.Log.Lightened(0.06f), ink = Palette.Ink;
        b.Clear().RoundRect(cx - h - 1f, y0, 2f * h + 2f, T, 1.5f).Fill(Palette.Mud.WithAlpha(0.85f));
        b.Clear().RoundRect(cx - h, y0 + T * 0.5f - 1.4f, 2f * h, 2.8f, 1f).Shade(Palette.Log, Palette.Log.Darkened(0.4f), new Vector2(0f, 1f)).Stroke(ink, 0.7f);
        if (rise >= 0.75f)
        {
            float top = y0 + 2.5f - WallH;
            Vector2 a = new(cx + h, top), bb = new(cx + h + T * 0.55f, top + 1.2f), c = new(cx + h + T * 0.55f, y0 + 3.7f), d = new(cx + h, y0 + 2.5f);
            b.Clear().Add(a).Add(bb).Add(c).Add(d).Shade(Palette.Log.Lightened(0.04f), Palette.Log.Darkened(0.4f), new Vector2(1f, 0f)).Stroke(ink, 0.8f);
            for (int k = 1; k < 3; k++)
            {
                float t = k / 3f;
                b.Line(a.Lerp(bb, t), d.Lerp(c, t), Palette.TrunkLo.WithAlpha(0.85f), 0.55f);
            }
        }
        b.Clear().RoundRect(cx - h, y0 + 2.5f - tall, 2f * h, tall, 1f).Shade(post, Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(ink, 0.8f);
        b.Clear().RoundRect(cx - h, y0 + T - 0.5f - tall, 2f * h, tall, 1f).Shade(post, Palette.TrunkLo, new Vector2(1f, 0f)).Stroke(ink, 0.8f);
    }

    // ================= beds =================

    /// <summary>
    /// A bed: the old camp's bedrolls stay bedrolls; a bed built in this playthrough has a log frame, a straw
    /// mattress and a pillow. Either way the blanket is its owner's cloth, or undyed when nobody has claimed it,
    /// so whose bed it is reads at a glance. <paramref name="rise"/> below 1 draws a bed being built: the rails,
    /// then the head and foot, then the mattress; the blanket comes when it is done.
    /// </summary>
    public static void DrawBed(Brush b, SimPoint p, bool framed, Color blanket, float rise = 1f)
    {
        if (!framed)
        {
            Bedroll(b, p, blanket);
            return;
        }
        float x = p.X * T, y = p.Y * T;
        Color wood = Palette.Log, lo = Palette.Log.Darkened(0.38f), ink = Palette.Ink;
        b.Clear().RoundRect(x + 3f, y + 2.5f, 2.8f, T - 4.5f, 1.2f).Shade(wood.Lightened(0.1f), lo, new Vector2(1f, 0f)).Stroke(ink, 0.7f);
        b.Clear().RoundRect(x + T - 5.8f, y + 2.5f, 2.8f, T - 4.5f, 1.2f).Shade(wood.Lightened(0.1f), lo, new Vector2(1f, 0f)).Stroke(ink, 0.7f);
        if (rise >= 0.75f)
        {
            b.Clear().RoundRect(x + 5.6f, y + 4f, T - 11.2f, T - 7.5f, 2f).Lit(Palette.Straw, 0.2f).Stroke(Palette.InkSoft, 0.7f);
            for (int k = 0; k < 3; k++)
                b.Line(x + 7f + k * 3.4f, y + 6f + (k % 2), x + 8.5f + k * 3.4f, y + T - 6f, Palette.Straw.Darkened(0.25f).WithAlpha(0.6f), 0.5f);
        }
        if (rise >= 1f)
        {
            b.Clear().RoundRect(x + 7f, y + 4.4f, T - 14f, 4.2f, 2f).Fill(Palette.Hex(0x9a8f78)).Stroke(Palette.InkSoft, 0.6f);
            b.Clear().RoundRect(x + 5.4f, y + 9.6f, T - 10.8f, T - 12.6f, 2f).Lit(blanket, 0.2f).Stroke(Palette.InkSoft, 0.7f);
            b.Line(x + 5.8f, y + 11f, x + T - 5.8f, y + 11f, blanket.Lightened(0.18f).WithAlpha(0.7f), 0.8f);
        }
        if (rise >= 0.4f)
        {
            b.Clear().RoundRect(x + 2.4f, y + 0.6f, T - 4.8f, 3.6f, 1.2f).Shade(wood.Lightened(0.12f), lo, new Vector2(0f, 1f)).Stroke(ink, 0.8f);
            b.Clear().RoundRect(x + 2.4f, y + T - 3.8f, T - 4.8f, 2.6f, 1f).Shade(wood.Lightened(0.06f), lo, new Vector2(0f, 1f)).Stroke(ink, 0.7f);
        }
    }

    /// <summary>Beds sort a little above a sleeper's feet, so whoever lies or stands on one is drawn on top of it.</summary>
    public static float BedSortY(SimPoint p) => (p.Y + 0.6f) * T;

    // ================= plans under way =================

    /// <summary>
    /// A roof section going up: rafters across from wall to wall, then boards laid from the north edge as
    /// the work goes on (<paramref name="progress"/>, 0 to 1). Faded like a finished roof while someone is under it.
    /// </summary>
    public void DrawRoofFrame(Brush b, SimRect area, float progress, float alpha, int salt)
    {
        Rect2 top = RoofTop(area).Grow(-0.5f);
        float frame = Math.Max(alpha, 0.8f);
        Color ink = Palette.Ink.WithAlpha(frame);
        Color beam = Palette.Log.WithAlpha(frame);
        int beams = Math.Max(2, (int)MathF.Round(top.Size.X / T) + 1);
        for (int k = 0; k < beams; k++)
        {
            float bx = top.Position.X + 2f + k * (top.Size.X - 4f) / (beams - 1);
            b.Clear().RoundRect(bx - 1.4f, top.Position.Y, 2.8f, top.Size.Y + 3f, 1f).Shade(beam.Lightened(0.1f), beam.Darkened(0.35f), new Vector2(1f, 0f)).Stroke(ink, 0.6f);
        }
        const float board = 4f;
        int rows = (int)(top.Size.Y / board), laid = (int)(rows * Math.Clamp(progress, 0f, 1f));
        for (int i = 0; i < laid; i++)
        {
            float y = top.Position.Y + i * board;
            Color c = Palette.RoofHi.Lerp(Palette.Roof, ArtHash.Unit(salt, i, 155) * 0.5f);
            b.Rect(top.Position.X, y, top.Size.X, board - 0.6f, c.WithAlpha(alpha));
            b.Rect(top.Position.X, y + board - 0.6f, top.Size.X, 0.6f, Palette.RoofLine.WithAlpha(0.9f * alpha));
        }
        if (laid > 0) b.RectOutline(new Rect2(top.Position.X, top.Position.Y, top.Size.X, laid * board), ink, 0.8f);
        b.Clear().RoundRect(top.Position.X - 1f, top.End.Y, top.Size.X + 2f, 3f, 1f).Shade(beam, beam.Darkened(0.4f), new Vector2(0f, 1f)).Stroke(ink, 0.7f);
    }

    /// <summary>A pile of logs: timber in the stockpile, or delivered to a plan's site.</summary>
    public static void DrawLogPile(Brush b, float x, float y, int count, int salt)
    {
        int logs = Math.Clamp(count, 1, 6);
        for (int i = 0; i < logs; i++)
        {
            int row = i < 3 ? 0 : i < 5 ? 1 : 2;
            int inRow = row == 0 ? i : row == 1 ? i - 3 : 0;
            int perRow = row == 0 ? Math.Min(3, logs) : row == 1 ? Math.Min(2, logs - 3) : 1;
            float ly = y - row * 4.2f - inRow * 0.3f;
            float lx = x - (perRow - 1) * 0.5f + (inRow - (perRow - 1) / 2f) * 0.6f;
            b.Push();
            b.Translate(lx, ly - inRow * 3.6f + row * 1.8f);
            b.Rotate(ArtHash.Signed(salt, i, 140) * 0.08f);
            b.Clear().RoundRect(-9f, -2.1f, 18f, 4.2f, 1.8f).Lit(Palette.Log, 0.2f).Stroke(Palette.Ink, 0.75f);
            b.Clear().Ellipse(8.6f, 0f, 1.6f, 2.1f, 10).Fill(Palette.LogEnd).Stroke(Palette.Ink, 0.6f);
            b.Pop();
        }
    }
}
