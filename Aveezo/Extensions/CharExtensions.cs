using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public static class CharExtensions
    {
        public static int[] ToInt(this char[] values)
        {
            return Array.ConvertAll(values, c => (int)c);
        }

        public static char Convert(this char value, CharConvertOptions options)
        {
            if (options.HasFlag(CharConvertOptions.ToLower) && !options.HasFlag(CharConvertOptions.ToUpper))
                value = char.ToLower(value);
            if (options.HasFlag(CharConvertOptions.ToUpper) && !options.HasFlag(CharConvertOptions.ToLower))
                value = char.ToUpper(value);

            return value;
        }
    }

    [Flags]
    public enum CharConvertOptions
    {
        None = 0,
        ToLower = 1,
        ToUpper = 2
    }
}
