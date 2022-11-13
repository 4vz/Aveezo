using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class GuidAttribute : ValidationAttribute
    {
        #region Methods

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {

            if (value is Guid)
                return ValidationResult.Success;
            else if (value is string str)
            {
                var isBase64Attr = false;
                if (validationContext.ObjectType != null && validationContext.ObjectType.GetMember(validationContext.DisplayName).Has<Base64Attribute>())
                    isBase64Attr = true;

                if (isBase64Attr && str.IsBase64())
                {
                    if (Base64.TryUrlGuidDecode(str, out var _))
                        return ValidationResult.Success;
                }

                if (str.IsGuid())
                    return ValidationResult.Success;
                else
                    return new ValidationResult("Not a valid GUID object");
            }
            else
                return null;
        }

        #endregion
    }
}
