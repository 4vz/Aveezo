using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public abstract class SqlManipulationBase : SqlQueryBase
    {
        #region Fieldsx

        internal bool OutputResult { get; set; } = false;

        #endregion

        #region Constructors

        internal SqlManipulationBase(string table, Sql database, SqlQueryType type) : base(table, database, type)
        {
        }

        #endregion

        #region Methods

        public SqlManipulationBase Output()
        {
            OutputResult = true;
            return this;
        }

        #endregion
    }
}
