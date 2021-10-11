using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public sealed class SqlInsertTable : SqlManipulationBase
    {
        #region Fields

        private readonly List<SqlInsertTableEntry> entries = new List<SqlInsertTableEntry>();

        private string[] columns;

        #endregion

        #region Constructors

        internal SqlInsertTable(string table, Sql database, string[] columns) : base(table, database, SqlQueryType.Execute)
        {
            this.columns = columns;
        }

        #endregion

        #region Methods

        public SqlInsertTable Values(params object[] values)
        {
            if (columns.Length == 0 || columns.Length == values.Length)
                entries.Add(new SqlInsertTableEntry(values));
            else
                throw new ArgumentException("Values parameters length should be at the same length of specified columns names", nameof(values));

            return this;
        }

        public SqlInsertTable Values(out Guid primaryKey, params object[] values)
        {
            primaryKey = Guid.NewGuid();

            if (columns.Length == 0 || columns.Length == values.Length + 1)
            {
                List<object> v = new List<object>();
                v.Add(primaryKey);
                v.AddRange(values);

                entries.Add(new SqlInsertTableEntry(v.ToArray()));
            }
            else
                throw new ArgumentException("Values parameters length plus one (the primary key), should be at the same length of specified columns names", nameof(values));

            return this;
        }

        protected override string[] GetStatements() => Database.Connection.Insert(Table, columns, entries.ToArray(), OutputResult);

        #endregion
    }

    internal sealed class SqlInsertTableEntry
    {
        #region Fields

        public object[] Values { get; }

        #endregion

        #region Constructors

        public SqlInsertTableEntry(object[] values)
        {
            Values = values;
        }

        #endregion

        #region Methods

        #endregion
    }


}
