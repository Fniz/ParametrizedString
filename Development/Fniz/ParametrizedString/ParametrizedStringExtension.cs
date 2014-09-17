using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fniz.ParametrizedString
{
    /// <summary>
    /// This class allows to handle string with some parameters and replace them simply by their value.
    /// </summary>
    public static class ParametrizedStringExtension
    {
        private static readonly Dictionary<string, List<Pair<string, string>>> _dictionary =
            new Dictionary<string, List<Pair<string, string>>>();

        private static readonly Dictionary<string, Pair<string, string>> _delimiters =
            new Dictionary<string, Pair<string, string>>();

        private static string _currentString;
        private static DelimiterIndices _delimiterIndices;

        /// <summary>
        /// Get parameters of the string identified by a delimiter char.
        /// </summary>
        /// <param name="s">string </param>
        /// <param name="delimiterChars">array of 1 or 2 delimiter chars.</param>
        /// <returns>A list of the name of parameters inside the string</returns>
        /// <exception cref="ArgumentOutOfRangeException">throws an ArgumentOutOfRangeException if there is more than 2 delimiters chars</exception>
        /// <example>
        /// string stringToResolve = "$Volume$\\$Directory$\\$File$";
        /// List&lt;string&gt; result = stringToResolve.GetParameterNames('$'); // result contains 3 items : "Volume", "Directory" and "File"
        /// </example>
        public static List<string> GetParameterNames(this string s, params char[] delimiterChars)
        {
            if (delimiterChars.Length > 2)
            {
                throw new ArgumentOutOfRangeException("delimiterChars");
            }

            string[] delimiterStrings = Array.ConvertAll(delimiterChars, c => c.ToString());

            return GetParameterNames(s, delimiterStrings);
        }

        /// <summary>
        /// Get parameters of the string identified by a delimiter string.
        /// </summary>
        /// <param name="s">string </param>
        /// <param name="delimiterStrings">string array of 1 or 2 delimiters</param>
        /// <returns>A list of the name of parameters inside the string</returns>
        /// <exception cref="ArgumentOutOfRangeException">throws an ArgumentOutOfRangeException if there is more than 2 delimiters string</exception>
        /// <example>
        /// string stringToResolve = "{Volume}\\{Directory}\\{File}";
        /// List&lt;string&gt; result = stringToResolve.GetParameterNames("{","}"); // result contains 3 items : "Volume", "Directory" and "File"
        /// </example>
        public static List<string> GetParameterNames(this string s, params string[] delimiterStrings)
        {
            if (delimiterStrings.Length > 2)
            {
                throw new ArgumentOutOfRangeException("delimiterStrings");
            }

            ExtractParameters(s, delimiterStrings);

            if (_dictionary.ContainsKey(s))
                return _dictionary[s].Select(k => k.Key).ToList();

            return new List<string>();
        }

        private static void ExtractParameters(string s, string[] delimiterStrings)
        {
            string startDelimiterString = delimiterStrings[0];
            string endDelimiterString = delimiterStrings.Length > 1 ? delimiterStrings[1] : delimiterStrings[0];

            AddOrUpdateDelimiterInDictionary(s, endDelimiterString, startDelimiterString);
            _currentString = s;

            DelimiterIndices delimiterIndices = FindAllIndices(s, startDelimiterString, endDelimiterString);

            List<int> startCharIndices = delimiterIndices.StartDelimitersIndices;
            List<int> endCharIndices = delimiterIndices.EndDelimitersIndices;

            if ((startCharIndices.Count + endCharIndices.Count)%2 != 0)
                throw new FormatException("The number of delimitersString should be even.");

            string unescapedString = UnescapeDelimiters(s);

            List<Tuple<int, int>> indices = GetPairIndices(startCharIndices, endCharIndices).ToList();

            foreach (var va in indices)
            {
                string param = unescapedString.Substring(va.Item1,
                                                         va.Item2 - endDelimiterString.Length - va.Item1 +
                                                         startDelimiterString.Length);
                AddPair(s, new Pair<string, string> {Key = param});
            }
        }

        private static string UnescapeDelimiters(string s)
        {
            if (_delimiterIndices != null)
            {
                var unescapedDelimiter = new StringBuilder(s);

                foreach (int index in _delimiterIndices.EscapedDelimitersIndices)
                {
                    unescapedDelimiter.Remove(index, _delimiters[s].Key.Length);

                    // Update of Indices
                    for (int i = 0; i < _delimiterIndices.StartDelimitersIndices.Count; i++)
                    {
                        if (_delimiterIndices.StartDelimitersIndices[i] > index)
                            _delimiterIndices.StartDelimitersIndices[i] -= _delimiters[s].Key.Length;
                    }

                    // Update of Indices
                    for (int i = 0; i < _delimiterIndices.EndDelimitersIndices.Count; i++)
                    {
                        if (_delimiterIndices.EndDelimitersIndices[i] > index)
                            _delimiterIndices.EndDelimitersIndices[i] -= _delimiters[s].Key.Length;
                    }
                }

                return unescapedDelimiter.ToString();
            }

            return s;
        }

        public static void SetParameter(this string s, string keyName, string keyValue)
        {
            if (!_dictionary.ContainsKey(s))
                _dictionary.Add(s, new List<Pair<string, string>>());

            Pair<string, string> pair = _dictionary[s].Find(k => k.Key == keyName);

            if (pair == null)
                _dictionary[s].Add(new Pair<string, string> {Key = keyName, Value = keyValue});
            else
                pair.Value = keyValue;
        }

        /// <summary>
        /// Resolve the string containing parameters
        /// </summary>
        /// <param name="s"></param>
        /// <param name="delimiterChars">string</param>
        /// <returns>resolved string</returns>
        public static string Resolve(this string s, params char[] delimiterChars)
        {
            string[] delimiterStrings = Array.ConvertAll(delimiterChars, c => c.ToString());
            return Resolve(s, delimiterStrings);
        }

        public static string Resolve(this string s, params string[] delimiterStrings)
        {
            ExtractParameters(s, delimiterStrings);
            return Resolve(s);
        }

        /// <summary>
        /// Resolve the string containing parameters
        /// </summary>
        /// <param name="s">string</param>
        /// <returns>resolved string</returns>
        public static string Resolve(this string s)
        {
            var sb = new StringBuilder(UnescapeDelimiters(s));
            if (_dictionary.ContainsKey(s))
            {
                foreach (var v in _dictionary[s])
                {
                    if (!String.IsNullOrEmpty(v.Value))
                        sb.Replace(_delimiters[s].Key + v.Key + _delimiters[s].Value, v.Value);
                }

                ClearDictionaries(s);

                return sb.ToString();
            }

            return s;
        }

        private static void ClearDictionaries(string s)
        {
            _dictionary.Remove(s);
            _delimiters.Remove(s);
            _currentString = null;
        }

        private static DelimiterIndices FindAllIndices(string stringToParse, string startDelimiterString,
                                                       string endDelimiterString)
        {
            var escapedDelimitersIndices = new List<int>();
            var delimiterIndices = new List<List<int>>();

            int i; // current stringToParse index.
            int j;
                // delimiterString index : To compare chars of stringToParse and chars of delimiterString (char by char)
            int k; // escaped delimiter index : to check if a delimiterString is escaped (ex : $* -> $*$*)

            var delimiters = new List<string>();

            delimiterIndices.Add(new List<int>());
            delimiterIndices.Add(new List<int>());

            delimiters.Add(startDelimiterString);

            if (!String.IsNullOrWhiteSpace(endDelimiterString) && startDelimiterString != endDelimiterString)
                delimiters.Add(endDelimiterString);

            for (int m = 0; m < delimiters.Count; m++)
            {
                for (i = 0; i < stringToParse.Length; i++)
                {
                    // Check if current char equals to the first current char of delimiterString
                    if (stringToParse[i] == delimiters[m][0])
                    {
                        bool isAnEscapeDelimiter = true;

                        if (delimiters[m].Length == 1)
                        {
                            if (i + 1 < stringToParse.Length)
                            {
                                if (stringToParse[i + 1] == delimiters[m][0] && i > 0)
                                    i++;
                                else
                                    delimiterIndices[m].Add(i);
                            }
                            else
                            {
                                delimiterIndices[m].Add(i);
                            }
                        }
                        else
                        {
                            for (j = 1; j < delimiters[m].Length; j++)
                            {
                                // Check if all chars match
                                if (i + j > stringToParse.Length - 1) break;
                                if (stringToParse[i + j] == delimiters[m][j])
                                {
                                    if (j != delimiters[m].Length - 1)
                                        continue;

                                    // if i == 0, automatically consider as a delimiter.
                                    if (i == 0)
                                    {
                                        delimiterIndices[m].Add(i);
                                        break;
                                    }

                                    // Last iteration, Test if it is an escaped delimiter
                                    for (k = j; k <= delimiters[m].Length; k++)
                                    {
                                        int nextCharIndex = i + k + 1;
                                        int pastFirstCharIndex = i + k - (delimiters[m].Length - 1);

                                        if (nextCharIndex < stringToParse.Length &&
                                            stringToParse[nextCharIndex] == stringToParse[pastFirstCharIndex])
                                            continue;

                                        // Not an escape delimiter
                                        isAnEscapeDelimiter = false;
                                        break;
                                    }

                                    if (!isAnEscapeDelimiter)
                                    {
                                        delimiterIndices[m].Add(i);
                                        break;
                                    }

                                    // Advancing index.
                                    escapedDelimitersIndices.Add(i);
                                    i += startDelimiterString.Length*2 - 1; // -1 : to anticipate the loop increment
                                }
                            }
                        }
                    }
                }
            }
            _delimiterIndices = new DelimiterIndices
                                    {
                                        StartDelimitersIndices = delimiterIndices[0],
                                        EndDelimitersIndices = delimiterIndices[1],
                                        EscapedDelimitersIndices = escapedDelimitersIndices
                                    };

            return _delimiterIndices;
        }

        private static IEnumerable<Tuple<int, int>> GetPairIndices(IList<int> startCharIndices,
                                                                   IList<int> endCharIndices)
        {
            // if delimiters are different (ex : {})
            if (endCharIndices != null && endCharIndices.Count > 0)
            {
                for (int i = 0; i < startCharIndices.Count; i++)
                {
                    yield return new Tuple<int, int>(
                      startCharIndices[i] + (_delimiters[_currentString].Key.Length),
                      endCharIndices[i]);  
                }
            }
            else // delimiters are same (ex : $..$)
            {
                for (int i = 0; i < startCharIndices.Count - 1; i += 2)
                {
                    yield return new Tuple<int, int>(
                        startCharIndices[i] + (_delimiters[_currentString].Key.Length),
                        startCharIndices[i + 1]);
                }
            }
        }

        private static void AddOrUpdateDelimiterInDictionary(string s, string endDelimiterString,
                                                             string startDelimiterString)
        {
            if (!_delimiters.ContainsKey(s))
            {
                _delimiters.Add(s, new Pair<string, string> {Key = startDelimiterString, Value = endDelimiterString});
            }
            else
            {
                _delimiters[s].Key = startDelimiterString;
                _delimiters[s].Value = endDelimiterString;
            }
        }

        private static void AddPair(string s, Pair<string, string> pair)
        {
            if (!_dictionary.ContainsKey(s))
                _dictionary.Add(s, new List<Pair<string, string>>());

            _dictionary[s].Add(pair);
        }
    }
}