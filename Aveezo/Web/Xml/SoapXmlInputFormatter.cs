using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.IO;

using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Aveezo
{
    public class SoapXmlInputFormatter : XmlInputFormatter
    {
        #region Constructors

        public SoapXmlInputFormatter(XmlPrefix xmlPrefix, TypeRepository typeCollections) : base(xmlPrefix, typeCollections)
        {
            SupportedMediaTypes.Add("application/soap+xml");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        #endregion

        #region Methods

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            using (var reader = new StreamReader(context.HttpContext.Request.Body, encoding))
            {
                try
                {
                    var input = await reader.ReadToEndAsync();

                    var obj = Xml.Deserialize<SoapXmlEnvelope>(input, typeCollections);

                    if (obj != null)
                        return await InputFormatterResult.SuccessAsync(obj.Body.Data);
                    else
                        return await InputFormatterResult.NoValueAsync();
                }
                catch
                {
                    return await InputFormatterResult.FailureAsync();
                }
            }
        }

        #endregion
    }

}