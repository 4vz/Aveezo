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
    }
}
