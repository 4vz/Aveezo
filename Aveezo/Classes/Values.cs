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

        private T[] values;

        public int Count => values.Length;

        public T this[int index] => values[index];

        #endregion

        #region Constructors

        public Values(params T[] values)
        {
            this.values = values ?? Array.Empty<T>();
        }

        #endregion

        #region Operators

        public static implicit operator Values<T>(T[] s) => new(s);

        public static implicit operator Values<T>(T s) => new(s);

        public static implicit operator T[](Values<T> v) => v.values.ToArray();

        public static implicit operator T(Values<T> v) => v.values[0];

        public static implicit operator Values<T>((T, T) s) => new(s.Item1, s.Item2);

        public static implicit operator Values<T>((T, T, T) s) => new(s.Item1, s.Item2, s.Item3);

        public static implicit operator Values<T>((T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4);

        public static implicit operator Values<T>((T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5);

        public static implicit operator Values<T>((T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6);

        public static implicit operator Values<T>((T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7);

        public static implicit operator Values<T>((T, T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7, s.Item8);

        public static implicit operator Values<T>((T, T, T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7, s.Item8, s.Item9);

        public static implicit operator Values<T>((T, T, T, T, T, T, T, T, T, T) s) => new(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5, s.Item6, s.Item7, s.Item8, s.Item9, s.Item10);

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

        #endregion

        #region Statics

        #endregion
    }
}
