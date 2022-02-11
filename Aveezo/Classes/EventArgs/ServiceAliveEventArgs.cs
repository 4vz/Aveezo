using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class ServiceAliveEventArgs : EventArgs
    {
        #region Fields

        public bool Alive { get; set; } = false;

        #endregion

        #region Constructors

        public ServiceAliveEventArgs()
        {

        }

        #endregion

        #region Methods

        #endregion
    }
}
