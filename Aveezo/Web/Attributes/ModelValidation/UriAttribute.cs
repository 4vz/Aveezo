using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{

    [Flags]
    public enum UriProperties
    {
        None = 0,
        IsAbsolute = 1
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class UriAttribute : ValidationAttribute
    {
        #region Fields

        public UriProperties Properties { get; }

        #endregion

        #region Constructors

        public UriAttribute() : this(UriProperties.None) { }

        public UriAttribute(UriProperties properties)
        {
            Properties = properties;
        }

        #endregion

        #region Methods

        public override bool IsValid(object value)
        {
            if (value is Uri uri)
            {
                if (Properties.HasFlag(UriProperties.IsAbsolute) && !uri.IsAbsoluteUri)
                    return false;
                else
                    return true;
            }
            else
                return true;
        }

        #endregion
    }
}
