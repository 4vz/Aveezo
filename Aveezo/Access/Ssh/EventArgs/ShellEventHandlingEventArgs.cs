using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ShellEventHandlingEventArgs : EventArgs
    {
        #region Fields

        public string[] Messages { get; }

        public string Context { get; }

        public bool Error { get; }

        public Exception Exception { get; }

        #endregion

        #region Constructors

        public ShellEventHandlingEventArgs(string[] messages, string context, bool error, Exception exception)
        {
            Messages = messages;
            Context = context;
            Error = error;
            Exception = exception;
        }

        #endregion

        #region Methods

        #endregion
    }
}
