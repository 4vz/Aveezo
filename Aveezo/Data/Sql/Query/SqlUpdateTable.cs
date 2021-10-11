using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public sealed class SqlUpdateTable : SqlManipulationBase
    {
        #region Fields

        public string WhereColumn { get; }

        private readonly Dictionary<object, SqlUpdateTableEntry> entries = new Dictionary<object, SqlUpdateTableEntry>();

        #endregion

        #region Constructors

        internal SqlUpdateTable(string table, Sql database, string whereColumn) : base(table, database, SqlQueryType.Execute)
        {
            WhereColumn = whereColumn;
        }

        #endregion

        #region Methods

        public SqlUpdateTableEntry Where(object whereValue)
        {
            if (!entries.ContainsKey(whereValue))
            {
                var entry = new SqlUpdateTableEntry(Database, Table, whereValue, this);
                entries.Add(whereValue, entry);
                return entry;
            }
            else
            {
                return entries[whereValue];
            }
        }

        protected override string[] GetStatements()
        {
            if (entries.Count > 0)
            {
                if (entries.Count == 1)
                {
                    var ek = entries.Keys.ToArray();
                    var ev = entries.Values.ToArray();

                    var entry = new SqlUpdateEntry(Database, Table);

                    entry.WhereCondition = (SqlColumn)WhereColumn == ek[0];

                    foreach ((string evk, object evo) in ev[0].Update.Sets)
                    {
                        entry.Set(evk, evo);
                    }

                    return Database.Connection.Update(entry.Array(), OutputResult);

                }
                else
                {
                    return Database.Connection.UpdateTable(Table, WhereColumn, entries.Keys.ToArray(), entries.Values.ToArray(), OutputResult);
                }
            }
            else
            {
                return null;
            }
        }

        #endregion
    }

    public sealed class SqlUpdateTableEntry : SqlUpdateEntryBase
    {
        #region Fields

        private SqlManipulationBase updateTable;

        public object Where { get; }

        #endregion

        #region Constructors

        internal SqlUpdateTableEntry(Sql database, string table, object where, SqlManipulationBase updateTable) : base(database, table)
        {
            Where = where;
            this.updateTable = updateTable;
        }

        #endregion

        #region Methods

        public SqlManipulationBase Set(string column, object value)
        {
            if (!Update.Sets.ContainsKey(column))
                Update.Sets.Add(column, value);
            else
                Update[column] = value;

            return updateTable;
        }

        #endregion
    }
}
