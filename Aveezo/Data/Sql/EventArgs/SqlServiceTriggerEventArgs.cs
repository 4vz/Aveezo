using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class SqlServiceTriggerEventArgs : EventArgs
    {
        #region Fields

        public string Tag { get; }

        #endregion

        #region Constructors

        public SqlServiceTriggerEventArgs(string tag)
        {
            Tag = tag;
        }

        #endregion

        #region Methods

        #endregion
    }
}
