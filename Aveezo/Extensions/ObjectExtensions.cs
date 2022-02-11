using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;

namespace Aveezo
{
    public static class ObjectExtensions
    {
        public static T[] Filter<T>(this T[] array, Predicate<T> filter)
        {
            List<T> newArray = new List<T>();

            foreach (var item in array)
            {
                var keep = filter(item);

                if (keep) newArray.Add(item);
            }
            return newArray.ToArray();
        }

        public static T Find<T>(this T[] array, Predicate<T> find)
        {
            T found = default;

            foreach (var t in array)
            {
                if (find(t))
                {
                    found = t;
                    break;
                }
            }

            return found;
        }

        public static bool Contains<T>(this T[] array, T find) => array.Find(t => find != null && find.Equals(t)) != null;

        public static TResult[] Invoke<TResult>(this Array array, Func<object, TResult> cast)
        {
            var list = new List<TResult>();

            foreach (var t in array)
            {
                list.Add(cast(t));
            }

            return list.ToArray();
        }

        public static TResult[] Invoke<T, TResult>(this T[] array, Func<T, TResult> cast)
        {
            var list = new List<TResult>();

            foreach (var t in array)
            {
                list.Add(cast(t));
            }

            return list.ToArray();
        }

        public static TResult Format<T, TResult>(this T x, Func<T, TResult> f) => x is null ? default : f(x);

        public static TResult Format<T, TResult>(this T x, Func<T, TResult> f, TResult ifnull) => x is null ? ifnull : f(x);

        public static string Format<T>(this T x, string format) => x.Format(o => string.Format(format, o));

        public static T Cast<T>(this object value)
        {
            value.TryCast<T>(out var cast);
            return cast;
        }

        public static bool TryCast<T>(this object value, out T cast)
        {
            var notExcepted = true;

            if (value is T variable)
                cast = variable;
            else
            {
                var to = typeof(T);
                object casted = null;

                if (casted == null && to == typeof(IPAddress))
                {
                    if (value is string str && IPAddress.TryParse(str, out var ipAddress))
                        casted = ipAddress;
                }

                try
                {
                    if (casted != null)
                        cast = (T)Convert.ChangeType(casted, typeof(T));
                    else
                    {
                        //Handling Nullable types i.e, int?, double?, bool? .. etc
                        if (Nullable.GetUnderlyingType(typeof(T)) != null)
                            cast = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value);
                        else
                            cast = (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                catch (Exception)
                {
                    notExcepted = false;
                    cast = default;
                }
            }

            return notExcepted;
        }

        public static int IndexOf<T>(this T[] array, T value)
        {
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
        /// Reports the zero-based index of the first occurrence this object in the specified array.
        /// </summary>
        public static int IndexIn<T>(this T value, params T[] array)
        {
            var result = -1;
            var index = 0;

            foreach (var item in array)
            {
                if (Equals(value, item))
                {
                    result = index;
                    break;
                }
                index++;
            }

            return result;
        }

        public static T[][] Split<T>(this T[] array, int size)
        {
            if (size < 1) throw new ArgumentOutOfRangeException();

            var marray = new List<T[]>();
            var oarray = new List<T>();

            int c = 0;
            foreach (T t in array)
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

        public static bool IsNumeric(this object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

        public static T[] Array<T>(this T value)
        {
            if (value is Array array)
                return (T[])array;
            else
                return new T[] { value }; 
        }

        public static bool Is<T>(this T value, Predicate<T> filter, out T obj)
        {
            obj = value;
            return filter(obj);
        }

        public static bool? TrueFalse<T>(this T value, T trueIfValue, T falseIfValue)
        {
            if (Equals(value, trueIfValue))
                return true;
            else if (Equals(value, falseIfValue))
                return false;
            else
                return null;
        }

        public static T NullIf<T>(this T value, T when) => Equals(value, when) ? default : value;

    }
}
