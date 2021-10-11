using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public class SshConnectionFailEventArgs : EventArgs
    {
        #region Fields

        public SshConnectionFailReason Reason { get; }
        public string Message { get; }

        #endregion

        #region Constructors

        public SshConnectionFailEventArgs(SshConnectionFailReason reason, string message)
        {
            Reason = reason;
            Message = message;
        }

        #endregion
    }
}
