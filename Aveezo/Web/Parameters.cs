using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class Parameters : IDictionary<string, object>
    {
        #region Fields

        private Dictionary<string, object> values = new();

        public object this[string key] { get => values[key]; set => values[key] = value; }

        public ICollection<string> Keys => values.Keys;

        public ICollection<object> Values => values.Values;

        public int Count => values.Count;

        public bool IsReadOnly => false;

        #endregion

        #region Constructors

        #endregion

        #region Methods

        public void Add(string key, object value)
        {
            values.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            values.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            values.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return values.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return values.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return values.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return values.Remove(item.Key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            return values.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        #endregion
    }
}
