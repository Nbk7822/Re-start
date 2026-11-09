using System;
using Godot;

namespace Return.Game.Art;

/// <summary>
/// A canvas-style drawing kit, so the art direction's canvas code ports almost line for line: a
/// transform stack (translate, rotate, scale in local space) and shapes built into a scratch buffer
/// (ellipses, rounded rects, capsules, polygons) that are filled, shaded with a per-vertex gradient
/// (light from the upper left) or outlined.
///
/// For speed it batches: every shape is triangulated on the CPU, transformed, and appended to one
/// big triangle list, which goes to Godot as a single command when it must (before a texture is
/// drawn, or at the end of the pass). A frame of the camp is a handful of draw calls instead of
/// thousands. Outlines are antialiased by hand: a one-pixel feather of fading alpha on each side
/// (the Compatibility renderer has no 2D MSAA). Buffers are reused; no allocation per frame. Only
/// valid inside the target's _Draw, between Begin and End.
/// </summary>
public sealed class Brush
{
    public const int MaxPoints = 96;
    private const int MaxVertices = 1 << 16;

    private CanvasItem _target = null!;
    private Rid _rid;
    private Transform2D _xf = Transform2D.Identity;
    private float _xfScale = 1f;
    private readonly Transform2D[] _stack = new Transform2D[32];
    private int _depth;
    private float _pxPerUnit = 1f;

    private readonly Vector2[] _work = new Vector2[MaxPoints];
    private readonly int[] _ring = new int[MaxPoints];
    private int _count;
    private bool _convex;

    private readonly Vector2[] _pts = new Vector2[MaxVertices];
    private readonly Color[] _cols = new Color[MaxVertices];
    private readonly int[] _idx = new int[MaxVertices * 3];
    private int _nv;
    private int _ni;
    private readonly Vector2[]?[] _bucketPts = new Vector2[17][];
    private readonly Color[]?[] _bucketCols = new Color[17][];
    private readonly int[]?[] _bucketIdx = new int[19][];
    private readonly Vector2[]?[] _exact = new Vector2[MaxPoints + 2][];
    private static readonly Vector2[] Unit = BuildUnitCircle(24);

    /// <summary>The direction light falls along (from the upper left toward the lower right).</summary>
    public static readonly Vector2 LightDir = new Vector2(0.75f, 0.66f).Normalized();

    public CanvasItem Target => _target;

    /// <summary>The current transform (local units to the target's space).</summary>
    public Transform2D Transform => _xf;

    public int Count => _count;

    /// <summary>Draw calls issued since Begin (batches plus textures), for the frame check.</summary>
    public int Calls { get; private set; }

    /// <summary>
    /// Starts a pass on a canvas item. <paramref name="pixelsPerUnit"/> is how many physical screen
    /// pixels one unit of the item's space covers (for the outline feather).
    /// </summary>
    public void Begin(CanvasItem target, float pixelsPerUnit)
    {
        _target = target;
        _rid = target.GetCanvasItem();
        _xf = Transform2D.Identity;
        _xfScale = 1f;
        _depth = 0;
        _nv = 0;
        _ni = 0;
        Calls = 0;
        _pxPerUnit = MathF.Max(0.01f, pixelsPerUnit);
        _target.DrawSetTransformMatrix(Transform2D.Identity);
    }

    /// <summary>Sends what is batched so far.</summary>
    public void Flush()
    {
        if (_ni == 0)
        {
            _nv = 0;
            return;
        }
        int vb = Bucket(_nv, 8), ib = Bucket(_ni, 8);
        Vector2[] p = _bucketPts[vb] ??= new Vector2[1 << vb];
        Color[] c = _bucketCols[vb] ??= new Color[1 << vb];
        int[] ix = _bucketIdx[ib] ??= new int[1 << ib];
        Array.Copy(_pts, p, _nv);
        Array.Copy(_cols, c, _nv);
        Array.Copy(_idx, ix, _ni);
        Array.Clear(ix, _ni, ix.Length - _ni); // degenerate triangles in the unused tail
        _target.DrawSetTransformMatrix(Transform2D.Identity);
        RenderingServer.CanvasItemAddTriangleArray(_rid, ix, p, c, null, null, null, default, _ni / 3);
        Calls++;
        _nv = 0;
        _ni = 0;
    }

    public void End()
    {
        Flush();
        _xf = Transform2D.Identity;
        _xfScale = 1f;
        _depth = 0;
    }

    public void Push() => _stack[_depth++] = _xf;

    public void Pop()
    {
        _xf = _stack[--_depth];
        _xfScale = ScaleOf(_xf);
    }

    public void Translate(float x, float y) => _xf *= new Transform2D(0f, new Vector2(x, y));

    public void Rotate(float radians)
    {
        if (radians != 0f) _xf *= new Transform2D(radians, Vector2.Zero);
    }

    public void Scale(float sx, float sy)
    {
        _xf *= new Transform2D(0f, new Vector2(sx, sy), 0f, Vector2.Zero);
        _xfScale = ScaleOf(_xf);
    }

    // ---- building a shape in the scratch buffer ----

    public Brush Clear()
    {
        _count = 0;
        _convex = false;
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
        _convex = true;
        return this;
    }

    /// <summary>A rounded rectangle (x, y top-left).</summary>
    public Brush RoundRect(float x, float y, float w, float h, float r)
    {
        _count = 0;
        r = MathF.Max(0f, MathF.Min(r, MathF.Min(w, h) / 2f));
        if (r < 0.05f) Add(x, y).Add(x + w, y).Add(x + w, y + h).Add(x, y + h);
        else
        {
            Corner(x + w - r, y + r, r, -MathF.PI / 2f);
            Corner(x + w - r, y + h - r, r, 0f);
            Corner(x + r, y + h - r, r, MathF.PI / 2f);
            Corner(x + r, y + r, r, MathF.PI);
        }
        _convex = true;
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
        _convex = true;
        return this;
    }

    public Brush Poly(ReadOnlySpan<float> xy)
    {
        Clear();
        for (int i = 0; i + 1 < xy.Length; i += 2) Add(xy[i], xy[i + 1]);
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
        Reserve(_count, _count * 3);
        int b = _nv;
        for (int i = 0; i < _count; i++) Vertex(_xf * _work[i], c);
        Triangulate(b);
        return this;
    }

    /// <summary>Fills with a gradient across the shape along <paramref name="dir"/>: <paramref name="lit"/> where the light comes from, <paramref name="dark"/> on the far side.</summary>
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
        Reserve(_count, _count * 3);
        int b = _nv;
        for (int i = 0; i < _count; i++) Vertex(_xf * _work[i], lit.Lerp(dark, (_work[i].Dot(dir) - min) / span));
        Triangulate(b);
        return this;
    }

    /// <summary>Light from the upper left: the base colour lifted on the lit side and dropped on the far side.</summary>
    public Brush Lit(Color baseColor, float amount = 0.22f) =>
        Shade(baseColor.Lightened(amount * 0.6f), baseColor.Darkened(amount), LightDir);

    /// <summary>Outlines the shape as a closed loop.</summary>
    public Brush Stroke(Color c, float width)
    {
        if (_count < 2 || c.A <= 0.001f) return this;
        for (int i = 0; i < _count; i++) Segment(_xf * _work[i], _xf * _work[(i + 1) % _count], c, width * _xfScale, true);
        return this;
    }

    public Brush StrokeOpen(Color c, float width)
    {
        if (_count < 2 || c.A <= 0.001f) return this;
        for (int i = 0; i + 1 < _count; i++) Segment(_xf * _work[i], _xf * _work[i + 1], c, width * _xfScale, i + 2 < _count);
        return this;
    }

    // ---- direct primitives (batched) ----

    public void Line(float x1, float y1, float x2, float y2, Color c, float width)
    {
        if (c.A <= 0.001f) return;
        Segment(_xf * new Vector2(x1, y1), _xf * new Vector2(x2, y2), c, width * _xfScale, false);
    }

    public void Line(Vector2 a, Vector2 b, Color c, float width) => Line(a.X, a.Y, b.X, b.Y, c, width);

    /// <summary>Line segments as pairs of points (like DrawMultiline), one colour each.</summary>
    public void Segments(Vector2[] pairs, Color[]? colors, Color color, float width)
    {
        for (int i = 0; i + 1 < pairs.Length; i += 2)
            Segment(_xf * pairs[i], _xf * pairs[i + 1], colors == null ? color : colors[i / 2], width * _xfScale, false);
    }

    public void Dashed(Vector2 a, Vector2 b, Color c, float width, float dash)
    {
        float len = a.DistanceTo(b);
        if (len < 0.01f) return;
        Vector2 d = (b - a) / len;
        for (float t = 0f; t < len; t += dash * 2f) Line(a + d * t, a + d * MathF.Min(len, t + dash), c, width);
    }

    public void Dot(float x, float y, float r, Color c)
    {
        if (c.A <= 0.001f) return;
        int segs = r * _xfScale * _pxPerUnit > 6f ? 16 : 8;
        Ellipse(x, y, r, r, segs).Fill(c);
    }

    public void Rect(float x, float y, float w, float h, Color c)
    {
        if (c.A <= 0.001f) return;
        Reserve(4, 6);
        int b = _nv;
        Vertex(_xf * new Vector2(x, y), c);
        Vertex(_xf * new Vector2(x + w, y), c);
        Vertex(_xf * new Vector2(x + w, y + h), c);
        Vertex(_xf * new Vector2(x, y + h), c);
        Tri(b, b + 1, b + 2);
        Tri(b, b + 2, b + 3);
    }

    public void Rect(Rect2 r, Color c) => Rect(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y, c);

    /// <summary>A blade of grass: a thin triangle from a two-point root to a tip, darker at the root.</summary>
    public void Blade(Vector2 left, Vector2 right, Vector2 tip, Color root, Color top)
    {
        Reserve(3, 3);
        int v = _nv;
        Vertex(_xf * left, root);
        Vertex(_xf * right, root);
        Vertex(_xf * tip, top);
        Tri(v, v + 1, v + 2);
    }

    public void RectOutline(Rect2 r, Color c, float width)
    {
        Vector2 a = r.Position, b = new(r.End.X, r.Position.Y), cc = r.End, d = new(r.Position.X, r.End.Y);
        Segment(_xf * a, _xf * b, c, width * _xfScale, true);
        Segment(_xf * b, _xf * cc, c, width * _xfScale, true);
        Segment(_xf * cc, _xf * d, c, width * _xfScale, true);
        Segment(_xf * d, _xf * a, c, width * _xfScale, true);
    }

    // ---- textures (flush the batch, then draw directly) ----

    /// <summary>A texture centred on (x, y), w by h, turned by <paramref name="rotation"/>.</summary>
    public void Texture(Texture2D? tex, float x, float y, float w, float h, float rotation, Color modulate)
    {
        if (tex == null || modulate.A <= 0.001f) return;
        Flush();
        Transform2D t = _xf * new Transform2D(rotation, new Vector2(x, y));
        _target.DrawSetTransformMatrix(t);
        _target.DrawTextureRect(tex, new Rect2(-w / 2f, -h / 2f, w, h), false, modulate);
        _target.DrawSetTransformMatrix(Transform2D.Identity);
        Calls++;
    }

    /// <summary>A texture centred on (x, y), squashed vertically by <paramref name="squash"/> after turning (a decal lying on the ground).</summary>
    public void Decal(Texture2D? tex, float x, float y, float size, float rotation, float squash, Color modulate)
    {
        if (tex == null || modulate.A <= 0.001f) return;
        Flush();
        Transform2D t = _xf * new Transform2D(0f, new Vector2(x, y)) * new Transform2D(0f, new Vector2(1f, squash), 0f, Vector2.Zero) * new Transform2D(rotation, Vector2.Zero);
        _target.DrawSetTransformMatrix(t);
        _target.DrawTextureRect(tex, new Rect2(-size / 2f, -size / 2f, size, size), false, modulate);
        _target.DrawSetTransformMatrix(Transform2D.Identity);
        Calls++;
    }

    public void TextureRect(Texture2D? tex, Rect2 rect, Color modulate)
    {
        if (tex == null || modulate.A <= 0.001f) return;
        Flush();
        _target.DrawSetTransformMatrix(_xf);
        _target.DrawTextureRect(tex, rect, false, modulate);
        _target.DrawSetTransformMatrix(Transform2D.Identity);
        Calls++;
    }

    /// <summary>Part of a texture over a rect; with the node's texture repeat on, a source larger than the texture tiles it.</summary>
    public void TextureRegion(Texture2D? tex, Rect2 rect, Rect2 source, Color modulate, bool transpose = false)
    {
        if (tex == null || modulate.A <= 0.001f) return;
        Flush();
        _target.DrawSetTransformMatrix(_xf);
        _target.DrawTextureRectRegion(tex, rect, source, modulate, transpose);
        _target.DrawSetTransformMatrix(Transform2D.Identity);
        Calls++;
    }

    /// <summary>A textured polygon (the river surface).</summary>
    public void TexturedPolygon(Vector2[] points, Color[] colors, Vector2[] uvs, Texture2D? tex)
    {
        if (tex == null || points.Length < 3) return;
        Flush();
        _target.DrawSetTransformMatrix(_xf);
        _target.DrawPolygon(points, colors, uvs, tex);
        _target.DrawSetTransformMatrix(Transform2D.Identity);
        Calls++;
    }

    /// <summary>A large untextured polygon given as an array (the river), batched.</summary>
    public void Polygon(Vector2[] points, Color c)
    {
        Clear();
        if (points.Length > MaxPoints)
        {
            Flush();
            _target.DrawSetTransformMatrix(_xf);
            _target.DrawColoredPolygon(points, c);
            _target.DrawSetTransformMatrix(Transform2D.Identity);
            Calls++;
            return;
        }
        foreach (Vector2 p in points) Add(p);
        Fill(c);
    }

    /// <summary>An open polyline given as an array, batched.</summary>
    public void Polyline(Vector2[] points, Color c, float width)
    {
        for (int i = 0; i + 1 < points.Length; i++) Segment(_xf * points[i], _xf * points[i + 1], c, width * _xfScale, true);
    }

    /// <summary>The shape as an exact-length array (for callers that need one).</summary>
    public Vector2[] ShapeArray()
    {
        Vector2[] a = _exact[_count] ??= new Vector2[_count];
        Array.Copy(_work, a, _count);
        return a;
    }

    // ---- internals ----

    private void Vertex(Vector2 p, Color c)
    {
        _pts[_nv] = p;
        _cols[_nv] = c;
        _nv++;
    }

    private void Tri(int a, int b, int c)
    {
        _idx[_ni++] = a;
        _idx[_ni++] = b;
        _idx[_ni++] = c;
    }

    private void Reserve(int vertices, int indices)
    {
        if (_nv + vertices > MaxVertices || _ni + indices > _idx.Length) Flush();
    }

    /// <summary>Triangles for the scratch shape, whose vertices were just appended from index b.</summary>
    private void Triangulate(int b)
    {
        int n = _count;
        if (_convex || n == 3)
        {
            for (int i = 1; i + 1 < n; i++) Tri(b, b + i, b + i + 1);
            return;
        }
        // Ear clipping for concave outlines (hair, hems, leaves); a fan if it ever gets stuck.
        float area = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = _work[i], q = _work[(i + 1) % n];
            area += p.X * q.Y - q.X * p.Y;
        }
        bool ccw = area > 0f;
        for (int i = 0; i < n; i++) _ring[i] = i;
        int m = n, guard = 0, k = 0;
        while (m > 3 && guard < m)
        {
            int i0 = _ring[(k + m - 1) % m], i1 = _ring[k % m], i2 = _ring[(k + 1) % m];
            if (IsEar(i0, i1, i2, ccw, m))
            {
                Tri(b + i0, b + i1, b + i2);
                int at = k % m;
                for (int j = at; j < m - 1; j++) _ring[j] = _ring[j + 1];
                m--;
                guard = 0;
            }
            else
            {
                k++;
                guard++;
            }
        }
        if (m == 3) Tri(b + _ring[0], b + _ring[1], b + _ring[2]);
        else for (int i = 1; i + 1 < m; i++) Tri(b + _ring[0], b + _ring[i], b + _ring[i + 1]);
    }

    private bool IsEar(int i0, int i1, int i2, bool ccw, int m)
    {
        Vector2 a = _work[i0], p = _work[i1], c = _work[i2];
        float cross = (p.X - a.X) * (c.Y - a.Y) - (p.Y - a.Y) * (c.X - a.X);
        if (ccw ? cross <= 0f : cross >= 0f) return false;
        for (int j = 0; j < m; j++)
        {
            int v = _ring[j];
            if (v == i0 || v == i1 || v == i2) continue;
            if (Inside(_work[v], a, p, c)) return false;
        }
        return true;
    }

    private static bool Inside(Vector2 q, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (q.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (q.Y - b.Y);
        float d2 = (q.X - c.X) * (b.Y - c.Y) - (b.X - c.X) * (q.Y - c.Y);
        float d3 = (q.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (q.Y - a.Y);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    /// <summary>
    /// A line segment in layer space with a one-pixel feather on each side: a solid core and two
    /// strips fading to nothing. Thinner than a pixel, the core stays a pixel wide and fades instead.
    /// </summary>
    private void Segment(Vector2 a, Vector2 b, Color c, float width, bool extend)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.0001f) return;
        d /= len;
        float px = 1f / _pxPerUnit;
        float half = width * 0.5f;
        float alpha = c.A;
        if (width < px)
        {
            alpha *= MathF.Max(0.25f, width / px);
            half = px * 0.5f;
        }
        if (extend)
        {
            a -= d * half * 0.5f;
            b += d * half * 0.5f;
        }
        var n = new Vector2(-d.Y, d.X);
        float feather = px * 0.9f;
        Color solid = new(c.R, c.G, c.B, alpha), clear = new(c.R, c.G, c.B, 0f);
        Reserve(8, 18);
        int v = _nv;
        Vertex(a + n * (half + feather), clear);
        Vertex(a + n * half, solid);
        Vertex(a - n * half, solid);
        Vertex(a - n * (half + feather), clear);
        Vertex(b + n * (half + feather), clear);
        Vertex(b + n * half, solid);
        Vertex(b - n * half, solid);
        Vertex(b - n * (half + feather), clear);
        Tri(v, v + 1, v + 5);
        Tri(v, v + 5, v + 4);
        Tri(v + 1, v + 2, v + 6);
        Tri(v + 1, v + 6, v + 5);
        Tri(v + 2, v + 3, v + 7);
        Tri(v + 2, v + 7, v + 6);
    }

    private static int Bucket(int n, int min)
    {
        int b = min;
        while ((1 << b) < n) b++;
        return b;
    }

    private static float ScaleOf(Transform2D t) => MathF.Sqrt(MathF.Abs(t.X.X * t.Y.Y - t.X.Y * t.Y.X));

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
