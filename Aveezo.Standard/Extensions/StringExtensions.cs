using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public static class StringExtensions
    {
        private static string TokenizeOperation(string value, StringOperations operations)
        {
            if (operations.HasFlag(StringOperations.TrimStart))
            {
                value = value.TrimStart();
            }
            if (operations.HasFlag(StringOperations.TrimEnd))
            {
                value = value.TrimEnd();
            }

            return value;
        }

        public static string[] Tokenize(this string value, int count, StringOperations operations, params int[] indexes)
        {   
            List<string> results = new List<string>();

            int length = value.Length;

            List<int> lengths = new List<int>();

            int ti = 0;

            foreach (int index in indexes)
            {
                if (index < length)
                {
                    if (ti > 0)
                    {
                        lengths.Add(index - indexes[ti - 1]);
                    }
                }
                else
                {
                    break;
                }

                ti++;
            }

            if (ti > 0)
            {
                lengths.Add(length - indexes[ti - 1]);
            }

            ti = 0;

            foreach (int tokenLength in lengths)
            {
                results.Add(TokenizeOperation(value.Substring(indexes[ti], tokenLength), operations));

                ti++;
            }

            if (results.Count < count)
            {
                int div = count - results.Count;

                for (int i = 0; i < div; i++)
                {
                    results.Add("");
                }
            }

            return results.ToArray();
        }

        public static string[] Tokenize(this string value, StringOperations operations, params int[] indexes)
        {
            return value.Tokenize(0, operations, indexes);
        }

        public static string[] Tokenize(this string value, params int[] indexes)
        {
            return value.Tokenize(StringOperations.None, indexes);
        }

        public static string[] Tokenize(this string value, int count, StringOperations operations)
        {
            count = count < 1 ? 0 : count;

            List<string> results = new List<string>();

            foreach (string token in value.Split(Arrays.Space, StringSplitOptions.RemoveEmptyEntries))
            {
                results.Add(TokenizeOperation(token, operations));
            }

            if (results.Count < count)
            {
                int div = count - results.Count;

                for (int i = 0; i < div; i++)
                {
                    results.Add("");
                }
            }

            return results.ToArray();
        }

        public static string[] Tokenize(this string value, StringOperations operations)
        {
            return value.Tokenize(0, operations);
        }

        public static string[] Tokenize(this string value)
        {
            return value.Tokenize(StringOperations.None);
        }

        public static string[] Trim(this string[] values)
        {
            List<string> valuesx = new List<string>();

            foreach (string value in values)
            {
                valuesx.Add(value.Trim());
            }

            return valuesx.ToArray();
        }

        public static string[] TrimStart(this string[] values)
        {
            List<string> valuesx = new List<string>();

            foreach (string value in values)
            {
                valuesx.Add(value.TrimStart());
            }

            return valuesx.ToArray();
        }

        public static string[] TrimEnd(this string[] values)
        {
            List<string> valuesx = new List<string>();

            foreach (string value in values)
            {
                valuesx.Add(value.TrimEnd());
            }

            return valuesx.ToArray();
        }
    }

    [Flags]
    public enum StringOperations
    {
        None = 0,
        TrimStart = 1,
        TrimEnd = 2,
        Trim = 3
    }
}
