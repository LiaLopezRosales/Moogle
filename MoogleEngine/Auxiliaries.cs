using System.Text;
using System.Text.RegularExpressions;

/*Clase que contiene todos los métodos auxiliares(en general relacionados con el procesamiento de
la búsqueda,incluye los operadores y métodos de sugerencia)
*/
class Auxiliaries
{

/*Método para relacionar posición de columna en la matriz con título del texto correspondiente
(se utiliza para obtener el Title en depencia del score que se este calculando)
*/
private static Dictionary<int,string> Index(string[]directions)
{
  Dictionary<int,string> index = new Dictionary<int,string>();
  for (int i = 0; i < directions.Length; i++)
  {
    string temporal = Path.GetFileNameWithoutExtension(directions[i]);

    index.Add(i,temporal);
  }
  return index;
}

/*Metodo que calcula la similaridad de coseno(la fórmula clásica donde se calcula el producto 
punto y las magnitudes de cada vector)
*/
public static double CosineSimilarity(double[] search,double[] document)
{
  double similarity;
  double pointproduct = 0;
  double semimagnitudeofsearch =0;
  double semimagnitudeofdocument =0;
  for (int i = 0; i < search.Length; i++)
  {
    if(search[i]!=0 && document[i]!=0)
     pointproduct += search[i]*document[i];
      if(search[i]!=0)
     semimagnitudeofsearch += Math.Pow(search[i],2);
     if(document[i]!=0)
     semimagnitudeofdocument += Math.Pow(document[i],2);
  }
  double magnitudeofsearch = Math.Sqrt(semimagnitudeofsearch);
  double magnitudeofdocument = Math.Sqrt(semimagnitudeofdocument);
  similarity = pointproduct/(magnitudeofsearch*magnitudeofdocument);
  return similarity;
}

//Metodo que separa una columna indicada de una matrix
 private static double[] ExtracColumn(double[,] tfidfofdocuments,int column)
 {
   int size = tfidfofdocuments.GetLength(0);
   double[] newcolumn = new double[size];
   for (int j = 0; j < size; j++)
   {
     newcolumn[j] = tfidfofdocuments[j,column];
   }
   return newcolumn;
 }

//Metodo para organizar el conjunto de resultados(se basa en el algoritmo de ordenación por inserción)
  private static void Order(MoogleEngine.SearchItem[] relevantdocuments)
{
  double temporal;
  int j;
  for (int i = 0; i < relevantdocuments.Length; i++)
  {
    temporal=relevantdocuments[i].Score;
     MoogleEngine.SearchItem temp=relevantdocuments[i];
    j=i-1;
    while (j>=0 && relevantdocuments[j].Score<temporal)
    {
      relevantdocuments[j+1]=relevantdocuments[j];
      j--;
    }
    relevantdocuments[j+1]=temp;
  }
}

//Operadores
//Operador *
private static Tuple<string[],int[]> WordswithAsterisk(string search,string suggestion,Dictionary<string,string> wordstochange)
{
  //Se crean listas para identificar las palabras con asterisco y cuántos presenta cada palabra
  List<string> withasterisks = new List<string>();
  List<int> amountofasterisk= new List<int>();
  string[] wordswithasterisks ;
  int[] amountofasteriskofword ;
  
  //Se utiliza el método Regex para poder encontrar las ocurrencias dado el patrón indicado(* ó *+)
  Match patron = Regex.Match(search,@"\*+\w+\b");
  if (patron.Success)
  {
    while (patron.Success)
    {
      int temp;
      /*Se crean condicionales para analizar el caso de la existencia de sugerencias(lo cual 
      conlleva a la sustittución de la palabra por una existente en el vocabulario en lugar de un 
      análisis directo de la búsqueda)
      */
      if (suggestion!="")
     {
      if (wordstochange.ContainsKey(DataBase.NormalizeExpresion(patron.Value)))
      {
        withasterisks.Add(wordstochange[DataBase.NormalizeExpresion(patron.Value)]);
        temp = patron.Value.Count(d=>d== '*');
        amountofasterisk.Add(temp);
      }
      else
      {
        withasterisks.Add(DataBase.NormalizeExpresion(patron.Value));
        temp = patron.Value.Count(d=>d== '*');
        amountofasterisk.Add(temp);
      }
     }
     else
     { 
      withasterisks.Add(DataBase.NormalizeExpresion(patron.Value));
      temp = patron.Value.Count(d=>d== '*');
      amountofasterisk.Add(temp);
     }
      patron = patron.NextMatch();
    }
    wordswithasterisks =withasterisks.ToArray();
    amountofasteriskofword = amountofasterisk.ToArray();
    return Tuple.Create(wordswithasterisks,amountofasteriskofword);
  }
  else 
  {
    //Se crea un resultado en caso de no encontranse "*" para poder identificar este caso al procesar la búsqueda
    Tuple<string[],int[]> temporal = new Tuple<string[], int[]>(new string[1]{"No asterisk"},new int[1]);
    return temporal;  
  }
}

//Operador !(devuelve un bool que indica si incumple la condición del operador)
private static bool Exclusion(string search,string[] document,string suggestion, Dictionary<string,string> wordstochange)
{
  //Análogamente a lo ocurrido en el operador * se identifica primero la ocurrencia del patrón deseado
   Match patron = Regex.Match(search,@"\!\w+\b");
   bool condition = true;
   if (patron.Success)
   {
     while (patron.Success)
     {
      //Análogamente se analiza la presencia y cambios que conlleva la sugerencia
      if (suggestion!="")
     {
      if (wordstochange.ContainsKey(DataBase.NormalizeExpresion(patron.Value)))
      {
        /*Aunque existan varias palabras que presentan el operador, la condición solo es verdadera
        si el documento analizado no contiene a ninguna de ellas(si contiene alguna,la condición es
        automáticamente falsa y no se continua analizando)
        */
        if (!document.Contains(wordstochange[DataBase.NormalizeExpresion(patron.Value)]))
       {
        condition = true;
       }
       else
       {
        condition=false;
        break;
       }
      }
      else
      {
        if (!document.Contains(DataBase.NormalizeExpresion(patron.Value)))
       {
        condition = true;
       }
       else
       {
        condition=false;
        break;
       }
      }
     }
     else
     {
      if (!document.Contains(DataBase.NormalizeExpresion(patron.Value)))
      {
        condition = true;
      }
      else
      {
        condition=false;
        break;
      }
     }
      patron = patron.NextMatch();
     }
   }
   return condition;
}
//Operador ^
private static bool Inclusion(string search,string[] document,string suggestion,Dictionary<string,string> wordstochange)
{
  //Análogamente a lo ocurrido anteriormente se identifica primero la ocurrencia del patrón deseado
  Match patron = Regex.Match(search,@"\^\w+\b");
   bool condition = true;
   if (patron.Success)
   {
    while (patron.Success)
    {
      //Análogamente se analiza la presencia y cambios que conlleva la sugerencia
      if (suggestion!="")
     {
      if (wordstochange.ContainsKey(DataBase.NormalizeExpresion(patron.Value)))
      {
        /*Al igual que en el operador anterior la condición solo es verdadera si el documento 
        contiene todos las palabras que coincidan con el patrón si no contiene alguna la condición es
        falsa y no se continua analizando */
        if (document.Contains(wordstochange[DataBase.NormalizeExpresion(patron.Value)]))
       {
        condition = true;
       }
       else
       {
        condition=false;
        break;
       }
      }
      else
      {
        if (document.Contains(DataBase.NormalizeExpresion(patron.Value)))
       {
        condition = true;
       }
       else
       {
        condition=false;
        break;
       }
      }
     }
     else
     { if (document.Contains(DataBase.NormalizeExpresion(patron.Value)))
      {
        condition = true;
      }
      else
      {
        condition=false;
        break;
      }
     }
      patron = patron.NextMatch();
    }
   }
   return condition;
}

//Operador ~ (a)(Comprueba cuántos signos de cercania hay en la busqueda y extrae los pares de palabras)
private static Tuple<int,string[]> CloseSearch(string search,string suggestion,Dictionary<string,string> wordstochange)
{
  /*En primer lugar se divide la búsqueda original para identificar la presencia del operador
  (por eso es imprescindible la separación entre el las palabras y el operador)
  */
   string[] procesedsearch = search.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
   int simbols =0;
   List<string> pairs = new List<string>();

   /*Igualmente se analiza la presencia de la sugerencia a la hora de ejecutar la búsqueda de 
   ocurrencias del operador
   */
   if (suggestion!="")
   {
     for (int k = 0; k < procesedsearch.Length; k++)
    {
      if (wordstochange.ContainsKey(procesedsearch[k]))
     {
      procesedsearch[k]=wordstochange[procesedsearch[k]];
     }
    }
   }

   for (int i = 0; i < procesedsearch.Length; i++)
   {
    /*Por cada símbolo encontrado se comprueba que cumpla las siguientes condiciones(si lo hace 
    se buscan y añaden los pares de palabras correspondientes)
    */
     if (procesedsearch[i]=="~" && i!=0 && i!=procesedsearch.Length-1 && procesedsearch[i+1]!="~")
     {
      simbols+=1;
      pairs.Add(DataBase.NormalizeExpresion(procesedsearch[i+1]));
      int k =i-1;
      while (procesedsearch[k]=="~")
      {
        k--;
      }
      pairs.Add(DataBase.NormalizeExpresion(procesedsearch[k]));
     }
   }
   string[] pair = pairs.ToArray();
   Tuple<int,string[]> procesed = new Tuple<int, string[]>(simbols,pair);
   return procesed;
}

//Operador ~ (b)(Calcula la cercania general como la suma de la cercania entre cada par indicado en la busqueda)
private static double Close(Tuple<int,string[]> procesed,string[]documentx)
{
  //Utilizando los objetos obtenidos por el (a) de este operador se analizan todos los pares existentes
  int size = procesed.Item1;
  double between = 0;

//Como ser pares colocados en un array,las iteraciones de este avanzan de dos en dos
  for (int i = 0; i < procesed.Item2.Length; i+=2)
  {
    //Se crean las variables necesarias para calcular las distancias
    int temporal = 0;
    int count=int.MaxValue;
    int inicialindex=0;
    int finalindex=0;
    int comparations=0;
   /* Se analiza primeramente si el docmente contiene las dos palabras del par(en caso contrario 
   se pasa al siguiente par)*/
    if (documentx.Contains(procesed.Item2[i]) && documentx.Contains(procesed.Item2[i+1]))
    {
      int k=0;
      int j=0;
      int rest=0;
      while (j<documentx.Length && k<documentx.Length )
      {
        /*Se analiza en caso de ya haberse encontrado alguna distancia si esta es menor que 
        la existente antes de encontrar la siguiente ocurrencia de la palabra */
        if (temporal>0 && temporal<count)
        {
          count = temporal;
        }
        //Si se encuentra una distancia de 1 se sale del ciclo pues la menor distancia posible
        if (count==1)
        {
          break;
        }
        /*Se encuentra la ocurrencia de la primera palabra del par y dentro del ciclo correspondiente
         se busca una ocurrencia de la otra palabra del par, luego se calcula la distancia y se almacena
         temporalmente*/
        if (documentx[j]== procesed.Item2[i])
        {
          inicialindex=j;
          if (comparations>0)
          {
            rest=Math.Abs(finalindex-inicialindex);
            
          }
          while (k<documentx.Length && i+1<procesed.Item2.Length && documentx[k]!= procesed.Item2[i+1])
          {
            k++;
          }
          finalindex=k;
          k++;
          temporal=Math.Abs(finalindex-inicialindex);
          if (rest<temporal)
          {
            temporal=rest;
          }
          comparations+=1;

        }
        j++;
      }
      
      between+=count;
    }
  }
  double bonus =0;
  /*Si existe alguna distancia o suma de ellas, se coloca este número como denominador de una fracción
  con numerador constante(se suma al denominar porque en caso de ser distancia mínima es decir 1  
  el resultado es la constante, un número demasiado grande como para sumar al score)*/
  if (between!=0)
  {
    bonus = 2/(between+4);
  }
  return bonus;
}

//Distancia de Levenshtein (a)(fórmula clásica del algoritmo de la Distancia de Levenshtein)
private static int LevenshteinDistance(string wordofsearch,string word )
{
  int[,] distance = new int[wordofsearch.Length+1,word.Length+1];
  for (int i = 0; i < distance.GetLength(0); i++)
  {
    distance[i,0]=i;
  }
  for (int j = 0; j < distance.GetLength(1); j++)
  {
    distance[0,j]=j;
  }
  for (int k = 1; k < distance.GetLength(0); k++)
  {
    for (int m = 1; m < distance.GetLength(1); m++)
    {
      int change;
      if (wordofsearch[k-1]==word[m-1])
      {
        change=0;
      }
      else{change=1;}
      int temporal = Math.Min((distance[k-1,m]+1),distance[k,m-1]+1);
      distance[k,m]= Math.Min(temporal,distance[k-1,m-1]+change);
    }
  }
  return distance[wordofsearch.Length,word.Length];
}

//Método principal de la búsqueda de sugerencia apoyado en el aloritmo de distancia de Levenstein
private static Tuple<string,Dictionary<string,string>> Suggestion(string search,string[]wordsLevenshtein,string[]listwords,int similaritythreshold)
{
  /*Se crean objetos para almacenar la búsqueda normalizada(para posterior comparación),separar esta 
  para modificarla y formar la sugerencia final y un diccionaro que relaciona la palabra de la 
  búsqueda original con su correspondiente sugerencia */
  string[] normsearch = DataBase.NormalizeString(search);
  string oldsearch = DataBase.NormalizeExpresion(search);
  string suggestion = "";
  Dictionary<string,string> similarwords = new Dictionary<string, string>();
  for (int i = 0; i < wordsLevenshtein.Length; i++)
  {
    /* Se recibe como parámetro de entrada un conjunto de palabras de la búsqueda que no pertenecen
    al vocabulario y por cada una de ellas se busca en el mismo una palabra similar utilizando
    el método de distancia de Levenshtein y un umbral de similitud máximo(criterio que decide cuál
    es el máximo de transformaciones para que dos palabras sean similares) */
    string temporal = wordsLevenshtein[i];
    string tempword = "";
    int tempmin = int.MaxValue;
    for (int j = 0; j < listwords.Length; j++)
    {
      if (LevenshteinDistance(temporal,listwords[j])<tempmin)
      {
        tempmin = LevenshteinDistance(temporal,listwords[j]);
        tempword = listwords[j];
      }
    }
    /* Se almacena la palabra y menor número de transformaciones y luego se compara si cumple los
    requisitos para formar parte de la sugerencia final */
    if (tempmin<=similaritythreshold)
    {
      similarwords.Add(temporal,tempword);
    }
  }
  /* Se analiza si se encontró alguna sugerencia válida(y en caso positivo se procede a confeccionar
  la sugerencia final sustituyendo las palabras no existentes con las encontradas) */
  if(similarwords.Count>=1)
 { for (int k = 0; k < normsearch.Length; k++)
  {
    foreach (var word in similarwords)
    {
      string temp = word.Key;
      string newword = word.Value;
      if (normsearch[k]==temp)
     {
      normsearch[k]=newword;
     }
    }
    suggestion+=normsearch[k];
    suggestion+=" ";
  }
 }
  if (oldsearch==suggestion)
  {
    suggestion = "";
  }
  Tuple<string,Dictionary<string,string>> result = new Tuple<string, Dictionary<string, string>>(suggestion,similarwords);
  
  return result;
}

/* Método que busca el snippet dada la busqueda y el documento(llamames snippet al fragmento más 
relevante del texto dada esa búsqueda) */
public static string Snippet(string search,string[] documentx,string[]directions,string[]listofwords,double[]procesedsearch)
{
  string result = "";
  List<string> importantwords = new List<string>();
  
  string[] newsearch = DataBase.NormalizeString(search);
  ;
  int inicialindex=0;
  int finalindex=0;
  
  //Se analiza que palabras de la búsqueda son consideradas relevantes
  for (int i = 0; i < procesedsearch.Length; i++)
  {
    if (procesedsearch[i]>0)
    {
      importantwords.Add(listofwords[i]); 
    }
  }
  string[] words = importantwords.ToArray();
  int count =0;

  /* Se hace un conteo inicial de relevancia del primer fragmento del texto(cantidad de veces que
  aparece una palabra relevante en él), se escoge una distancia estándar de 50 palabras para nuestros
  fragmentos (se aclara el caso donde el documento posea una longitud inferior a esta) */
  if(documentx.Length>=50)
  {
    for (int i = 0,j=50; i < j; i++)
  {
    if (words.Contains(documentx[i]))
    {
      count+=1; 
    }
    finalindex=j;
  }
  }
  else
  {
    for (int i = 0,j=documentx.Length/2; i < j; i++)
  {
    if (words.Contains(documentx[i]))
    {
      count+=1;
    }
    finalindex=j;
  }
  }
  int temporal=count;
  int tempinicialindex=inicialindex;
  int tempfinalindex = finalindex;
  
  /* Se procede a identificar el fragmento de mayor relevancia analizando el siguiente índice y el
  nuevo índice final (para evitar revisiones innecesarias ya que las palabras intermedias no van 
  influir en los nuevos cálculos) */
  for (int k = inicialindex+1,m=finalindex; m < documentx.Length; k++,m++)
  {
    if ((!words.Contains(documentx[m]))&&(!words.Contains(documentx[k])))
    {
      continue;
    }
    
    if (!words.Contains(documentx[k])&& words.Contains(documentx[m]))
    {
      temporal+=1;
    }
    
      if(!words.Contains(documentx[m])&&words.Contains(documentx[k]))
    {
      temporal-=1;
    }
    if(words.Contains(documentx[m])&&words.Contains(documentx[k]))
    {
      temporal+=1;
     if (temporal>count)
    {
      count=temporal;
      tempinicialindex=k;
      tempfinalindex=m;
    }
    temporal-=1;
    }
    if (temporal>count)
    {
      count=temporal;
      tempinicialindex=k;
      tempfinalindex=m;
    }
  }
  inicialindex = tempinicialindex;
  finalindex=tempfinalindex;

//Se elabora el snippet final ya teniendo el índice inicial y final del fragmento 
  for (int n = inicialindex; n < finalindex+1; n++)
  {
    result+=documentx[n];
    result+=" ";
  }
  return result;
}

//Metodo que vectoriza la búsqueda(Con operador * incluido y sugerencia)
public static Tuple<double[],Dictionary<string,string>,string> WordsofSearch(string search, string[] directions,string[] listwords,int[]filescontainingword)
{

/*Se normaliza la búsqueda y se vectoriza con las operaciones ya conocidas(las mismas utilizadas
 en el procesamiento de los documentos)*/
  string[] words = DataBase.NormalizeString(search);
  
  double[] tfidfofsearch = new double[listwords.Length];

  List<string> wordsforLevenshtein = new List<string>();

  string suggestion="";
  
  for (int i = 0; i < words.Length; i++)
  {
      if (listwords.Contains(words[i]))
      {
        int position =Array.IndexOf(listwords,words[i]);
        tfidfofsearch[position]+=1;
      }
      /* Se aprovecha la necesidad de analizar cada palabra de la búsqueda y se elabora un conjunto
      de términos no pertenecientes al vocabulario para la búsqueda de una sugerencia */
      else
      {
        wordsforLevenshtein.Add(words[i]);
      }
  }

   string[] wordsLevenshtein = wordsforLevenshtein.ToArray();
   /* Si ocurre esto se procede a llamar al método Suggestion para obtener dicha sugerencia y se
   modifican los valores del vector de búsqueda para responder a la sugerencia */
  if (wordsLevenshtein.Length>0)
  {
    foreach (var dic in (Suggestion(search,wordsLevenshtein,listwords,3)).Item2)
    {
      int pos = Array.IndexOf(listwords,dic.Value);
      tfidfofsearch[pos]+=1;
    }
    suggestion=Suggestion(search,wordsLevenshtein,listwords,3).Item1;
  }
  
  for (int i = 0; i < tfidfofsearch.Length; i++)
  {
    tfidfofsearch[i]/= tfidfofsearch.Length;
  }

  Dictionary<string,string> wordstochange = Suggestion(search,wordsLevenshtein,listwords,3).Item2;

// Se aplica el operador * y se modifican los valores de las palabras correspondientes según cuantos *(s) presenten
 if (WordswithAsterisk(search,suggestion,wordstochange).Item2[0]!=0)
    {
      for (int j = 0; j < WordswithAsterisk(search,suggestion,wordstochange).Item1.Length; j++)
      {
        tfidfofsearch[Array.IndexOf(listwords,WordswithAsterisk(search,suggestion,wordstochange).Item1[j])]*=2*(WordswithAsterisk(search,suggestion,wordstochange).Item2[j]);
      }
    }
    for (int i = 0; i < tfidfofsearch.Length; i++)
    {
     double idftemporal = Math.Log10(Convert.ToDouble(directions.Length)/filescontainingword[i]);
     tfidfofsearch[i] *=idftemporal;
    }

    Tuple<double[],Dictionary<string,string>,string> result = new Tuple<double[],Dictionary<string,string>,string>(tfidfofsearch,wordstochange,suggestion);
  return result;
}

//Método que busca score ,title y snippet dada una busqueda
public static MoogleEngine.SearchItem[] RelevantDocuments(string search ,double[] tfidfofsearch,string[]documents,double[,]tfidfofdocuments,string[]listofwords,string suggestion,Dictionary<string,string> wordstochange)
{
  //Se crea una lista de objetos SearchItem para ir almacenando los valores que se vayan encontrando
  List<MoogleEngine.SearchItem> scoreofrelevantdocuments = new List<MoogleEngine.SearchItem>();
  
  // Se llama a la primera parte del operador de cercanía para su posterior análisis y se crea el índice
  Tuple<int,string[]> closesearch = CloseSearch(search,suggestion,wordstochange);
  Dictionary<int,string> index = Index(documents);
  for (int i = 0; i < documents.Length; i++)
  {
    /* Se analiza cada texto de la base de datos y se comprueba que cumpla los requisitos(score superior
    a 0 y cumplimiento de los requisitos de los operadores Inclusion y Exclusion) */
    double[] temporal = ExtracColumn(tfidfofdocuments,i);
    double score = CosineSimilarity(tfidfofsearch,temporal);
    if (score>0 && Exclusion(search,DataBase.Document(documents[i]),suggestion,wordstochange) && Inclusion(search,DataBase.Document(documents[i]),suggestion,wordstochange))
    {
      //Si lo hacen se procede a calcular y añadir el bono del operador cercanía si es necesario
      if (closesearch.Item1!=0)
      {
        if(Close(closesearch,DataBase.Document(documents[i]))!=0)
        {
        score+=Close(closesearch,DataBase.Document(documents[i]));
        }
      }
      /* Se utiliza el índice para relacionar el número del documento que se está analizando con su
      título, se procede a obtener el snippet correspondiente, se crea un SearchItem con estos objetos
      y se añade a los resultados */
      string title = index[i];
      string snippet = Snippet(search,DataBase.Document(documents[i]),documents,listofwords,tfidfofsearch);
      
      scoreofrelevantdocuments.Add(new MoogleEngine.SearchItem(title, snippet, (float) score));
    }

  }
  //Se analiza el caso de que no existan resultados válidos para evitar excepciones
  if(scoreofrelevantdocuments.Count<=0)
  {
    scoreofrelevantdocuments.Add(new MoogleEngine.SearchItem("No se encontraron textos coincidentes","Por favor realice una busqueda diferente", 0));
  }
  MoogleEngine.SearchItem[] listofmatches = scoreofrelevantdocuments.ToArray();
  //Se organizan estos resultados por su valor de score y se devuelve el conjunto organizado
  Order(listofmatches);
  return listofmatches;
}
}

