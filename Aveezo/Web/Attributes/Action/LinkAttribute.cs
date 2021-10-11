using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LinkAttribute : Attribute
    {
        #region Fields

        public string Self { get; set; }

        #endregion
    }

}
