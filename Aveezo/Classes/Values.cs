using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class Values<T> : IEnumerable<T>, IEquatable<Values<T>>
    {
        #region Fields

        private readonly T[] values;

        public int Count => values.Length;

        public T this[int index] => values[index];

        public bool IsEmpty => Count == 0;

        #endregion

        #region Constructors

        public Values(params T[] values)
        {
            this.values = values ?? Array.Empty<T>();
        }

        public Values(StringValues values)
        {
            List<T> s = new();
            foreach (var value in values)
                s.Add(value.Cast<T>());

            this.values = s.ToArray();
        }

        #endregion

        #region Operators

        public static implicit operator Values<T>(T[] s) => s == null ? null : new(s);

        public static implicit operator Values<T>(StringValues s) => new(s);

        public static implicit operator Values<T>(T s) => s == null ? null : new(s);

        public static implicit operator T[](Values<T> v) => v?.values.ToArray();

        public static implicit operator List<T>(Values<T> v) => v == null ? null : new(v.values);

        public static implicit operator T(Values<T> v) => v.Count == 0 ? default : v.values[0];

        public static implicit operator Values<T>((T, T) s) => new(s.Item1, s.Item2);

        public static implicit operator Values<T>((T, T, T) s) => new(s.Item1, s.Item2, s.Item3);

        public static implicit operator Values<T>((T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4);

        public static implicit operator Values<T>((T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5);

        public static implicit operator Values<T>((T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6);

        public static implicit operator Values<T>((T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7);

        public static implicit operator Values<T>((T, T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7, s.Item8);

        public static implicit operator Values<T>((T, T, T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7, s.Item8, s.Item9);

        public static implicit operator Values<T>((T, T, T, T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7, s.Item8, s.Item9, s.Item10);

        public static Values<T> operator +(Values<T> values1, Values<T> values2)
        {
            if (values1 == null && values2 == null)
                return null;

            List<T> list1 = values1 ?? (new());
            List<T> list2 = values2 ?? (new());

            list1.AddRange(list2);

            return new(list1.ToArray());
        }

        #endregion

        #region Methods

        public IEnumerator<T> GetEnumerator() => values.GetEnumerator<T>();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(Values<T> other)
        {
            if (other == null)
                return false;
            else
            {
                T[] otherValues = other;
                return Equals(values, otherValues);
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Values<T>);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var val in values)
                {
                    hash = hash * 31 + val.GetHashCode();
                }
                return hash;
            }
        }

        public T[] ToArray() => values.ToArray();

        public List<T> ToList() => values.ToList();

        #endregion

        #region Statics

        #endregion
    }
}
