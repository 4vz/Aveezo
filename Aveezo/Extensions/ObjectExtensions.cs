using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class ObjectExtensions
    {
        public static TResult IfNotNull<T, TResult>(this T x, Func<T, TResult> f) => x is null ? default : f(x);

        public static TResult IfNotNull<T, TResult>(this T x, Func<T, TResult> f, TResult orElse) => x is null ? orElse : f(x);

        public static string Format<T>(this T x, string format, Func<T, object[]> func) => x.IfNotNull(o => string.Format(format, func(o)));

        public static string Format<T>(this T x, string format) => x.Format(format, o => new object[] { o });

        public static bool TryConvert(this object value, Type target, out object cast)
        {
            if (value is Array array)
            {
                List<object> list = new();

                Type elementTarget;
                if (target.IsArray)
                    elementTarget = target.GetElementType();
                else
                    elementTarget = target;

                foreach (var val in array)
                {
                    if (val.TryConvert(elementTarget, out var od))
                        list.Add(od);
                    else
                        list.Add(default);
                }

                cast = list.ToArray();
                return true;
            }
            else
            {
                var ok = true;

                try
                {
                    if (target == typeof(IPAddress) && value is string str && IPAddress.TryParse(str, out var ipAddress))
                        cast = ipAddress;                    
                    else if (Nullable.GetUnderlyingType(target) != null)
                        cast = TypeDescriptor.GetConverter(target).ConvertFrom(value);
                    else
                        cast = System.Convert.ChangeType(value, target);
                }
                catch (Exception)
                {
                    ok = false;
                    cast = default;
                }

                return ok;
            }
        }

        public static bool TryConvert<TResult>(this IEnumerable<object> array, out IEnumerable<TResult> cast)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));

            bool ok = true;
            List<TResult> n = new();

            foreach (var value in array)
            {
                ok = value.TryConvert<TResult>(out TResult valueCast);

                if (!ok)
                    break;
                else
                    n.Add(valueCast);
            }

            if (!ok)
                cast = default;
            else
                cast = n;

            return ok;
        }

        public static bool TryConvert<TResult>(this object value, out TResult cast)
        {
            bool ok = true;
            cast = default;

            try
            {
                cast = (TResult)value;
            }
            catch
            {
                ok = value.TryConvert(typeof(TResult), out object icast);
                cast = (TResult)icast;
            }

            return ok;
        }

        public static IEnumerable<TResult> Convert<TResult>(this IEnumerable<object> value)
        {
            value.TryConvert<TResult>(out var cast);
            return cast;
        }

        public static TResult[] Convert<TResult>(this object[] value) => ((IEnumerable<object>)value).Convert<TResult>().ToArray();

        public static TResult Convert<TResult>(this object value)
        {
            value.TryConvert<TResult>(out var cast);
            return cast;
        }

        /// <summary>
        /// Returns array in the value is an array, else creates new array with the value will be the first entry.
        /// </summary>
        public static T[] Array<T>(this T value)
        {
            if (value is T[] array)
                return array;
            else
                return new T[] { value };
        }

        /// <summary>
        /// Gets whether the value is a numeric type.
        /// </summary>
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

        /// <summary>
        /// Returns true if the value has passed the filter, and for convinience output the value through out object.
        /// </summary>
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

        public static string NullIfEmpty(this string value) => Equals(value, string.Empty) ? null : value;

    }
}
