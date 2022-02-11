using System;
using System.Collections;
using System.Collections.Generic;

using System.Text;

namespace Aveezo
{
    public static class BitArrayExtensions
    {
        public static bool[] ToArray(this BitArray value)
        {
            var bools = new List<bool>();
            foreach (var v in value) bools.Add((bool)v);
            return bools.ToArray();
        }

        public static string ToString(this BitArray value, char zero, char one)
        {
            var sb = new StringBuilder();
            foreach (var v in value) sb.Append((bool)v == true ? one : zero);
            return sb.ToString();
        }
    }
}
