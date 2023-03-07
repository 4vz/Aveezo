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
    public class WsdlXmlOutputFormatter : XmlOutputFormatter
    {
        #region Constructors

        public WsdlXmlOutputFormatter(XmlPrefix xmlPrefix, TypeRepository typeCollections) : base(xmlPrefix, typeCollections)
        {
            SupportedMediaTypes.Add("application/wsdl+xml");
            SupportedEncodings.Add(Encoding.UTF8);
        }

        #endregion

        #region Methods

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var httpContext = context.HttpContext;
            var response = httpContext.Response;
            var request = httpContext.Request;

            var soapxml = httpContext.Items["soapxml"] as string;

            var rest = context.HttpContext.Request.Path.ToString().ToLower();
            var service = "";// $"http://{context.HttpContext.Shell.Host}{Rest.SoapXmlPrefix}{rest}";
            var serviceWsdl = ""; // $"http://{context.HttpContext.Shell.Host}{Rest.SoapXmlPrefix}{Rest.WsdlXmlPrefix}{rest}";
            var data = context.Object;

            string interfaceName = "interface";
            string bindingName = "binding";
            string serviceName = "service";
            string endpointName = "endpoint";

            var wsdl = new WsdlXmlDescription(serviceWsdl, service)
            {
                Documentation = "Afis Herman Reza Devara",
                Types = new WsdlXmlTypes { Schema = data },
                Interface = new WsdlXmlInterface(interfaceName),
                Binding = new WsdlXmlBinding(interfaceName, bindingName),
                Service = new WsdlXmlService(interfaceName, serviceName)
                {
                    Endpoint = new WsdlXmlEndpoint(endpointName, bindingName, service)
                }
            };

            XmlObjectElement dataElement = null;

            var responseBody = await Xml.Serialize(wsdl, xmlPrefix, typeCollections, (obj, args) =>
            {
                var element = args.Element;

                if (element.Property != null && element.Property.Name == "Documentation")
                {
                    element.Name = "documentation";
                }
                else if (obj == data)
                {
                    dataElement = element;

                    element.Name = "schema";
                    element.Options |= XmlObjectAttributeOptions.XmlSchema;

                    Xml.AddBuiltInSchemaElements(element);
                    Xml.AddTargetNamespace(element, service);
                }
                else if (dataElement != null && element.IsDescendantOf(dataElement))
                {
                    element.Options |= XmlObjectAttributeOptions.XmlSchemaElement;

                    if (element.IsSerialized)
                    {
                        element.SchemaName = XmlObjectSchemaName.ComplexType;
                        element.Add(XmlObjectSchemaElement.Sequence);
                        element.SchemaGroup = XmlObjectSchemaElement.Sequence;
                    }
                    else
                    {
                        element.SchemaName = XmlObjectSchemaName.Element;
                        element.SchemaGroup = XmlObjectSchemaElement.Sequence;
                    }
                }
            });

            await response.WriteAsync($"{responseBody}");
        }

        #endregion
    }

}
