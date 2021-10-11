using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public sealed class SqlResultCollection : IPrintable, IEnumerable<SqlResult>
    {
        #region Fields

        private readonly List<SqlResult> results = new List<SqlResult>();

        public int Count => results.Count;

        public SqlResult First => Count > 0 ? this[0] : null;

        public SqlResult this[int index] => index >= 0 && index < Count ? results[index] : null;       

        public TimeSpan ExecutionTime { get; internal set; }
        
        public SqlException Exception { get; internal set; } = null;

        #endregion

        #region Constructors

        public SqlResultCollection()
        {
        }

        #endregion

        #region Operators

        public static implicit operator bool(SqlResultCollection collection) => collection.Exception == null && collection.Count > 0 && collection.First.Count > 0;

        public static implicit operator SqlResult(SqlResultCollection collection) => collection.First;

        public static implicit operator SqlRow(SqlResultCollection collection) => (SqlResult)collection;

        public static implicit operator SqlCell(SqlResultCollection collection) => (SqlRow)collection;

        public static SqlResultCollection operator +(SqlResultCollection collection1, SqlResultCollection collection2)
        {
            if (collection1 == null || collection2 == null)
                throw new ArgumentNullException();

            var result = new SqlResultCollection();

            // combine resultitem
            foreach (var item in collection1)
                result.Add(item);

            foreach (var item in collection2)
                result.Add(item);

            // combine exception
            if (collection1.Exception != null)
                result.Exception = collection1.Exception;
            else if (collection2.Exception != null)
                result.Exception = collection2.Exception;

            // combine execution time
            result.ExecutionTime = collection1.ExecutionTime + collection2.ExecutionTime;

            return result;
        }

        public static SqlResultCollection operator +(SqlResultCollection collection, SqlResult result)
        {
            if (collection == null || result == null)
                throw new ArgumentNullException();

            var newcollection = new SqlResultCollection();

            // combine resultitem
            foreach (var item in collection)
                newcollection.Add(item);

            newcollection.Add(result);

            // combine execution time
            newcollection.ExecutionTime = collection.ExecutionTime + result.ExecutionTime;

            return newcollection;
        }

        public static SqlResultCollection operator +(SqlResult result, SqlResultCollection collection) => collection + result;

        #endregion

        #region Methods

        public IEnumerator<SqlResult> GetEnumerator() => results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => results.GetEnumerator();

        internal void Add(SqlResult item)
        {
            results.Add(item);
        }

        public string[] Print() => Print(100);

        public string[] Print(int limit)
        {
            if (Exception != null)
            {
                return new string[]{ $"Exception: {Exception.Type}: {Exception.Exception.Message}", $"SQL: {Exception.Sql}" };
            }
            else if (limit > Count && Count > 0)
            {
                var lines = new List<string>();

                int[] splits = (limit - Count).Split(Count);

                var i = 0;
                foreach (var item in results)
                {
                    lines.Add($"Result #{(i + 1)}");

                    var itemLines = item.Print(splits[i]);

                    if (itemLines != null) lines.AddRange(itemLines);

                    i++;
                }

                return lines.ToArray();
            }
            else
                return new[] { "No Result" };
        }

        #endregion
    }
}
