 
public class Vector
{
  public double[] Values { get; }

  public Vector(double[] b)
  {
    Values = b;
  }

  public double Dot(Vector other)
  {
    double result = 0;
    for (int i = 0; i < Values.Length; i++)
    {
      result += Values[i] * other.Values[i];
    }
    return result;
  }

  public double Magnitude()
  {
    double sum = 0;
    for (int i = 0; i < Values.Length; i++)
    {
      sum += Values[i] * Values[i];
    }
    return Math.Sqrt(sum);
  }

  public double CosineSimilarity(Vector other)
  {
    double dot = Dot(other);
    double mag = Magnitude() * other.Magnitude();
    if (mag == 0) return 0;
    return dot / mag;
  }
}

