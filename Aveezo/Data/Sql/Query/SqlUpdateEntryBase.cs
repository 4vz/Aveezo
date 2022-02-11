using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Aveezo
{

    public abstract class SqlUpdateEntryBase : SqlExecuteBase
    {
        #region Fields

        public SqlUpdateSets Update { get; }

        #endregion

        #region Constructors

        internal SqlUpdateEntryBase(Sql database, SqlTable table) : base(database, table, SqlQueryType.Execute)
        {
            Update = new SqlUpdateSets();
        }

        #endregion

        #region Methods

        #endregion

    }

}
