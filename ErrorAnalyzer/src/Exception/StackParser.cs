using System;
using System.Collections.Generic;
using System.Text;

namespace ErrorAnalyzer
{
    /// <summary>
    /// Provides utilities for parsing and formatting stack trace information.
    /// </summary>
    public static class StackParser
    {
        /// <summary>
        /// Mapping table for converting common .NET type names to their C# equivalents.
        /// </summary>
        private static readonly List<(string, string)> replaceTable = new()
        {
            ( "System.Void ", "void " ),
            ( "System.Boolean ", "bool " ),
            ( "System.Byte ", "byte " ),
            ( "System.Char ", "char " ),
            ( "System.Decimal ", "decimal " ),
            ( "System.Double ", "double " ),
            ( "System.Single ", "float " ),
            ( "System.Int32 ", "int " ),
            ( "System.Int64 ", "long " ),
            ( "System.Object ", "object " ),
            ( "System.SByte ", "sbyte " ),
            ( "System.Int16 ", "short " ),
            ( "System.String ", "string " ),
            ( "System.UInt32 ", "uint " ),
            ( "System.UInt64 ", "ulong " ),
            ( "System.UInt16 ", "ushort " ),
            ( "System.Collections.Generic.List`1[T]", "List<T>"),
            ( "System.Collections.Generic.Dictionary`2[TKey,TValue]", "Dictionary<TKey, TValue>")
        };

        /// <summary>
        /// Parses a stack trace string into a list of type and method name pairs.
        /// </summary>
        /// <param name="source">The stack trace string to parse.</param>
        /// <returns>A list of tuples containing type name and method name pairs.</returns>
        public static List<(string, string)> ParseStackTraceLines(string source)
        {
            var list = new List<(string, string)>();
            if (string.IsNullOrEmpty(source)) return list;

            string[] sourceLines = source.Split('\n', '\r');
            foreach (string line in sourceLines)
            {
                int end = line.IndexOf('(');
                if (end == -1) continue;

                int typeEnd = line.LastIndexOf('.', end);
                if (typeEnd == -1 || (end - typeEnd - 2) <= 0) continue;

                string typeString = line.StartsWith("  at ")
                    ? line.Substring(5, typeEnd - 5)
                    : line.Substring(0, typeEnd);
                string methodString = line.Substring(typeEnd + 1, end - typeEnd - 2);

                list.Add((typeString, methodString));
            }
            return list;
        }

        /// <summary>
        /// Cleans and formats a stack trace string to improve readability.
        /// </summary>
        /// <param name="source">The original stack trace string to clean.</param>
        /// <returns>A cleaned and formatted stack trace string.</returns>
        public static string CleanStacktrace(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;

            var lines = source.Split('\n', '\r');
            var lineBuilder = new StringBuilder();
            var resultBuilder = new StringBuilder();
            bool isFistLine = true;
            foreach (string str in lines)
            {
                if (string.IsNullOrWhiteSpace(str))
                {
                    continue;
                }
                if (str.IndexOf(')') == -1)
                {
                    resultBuilder.AppendLine(str);
                    if (isFistLine) // Add extra line break for the first line to note the exception type
                    {
                        isFistLine = false;
                        resultBuilder.AppendLine();
                    }
                    continue;
                }
                lineBuilder.Clear();
                lineBuilder.Append(str);

                // Remove hash string in " <AA...AA>:"
                var start = str.LastIndexOf(" <", StringComparison.Ordinal);
                var end = str.LastIndexOf(">:", StringComparison.Ordinal);
                if (start != -1 && end > start)
                {
                    lineBuilder.Remove(start, end - start + 2);
                }

                // Beautify IL strings
                lineBuilder.Replace(" (at", "; (");
                lineBuilder.Replace(" inIL_", " ;IL_");

                // Replace .Net type to C# built-in type name
                foreach (var tuple in replaceTable)
                {
                    lineBuilder.Replace(tuple.Item1, tuple.Item2);
                }
                resultBuilder.AppendLine(lineBuilder.ToString());
            }
            return resultBuilder.ToString();
        }
    }
}
