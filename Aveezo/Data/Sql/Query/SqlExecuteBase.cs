using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public abstract class SqlExecuteBase : SqlQueryBase
    {
        #region Fieldsx

        internal bool OutputResult { get; set; } = false;

        #endregion

        #region Constructors

        internal SqlExecuteBase(Sql database, SqlTable table, SqlQueryType type) : base(database, table, type)
        {
        }

        #endregion

        #region Methods

        public SqlExecuteBase Output()
        {
            OutputResult = true;
            return this;
        }

        #endregion
    }
}
