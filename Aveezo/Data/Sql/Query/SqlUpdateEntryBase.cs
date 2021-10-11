using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Aveezo
{

    public abstract class SqlUpdateEntryBase : SqlManipulationBase
    {
        #region Fields

        public SqlUpdateSets Update { get; }

        #endregion

        #region Constructors

        internal SqlUpdateEntryBase(Sql database, string table) : base(table, database, SqlQueryType.Execute)
        {
            Update = new SqlUpdateSets();
        }

        #endregion

        #region Methods

        #endregion

    }

}
