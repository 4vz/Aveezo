using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class SoapXmlEnvelope : IXmlObject
    {
        public XmlObject Object { get; } = new XmlObject() { Prefix = "soap", Name = "Envelope" };

        public SoapXmlBody Body { get; init; }
    }

    public class SoapXmlBody : IXmlObject
    {
        public XmlObject Object { get; } = new XmlObject() { Prefix = "soap", Name = "Body" };

        public object Data { get; init; }
    }

    public static class SoapXml
    {
        public static void AddSoapXml(this MvcOptions options, XmlPrefix xmlPrefix, Assembly[] assemblies)
        {
            var types = new List<Type>();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) types.Add(type);
            foreach (var assembly in assemblies) types.AddRange(assembly.GetTypes());

            var typeCollections = new TypeRepository(types.ToArray());

            var jsonInputIndex = options.InputFormatters.IndexOf(options.InputFormatters.Find(typeof(SystemTextJsonInputFormatter)));
            options.InputFormatters.Insert(jsonInputIndex + 1, new SoapXmlInputFormatter(xmlPrefix, typeCollections));

            var jsonOutputIndex = options.OutputFormatters.IndexOf(options.OutputFormatters.Find(typeof(SystemTextJsonOutputFormatter)));
            options.OutputFormatters.Insert(jsonOutputIndex + 1, new SoapXmlOutputFormatter(xmlPrefix, typeCollections));
            //options.OutputFormatters.Insert(jsonOutputIndex + 2, new WsdlXmlOutputFormatter(xmlPrefix, typeCollections));

            options.AllowEmptyInputInBodyModelBinding = true;
        }

        public static void AddSoapXml(this SwaggerGenOptions options, XmlPrefix xmlPrefix)
        {
            var fd = new FilterDescriptor() { Arguments = new[] { xmlPrefix } };

            options.DocumentFilter<SoapXmlDocumentationFilter>(fd);
            options.OperationFilter<SoapXmlDocumentationFilter>(fd);            
            options.SchemaFilter<SoapXmlDocumentationFilter>(fd);
            options.RequestBodyFilter<SoapXmlDocumentationFilter>(fd);
        }

        public static void UseSoapXml(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path != null)
                {
                    var paths = context.Request.Path.ToString().ToLower();

                    if (paths.StartsWith("/wsdl", out string remaining))
                    {
                        var indx = remaining.IndexOf("/v", 1);

                        if (indx > -1)
                        {
                            string schema = "main";
                            string version = remaining[indx..];

                            if (indx > 0) schema = remaining.Substring(1, indx - 1).Replace("v", "ooo");
                            context.Request.Path = $"/wsdl{version}-{schema}";
                        }
                    }
                }

                await next.Invoke();
            });
        }
    }

    public static class SoapXmlExtensions
    {
        public static bool IsSoapXml(this HttpContext context)
        {
            var acceptHeaders = context.Request.Headers["Accept"];

            if (acceptHeaders.Count > 0 && acceptHeaders[0].ToLower() == "application/soap+xml")
                return true;
            else
                return false;
        }
    }
}
