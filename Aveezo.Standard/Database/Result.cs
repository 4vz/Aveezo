using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [Serializable]
    public sealed class Result : IList<Row>
    {
        #region Fields

        public bool IsExceptionThrown { get; set; } = false;

        private List<Row> rows;

        public TimeSpan ExecutionTime { get; set; }

        public int AffectedRows { get; set; } = 0;

        public Int64 Identity { get; set; }

        public string[] ColumnNames { get; set; }

        public string Sql { get; set; }

        public static Result Null
        {
            get { return new Result(""); }
        }

        #endregion

        #region Constructor

        public Result(string sql)
        {
            rows = new List<Row>();
            this.Sql = sql;
        }

        #endregion

        #region Methods

        public int IndexOf(Row item)
        {
            return rows.IndexOf(item);
        }

        public void Insert(int index, Row item)
        {
            rows.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            rows.RemoveAt(index);
        }

        public Row this[int index]
        {
            get
            {
                if (IsExceptionThrown)
                {
                    throw new Exception("Warning: This result contain exceptions");
                }
                return rows[index];
            }
            set { }
        }

        public Row Last()
        {
            return rows == null ? null : rows.Count == 0 ? null : rows[rows.Count - 1];
        }

        public void Add(Row item)
        {
            rows.Add(item);
        }

        public void Clear()
        {
            rows.Clear();
        }

        public bool Contains(Row item)
        {
            return rows.Contains(item);
        }

        public void CopyTo(Row[] array, int arrayIndex)
        {
            rows.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return rows.Count; }
        }

        public static implicit operator bool(Result d)
        {
            return !d.IsExceptionThrown;
        }

        public static implicit operator int(Result d)
        {
            return d.Count;
        }

        public override string ToString()
        {
            return Count.ToString();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Row item)
        {
            return rows.Remove(item);
        }

        public IEnumerator<Row> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)rows.GetEnumerator();
        }

        #endregion
    }
}
