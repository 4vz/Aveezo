using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public sealed class SqlDeleteTable : SqlManipulationBase
    {
        #region Fields

        public string WhereColumn { get; }

        private List<object> entries;

        #endregion

        #region Constructors

        internal SqlDeleteTable(string table, Sql database, string whereColumn) : base(table, database, SqlQueryType.Execute)
        {
            WhereColumn = whereColumn;

            entries = new List<object>();
        }

        #endregion

        #region Methods

        public void Where(object equalTo)
        {
            if (!entries.Contains(equalTo))
            {
                entries.Add(equalTo);
            }
        }

        public SqlDeleteTable Where(params object[] equalTo)
        {
            foreach (var e in equalTo)
                Where(e);
            return this;
        }

        protected override string[] GetStatements() => Database.Connection.DeleteTable(Table, WhereColumn, entries.ToArray(), OutputResult);

        #endregion
    }
}
