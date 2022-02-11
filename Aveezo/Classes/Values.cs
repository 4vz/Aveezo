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
