using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public abstract class SqlQueryBase : SqlBase
    {
        #region Fields

        internal SqlQueryType Type { get; set; }

        public string Table { get; internal set; } = null;

        public string[] Statements => GetStatements();

        #endregion

        #region Constructors

        internal SqlQueryBase(string table, Sql database, SqlQueryType type) : base(database)
        {
            Table = table;
            Type = type;
        }

        internal SqlQueryBase(Sql database, SqlQueryType type) : this(null, database, type) { }

        #endregion

        #region Operators

        #endregion

        #region Methods

        protected virtual string[] GetStatements() => throw new NotImplementedException();

        public SqlResultCollection Execute() => Execute(null);

        protected SqlResultCollection Execute(string statement)
        {
            var result = new SqlResultCollection();

            string[] sts = null;

            if (statement != null)
                sts = statement.Array();
            else if (Statements != null)
                sts = Statements;
            else
                throw new InvalidOperationException();

            foreach (string sql in sts)
            {
                if (sql == null || sql.Trim() == "") continue;

                SqlResultCollection currentResult = null;

                if (Type == SqlQueryType.Reader || (this is SqlManipulationBase smb && (_ = smb.OutputResult)))
                {
                    currentResult = Database.FormatedQuery(sql, 0, 0, null);

                    if (Type != SqlQueryType.Reader)
                    {
                        foreach (var res in currentResult)
                        {
                            res.ExecuteOutput = true;
                        }
                    }
                }
                else if (Type == SqlQueryType.Execute)
                {
                    currentResult = Database.FormatedExecute(sql);
                }

                if (currentResult != null)
                {
                    // combine item
                    foreach (var item in currentResult)
                    {
                        result.Add(item);
                    }

                    // combine exception
                    if (result.Exception == null)
                        result.Exception = currentResult.Exception;

                    // combine execution time
                    result.ExecutionTime += currentResult.ExecutionTime;
                }
            }

            return result;
        }

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
