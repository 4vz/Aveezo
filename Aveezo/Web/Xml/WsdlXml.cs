using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class WsdlXmlDescription : IXmlObject
    {
        #region Fields

        public XmlObject Object { get; } = new XmlObject() { Name = "description", Namespace = "http://www.w3.org/ns/wsdl" };

        [XmlObject(XmlObjectAttributeOptions.HideTypeDeclaration)]
        public string Documentation { get; set; }

        public WsdlXmlTypes Types { get; init; }

        public WsdlXmlInterface Interface { get; init; }

        public WsdlXmlBinding Binding { get; init; }

        public WsdlXmlService Service { get; init; }

        #endregion

        #region Constructors

        public WsdlXmlDescription(string schemaNamespace, string operationNamespace)
        {
            Object.Attributes.Add(XmlObjectElementAttribute.XmlnsPrefix("wsoap", "http://www.w3.org/ns/wsdl/soap"));
            Object.Attributes.Add(XmlObjectElementAttribute.XmlnsPrefix("soap", "http://www.w3.org/2003/05/soap-envelope"));
            Object.Attributes.Add(XmlObjectElementAttribute.XmlnsPrefix("wsdlx", "http://www.w3.org/ns/wsdl-extensions"));

            Xml.AddSchemaNamespace(Object, schemaNamespace, operationNamespace);
            Xml.AddTargetNamespace(Object, operationNamespace);
        }

        #endregion
    }

    public class WsdlXmlTypes : IXmlObject
    {
        public XmlObject Object { get; } = new XmlObject() { Name = "types" };

        public object Schema { get; init; }
    }

    public class WsdlXmlInterface : IXmlObject
    {
        #region Fields

        public XmlObject Object { get; } = new XmlObject() { Name = "interface" };

        #endregion

        #region Constructors

        public WsdlXmlInterface(string interfaceName)
        {
            Object.Attributes.Add(XmlObjectElementAttribute.NameAttribute(interfaceName));
        }

        #endregion
    }

    public class WsdlXmlBinding : IXmlObject
    {
        #region Fields

        public XmlObject Object { get; } = new XmlObject() { Name = "binding" };

        #endregion

        #region Constructors

        public WsdlXmlBinding(string interfaceName, string bindingName)
        {
            Object.Attributes.Add(XmlObjectElementAttribute.NameAttribute(bindingName));
            Object.Attributes.Add(new XmlObjectElementAttribute("interface", interfaceName));
            Object.Attributes.Add(XmlObjectElementAttribute.TypeAttribute("http://www.w3.org/ns/wsdl/soap"));
            Object.Attributes.Add(new XmlObjectElementAttribute("wsoap", "protocol", null, "http://www.w3.org/2003/05/soap/bindings/HTTP/"));
        }

        #endregion
    }

    public class WsdlXmlService : IXmlObject
    {
        #region Fields

        public XmlObject Object { get; } = new XmlObject() { Name = "service" };

        public WsdlXmlEndpoint Endpoint { get; init; }

        #endregion

        #region Constructors

        public WsdlXmlService(string interfaceName, string serviceName)
        {
            Object.Attributes.Add(XmlObjectElementAttribute.NameAttribute(serviceName));
            Object.Attributes.Add(new XmlObjectElementAttribute("interface", interfaceName));
        }

        #endregion
    }

    public class WsdlXmlEndpoint : IXmlObject
    {
        #region Fields

        public XmlObject Object { get; } = new XmlObject() { Name = "endpoint" };

        #endregion

        #region Constructors

        public WsdlXmlEndpoint(string endpointName, string bindingName, string address)
        {
            Object.Attributes.Add(XmlObjectElementAttribute.NameAttribute(endpointName));
            Object.Attributes.Add(new XmlObjectElementAttribute("binding", bindingName));
            Object.Attributes.Add(new XmlObjectElementAttribute("address", address));
        }

        #endregion
    }

    [PathNeutral("/wsdl/v{version:apiVersion}")]
    public class WsdlXml : Api
    {
        public WsdlXml(IServiceProvider i) : base(i) { }

        [Get]
        public string Get()
        {
            return null;
        }
    }
}
