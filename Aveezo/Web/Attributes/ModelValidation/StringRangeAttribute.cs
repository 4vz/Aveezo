using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public class StringRangeAttribute : ValidationAttribute
    {
        #region Fields

        private readonly string[] values;

        #endregion

        #region Constructors

        public StringRangeAttribute(params string[] values)
        {
            this.values = values;
        }

        #endregion

        #region Methods

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            if (value is string str)
            {
                if (values.IndexOf(str) > -1)
                    return ValidationResult.Success;
                else
                    return new ValidationResult($"The {context.MemberName} field is not in StringRange attribute.");
            }
            else
                return new ValidationResult($"The {context.MemberName} field is not a string.");
        }


        #endregion
    }
}
