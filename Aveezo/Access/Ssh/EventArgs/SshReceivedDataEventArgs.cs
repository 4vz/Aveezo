using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public class SshReceivedDataEventArgs : EventArgs
    {
        #region Fields

        public string[] Lines { get; }

        public string CurrentLine { get; }

        #endregion

        #region Constructors

        public SshReceivedDataEventArgs(string data, string[] lines, string currentLine)
        {
            Lines = lines;
            CurrentLine = currentLine;
        }

        #endregion

        #region Methods

        #endregion
    }
}
