using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public enum SqlJoinType
    {
        Inner,
        Left,
        Right,
        Full
    }

    public sealed class SqlJoin
    {
        #region Fields

        public SqlJoinType Type { get; }

        public SqlTable Table { get; }

        public SqlCondition WhereCondition { get; }

        #endregion

        #region Constructors

        public SqlJoin(SqlJoinType type, SqlTable table, SqlCondition where)
        {
            Type = type;
            Table = table;
            WhereCondition = where;
        }

        #endregion
    }
}
