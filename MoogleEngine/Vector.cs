
public class Vector
{
  double[] vector;

  public Vector(double[] b)
  {
    vector = b;
  }
    //Método para comprobar que los vectores son del mismo tamaño
  private static bool SameSize(double[]a,double[]b)
  {
    bool issamesize=false;
    if (a.Length==b.Length)
    {
        issamesize=true;
    }
    return issamesize;
  }

  //Método para sumar vectores
  public static double[] Sum(double[]a,double[]b)
  {
    double[] sum=new double[0];
    if (SameSize(a,b))
    {
        sum=new double[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            sum[i]=a[i]+b[i];
        }
    }
    else {Console.WriteLine("No se pueden sumar");}
    return sum;
  }
   
   //Método para multiplicar un vector por un escalar
   public static double[] ScalarxVector(double[]a,double c)
   {
      double[] multiplied=new double[a.Length];
      for (int i = 0; i < a.Length; i++)
      {
        multiplied[i]=a[i]*c;
      }
      return multiplied;
   }

//Método par multiplicar dos vectores
   public static double VectorxVector(double[]a,double[]b)
   {
     double result=0;
     if (SameSize(a,b))
     {
        for (int i = 0; i < a.Length; i++)
        {
            result+=a[i]*b[i];
        }
     }
     else
     {
        Console.WriteLine("Vectores desiguales");
        return double.NaN;
     }
     return result;
   }

   //Método para calcular la norma de un vector
   public static double Magnitude(double[]a)
   {
    double magnitude =0;
    for (int i = 0; i < a.Length; i++)
    {
        magnitude+=Math.Pow(a[i],2);
    }
    magnitude=Math.Sqrt(magnitude);
    return magnitude;
   }

// Si se desea restar vectores solo se debe multiplicar uno de ellos por -1 y sumarlo al otro
}

