using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class ValidationContextExtensions
    {
        public static bool IsRequired(this ValidationContext context)
        {
            bool required = false;

            if (context.ObjectInstance != null)
            {
                foreach (var property in context.ObjectInstance.GetType().GetProperties())
                {
                    if (property.Name == context.MemberName)
                    {
                        if (property.Has<RequiredAttribute>())
                        {
                            required = true;
                            break;
                        }
                    }
                }
            }

            return required;
        }
    }
}
