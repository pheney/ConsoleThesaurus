using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ConsoleThesaurus
{
    //  Ref: https://developer.oxforddictionaries.com/documentation
    class Program
    {
        static void Main(string[] args)
        {
            bool exit = false;
            while (!exit) {                 
                Console.Clear();
                Console.Write("Enter word: ");
                string input = Console.ReadLine();
                exit = string.IsNullOrWhiteSpace(input);
                if (!exit)
                {
                    Console.WriteLine("\nSynonyms:\n" + SuggestWordsFor(input, "synonyms"));
                    Console.WriteLine("\nAntonyms:\n" + SuggestWordsFor(input, "antonyms"));
                    Console.WriteLine("\nPress any key to continue");
                    Console.ReadLine();
                }
            }
        }

        static string protocol = "https";
        static string endpoint = "od-api.oxforddictionaries.com";
        static string port = "443";
        static string version = "v1";
        static string lang = "en";
        static string template = "{protocol}://{endpoint}:{port}/api/{version}/entries/{lang}/{word}";
        
        static string id = "62e7ee761";
        static string key = "12e5c078cbd8202e07d6cee6f95efac4e";
        static string de = "1";
        
        /// <summary>
        /// 2017-8-17
        /// Sample URL: https://od-api.oxforddictionaries.com:443/api/v1/entries/en/ace/synonyms
        /// </summary>
        /// <param name="word">word to look up</param>
        /// <param name="wordType">synonyms, antonyms, etc</param>
        /// <returns></returns>
        public static string SuggestWordsFor(string word, string wordType) {
            string result = "No " + wordType + " found for " + word;

            //  query string
            string fullurl = template
                .Replace("{protocol}", protocol)
                .Replace("{endpoint}", endpoint)
                .Replace("{port}", port)
                .Replace("{version}", version)
                .Replace("{lang}", lang)
                .Replace("{word}", word.ToLower());

            //  alternate word type
            fullurl += "/" + wordType.ToLower();

            //  Build the web request
            WebRequest webRequest = WebRequest.Create(fullurl);
            webRequest.ContentType = "application/json";
            webRequest.Headers.Add("app_id", id);
            webRequest.Headers.Add("app_key", key);

            //  Create an empty response
            WebResponse webResp = null;

            try
            {
                //  Execute the request and put the result into response
                webResp = webRequest.GetResponse();                
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(webResp.GetResponseStream(), encoding))
                {
                    //  Convert the json string to a json object
                    JObject json = (JObject) JsonConvert.DeserializeObject(reader.ReadToEnd());

                    //  Find synonyms
                    var found = GetAlternateWords(json, wordType);
                    string[] resultArray = JObjectListToStringArray(found, "text");
                    result = string.Join(",", resultArray);
                }
            }
            catch (WebException)
            {
                //  404	: No entry is found matching supplied id and source_lang or filters are not recognized
                //  500 : Internal Error. An error occurred while processing the data.
                //Console.WriteLine("Word not found (or a server error occured)");
            }

            return result;
        }

        /// <summary>
        /// Sample input:
        ///     "synonyms": [
        ///         {
        ///             "id": "wunderkind",
        ///             "language": "en",
        ///             "text": "wunderkind"
        ///         },
        ///         {
        ///             "id": "hotshot",
        ///             "language": "en",
        ///             "text": "hotshot"
        ///         }
        ///     ]
        /// Sample output:
        ///     [ "wunderkind", "hotshot" ]
        /// </summary>
        private static List<JObject> GetAlternateWords(JObject source, string type)
        {
            List<JObject> result = new List<JObject>();

            foreach (var item in source)
            {
                if (item.Key.Equals(type))
                {
                    //  The synonym object is ALWAYS an array of objects
                    //  although it may be an empty array
                    foreach (var syn in item.Value)
                    {
                        result.Add((JObject)syn);
                    }
                    continue;
                }

                bool valueIsJsonObject = item.Value.Type.Equals(JTokenType.Object);
                if (valueIsJsonObject)
                {
                    result.AddRange(GetAlternateWords((JObject)item.Value, type));
                    continue;
                }

                bool valueIsArray = item.Value.Type.Equals(JTokenType.Array);
                if (valueIsArray)
                {
                    foreach (var arrayItem in item.Value)
                    {
                        if (arrayItem.Type.Equals(JTokenType.Object))
                        {
                            result.AddRange(GetAlternateWords((JObject)arrayItem, type));
                        }
                        continue;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// tokenName = "text"
        /// </summary>
        /// <param name="source"></param>
        /// <param name="tokenName"></param>
        /// <returns></returns>
        private static string[] JObjectListToStringArray(List<JObject> source, string tokenName)
        {
            List<string> results = new List<string>();
            
            for (int i = 0; i < source.Count; i++)
            {
                results.AddUnique(source[i].Value<string>(tokenName));
            }
            return results.ToArray();
        }
    }

    public static class Extensions
    {
        public static List<T> AddUnique<T>(this List<T> source, T item)
        {
            if (!source.Contains(item)) source.Add(item);
            return source;
        }
    }
}
