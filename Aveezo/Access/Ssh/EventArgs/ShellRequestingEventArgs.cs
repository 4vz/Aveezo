using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ShellRequestingEventArgs : EventArgs
    {
        #region Fields

        public string RequestString { get; }

        #endregion

        #region Constructors

        public ShellRequestingEventArgs(string requestString)
        {
            RequestString = requestString;  
        }

        #endregion

        #region Methods

        #endregion
    }
}
