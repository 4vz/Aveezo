using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class IEnumerableExtensions
    {
        public static T Find<T>(this IEnumerable<T> value, Type type)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            foreach (var v in value)
            {
                if (v.GetType() == type)
                    return v;
            }
            return default;
        }

        public static bool Has<T>(this IEnumerable<T> value, Type type) => value.Find(type) != null;

        public static IEnumerable<T> Keep<T>(this IEnumerable<T> value, Predicate<T> keep)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var list = new List<T>(value);

            foreach (var v in value)
            {
                if (!keep(v))
                {
                    list.Remove(v);
                }
            }

            return list;

        }

        public static IEnumerable<T> Take<T>(this IEnumerable<T> value, int count)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var list = new List<T>();

            int i = 0;
            foreach (var v in value)
            {
                if (i < count)
                    list.Add(v);
                i++;
            }

            return list;
        }

        public static IEnumerable<T> Combine<T>(this IEnumerable<T> value, IEnumerable<T> comb)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (comb is null) throw new ArgumentNullException(nameof(comb));

            var list = value.ToList();
            list.AddRange(comb);

            return list;
        }

        public static IEnumerable<T> Combine<T>(this IEnumerable<T> value, T comb)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (comb is null) throw new ArgumentNullException(nameof(comb));

            var list = value.ToList();
            list.Add(comb);

            return list;
        }

        public static T[] ToArray<T>(this IEnumerable<T> value) => value.ToList().ToArray();

        public static List<T> ToList<T>(this IEnumerable<T> value) => new(value);
    }
} 


