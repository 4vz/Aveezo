using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Aveezo
{
    public enum CaseStyle
    {
        CamelCase,
        UpperSnakeCase,
        LowerSnakeCase,
        UpperKebabCase,
        LowerKebabCase,
        PascalCase
    }

    public static class StringExtensions
    {
        public static string ToCase(this string value, CaseStyle caseStyle)
        {
            // get words
            var words = new List<string>();
            var wordBuilder = new StringBuilder();

            foreach (var c in value)
            {
                if (char.IsLower(c))
                {
                    if (wordBuilder.Length > 0 && char.IsDigit(wordBuilder[^1]))
                    {
                        words.Add(wordBuilder.ToString());
                        wordBuilder.Clear();
                    }
                    else if (wordBuilder.Length > 1 && char.IsUpper(wordBuilder[^1]))
                    {
                        var moveChar = wordBuilder[^1];
                        wordBuilder.Remove(wordBuilder.Length - 1, 1);
                        words.Add(wordBuilder.ToString());
                        wordBuilder.Clear();
                        wordBuilder.Append(moveChar);
                    }
                    wordBuilder.Append(c);
                }
                else if (char.IsUpper(c) || char.IsDigit(c))
                {
                    if (wordBuilder.Length > 0 && ((char.IsUpper(c) && !char.IsUpper(wordBuilder[^1])) || (char.IsDigit(c) && !char.IsDigit(wordBuilder[^1]))))
                    {
                        words.Add(wordBuilder.ToString());
                        wordBuilder.Clear();
                    }
                    wordBuilder.Append(c);
                }
                else if (wordBuilder.Length > 0)
                {
                    words.Add(wordBuilder.ToString());
                    wordBuilder.Clear();
                }
                else if (c == '_')
                {
                    words.Add("");
                }
            }

            if (wordBuilder.Length > 0)
                words.Add(wordBuilder.ToString());

            var final = new StringBuilder();

            if (caseStyle == CaseStyle.PascalCase || caseStyle == CaseStyle.CamelCase)
            {
                foreach (var word in words)
                {
                    var wb = new StringBuilder();

                    if (final.Length == 0 && caseStyle == CaseStyle.CamelCase)
                        wb.Append(word.ToLower());
                    else
                    {
                        wb.Append(word);
                        wb[0] = char.ToUpper(wb[0]);
                    }

                    final.Append(wb);
                }
            }
            else if (caseStyle == CaseStyle.UpperSnakeCase || caseStyle == CaseStyle.LowerSnakeCase || caseStyle == CaseStyle.LowerKebabCase || caseStyle == CaseStyle.UpperKebabCase)
            {
                int iword = 0;
                foreach (var word in words)
                {
                    if (iword > 0)
                    {
                        if (caseStyle == CaseStyle.UpperSnakeCase || caseStyle == CaseStyle.LowerSnakeCase)
                            final.Append('_');
                        else
                            final.Append('-');
                    }

                    if (caseStyle == CaseStyle.UpperSnakeCase || caseStyle == CaseStyle.UpperKebabCase)
                        final.Append(word.ToUpper());
                    if (caseStyle == CaseStyle.LowerSnakeCase || caseStyle == CaseStyle.LowerKebabCase)
                        final.Append(word.ToLower());

                    iword++;
                }
            }

            return final.ToString();
        }

        public static string ToPascalCase(this string value) => value.ToCase(CaseStyle.PascalCase);

        public static string ToSnakeCase(this string value) => value.ToCase(CaseStyle.LowerSnakeCase);

        public static string ToCamelCase(this string value) => value.ToCase(CaseStyle.CamelCase);

        public static string ToKebabCase(this string value) => value.ToCase(CaseStyle.LowerKebabCase);

        /// <summary>
        /// Returns a new string in which all case styles of all occurrences of a specified string in the current instance are replaced with another specified string.
        /// </summary>
        public static string ReplaceAllCase(this string value, string oldValue, string? newValue)
        {
            foreach (var style in (CaseStyle[])Enum.GetValues(typeof(CaseStyle)))
            {
                var oldValueStyle = oldValue.ToCase(style);
                value = value.Replace(oldValueStyle, newValue);
            }
            return value;
        }

        public static string Convert(this string value, StringConvertOptions options)
        {
            if (options.HasFlag(StringConvertOptions.TrimStart))
                value = value.TrimStart();
            if (options.HasFlag(StringConvertOptions.TrimEnd))
                value = value.TrimEnd();
            if (options.HasFlag(StringConvertOptions.ToLower) && !options.HasFlag(StringConvertOptions.ToUpper))
                value = value.ToLower();
            if (options.HasFlag(StringConvertOptions.ToUpper) && !options.HasFlag(StringConvertOptions.ToLower))
                value = value.ToUpper();

            return value;
        }

        public static string[] Tokenize(this string value, int count, StringConvertOptions operations, params int[] indexes)
        {   
            var results = new List<string>();

            int length = value.Length;

            var  lengths = new List<int>();

            int ti = 0;

            foreach (int index in indexes)
            {
                if (index < length)
                {
                    if (ti > 0)
                        lengths.Add(index - indexes[ti - 1]);
                }
                else
                    break;

                ti++;
            }

            if (ti > 0)
                lengths.Add(length - indexes[ti - 1]);

            ti = 0;

            foreach (int tokenLength in lengths)
            {
                results.Add(value.Substring(indexes[ti], tokenLength).Convert(operations));

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

        public static string[] Tokenize(this string value, StringConvertOptions options, params int[] indexes) => value.Tokenize(0, options, indexes);

        public static string[] Tokenize(this string value, params int[] indexes) => value.Tokenize(StringConvertOptions.None, indexes);

        public static string[] Tokenize(this string value, int count, StringConvertOptions options)
        {
            if (value == null) throw new NullReferenceException();

            count = count < 1 ? 0 : count;

            var results = new List<string>();

            foreach (string token in value.Split(Collections.SpaceTab, StringSplitOptions.RemoveEmptyEntries))
            {
                results.Add(token.Convert(options));
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

        public static string[] Tokenize(this string value, StringConvertOptions options) => value.Tokenize(0, options);

        public static string[] Tokenize(this string value) => value.Tokenize(StringConvertOptions.None);

        public static string[] Trim(this string[] values)
        {
            if (values == null) throw new NullReferenceException();

            var list = new List<string>();

            foreach (string value in values)
            {
                list.Add(value.Trim());
            }

            return list.ToArray();
        }

        public static string[] TrimStart(this string[] values)
        {
            if (values == null) throw new NullReferenceException();

            var list = new List<string>();

            foreach (string value in values)
            {
                list.Add(value.TrimStart());
            }

            return list.ToArray();
        }

        public static string[] TrimEnd(this string[] values)
        {
            if (values == null) throw new NullReferenceException();

            var list = new List<string>();

            foreach (string value in values)
            {
                list.Add(value.TrimEnd());
            }

            return list.ToArray();
        }

        /// <summary>
        /// Join by space
        /// </summary>
        public static string Join(this string[] values) => values.Join(' ');

        public static string Join(this string[] values, char separator) => values == null ? throw new NullReferenceException() : string.Join(separator, values);

        public static string Join(this string[] values, string separator) => values == null ? throw new NullReferenceException() : string.Join(separator, values);

        public static string Join(this string[] values, char separator, int startIndex, int count) => values == null ? throw new NullReferenceException() : string.Join(separator, values, startIndex, count);

        public static string Join(this string[] values, string separator, int startIndex, int count) => values == null ? throw new NullReferenceException() : string.Join(separator, values, startIndex, count);

        /// <summary>
        /// Join by space
        /// </summary>
        public static string Join(this List<string> values) => values.Join(' ');

        public static string Join(this List<string> values, char separator) => values?.ToArray().Join(separator);

        public static string Join(this List<string> values, string separator) => values?.ToArray().Join(separator);

        public static string Join(this List<string> values, char separator, int startIndex, int count) => values?.ToArray().Join(separator, startIndex, count);

        public static string Join(this List<string> values, string separator, int startIndex, int count) => values?.ToArray().Join(separator, startIndex, count);

        public static string Append(this string value, char by, int repeat)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (repeat < 0) 
                throw new ArgumentOutOfRangeException(nameof(repeat));
            else if (repeat == 0)
                return value;
            else
            {
                var sb = new StringBuilder(value);

                for (int i = 0; i < repeat; i++)
                    sb.Append(by);

                return sb.ToString();
            }
        }

        public static bool SurroundedBy(this string value, string start, string end, out string remaining) => value.SurroundedBy(start, end, value, out remaining);

        public static bool SurroundedBy(this string value, string start, string end, string originalValue, out string remaining)
        {
            if (value == null) throw new NullReferenceException();

            var sid = start != null && value.StartsWith(start) ? start.Length : 0;
            var eid = end != null && value.EndsWith(end) ? end.Length : 0;

            remaining = null;
            var length = value.Length;

            // sidremainingvid
            // 012345678901234
            //           1

            if (originalValue == null) originalValue = value;

            if (sid == 0 && eid == 0)
                return false;
            else if ((sid + eid) >= length)
                remaining = "";
            else
                remaining = originalValue.Substring(sid, length - sid - eid);

            return true;
        }

        public static bool StartsWith(this string value, string start, out string remaining) => value.SurroundedBy(start, null, null, out remaining);

        public static bool StartsWith(this string value, string start, string originalValue, out string remaining) => value.SurroundedBy(start, null, originalValue, out remaining);

        public static bool EndsWith(this string value, string end, out string remaining) => value.SurroundedBy(null, end, null, out remaining);

        public static bool EndsWith(this string value, string end, string originalValue, out string remaining) => value.SurroundedBy(null, end, originalValue, out remaining);

        public static string CleanSpaces(this string value) => Regex.Replace(value, @"\s+", " ");

        public static string Clean(this string value, string regex) => Regex.Replace(value, regex, "");

        public static string JoinNewline(this string[] values, Newline newline)
        {
            string nl = null;
            switch (newline)
            {
                case Newline.Lf: nl = "\n";
                    break;
                case Newline.CrLf: nl = "\r\n";
                    break;
                case Newline.Cr: nl = "\r"; 
                    break;
                case Newline.LfCr: nl = "\n\r";
                    break;
            }

            return values.Join(nl);
        }

        public static string JoinNewline(this string[] values) => values.JoinNewline(Newline.Lf);

        public static string[] Split(this string value, string[] separator) => value.Split(separator, StringSplitOptions.None);

        public static string Repeat(this string value, int count)
        {
            if (string.IsNullOrEmpty(value)) throw new NullReferenceException();

            var sb = new StringBuilder();

            for (var i = 0; i < count; i++)
                sb.Append(value);

            return sb.ToString();
        }

        public static string ToAscii(this string value)
        {
            if (value == null) throw new NullReferenceException();

            var s = new StringBuilder();

            foreach (var c in value)
            {
                if (char.IsControl(c) || c > 127)
                    s.Append($@"\u{System.Convert.ToInt16(c):x4}");
                else
                    s.Append(c);
            }

            return s.ToString();
        }

        public static string Keep(this string value, string keepCharacters)
        {
            if (value == null) throw new NullReferenceException();

            var sb = new StringBuilder();

            foreach (var c in value)
            {
                if (keepCharacters.Contains(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static Guid ToGuid(this string value) => new Guid(value);

        public static BitArray ToBitArray(this string value, char zero, char one)
        {
            if (string.IsNullOrEmpty(value)) throw new NullReferenceException();

            var bitArray = new BitArray(value.Length);

            var i = 0;
            foreach (var c in value)
            {
                bool? a = null;
                if (c == one) a = true;
                else if (c == zero) a = false;

                if (a.HasValue) bitArray[i++] = a.Value;
            }

            return bitArray;
        }

        public static BitArray ToBitArray(this string value) => value.ToBitArray('0', '1');

        public static byte[] ToBytes(this string value) => Encoding.UTF8.GetBytes(value);

        public static bool IsBase64(this string value) => Base64.Is(value);

        public static bool IsBase64Url(this string value) => Base64.IsUrl(value);

        public static bool IsGuid(this string value) => Guid.TryParse(value, out _);

        public static bool IsPrintable(this string value)
        {
            foreach (var c in value)
                if (c < 32 || c > 126)
                    return false;

            return true;
        }

        public static bool Is(this string value, StringChecks checks)
        {
            bool no = true;
            if (checks == StringChecks.None)
                no = false;
            if (no && checks.HasFlag(StringChecks.Printable))
                no = value.IsPrintable();
            if (no && checks.HasFlag(StringChecks.Guid))
                no = value.IsGuid();

            return no;
        }

        public static void Find(this string value, string regex, Action<string> action) => value.Find(new Regex(regex), action);

        public static void Find(this string value, Regex regex, Action<string> action)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (regex == null) throw new ArgumentNullException(nameof(regex));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (Match match in regex.Matches(value))
            {
                var gc = match.Groups;

                var gcc = gc[0];

                action(gcc.Value);
            }
        }
     
    }

    [Flags]
    public enum StringConvertOptions
    {
        None = 0,
        TrimStart = 1,
        TrimEnd = 2,
        Trim = TrimStart | TrimEnd,
        ToLower = 4,
        ToUpper = 8
    }

    [Flags]
    public enum StringChecks
    {
        None = 0,
        Printable = 1,
        Guid = 2
    }

    public enum Newline
    {
        Lf,
        CrLf,
        Cr,
        LfCr
    }
}
