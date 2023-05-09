
public class Matrix
{
  double[,] matrix;

  public Matrix(double[,] a)
  {
    matrix = a;
  }
  //Método para comprobar que las matrices se puedan sumar
  private static bool SameSize(double[,]a,double[,]b)
  {
    bool issamesize = false;
    if ((a.GetLength(0)==b.GetLength(0))&&(a.GetLength(1)==b.GetLength(1)))
    {
        issamesize=true;
    }
    return issamesize;
  }

  /*Método para comprobar que las matrices se puedan multiplicar(resultados:-1
  (no son multiplicables);0(son multiplicables pero en el orden inverso al que fue ingresado)
  ;1(son multiplicables))*/
  private static int AreMultipliable(double[,]a,double[,]b)
  {
    int multipliable = -1;
    if (a.GetLength(1)==b.GetLength(0))
    {
        multipliable=1;
    }
    else if (a.GetLength(0)==b.GetLength(1))
    {
        multipliable=0;
        
    }
    return multipliable;
  }

  // Método para comprobar que la matriz se pueda multiplicar por el vector

  private static bool IsMultipliablebyVector(double[,]a,double[]d)
  {
    bool multipliable = false;
    if (a.GetLength(1)==d.Length)
    {
        multipliable=true;
    }
    return multipliable;
  }

 //Método que suma dos matrices(si resultado es matriz de longitud 0 no eran del mismo tamaño)
  public static double[,] Sum(double[,]a,double[,]b)
  {
    double[,] sum = new double[0,0];
    if (SameSize(a,b))
    {
      sum = new double[a.GetLength(0),a.GetLength(1)];
     for (int i = 0; i < a.GetLength(0); i++)
    {
        for (int j = 0; j < a.GetLength(1); j++)
        {
            sum[i,j] =a[i,j] + b[i,j];
        }
    }

    }
    return sum;
    
  }
  //Método que multiplica una matriz por un escalar
  public static double[,] ScalarxMatrix(double[,]a,double c)
  {
    double[,] multiplied = new double[a.GetLength(0),a.GetLength(1)];
    for (int i = 0; i < a.GetLength(0); i++)
    {
        for (int j = 0; j < a.GetLength(1); j++)
        {
            multiplied[i,j] = a[i,j]*c;
        }
    }
    return multiplied;
  }

   //Método que multiplica dos matrices(si resultado es la matriz de longitud 0 no eran multiplicables)
   public static double[,] MatrixxMatrix(double[,]a,double[,]b)
   {
     int proceed=AreMultipliable(a,b);

     if (proceed==0)
     {
        double[,] temporal = a;
        a=b;
        b=temporal;
     }
     double[,] multiplied = new double[0,0];
     if (proceed>=0)
     {
      multiplied = new double[a.GetLength(0),b.GetLength(1)];
        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < b.GetLength(1); j++)
            {
                for (int k = 0; k < a.GetLength(1); k++)
                {
                    multiplied[i,j]+=a[i,k]*b[k,j];
                }
            }
        }
     }
     else {Console.WriteLine("No se pueden multiplicar");}
     return multiplied;

   }

  //Método que multiplica una matriz por un vector(si resultado es un vector de longitud 0 no eran multiplicables)
   public static double[] MatrixVector(double[,]a,double[]d)
   {
     double[] multiplied=new double[0];
      if (IsMultipliablebyVector(a,d))
      {
        multiplied = new double[a.GetLength(0)];
       
       for (int i = 0; i < multiplied.Length; i++)
       {
         for (int j = 0; j < a.GetLength(1); j++)
         {
           multiplied[i] += a[i,j] * d[j];
         }
       }

      }
      else {Console.WriteLine("No se pueden multiplicar");}

     return multiplied;
}

// Si se desea restar matrices solo se debe multiplicar una de ellas por -1 y sumarla a la otra

}

