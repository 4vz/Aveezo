using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo.Test
{
    public class TestApp : App
    {
        #region Fields

        #endregion

        #region Constructors

        public TestApp()
        {
        }

        #endregion

        #region Methods

        protected override async Task<bool> OnStart()
        {
            Event("Starttt");

            return false;
        }

        #endregion

        #region Statics

        #endregion
    }
}
