using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public class SqlDelete : SqlManipulationBase
    {
        #region Fields

        private List<SqlDeleteEntry> entries;

        #endregion

        #region Constructors

        internal SqlDelete(Sql database) : base(null, database, SqlQueryType.Execute)
        {
            entries = new List<SqlDeleteEntry>();
        }

        #endregion

        #region Methods

        public SqlDeleteEntry From(string table)
        {
            if (table != null)
            {
                var entry = new SqlDeleteEntry(Database, table);
                entries.Add(entry);
                return entry;
            }
            else
                throw new ArgumentNullException();
        }

        protected override string[] GetStatements() => Database.Connection.Delete(entries.ToArray(), OutputResult);

        #endregion
    }

    public sealed class SqlDeleteEntry : SqlManipulationBase
    {
        #region Fields

        public SqlCondition WhereCondition { get; set; } = null;

        #endregion

        #region Constructors

        internal SqlDeleteEntry(Sql database, string table) : base(table, database, SqlQueryType.Execute)
        {
        }

        #endregion

        #region Methods

        public SqlDeleteEntry Where(SqlCondition condition)
        {
            WhereCondition = condition;
            return this;
        }

        public SqlDeleteEntry And(SqlCondition condition)
        {
            if (WhereCondition is not null)
            {
                WhereCondition = WhereCondition && condition;
            }
            else throw new InvalidOperationException();
            return this;
        }

        public SqlDeleteEntry Or(SqlCondition condition)
        {
            if (WhereCondition is not null)
            {
                WhereCondition = WhereCondition || condition;
            }
            else throw new InvalidOperationException();
            return this;
        }

        public SqlDeleteEntry Where(SqlColumn whereColumn, object whereValue) => Where(whereColumn == whereValue);

        public SqlDeleteEntry Where(SqlColumn leftColumn, SqlColumn rightColumn) => Where(leftColumn == rightColumn);

        protected override string[] GetStatements() => Database.Connection.Delete(this.Array(), OutputResult);

        #endregion
    }
}
