using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public class RequiredIfAttribute : RequiredAttribute
    {
        #region Fields

        public override bool RequiresValidationContext => true;

        private readonly string name = null;

        private readonly object value = null;

        #endregion

        #region Constructors

        public RequiredIfAttribute(string name, object value)
        {
            this.name = name;
            this.value = value;
        }

        #endregion

        #region Methods

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            bool required = false;

            if (context.ObjectInstance != null)
            {
                foreach (var property in context.ObjectInstance.GetType().GetProperties())
                {
                    if (property.Name == name)
                    {
                        var propertyValue = property.GetValue(context.ObjectInstance);

                        if (Equals(propertyValue, this.value))
                        {
                            required = true;
                        }
                    }
                }
            }


            if (required)
            {
                if (value == null)
                    return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }

        #endregion
    }
}
