using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public static class IntExtensions
    {
        public static int[] Split(this int value, int count)
        {
            if (count > 0)
            {
                var groups = new List<int>(count);

                for (int i = 0; i < count; i++)
                    groups.Add(0);

                for (int i = 0; i < value; i++)
                {
                    var group = i % count;
                    groups[group]++;
                }

                return groups.ToArray();
            }
            else return null;
        }

        public static int GetAvailable(this List<int> list)
        {
            var index = 1;
            do
            {
                if (!list.Contains(index)) break;
                index++;
            }
            while (index < int.MaxValue);
            list.Add(index);

            return index;
        }

        public static bool Between(this int value, int first, int last)
        {
            if (first > last) throw new Exception("Invalid range");

            if (value >= first && value <= last)
                return true;
            else
                return false;
        }
    }
}
