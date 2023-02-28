using System;
using System.Linq;

namespace DotNetDesignPatternDemos.Structural.Adapter
{
  // Vector2f, Vector3i

  public interface IInteger
  {
    int Value { get; }
  }

  public static class Dimensions
  {
    public class Two : IInteger
    {
      public int Value => 2;
    }

    public class Three : IInteger
    {
      public int Value => 3;
    }
  }

  public abstract class Vector<TSelf, T, D>
    where D : IInteger, new()
    where TSelf : Vector<TSelf, T, D>, new()
  {
    protected T[] Data;

    public Vector()
    {
      Data = new T[new D().Value];
    }

    public Vector(params T[] values)
    {
      var requiredSize = new D().Value;
      Data = new T[requiredSize];

      var providedSize = values.Length;

      for (int i = 0; i < Math.Min(requiredSize, providedSize); ++i)
        Data[i] = values[i];
    }

    public static TSelf Create(params T[] values)
    {
      var result = new TSelf();
      var requiredSize = new D().Value;
      result.Data = new T[requiredSize];

      var providedSize = values.Length;

      for (int i = 0; i < Math.Min(requiredSize, providedSize); ++i)
        result.Data[i] = values[i];

      return result;
    }

    public T this[int index]
    {
      get => Data[index];
      set => Data[index] = value;
    }

    public T X
    {
      get => Data[0];
      set => Data[0] = value;
    }
  }

  public class VectorOfFloat<TSelf, D>
    : Vector<TSelf, float, D>
    where D : IInteger, new()
    where TSelf : Vector<TSelf, float, D>, new()
  {
  }

  public class VectorOfInt<TSelf, D> : Vector<TSelf, int, D>
    where D : IInteger, new()
    where TSelf : Vector<TSelf, int, D>, new()
  {
    public VectorOfInt()
    {
    }

    public VectorOfInt(params int[] values) : base(values)
    {
    }

    public static TSelf operator +
      (VectorOfInt<TSelf, D> lhs, VectorOfInt<TSelf, D> rhs)
    {
      var result = new VectorOfInt<TSelf, D>();
      var dim = new D().Value;
      for (int i = 0; i < dim; i++)
      {
        result[i] = lhs[i] + rhs[i];
      }

      return Create(result.Data);
    }
  }

  public class Vector2i : VectorOfInt<Vector2i, Dimensions.Two>
  {
    public Vector2i()
    {

    }
    public Vector2i(params int[] values) : base(values)
    {
    }
  }

  public class Vector3f
    : VectorOfFloat<Vector3f, Dimensions.Three>
  {
    public override string ToString()
    {
      return $"{string.Join(",", Data)}";
    }
  }

  class Demo
  {
    public static void Main(string[] args)
    {
      Vector2i v = new Vector2i(1, 2);
      v[0] = 0;

      var vv = new Vector2i(3, 2);

      Vector2i customCreateResult = Vector2i.Create(1, 2, 3);
      Vector2i sumResult = v + vv;

      //var u = Vector3f.Create(3.5f, 2.2f, 1);

    }
  }
}