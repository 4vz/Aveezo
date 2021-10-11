using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class UnixSshPromptChangedEventArgs : EventArgs
    {
        #region Fields

        public string Prompt { get; }

        #endregion

        #region Constructors

        public UnixSshPromptChangedEventArgs(string prompt)
        {
            Prompt = prompt;
        }

        #endregion

        #region Methods

        #endregion
    }
}
