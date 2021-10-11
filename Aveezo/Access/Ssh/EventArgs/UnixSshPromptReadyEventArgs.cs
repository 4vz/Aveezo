using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class UnixSshPromptReadyEventArgs : EventArgs
    {
        #region Fields

        public bool First { get; }

        #endregion

        #region Constructors

        public UnixSshPromptReadyEventArgs(bool first)
        {
            First = first;
        }

        #endregion

        #region Methods

        #endregion
    }
}
