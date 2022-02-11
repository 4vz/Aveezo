using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public sealed class Context : IDictionary<object, object>
    {
        #region Fields

        public Dictionary<object, object> dictionary = null;

        public object this[object key]
        { 
            get => dictionary.ContainsKey(key) ? dictionary[key] : null; 
            set 
            {
                if (dictionary.ContainsKey(key))
                    dictionary[key] = value;
                else
                    Add(key, value);
            }
        }

        public ICollection<object> Keys => dictionary.Keys;

        public ICollection<object> Values => dictionary.Values;

        public int Count => dictionary.Count;

        public bool IsReadOnly => false;

        #endregion

        #region Constructors

        public Context()
        {
            dictionary = new Dictionary<object, object>();
        }

        #endregion

        #region Operators


        #endregion

        #region Methods

        public void Add(object key, object value) => dictionary.Add(key, value);

        public void Add(KeyValuePair<object, object> item) => Add(item.Key, item.Value);

        public void Clear() => dictionary.Clear();

        public bool Contains(KeyValuePair<object, object> item) => dictionary.Contains(item);

        public bool ContainsKey(object key) => dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) => throw new NotImplementedException();

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => dictionary.GetEnumerator();

        public bool Remove(object key) => dictionary.Remove(key);

        public bool Remove(KeyValuePair<object, object> item) => dictionary.Remove(item.Key);

        public bool TryGetValue(object key, [MaybeNullWhen(false)] out object value) => dictionary.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();

        #endregion

        #region Statics

        #endregion

    }
}
