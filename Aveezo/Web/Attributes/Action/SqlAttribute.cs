using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SqlAttribute : Attribute
    {
        #region Fields

        public string Name { get; set; }

        #endregion

        #region Constructors

        public SqlAttribute(string name)
        {
            Name = name;
        }

        #endregion
    }
}
