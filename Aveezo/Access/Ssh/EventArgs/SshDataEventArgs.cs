using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public class SshDataEventArgs : EventArgs
    {
        #region Fields

        public string Data { get; }

        public string[] Lines { get; }

        #endregion

        #region Constructors

        public SshDataEventArgs(string data, string[] lines)
        {
            Data = data;
            Lines = lines;
        }

        #endregion

        #region Methods

        #endregion
    }
}
