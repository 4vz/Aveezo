using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;

using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Aveezo
{
    public class SoapXmlOutputFormatter : XmlOutputFormatter
    {
        #region Constructors

        public SoapXmlOutputFormatter(XmlPrefix xmlPrefix, TypeRepository typeCollections) : base(xmlPrefix, typeCollections)
        {
            SupportedMediaTypes.Add("application/soap+xml");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        #endregion

        #region Methods

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            XmlObjectElement mainElement = null;

            var envelope = new SoapXmlEnvelope() { Body = new SoapXmlBody() { Data = context.Object } };
            await context.HttpContext.Response.WriteAsync(await Xml.Serialize(envelope, xmlPrefix, typeCollections, (obj, args) =>
            {
                var element = args.Element;

                if (obj == context.Object)
                {
                    element.Prefix = xmlPrefix.Local;
                    element.Name = obj.GetType().Name;
                    mainElement = element;
                }
                else if (mainElement != null && element.IsDescendantOf(mainElement))
                {
                    element.Prefix = xmlPrefix.Local;
                }
            }));
        }

        #endregion
    }

}
