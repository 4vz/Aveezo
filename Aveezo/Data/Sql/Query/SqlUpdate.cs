using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Aveezo
{
    public sealed class SqlUpdate : SqlExecuteBase
    {
        #region Fields

        private List<SqlUpdateEntry> entries;

        #endregion

        #region Constructors

        internal SqlUpdate(Sql database) : base(database, null, SqlQueryType.Execute)
        {
            entries = new List<SqlUpdateEntry>();
        }

        #endregion

        #region Methods

        public SqlUpdateEntry Update(string table)
        {
            if (table != null)
            {
                var entry = new SqlUpdateEntry(Database, table);
                entries.Add(entry);
                return entry;
            }
            else
                throw new ArgumentNullException();
        }

        protected override string[] GetStatements(Values<string> _) => Database.Connection.Update(entries.Keep(delegate (SqlUpdateEntry entry) { return entry.Update.Sets.Count != 0; }).ToArray(), OutputResult);

        #endregion
    }

    public sealed class SqlUpdateEntry : SqlUpdateEntryBase
    {
        #region Fields

        public SqlCondition WhereCondition { get; set; } = null;


        #endregion

        #region Constructors

        internal SqlUpdateEntry(Sql database, SqlTable table) : base(database, table)
        {
        }

        #endregion

        #region Methods

        public SqlUpdateEntry Set(string column, object value)
        {
            if (!Update.Sets.ContainsKey(column))
                Update.Sets.Add(column, value);
            else
                Update[column] = value;

            return this;
        }

        public SqlUpdateEntry Set(Action<SqlUpdateSets> set)
        {
            set(Update);
            return this;
        }

        public SqlUpdateEntry Where(SqlCondition condition)
        {
            WhereCondition = condition;
            return this;
        }

        public SqlUpdateEntry And(SqlCondition condition)
        {
            if (WhereCondition is not null)
            {
                WhereCondition = WhereCondition && condition;
            }
            else throw new InvalidOperationException();
            return this;
        }

        public SqlUpdateEntry Or(SqlCondition condition)
        {
            if (WhereCondition is not null)
            {
                WhereCondition = WhereCondition || condition;
            }
            else throw new InvalidOperationException();
            return this;
        }

        public SqlUpdateEntry Where(SqlColumn whereColumn, object whereValue) => Where(whereColumn == whereValue);

        public SqlUpdateEntry Where(SqlColumn leftColumn, SqlColumn rightColumn) => Where(leftColumn == rightColumn);

        protected override string[] GetStatements(Values<string> _) => Database.Connection.Update(this.Array(), OutputResult);

        #endregion
    }

    public sealed class SqlUpdateSets
    {
        #region Fields

        public Dictionary<string, object> Sets { get; }

        public object this[string key]
        {
            get => Sets.ContainsKey(key) ? Sets[key] : null;
            set 
            {
                if (!Sets.ContainsKey(key))
                    Sets.Add(key, value);
                else
                    Sets[key] = value;
            }
        }

        #endregion

        #region Constructors

        public SqlUpdateSets()
        {
            Sets = new Dictionary<string, object>();
        }

        #endregion

        #region Operators


        #endregion

        #region Methods

        #endregion

        #region Statics

        #endregion
    }
}
