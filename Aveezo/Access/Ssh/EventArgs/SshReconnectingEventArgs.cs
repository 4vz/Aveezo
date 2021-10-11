using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public class SshReconnectingEventArgs : EventArgs
    {
        #region Fields

        public bool Reconnect { get; set; }

        #endregion
    }
}
