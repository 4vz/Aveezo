using System;
using System.Collections.Generic;
using System.IO;

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Aveezo
{
    public class FormInputFormatter : TextInputFormatter
    {
        public FormInputFormatter()
        {
            SupportedMediaTypes.Add("application/x-www-form-urlencoded");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (context.HttpContext.Request.Form != null)
            {
                var type = context.ModelType;
                var properties = type.GetProperties();
                object instance = null;


                foreach (var property in properties)
                {
                    var propertyName = property.Name;
                    var snakeCase = propertyName.ToSnakeCase();

                    if (context.HttpContext.Request.Form.ContainsKey(snakeCase))
                    {
                        var val = context.HttpContext.Request.Form[snakeCase];

                        if (val.Count > 0)
                        {
                            var da = val[0];

                            if (instance == null)
                                instance = Activator.CreateInstance(type);

                            property.SetValue(instance, da);
                        }
                    }
                }

                if (instance != null)
                    return await InputFormatterResult.SuccessAsync(instance);
                else
                    return await InputFormatterResult.NoValueAsync();
            }
            else
            {
                return await InputFormatterResult.FailureAsync();
            }

        }
    }
}
