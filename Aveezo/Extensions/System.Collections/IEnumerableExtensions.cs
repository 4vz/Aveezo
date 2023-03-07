using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public static bool Has<T>(this IEnumerable<T> value, Predicate<T> a)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            
            foreach (var v in value)
            {
                if (a(v))
                    return true;
            }
            return false;
        }

        public static int Count<T>(this IEnumerable<T> value, Predicate<T> a)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var count = 0;

            foreach (var v in value)
            {
                if (a(v)) count++;
            }

            return count;
        }

        public static IEnumerable<T> Unique<T>(this IEnumerable<T> value, Func<T, string> hashFunction)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var dict = new List<string>();
            var uniques = new List<T>();

            foreach (var v in value)
            {
                var fx = hashFunction(v);
                if (!dict.Contains(fx))
                {
                    dict.Add(fx);
                    uniques.Add(v);
                }
            }

            return uniques;
        }

        /// <summary>
        /// Creates a new collection from specified entries that have returned true by the predicate.
        /// </summary>
        public static IEnumerable<T> Keep<T>(this IEnumerable<T> value, Predicate<T> keep)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var list = new List<T>(value);

            foreach (var v in value)
            {
                if (!keep(v))
                    list.Remove(v);
            }

            return list;

        }
        /// <summary>
        /// Creates a new collection with entries that is specified by the count parameter.
        /// </summary>
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

        public static IEnumerable<T> Append<T>(this IEnumerable<T> value, IEnumerable<T> comb)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (comb is null) throw new ArgumentNullException(nameof(comb));

            var list = value.ToList();
            list.AddRange(comb);

            return list;
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> value, T comb)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (comb is null) throw new ArgumentNullException(nameof(comb));

            var list = value.ToList();
            list.Add(comb);

            return list;
        }

        public static IEnumerable<T> AppendUnique<T>(this IEnumerable<T> value, T comb)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (comb is null) throw new ArgumentNullException(nameof(comb));

            var list = value.ToList();
            if (!list.Contains(comb))
                list.Add(comb);

            return list;
        }

        public static T[] ToArray<T>(this IEnumerable<T> value) => (value is T[] array) ? array : value.ToList().ToArray();

        public static TResult[] ToArray<T, TResult>(this IEnumerable<T> value, Func<T, TResult> func) => (value is TResult[] array) ? array : value.ToList(func).ToArray();

        public static List<T> ToList<T>(this IEnumerable<T> value) => (value is List<T> list) ? list : new(value);

        public static List<TResult> ToList<T, TResult>(this IEnumerable<T> value, Func<T, TResult> func)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (func is null) throw new ArgumentNullException(nameof(func));
            if (value is List<TResult> list) return list;

            List<TResult> n = new();
            foreach (var v in value) n.Add(func(v));
            return n;
        }

        public static IEnumerator<T> GetEnumerator<T>(this IEnumerable<T> value) => value.GetEnumerator();

        /// <summary>
        /// Iterates each object in the collection.
        /// </summary>
        public static IEnumerable<T> Each<T>(this IEnumerable<T> value, Action<T> action) 
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var v in value) action(v);

            return value;
        }

        /// <summary>
        /// Creates a new collection which iterate from specified collection with specified function.
        /// </summary>
        public static IEnumerable<T> Each<T>(this IEnumerable<T> value, Func<T, T> func)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (func is null) throw new ArgumentNullException(nameof(func));

            List<T> n = new();
            foreach (var v in value) n.Add(func(v));
            return n;
        }

        /// <summary>
        /// Creates a new value which iterate from specified value with specified function.
        /// </summary>
        public static T[] Each<T>(this T[] array, Func<T, T> func) => ((IEnumerable<T>)array).Each<T>(func).ToArray();

        /// <summary>
        /// Creates a new collection with new type, which is iterated from specified collection with specified function.
        /// </summary>
        public static IEnumerable<TResult> Each<T, TResult>(this IEnumerable<T> value, Func<T, TResult> func)
        {
            if (value is null)  throw new ArgumentNullException(nameof(value));
            if (func is null) throw new ArgumentNullException(nameof(func));

            List<TResult> n = new();
            foreach (var v in value) n.Add(func(v));
            return n;
        }

        /// <summary>
        /// Creates a new value with new type, which is iterated from specified value with specified function.
        /// </summary>
        public static TResult[] Each<T, TResult>(this T[] array, Func<T, TResult> func) => ((IEnumerable<T>)array).Each(func).ToArray();

        public static IEnumerable<TResult> Convert<T, TResult>(this IEnumerable<T> value) => value.Each(v => v.Convert<TResult>());

        public static TResult[] Convert<T, TResult>(this T[] value) => ((IEnumerable<T>)value).Convert<T, TResult>().ToArray();

        /// <summary>
        /// Returns zero based index of specified value in the collection.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> array, T value)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));

            var result = -1;
            var index = 0;

            foreach (T item in array)
            {
                if (Equals(item, value))
                {
                    result = index;
                    break;
                }
                index++;
            }
            return result;
        }

        /// <summary>
        /// Creates a new collection filtered from specified list.
        /// </summary>
        public static IEnumerable<T> Filter<T>(this IEnumerable<T> value, Predicate<T> filter)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (filter is null) throw new ArgumentNullException(nameof(filter));

            List<T> n = new();
            foreach (var v in value) if (filter(v)) n.Add(v);
            return n;
        }

        public static T[] Filter<T>(this T[] array, Predicate<T> filter) => ((IEnumerable<T>)array).Filter(filter).ToArray();

        /// <summary>
        /// Returns the first item that fulfilled the specified find function.
        /// </summary>
        public static T Find<T>(this IEnumerable<T> value, Predicate<T> find)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (find is null) throw new ArgumentNullException(nameof(find));

            T found = default;

            foreach (var v in value)
            {
                if (find(v))
                {
                    found = v;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Gets whether the specified items exist in the list.
        /// </summary>
        public static bool Contains<T>(this IEnumerable<T> value, IEnumerable<T> items)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (item == null) continue;

                var found = false;
                foreach (var t in value)
                {
                    if (t == null) continue;
                    if (t.Equals(item))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found) return false;
            }

            return true;
        }

        public static bool Contains<T>(this IEnumerable<T> value, params T[] items) => value.Contains((IEnumerable<T>)items);

        /// <summary>
        /// Splits the specified value to jagged array which size specified.
        /// </summary>
        public static T[][] Split<T>(this IEnumerable<T> value, int size)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (size < 1) throw new ArgumentOutOfRangeException(nameof(size));

            var marray = new List<T[]>();
            var oarray = new List<T>();

            int c = 0;
            foreach (T t in value)
            {
                oarray.Add(t);

                if (++c == size)
                {
                    marray.Add(oarray.ToArray());
                    oarray.Clear();
                    c = 0;
                }
            }

            if (oarray.Count > 0)
                marray.Add(oarray.ToArray());


            return marray.ToArray();
        }
    }
} 


