using System.Text;
using System.Text.RegularExpressions;

public class DataBase
{
  string[] directions;
  Tuple<string[],int[,],int[],int[]> procesed; 
  double[,] tfidf;
 /*Constructor con el cual al llamar "new DataBase()" cree nuestra base de datos de los documentos 
 presentes en la carpeta content(ejecuta los métodos(presentes en su mayoría en esta misma clase)
 en el orden adecuado para este proposito)*/
  public DataBase()
  {
    string ruta = Directory.GetParent(Path.Combine(Directory.GetCurrentDirectory()))!.FullName!;
    string file = Path.Combine(ruta,"Content");
    directions = Directory.GetFiles(file,"*txt*",SearchOption.AllDirectories);
    procesed = UniqueWords(directions);
    double[,] matrix = BruteFrecuency(directions,procesed.Item2,procesed.Item4);
    double[] idf = InverseFrecuency(directions,procesed.Item1,procesed.Item3);
    tfidf=TF_IDF(matrix,idf);

  }
    
  //Método para procesar un texto
public static string[] Document(string directions)
{
StreamReader reader = new StreamReader(directions);

string content = reader.ReadToEnd();
string[] document = NormalizeString(content);
return document;

}

//Metodo que normaliza una frase o texto(lo convierte en un string[])
public static string[] NormalizeString(string content)
{
  content = NormalizeExpresion(content);

  string[] temporal = content.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
  

return temporal;
}
//Metodo que normaliza una string
public static string NormalizeExpresion(string content)
{
  content = content.ToLower();
 Regex patron = new Regex("[^a-zA-Z0-9 -]");
 content = patron.Replace(content.Normalize(NormalizationForm.FormD),"");
 return content;
}

/*Método que crea nuestro vocabulario,la tabla de frecuencia inicial de nuestros documentos,
una tabla donde almacena en cuantos documentos esta presente cada palabra y un array con la longitud
de cada documento(en palabras)
*/
private static Tuple<string[],int[,],int[],int[]> UniqueWords(string[] directions)
{
  /*Se utiliza Dictionary para poder relacionar la palabra con su frecuencia
  (en lo que correspondería a una fila en la abstracción de tabla)
  */
   Dictionary<string,int[]> uniqueword = new Dictionary<string,int[]>();
   int[] lengthofdocuments = new int[directions.Length];
   for (int i = 0; i < directions.Length; i++)
   {
     string[] temporal = Document(directions[i]);
     
      for (int j = 0; j < temporal.Length; j++)
      {
        /*La longitud adicional del array creado corresponde a lo que despues será el array de 
        cantidad de documentos(se obtiene al extraer esa específica posición de cada uno de los arrays,
        la otra posición es una especie de guía para poder modificar adecuadamente la posición anterior)
        */
        int[] frecuencyofword = new int[directions.Length+2];
        if(!uniqueword.ContainsKey(temporal[j]))
        {
           uniqueword.Add(temporal[j],frecuencyofword);
           uniqueword[temporal[j]][i]+=1;
            uniqueword[temporal[j]][frecuencyofword.Length-1]+=1;
            uniqueword[temporal[j]][frecuencyofword.Length-2]=i;
        }
        else
        {
          uniqueword[temporal[j]][i]+=1;
          if (i!=uniqueword[temporal[j]][frecuencyofword.Length-2])
          {
            uniqueword[temporal[j]][frecuencyofword.Length-1]+=1;
            uniqueword[temporal[j]][frecuencyofword.Length-2]=i;  
          }
        }
      } 
      lengthofdocuments[i]=temporal.Length;
   }
   string[] words = uniqueword.Keys.ToArray();
   int[,] frecuency = new int[words.Length,directions.Length];
   int k =0;
   int[] filescontainingword = new int[words.Length];
   foreach (var array in uniqueword.Values)
   {
    for (int j = 0; j < array.Length-2; j++)
    {
      frecuency[k,j] = array[j];
    }
    filescontainingword[k]=array[array.Length-1];
    k++;
   }
   
   
   Tuple<string[],int[,],int[],int[]> lists = new Tuple<string[], int[,],int[],int[]>(words,frecuency,filescontainingword,lengthofdocuments);
    return lists;
   }

//Método que calcula TF(se utiliza una fórmula básica de TF y los objetos creados en el método anterior)
private static double[,] BruteFrecuency(string[] directions, int[,] matrix,int[] lengthofdocuments)
{
  
  double[,] tf = new double[matrix.GetLength(0),matrix.GetLength(1)];
  for (int i = 0; i < matrix.GetLength(1); i++)
  {
    double temporal = lengthofdocuments[i];
    for (int j = 0; j < matrix.GetLength(0); j++)
    {
      if (matrix[j,i]!=0)
      {
        tf[j,i] = Convert.ToDouble(matrix[j,i])/temporal;
      }
      else{tf[j,i] = (matrix[j,i]);}
    }
  }
  return tf;
}

//Método que calcula IDF
private static double[] InverseFrecuency(string[]directions,string[]listwords,int[]filescontainingword)
{
  double amountofdocuments = Convert.ToDouble(directions.Length);
  Double[] idf = new double[listwords.Length];
  for (int i = 0; i < listwords.Length; i++)
  {
    idf[i] = Math.Log10(amountofdocuments/filescontainingword[i]);
  }
  return idf;
}

/*Método que crea una tabla del TF-IDF de cada uno de los documentos(se multiplican los resultados 
obtenidos en los métodos de TF e IDF)
*/
private double[,] TF_IDF(double[,] brutefrecuency, double[]inversefrecuency)
{
  double[,] tfidf = new double[brutefrecuency.GetLength(0),brutefrecuency.GetLength(1)];

  for (int i = 0; i < brutefrecuency.GetLength(1); i++)
  {
    for (int j = 0; j < brutefrecuency.GetLength(0); j++)
    {
      tfidf[j,i] = brutefrecuency[j,i]*inversefrecuency[j];
    }
  }
  return tfidf;
}

/* Método principal que indica el procesamiento de la búsqueda ingresada(se obtiene el conjunto
de resultados y la sugerencia correspondiente,se aclara el caso de la no existencia de sugerencia
en cuyo caso se le asigna el mensaje  "No hay sugerencias disponibles para esta búsqueda")
*/
public Tuple<MoogleEngine.SearchItem[],string> Query(string query)
{ 
  Tuple<double[],Dictionary<string,string>,string> procesedquery =Auxiliaries.WordsofSearch(query,directions,procesed.Item1,procesed.Item3);
 double[] queryvector = procesedquery.Item1;
 string suggestion = procesedquery.Item3;
 if (suggestion=="")
 {
  suggestion="No hay sugerencias disponibles para esta búsqueda";
 }
 return new Tuple<MoogleEngine.SearchItem[],string>(Auxiliaries.RelevantDocuments(query,queryvector,directions,tfidf,procesed.Item1,procesedquery.Item3,procesedquery.Item2),suggestion);
}
}

