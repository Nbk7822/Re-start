using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// 8-way A* on the tile grid. Straight steps cost 10, diagonal 14, and entering a bush doubles the
/// cost. Diagonals never cut a corner: both orthogonal neighbours must be passable. Ties are broken
/// by f, then h, then y, then x, through a packed heap key, so the same query always returns the
/// same path. The returned path excludes the start and includes the goal.
/// </summary>
public static class Pathfinder
{
    private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

    public static List<Point>? Find(Map map, Point from, Point to)
    {
        if (!map.InBounds(from) || !map.Passable(to)) return null;
        if (from == to) return new List<Point>();

        int n = map.Width * map.Height;
        var g = new int[n];
        var parent = new int[n];
        var closed = new bool[n];
        Array.Fill(g, int.MaxValue);
        Array.Fill(parent, -1);

        int start = map.Index(from), goal = map.Index(to);
        var heap = new MinHeap(64);
        g[start] = 0;
        heap.Push(Key(Heuristic(from, to), Heuristic(from, to), from), start);

        while (heap.Count > 0)
        {
            int cur = heap.Pop();
            if (closed[cur]) continue;
            if (cur == goal) return Rebuild(map, parent, start, goal);
            closed[cur] = true;
            int cx = cur % map.Width, cy = cur / map.Width;
            for (int k = 0; k < 8; k++)
            {
                var next = new Point(cx + Dx[k], cy + Dy[k]);
                if (!map.Passable(next)) continue;
                bool diagonal = k >= 4;
                if (diagonal && (!map.Passable(new Point(cx + Dx[k], cy)) || !map.Passable(new Point(cx, cy + Dy[k])))) continue;
                int ni = map.Index(next);
                if (closed[ni]) continue;
                int step = diagonal ? 14 : 10;
                if (map.IsBush(next)) step *= 2;
                int ng = g[cur] + step;
                if (ng >= g[ni]) continue;
                g[ni] = ng;
                parent[ni] = cur;
                int h = Heuristic(next, to);
                heap.Push(Key(ng + h, h, next), ni);
            }
        }
        return null;
    }

    private static int Heuristic(Point a, Point b)
    {
        int dx = Math.Abs(a.X - b.X), dy = Math.Abs(a.Y - b.Y);
        return 10 * Math.Max(dx, dy) + 4 * Math.Min(dx, dy);
    }

    private static long Key(int f, int h, Point p) => ((long)f << 40) | ((long)h << 20) | ((long)p.Y << 10) | (long)p.X;

    private static List<Point> Rebuild(Map map, int[] parent, int start, int goal)
    {
        var path = new List<Point>();
        for (int i = goal; i != start; i = parent[i])
            path.Add(new Point(i % map.Width, i / map.Width));
        path.Reverse();
        return path;
    }

    /// <summary>Binary min-heap of (key, node); duplicate nodes are skipped on pop via the closed set.</summary>
    private sealed class MinHeap
    {
        private long[] _keys;
        private int[] _nodes;

        public MinHeap(int capacity)
        {
            _keys = new long[capacity];
            _nodes = new int[capacity];
        }

        public int Count { get; private set; }

        public void Push(long key, int node)
        {
            if (Count == _keys.Length)
            {
                Array.Resize(ref _keys, Count * 2);
                Array.Resize(ref _nodes, Count * 2);
            }
            int i = Count++;
            while (i > 0)
            {
                int up = (i - 1) / 2;
                if (_keys[up] <= key) break;
                _keys[i] = _keys[up];
                _nodes[i] = _nodes[up];
                i = up;
            }
            _keys[i] = key;
            _nodes[i] = node;
        }

        public int Pop()
        {
            int top = _nodes[0];
            long key = _keys[--Count];
            int node = _nodes[Count];
            int i = 0;
            while (true)
            {
                int child = 2 * i + 1;
                if (child >= Count) break;
                if (child + 1 < Count && _keys[child + 1] < _keys[child]) child++;
                if (_keys[child] >= key) break;
                _keys[i] = _keys[child];
                _nodes[i] = _nodes[child];
                i = child;
            }
            _keys[i] = key;
            _nodes[i] = node;
            return top;
        }
    }
}
