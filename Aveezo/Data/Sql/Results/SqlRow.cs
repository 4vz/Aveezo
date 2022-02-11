using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

using System.Text;

namespace Aveezo
{
    public sealed class SqlRow : IPrintable, IEnumerable<SqlCell>
    {
        #region Fields

        private readonly SqlResult result;

        private readonly SqlCell[] cells;

        public SqlCell First => cells.Length > 0 ? cells[0] : null;

        public SqlCell this[int index] => index >= 0 && index < cells.Length ? cells[index] : throw new IndexOutOfRangeException(nameof(index));

        public SqlCell this[string key] => result.ColumnIndex.ContainsKey(key) ? cells[result.ColumnIndex[key]] : throw new KeyNotFoundException(nameof(key));

        public (SqlCell, SqlCell) this[string key1, string key2]
            => (this[key1], this[key2]);

        public (SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3]
            => (this[key1], this[key2], this[key3]);

        public (SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4]
            => (this[key1], this[key2], this[key3], this[key4]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5]
            => (this[key1], this[key2], this[key3], this[key4], this[key5]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9, string key10]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9], this[key10]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9, string key10, string key11]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9], this[key10], this[key11]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9, string key10, string key11, string key12]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9], this[key10], this[key11], this[key12]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9, string key10, string key11, string key12, string key13]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9], this[key10], this[key11], this[key12], this[key13]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9, string key10, string key11, string key12, string key13, string key14]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9], this[key10], this[key11], this[key12], this[key13], this[key14]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9, string key10, string key11, string key12, string key13, string key14, string key15]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9], this[key10], this[key11], this[key12], this[key13], this[key14], this[key15]);

        public (SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell, SqlCell) this[string key1, string key2, string key3, string key4, string key5, string key6, string key7, string key8, string key9, string key10, string key11, string key12, string key13, string key14, string key15, string key16]
            => (this[key1], this[key2], this[key3], this[key4], this[key5], this[key6], this[key7], this[key8], this[key9], this[key10], this[key11], this[key12], this[key13], this[key14], this[key15], this[key16]);

        #endregion

        #region Constructor

        internal SqlRow(SqlResult result, SqlCell[] cells)
        {
            this.result = result;
            this.cells = cells;
        }

        #endregion

        #region Operators

        public static implicit operator SqlCell(SqlRow row) => row.First;

        #endregion

        #region Methods

        public IEnumerator<SqlCell> GetEnumerator() => ((IEnumerable<SqlCell>)cells).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => cells.GetEnumerator();

        public bool ContainsKey(string key) => result.ColumnIndex.ContainsKey(key);

        public string[] Print()
        {
            var builder = new StringBuilder();

            var index = 0;
            foreach (var cell in this)
            {
                if (builder.Length > 0) builder.Append(", ");
                builder.Append($"{result.ColumnNames[index]}:{cell}");
                index++;
            }

            return new[] { builder.ToString() };
        }

        public override string ToString() => Print()[0];

        #endregion
    }
}
