using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public static class Collections
    {
        public readonly static char[] Space = new char[] { ' ' };

        public readonly static char[] SpaceTab = new char[] { ' ', '\t' };

        public readonly static char[] Semicolon = new char[] { ';' };

        public readonly static char[] Colon = new char[] { ':' };

        public readonly static char[] At = new char[] { '@' };

        public readonly static char[] Equal = new char[] { '=' };

        public readonly static char[] Comma = new char[] { ',' };

        public readonly static char[] Dot = new char[] { '.' };

        public readonly static char[] HyphenMinus = new char[] { '-' };

        public readonly static char[] Slash = new char[] { '/' };

        public readonly static string[] NewLine = new string[] { "\r\n", "\r", "\n" };

        public readonly static string WordDigit = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public readonly static string WordDigitUnderscore = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";

        public readonly static string Printable = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        public static T[] Create<T>(T value, int count)
        {
            if (count > 0)
            {
                List<T> values = new List<T>(count);

                for (int i = 0; i < count; i++)
                {
                    values.Add(value);
                }

                return values.ToArray();
            }
            else
                return null;
        }

        public static Dictionary<string, string> CreateDictionary(string value, char[] entriesSeparator, char[] keyValueSeparator) => CreateDictionary(value, entriesSeparator, keyValueSeparator, StringOptions.None, StringOptions.None);

        public static Dictionary<string, string> CreateDictionary(string value, char[] entriesSeparator, char[] keyValueSeparator, StringOptions keyOptions, StringOptions valueOptions)
        {
            if (value != null)
            {
                var dict = new Dictionary<string, string>();

                var entries = value.Split(entriesSeparator, StringSplitOptions.RemoveEmptyEntries);

                foreach (var entry in entries)
                {
                    var entryKeyValue = entry.Split(keyValueSeparator, 2);

                    if (entryKeyValue.Length == 2)
                    {
                        var entryKey = entryKeyValue[0].Trim().Convert(keyOptions);
                        var entryValue = entryKeyValue[1].Convert(valueOptions);

                        if (entryKey.Length > 0)
                        {
                            dict.Add(entryKey, entryValue);
                        }
                    }
                }

                return dict;
            }
            else
                return null;
        }
    }
}
