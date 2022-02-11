using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public abstract class SqlBase
    {
        #region Fields

        public Sql Database { get; internal set; } = null;

        #endregion

        #region Constructors

        internal SqlBase(Sql database)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        #endregion
    }

}
