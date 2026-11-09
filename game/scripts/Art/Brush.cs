using System;
using Godot;

namespace Return.Game.Art;

/// <summary>
/// A small canvas-style drawing kit over a CanvasItem's draw calls, so the art direction's canvas
/// code ports almost line for line: a transform stack (translate, rotate, scale in local space),
/// shapes built into a scratch buffer (ellipses, rounded rects, capsules, polygons) and then
/// filled, shaded with a per-vertex gradient (light from the upper left), or outlined. Buffers are
/// reused (Godot takes exact-length arrays, so there is one array per length): no allocation per
/// frame. Only valid inside the target's _Draw.
/// </summary>
public sealed class Brush
{
    public const int MaxPoints = 96;

    private CanvasItem _target = null!;
    private Transform2D _xf = Transform2D.Identity;
    private readonly Transform2D[] _stack = new Transform2D[32];
    private int _depth;
    private readonly Vector2[] _work = new Vector2[MaxPoints];
    private int _count;
    private readonly Vector2[]?[] _exact = new Vector2[MaxPoints + 2][];
    private readonly Color[]?[] _colors = new Color[MaxPoints + 2][];
    private static readonly Vector2[] Unit = BuildUnitCircle(24);

    public CanvasItem Target => _target;

    /// <summary>The current transform (local figure units to the target's space).</summary>
    public Transform2D Transform => _xf;

    public int Count => _count;

    public void Begin(CanvasItem target)
    {
        _target = target;
        _xf = Transform2D.Identity;
        _depth = 0;
        _target.DrawSetTransformMatrix(_xf);
    }

    public void Push()
    {
        _stack[_depth++] = _xf;
    }

    public void Pop()
    {
        _xf = _stack[--_depth];
        _target.DrawSetTransformMatrix(_xf);
    }

    public void Translate(float x, float y)
    {
        _xf *= new Transform2D(0f, new Vector2(x, y));
        _target.DrawSetTransformMatrix(_xf);
    }

    public void Rotate(float radians)
    {
        if (radians == 0f) return;
        _xf *= new Transform2D(radians, Vector2.Zero);
        _target.DrawSetTransformMatrix(_xf);
    }

    public void Scale(float sx, float sy)
    {
        _xf *= new Transform2D(0f, new Vector2(sx, sy), 0f, Vector2.Zero);
        _target.DrawSetTransformMatrix(_xf);
    }

    // ---- building a shape in the scratch buffer ----

    public Brush Clear()
    {
        _count = 0;
        return this;
    }

    public Brush Add(float x, float y)
    {
        if (_count < MaxPoints) _work[_count++] = new Vector2(x, y);
        return this;
    }

    public Brush Add(Vector2 p) => Add(p.X, p.Y);

    /// <summary>An ellipse as a polygon (segments 8..24).</summary>
    public Brush Ellipse(float cx, float cy, float rx, float ry, int segments = 16)
    {
        _count = 0;
        rx = MathF.Max(0.01f, rx);
        ry = MathF.Max(0.01f, ry);
        int step = Math.Max(1, Unit.Length / Math.Clamp(segments, 8, Unit.Length));
        for (int i = 0; i < Unit.Length; i += step) Add(cx + Unit[i].X * rx, cy + Unit[i].Y * ry);
        return this;
    }

    /// <summary>A rounded rectangle (x, y top-left).</summary>
    public Brush RoundRect(float x, float y, float w, float h, float r)
    {
        _count = 0;
        r = MathF.Max(0f, MathF.Min(r, MathF.Min(w, h) / 2f));
        if (r < 0.05f)
        {
            Add(x, y).Add(x + w, y).Add(x + w, y + h).Add(x, y + h);
            return this;
        }
        Corner(x + w - r, y + r, r, -MathF.PI / 2f);
        Corner(x + w - r, y + h - r, r, 0f);
        Corner(x + r, y + h - r, r, MathF.PI / 2f);
        Corner(x + r, y + r, r, MathF.PI);
        return this;
    }

    private void Corner(float cx, float cy, float r, float start)
    {
        for (int i = 0; i <= 3; i++)
        {
            float a = start + i * (MathF.PI / 2f) / 3f;
            Add(cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
        }
    }

    /// <summary>A stadium (limb) from a to b with the given half-width.</summary>
    public Brush Capsule(Vector2 a, Vector2 b, float r)
    {
        _count = 0;
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.001f) return Ellipse(a.X, a.Y, r, r, 12);
        float ang = MathF.Atan2(d.Y, d.X);
        for (int i = 0; i <= 5; i++)
        {
            float t = ang - MathF.PI / 2f + i * MathF.PI / 5f;
            Add(b.X + MathF.Cos(t) * r, b.Y + MathF.Sin(t) * r);
        }
        for (int i = 0; i <= 5; i++)
        {
            float t = ang + MathF.PI / 2f + i * MathF.PI / 5f;
            Add(a.X + MathF.Cos(t) * r, a.Y + MathF.Sin(t) * r);
        }
        return this;
    }

    public Brush Poly(ReadOnlySpan<float> xy)
    {
        _count = 0;
        for (int i = 0; i + 1 < xy.Length; i += 2) Add(xy[i], xy[i + 1]);
        return this;
    }

    /// <summary>Mirrors the shape horizontally (for facing west).</summary>
    public Brush FlipX(bool flip = true)
    {
        if (!flip) return this;
        for (int i = 0; i < _count; i++) _work[i] = new Vector2(-_work[i].X, _work[i].Y);
        return this;
    }

    public Brush Offset(float dx, float dy)
    {
        for (int i = 0; i < _count; i++) _work[i] += new Vector2(dx, dy);
        return this;
    }

    // ---- painting the shape ----

    public Brush Fill(Color c)
    {
        if (_count < 3 || c.A <= 0.001f) return this;
        _target.DrawColoredPolygon(Exact(_count), c);
        return this;
    }

    /// <summary>
    /// Fills with a gradient across the shape along <paramref name="dir"/>: <paramref name="lit"/>
    /// where the light comes from, <paramref name="dark"/> on the far side.
    /// </summary>
    public Brush Shade(Color lit, Color dark, Vector2 dir)
    {
        if (_count < 3) return this;
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < _count; i++)
        {
            float p = _work[i].Dot(dir);
            if (p < min) min = p;
            if (p > max) max = p;
        }
        float span = MathF.Max(0.0001f, max - min);
        Color[] cols = Colors(_count);
        for (int i = 0; i < _count; i++) cols[i] = lit.Lerp(dark, (_work[i].Dot(dir) - min) / span);
        _target.DrawPolygon(Exact(_count), cols);
        return this;
    }

    /// <summary>Light from the upper left: the base colour lifted on the lit side and dropped on the far side.</summary>
    public Brush Lit(Color baseColor, float amount = 0.22f) =>
        Shade(baseColor.Lightened(amount * 0.6f), baseColor.Darkened(amount), LightDir);

    public Brush Stroke(Color c, float width)
    {
        if (_count < 2 || c.A <= 0.001f) return this;
        Vector2[] closed = Exact(_count + 1, closeLoop: true);
        _target.DrawPolyline(closed, c, width, true);
        return this;
    }

    public Brush StrokeOpen(Color c, float width)
    {
        if (_count < 2 || c.A <= 0.001f) return this;
        _target.DrawPolyline(Exact(_count), c, width, true);
        return this;
    }

    // ---- direct primitives ----

    public void Line(float x1, float y1, float x2, float y2, Color c, float width) =>
        _target.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), c, width, true);

    public void Dot(float x, float y, float r, Color c) => _target.DrawCircle(new Vector2(x, y), r, c);

    public void Rect(float x, float y, float w, float h, Color c) => _target.DrawRect(new Rect2(x, y, w, h), c);

    /// <summary>A texture centred on (x, y), w by h, turned by <paramref name="rotation"/>.</summary>
    public void Texture(Texture2D? tex, float x, float y, float w, float h, float rotation, Color modulate)
    {
        if (tex == null || modulate.A <= 0.001f) return;
        if (rotation == 0f)
        {
            _target.DrawTextureRect(tex, new Rect2(x - w / 2f, y - h / 2f, w, h), false, modulate);
            return;
        }
        Push();
        Translate(x, y);
        Rotate(rotation);
        _target.DrawTextureRect(tex, new Rect2(-w / 2f, -h / 2f, w, h), false, modulate);
        Pop();
    }

    /// <summary>The direction light falls along (from the upper left toward the lower right).</summary>
    public static readonly Vector2 LightDir = new Vector2(0.75f, 0.66f).Normalized();

    private Vector2[] Exact(int n, bool closeLoop = false)
    {
        Vector2[] a = _exact[n] ??= new Vector2[n];
        int copy = closeLoop ? n - 1 : n;
        Array.Copy(_work, a, copy);
        if (closeLoop) a[n - 1] = _work[0];
        return a;
    }

    private Color[] Colors(int n) => _colors[n] ??= new Color[n];

    private static Vector2[] BuildUnitCircle(int n)
    {
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * MathF.Tau / n;
            pts[i] = new Vector2(MathF.Cos(a), MathF.Sin(a));
        }
        return pts;
    }
}
