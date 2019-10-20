using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Aveezo
{
    [Serializable]
    public sealed class Row : IDictionary<string, Column>
    {
        #region Fields

        private Dictionary<string, Column> columns;

        #endregion

        #region Constructor

        public Row()
        {
            columns = new Dictionary<string, Column>();
        }

        #endregion

        #region Methods

        public void Add(string key, Column value)
        {
            columns.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return columns.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return columns.Keys; }
        }

        public bool Remove(string key)
        {
            return columns.Remove(key);
        }

        public bool TryGetValue(string key, out Column value)
        {
            return columns.TryGetValue(key, out value);
        }

        public ICollection<Column> Values
        {
            get { return columns.Values; }
        }

        public Column this[int index]
        {
            get
            {
                if (index >= 0 && index < columns.Count)
                {
                    int i = 0;
                    foreach (KeyValuePair<string, Column> kvpc in columns)
                    {
                        if (i == index) return kvpc.Value;
                        i++;
                    }
                    return null;
                }
                else return null;
            }
            set { }
        }

        public Column this[string key]
        {
            get
            {
                return columns[key];
            }
            set { }
        }

        public void Add(KeyValuePair<string, Column> item)
        {
            columns.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            columns.Clear();
        }

        public bool Contains(KeyValuePair<string, Column> item)
        {
            return columns.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<string, Column>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return columns.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, Column> item)
        {
            return columns.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, Column>> GetEnumerator()
        {
            return columns.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)columns.GetEnumerator();
        }

        #endregion
    }
}
