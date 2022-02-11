using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ServiceEventEventArgs : EventArgs
    {
        #region Fields

        public string Message { get; set; }

        #endregion

        #region Constructors

        public ServiceEventEventArgs(string message)
        {
            Message = message;
        }

        #endregion

        #region Operators


        #endregion

        #region Methods

        #endregion

        #region Statics

        #endregion
    }
}
