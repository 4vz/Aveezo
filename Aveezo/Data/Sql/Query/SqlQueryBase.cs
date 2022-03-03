using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public abstract class SqlQueryBase : SqlBase
    {
        #region Fields

        internal SqlExecuteType Type { get; set; }

        public SqlTable Table { get; internal set; } = null;

        public string[] Statements => GetStatements(null);

        #endregion

        #region Constructors

        internal SqlQueryBase(Sql database, SqlTable table, SqlExecuteType type) : base(database)
        {
            Table = table;
            Type = type;
        }

        #endregion

        #region Operators

        #endregion

        #region Methods

        protected virtual string[] GetStatements(Values<string> selectBuilders) => throw new NotImplementedException();

        protected SqlQuery Execute(Values<string> selectBuilders, string statement)
        {
            var query = new SqlQuery();

            if (selectBuilders != null)
            {
                query.select = (SqlSelect)this;
                query.selectBuilders = selectBuilders;
            }

            string[] statements;

            if (statement != null)
                statements = statement.Array();
            else
                statements = GetStatements(selectBuilders);

            if (statements == null || statements.Length == 0)
                throw new InvalidOperationException();

            foreach (string sql in statements)
            {
                if (sql == null || sql.Trim() == "") continue;

                SqlQuery currentResult = null;

                if (Type == SqlExecuteType.Reader || (this is SqlExecuteBase smb && (_ = smb.OutputResult)))
                {
                    currentResult = Database.FormatedQuery(sql, 0, 0, null);

                    if (Type != SqlExecuteType.Reader)
                    {
                        foreach (var res in currentResult)
                        {
                            res.ExecuteOutput = true;
                        }
                    }
                }
                else if (Type == SqlExecuteType.Execute)
                {
                    currentResult = Database.FormatedExecute(sql);
                }

                if (currentResult != null)
                {
                    // combine item
                    foreach (var item in currentResult)
                    {
                        query.Add(item);
                    }

                    // combine exception
                    if (query.Exception == null)
                        query.Exception = currentResult.Exception;

                    // combine execution time
                    query.ExecutionTime += currentResult.ExecutionTime;
                }
            }

            return query;
        }

        public SqlQuery Execute() => Execute(null, null);

        public SqlQuery Execute(Values<string> selectBuilders) => Execute(selectBuilders, null);

        public SqlQuery Execute(string statement) => Execute(null, statement);

        public bool Execute(out SqlResult result)
        {
            var collection = Execute();
            result = collection;
            return collection;
        }

        public bool Execute(out SqlRow row)
        {
            var collection = Execute();
            row = collection;
            return collection;
        }

        public bool Execute(out SqlCell cell)
        {
            var collection = Execute();
            cell = collection;
            return collection;
        }

        #endregion
    }
}
