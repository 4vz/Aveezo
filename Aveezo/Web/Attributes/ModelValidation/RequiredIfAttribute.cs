using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public class RequiredIfAttribute : ValidationAttribute
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
                //var httpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));
                //var httpContext = httpContextAccessor.HttpContext;

                if (value == null)
                    return new ValidationResult($"The {context.MemberName} field is required.");
            }

            return ValidationResult.Success;
        }

        #endregion
    }
}
