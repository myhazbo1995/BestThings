using System.Collections.ObjectModel;
using System.Linq;
using MoreLinq;
using static System.Console;

public class Point : IEquatable<Point?>
{
    public int X, Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object? obj)
    {
        return this.Equals(obj as Point);
    }

    public bool Equals(Point? other)
    {
        return other is not null &&
               X == other.X &&
               Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
    }

    public static bool operator ==(Point? left, Point? right)
    {
        return EqualityComparer<Point>.Default.Equals(left, right);
    }

    public static bool operator !=(Point? left, Point? right)
    {
        return !(left == right);
    }
}

public class Line : IEquatable<Line?>
{
    public Point Start, End;

    public Line(Point start, Point end)
    {
        Start = start;
        End = end;
    }

    public override bool Equals(object? obj)
    {
        return this.Equals(obj as Line);
    }

    public bool Equals(Line? other)
    {
        return other is not null &&
               EqualityComparer<Point>.Default.Equals(Start, other.Start) &&
               EqualityComparer<Point>.Default.Equals(End, other.End);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }

    public static bool operator ==(Line? left, Line? right)
    {
        return EqualityComparer<Line>.Default.Equals(left, right);
    }

    public static bool operator !=(Line? left, Line? right)
    {
        return !(left == right);
    }
}

public abstract class VectorObject
  : Collection<Line>
{ }

public class VectorRectangle : VectorObject
{
    public VectorRectangle(int x, int y, int width, int height)
    {
        Add(new Line(new Point(x, y), new Point(x + width, y)));
        Add(new Line(new Point(x + width, y), new Point(x + width, y + height)));
        Add(new Line(new Point(x, y), new Point(x, y + height)));
        Add(new Line(new Point(x, y + height), new Point(x + width, y + height)));
    }
}

public class LineToPointAdapter : Collection<Point>
{
    private static int count = 0;

    public LineToPointAdapter(Line line)
    {
        WriteLine($"{++count}: Generating points for line"
          + $" [{line.Start.X},{line.Start.Y}]-"
          + $"[{line.End.X},{line.End.Y}] (no caching)");

        int left = Math.Min(line.Start.X, line.End.X);
        int right = Math.Max(line.Start.X, line.End.X);
        int top = Math.Min(line.Start.Y, line.End.Y);
        int bottom = Math.Max(line.Start.Y, line.End.Y);

        if (right - left == 0)
        {
            for (int y = top; y <= bottom; ++y)
            {
                Add(new Point(left, y));
            }
        }
        else if (line.End.Y - line.Start.Y == 0)
        {
            for (int x = left; x <= right; ++x)
            {
                Add(new Point(x, top));
            }
        }
    }
}

public class Demo
{
    private static readonly List<VectorObject> vectorObjects
      = new List<VectorObject>
    {
      new VectorRectangle(1, 1, 10, 10),
      new VectorRectangle(3, 3, 6, 6)
    };

    // the interface we have
    public static void DrawPoint(Point p)
    {
        Write(".");
    }

    static void Main()
    {
        DrawPoints();
        DrawPoints();
    }

    private static List<Point> points = new List<Point>();
    private static bool prepared = false;

    private static void Prepare()
    {
        if (prepared) return;
        foreach (var vo in vectorObjects)
        {
            foreach (var line in vo)
            {
                var adapter = new LineToPointAdapter(line);
                adapter.ForEach(p => points.Add(p));
            }
        }
        prepared = true;
    }

    private static void DrawPointsLazy()
    {
        Prepare();
        points.ForEach(DrawPoint);
    }

    private static void DrawPoints()
    {
        foreach (var vo in vectorObjects)
        {
            foreach (var line in vo)
            {
                var adapter = new LineToPointAdapter(line);
                adapter.ForEach(DrawPoint);
            }
        }
    }
}