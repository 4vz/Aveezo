using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public static class IListExtensions
    {
        public static T[] ToArray<T>(this IList list)
        {
            var objects = new List<T>(list.Count);

            foreach (T o in list)
                objects.Add(o);

            return objects.ToArray();
        }
    }
}
    