using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EnableIfAttribute : Attribute
    {
        #region Fields

        public string Key { get; }

        public object Value { get; }

        #endregion

        #region Constructors

        public EnableIfAttribute(string key, object value)
        {
            Key = key;
            Value = value;
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
