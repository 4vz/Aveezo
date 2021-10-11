using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class Base64Attribute : ValidationAttribute
    {
        #region Methods

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var re = value is string str ? str.IsBase64() ? ValidationResult.Success : new ValidationResult("Base64 is not valid") : null;
            return re;
        }

        #endregion
    }
}
