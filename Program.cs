using Microsoft.DocAsCode.Glob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarkdownVariation
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
            }
            FindVariation(Path.GetFullPath(args[0]), args[1]);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"    {nameof(MarkdownVariation)} <cwd> <glob>");
        }

        private static void FindVariation(string folder, string glob)
        {
            var files = FileGlob.GetFiles(folder, new[] { glob }, new string[0]);
            var oldProxy = GetMarkProxy(CreateDomain("old", "old.config"));
            var newDomain = CreateDomain("new", "new/new.config");
            var newProxy = GetMarkProxy(newDomain);
            foreach (var file in files)
            {
                var markdown = File.ReadAllText(file);
                var oldTreeJson = oldProxy.Parse(markdown);
                var newTreeJson = newProxy.Parse(markdown);
                Compare(file, oldTreeJson, newTreeJson);
            }
        }

        private static void PrintAssemblies()
        {
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                Console.WriteLine(ass.GetName().FullName);
            }
        }

        private static void Compare(string file, string oldTreeJson, string newTreeJson)
        {
            if (oldTreeJson == newTreeJson)
            {
                Console.WriteLine($"File {file} has no change.");
            }
            var oldObject = Parse(oldTreeJson);
            var newObject = Parse(newTreeJson);
            try
            {
                Compare(oldObject, newObject);
            }
            catch (NotSameValueException ex)
            {
                Console.WriteLine($"File {file} has different: {ex.Message}:");
                Console.WriteLine($"\tOld: {oldObject}");
                Console.WriteLine($"\tNew: {newObject}");
            }
            catch (NotSameException ex)
            {
                Console.WriteLine($"File {file} has different: {ex.Message}:");
                Console.WriteLine($"\tOld: {ex.OldToken}");
                Console.WriteLine($"\tNew: {ex.NewToken}");
            }
        }

        private static JObject Parse(string json)
        {
            try
            {
                var fixedJson = Regex.Replace(json, @"(?<!\\)\\(?!(r|n|t|\|""|'|\\))", @"\\");
                return JObject.Parse(fixedJson);
            }
            catch (Exception)
            {
                return new JObject
                {
                    ["error"] = "bad json."
                };
            }
        }

        private static void Compare(JToken oldToken, JToken newToken)
        {
            if (oldToken is JArray && newToken is JArray)
            {
                Compare((JArray)oldToken, (JArray)newToken);
            }
            else if (oldToken is JObject && newToken is JObject)
            {
                Compare((JObject)oldToken, (JObject)newToken);
            }
            else if (oldToken.Type == JTokenType.String && newToken.Type == JTokenType.String)
            {
                Compare(oldToken.ToString(), newToken.ToString());
            }
            else
            {
                throw new NotSameValueException();
            }
        }

        private static void Compare(JArray oldArray, JArray newArray)
        {
            for (int i = 0; i < oldArray.Count; i++)
            {
                if (newArray.Count <= i)
                {
                    throw new NotSameException("More tokens in old version.", oldArray[i], null);
                }
                if (!JToken.EqualityComparer.Equals(oldArray[i], newArray[i]))
                {
                    try
                    {
                        Compare(oldArray[i], newArray[i]);
                    }
                    catch (NotSameValueException)
                    {
                        throw new NotSameException("Token is different.", oldArray[i], newArray[i]);
                    }
                }
            }
            if (newArray.Count > oldArray.Count)
            {
                throw new NotSameException("More tokens in new version.", null, newArray[oldArray.Count]);
            }
        }

        private static void Compare(JObject oldObject, JObject newObject)
        {
            var oldKeySet = ((IDictionary<string, JToken>)oldObject).Keys;
            var newKeySet = ((IDictionary<string, JToken>)newObject).Keys;
            foreach (var key in oldKeySet.Intersect(newKeySet))
            {
                if (!JToken.EqualityComparer.Equals(oldObject[key], newObject[key]))
                {
                    Compare(oldObject[key], newObject[key]);
                }
            }
        }

        private static void Compare(string oldText, string newText)
        {
            if (oldText.TrimEnd('\n') != newText.TrimEnd('\n'))
            {
                throw new NotSameValueException();
            }
        }

        private static AppDomain CreateDomain(string folder, string configFile)
        {
            var si = AppDomain.CurrentDomain.SetupInformation;
            si.PrivateBinPath = folder;
            si.ConfigurationFile = configFile;
            return AppDomain.CreateDomain(folder, null, si);
        }

        private static MarkProxy GetMarkProxy(AppDomain domain)
        {
            return (MarkProxy)domain.CreateInstanceAndUnwrap(nameof(MarkdownVariation), typeof(MarkProxy).FullName);
        }
    }
}
