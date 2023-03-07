using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public class SshSendDataEventArgs : EventArgs
    {
        #region Fields

        public string Data { get; }

        public bool NewLine { get; }

        #endregion

        #region Constructors

        public SshSendDataEventArgs(string data) : this(data, false) { }

        public SshSendDataEventArgs(string data, bool newLine)
        {
            Data = data;
            NewLine = newLine;
        }

        #endregion

        #region Methods

        #endregion
    }
}
