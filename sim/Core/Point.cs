namespace Return.Sim.Core;

/// <summary>A tile coordinate.</summary>
public readonly record struct Point(int X, int Y)
{
    public static readonly Point None = new(-1, -1);

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);

    public int ChebyshevTo(Point o) => Math.Max(Math.Abs(X - o.X), Math.Abs(Y - o.Y));

    public int ManhattanTo(Point o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y);

    public override string ToString() => "(" + X + "," + Y + ")";
}

/// <summary>An axis-aligned block of tiles.</summary>
public readonly record struct Rect(int X, int Y, int W, int H)
{
    public bool Contains(Point p) => p.X >= X && p.X < X + W && p.Y >= Y && p.Y < Y + H;

    public Point Center => new(X + W / 2, Y + H / 2);
}
