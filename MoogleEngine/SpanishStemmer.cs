public static class SpanishStemmer
{
    static string[] VOWELS = ["a", "e", "i", "o", "u", "á", "é", "í", "ó", "ú", "ü"];

    static bool IsVowel(char c) => "aeiouáéíóúü".Contains(c);

    static bool EndsWith(string w, string suffix)
    {
        if (suffix.Length > w.Length) return false;
        return w[^suffix.Length..] == suffix;
    }

    static string RemoveSuffix(string w, string suffix)
    {
        return w[..^suffix.Length];
    }

    static string ReplaceSuffix(string w, string oldSuf, string newSuf)
    {
        return RemoveSuffix(w, oldSuf) + newSuf;
    }

    // RV region: after first vowel if preceded by consonant, or after first vowel
    // if word starts with vowel+h+consonant, or at position 3 otherwise
    static int RVIndex(string w)
    {
        if (w.Length < 2) return w.Length;
        if (!IsVowel(w[1]))
        {
            for (int i = 2; i < w.Length; i++)
                if (IsVowel(w[i])) return i + 1;
        }
        else if (IsVowel(w[0]) && IsVowel(w[1]))
        {
            for (int i = 2; i < w.Length; i++)
                if (IsVowel(w[i])) return i + 1;
        }
        else
        {
            for (int i = 3; i < w.Length; i++)
                if (IsVowel(w[i])) return i + 1;
        }
        return w.Length;
    }

    // R1 region: region after first non-vowel following a vowel
    static int R1Index(string w)
    {
        for (int i = 0; i < w.Length - 1; i++)
        {
            if (IsVowel(w[i]) && !IsVowel(w[i + 1]))
                return i + 2;
        }
        return w.Length;
    }

    // R2 region: region after first non-vowel following a vowel in R1
    static int R2Index(string w)
    {
        int r1 = R1Index(w);
        for (int i = r1; i < w.Length - 1; i++)
        {
            if (IsVowel(w[i]) && !IsVowel(w[i + 1]))
                return i + 2;
        }
        return w.Length;
    }

    // Words that should not be stemmed (short function words, etc.)
    static string[] STOP_STEMS = ["a", "ante", "bajo", "cabe", "con", "contra", "de", "desde",
        "durante", "e", "el", "en", "entre", "hacia", "hasta", "la", "las", "le", "les", "lo",
        "los", "mediante", "o", "para", "por", "que", "segun", "sin", "so", "sobre", "tras",
        "un", "una", "unas", "unos", "y"];

    public static string Stem(string word)
    {
        if (word.Length < 2) return word;

        // Skip short function words
        if (STOP_STEMS.Contains(word)) return word;

        string w = word.ToLowerInvariant();
        string original = w;

        // Step 0: Attached pronoun removal
        string[] pronouns = ["selas", "selos", "sela", "selo", "me", "se", "nos",
                              "las", "les", "los", "la", "le", "lo"];

        foreach (var p in pronouns)
        {
            if (EndsWith(w, p) && w.Length - p.Length >= 2)
            {
                w = RemoveSuffix(w, p);

                // Verb ending adjustments after pronoun
                string[] verbEnd = ["iéndo", "ándo", "éndo", "yendo", "iendo", "ando",
                                     "ár", "ér", "ír", "ar", "er", "ir"];
                foreach (var ve in verbEnd)
                {
                    if (EndsWith(w, ve))
                    {
                        w = RemoveSuffix(w, ve);
                        // Add appropriate ending
                        if (ve == "iéndo" || ve == "yendo" || ve == "iendo")
                            w += "iendo";
                        else if (ve == "ándo" || ve == "ando")
                            w += "ando";
                        else if (ve == "éndo")
                            w += "er";
                        else if (ve == "ár")
                            w += "ar";
                        else if (ve == "ír")
                            w += "ir";
                        else if (ve == "ér")
                            w += "er";
                        break;
                    }
                }
                break;
            }
        }

        int rv = RVIndex(w);

        // Step 1: Standard suffixes (noun/adjective)

        // Adverbial -mente
        if (EndsWith(w, "mente") && w.Length - 5 >= rv)
        {
            w = RemoveSuffix(w, "mente");
            // Try to remove another suffix from the remaining word
            string[] mentSuffixes = ["able", "ible", "ante", "ista", "ico", "ica",
                                      "oso", "osa", "ivo", "iva"];
            bool removed = false;
            foreach (var s in mentSuffixes)
            {
                if (EndsWith(w, s) && w.Length - s.Length >= 4)
                {
                    w = RemoveSuffix(w, s);
                    removed = true;
                    break;
                }
            }
            if (!removed && EndsWith(w, "ble") && w.Length - 3 >= 4)
                w = RemoveSuffix(w, "ble");
        }

        // Noun/adjective suffixes (accents already stripped by NormalizeExpresion)
        string[][] step1Suffixes = [
             ["anza", "anzas", "ico", "ica", "icos", "icas",
              "ismo", "ismos", "able", "ables", "ible", "ibles",
              "ista", "istas", "oso", "osa", "osos", "osas",
              "ador", "adora", "adores", "adoras",
              "amiento", "amientos", "imiento", "imientos",
              "acion", "aciones", "ante", "antes",
              "ancia", "ancias",
              "edumbre", "edumbres",
              "logia", "logias",
              "ucion", "uciones",
              "encia", "encias",
              "ez", "eza", "ezas",
              "ivo", "iva", "ivos", "ivas",
              "dor", "dora", "dores", "doras",
              "miento", "mientos",
              "idad"],
            ["ancia", "ancias", "ente", "entes",
             "ivo", "iva", "ivos", "ivas",
             "ador", "adora", "adores", "adoras",
             "cion", "ciones", "sion", "siones",
             "miento", "mientos",
             "ez", "eza", "ezas",
             "logia", "logias",
             "ucion", "uciones",
             "encia", "encias",
             "ante", "antes",
             "dor", "dora"],
            ["ica", "icas", "ico", "icos",
             "osa", "osas", "oso", "osos",
             "ible", "ibles", "able", "ables",
             "ista", "istas",
             "iva", "ivas", "ivo", "ivos",
             "anza", "anzas"]
        ];

        bool found = false;
        foreach (var group in step1Suffixes)
        {
            foreach (var suf in group)
            {
                if (EndsWith(w, suf) && w.Length - suf.Length >= rv)
                {
                    int r2 = R2Index(w);
                    if (w.Length - suf.Length >= r2)
                    {
                        // Special transformations
                        if (suf is "logia" or "logias")
                            w = ReplaceSuffix(w, suf, "log");
                        else if (suf is "ucion" or "uciones")
                            w = ReplaceSuffix(w, suf, "u");
                        else if (suf is "encia" or "encias")
                            w = ReplaceSuffix(w, suf, "ente");
                        else if (suf is "anza" or "anzas" or "acion" or "aciones"
                                 or "ico" or "ica" or "icos" or "icas"
                                 or "ismo" or "ismos" or "able" or "ables" or "ible" or "ibles"
                                 or "ista" or "istas" or "oso" or "osa" or "osos" or "osas"
                                 or "amiento" or "amientos" or "imiento" or "imientos"
                                 or "edumbre" or "edumbres"
                                 or "ador" or "adora" or "adores" or "adoras"
                                 or "miento" or "mientos")
                            w = RemoveSuffix(w, suf);
                        else if (suf is "ante" or "antes"
                                 or "ancia" or "ancias")
                            w = RemoveSuffix(w, suf);
                        else if (suf is "cion" or "ciones" or "sion" or "siones"
                                 or "idad")
                            w = RemoveSuffix(w, suf);
                        else
                            w = RemoveSuffix(w, suf);

                        found = true;
                        break;
                    }
                }
            }
            if (found) break;
        }

        // Step 2: Verb suffixes (all unaccented — normalization strips accents first)
        string[] verbSuffixes = [
            "ariamos", "eriamos", "iriamos",
            "ariais", "eriais", "iriais",
            "arian", "erian", "irian",
            "arias", "erias", "irias",
            "abamos", "iamos",
            "asteis", "isteis",
            "asemos", "esemos",
            "aran", "ieran",
            "aren", "ieren",
            "asen", "iesen",
            "aste", "iste",
            "ases", "ieses",
            "aron", "ieron",
            "ando", "iendo",
            "aba", "ada", "ida",
            "ara", "iera",
            "are", "iere",
            "ase", "iese",
            "ado", "ido",
            "aban", "ian",
            "ar", "er", "ir",
            "o", "a", "e"
        ];

        int rv2 = RVIndex(w);
        foreach (var suf in verbSuffixes)
        {
            if (EndsWith(w, suf) && w.Length - suf.Length >= rv2)
            {
                int beforeLen = w.Length;
                w = RemoveSuffix(w, suf);

                // Special: remove -yendo when preceded by a vowel (not a consonant)
                if (suf == "yendo" && beforeLen > suf.Length)
                {
                    char prev = w[^1];
                    if (!IsVowel(prev))
                    {
                        w += "yendo";
                        continue;
                    }
                }

                // If verb ends in -ar, -er, -ir, check one more suffix
                if (suf is "ar" or "er" or "ir" or "o" or "a" or "e" or "i" or "e")
                {
                    // Optional: try to apply step 1 again
                    string[] extra = ["yendo"];
                    // Just remove and continue
                }

                found = true;
                break;
            }
        }

        // Step 2b: Handle -yendo, -yente etc.
        if (!found)
        {
            string[] ySuffixes = ["yendo", "yente", "yentes"];
            foreach (var ys in ySuffixes)
            {
                if (EndsWith(w, ys) && w.Length - ys.Length >= rv2)
                {
                    if (w.Length > ys.Length)
                    {
                        char prev = w[^(ys.Length + 1)];
                        if (IsVowel(prev))
                        {
                            w = RemoveSuffix(w, ys);
                            w += "iendo";
                        }
                    }
                    break;
                }
            }
        }

        // Step 3: Residual suffixes
        if (EndsWith(w, "os") && w.Length - 2 >= R2Index(w))
            w = RemoveSuffix(w, "os");

        if (EndsWith(w, "s") && w.Length - 1 >= R2Index(w))
        {
            // Check if preceded by a vowel
            if (w.Length >= 2 && IsVowel(w[^2]))
                w = RemoveSuffix(w, "s");
        }

        // Step 4: Remove residual -o, -a, -e
        foreach (var c in new[] { 'o', 'a', 'e' })
        {
            if (EndsWith(w, c.ToString()) && w.Length - 1 >= rv)
            {
                // Don't remove if word is too short
                string stripped = RemoveSuffix(w, c.ToString());
                if (stripped.Length >= 2)
                {
                    w = stripped;
                    break;
                }
            }
        }

        return w;
    }
}
