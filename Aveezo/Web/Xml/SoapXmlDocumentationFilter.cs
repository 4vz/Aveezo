using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class SoapXmlDocumentationFilter : IOperationFilter, ISchemaFilter, IDocumentFilter, IRequestBodyFilter
    {
        #region Fields

        private readonly XmlPrefix xmlPrefix;

        #endregion

        #region Constructors

        public SoapXmlDocumentationFilter(FilterDescriptor descriptor)
        {
            xmlPrefix = (XmlPrefix)descriptor.Arguments[0];
        }

        #endregion

        #region Methods

        public void Apply(OpenApiDocument document, DocumentFilterContext context)
        {
            var soapXmlSchemas = new Dictionary<string, OpenApiSchema>();

            // get wsdl path           
            string wsdlPath = null;
            foreach (var apidesc in context.ApiDescriptions)
            {
                wsdlPath = GetWsdlPath(apidesc);

                if (wsdlPath != null)
                    break;
            }

            foreach (var schema in document.Components.Schemas)
            {
                var dataSchema = Build(schema.Key, schema.Value, soapXmlSchemas, document.Components.Schemas);
                soapXmlSchemas.Add($"SoapXmlEnvelope{schema.Key}", CreateSoapEnvelope(schema.Key, xmlPrefix, dataSchema, wsdlPath));
            }

            foreach (var pair in soapXmlSchemas)
                document.Components.Schemas.Add(pair.Key, pair.Value);
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // get wsdl path   
            var wsdlPath = GetWsdlPath(context.ApiDescription);

            if (operation.RequestBody != null)
            {
                foreach (var requestBody in operation.RequestBody.Content)
                {
                    ValueTypedSchemaConfig(requestBody.Key, requestBody.Value, wsdlPath);
                }
            }
            
            foreach (var status in operation.Responses)
            {
                var contents = status.Value.Content;

                foreach (var response in contents)
                {
                    var schema = response.Value.Schema;

                    if (schema.Reference != null)
                    {
                        var sref = schema.Reference;

                        if (sref.Id.EndsWith("Result"))
                        {
                            sref.Id = sref.Id[0..^6];
                        }
                    }

                    ValueTypedSchemaConfig(response.Key, response.Value, wsdlPath);
                }
            }
        }

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != null && context.Type.IsGenericType && context.Type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                //schema.Reference = context.SchemaGenerator.GenerateSchema(context.Type, context.SchemaRepository).Reference;

            }
        }

        public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context)
        {

        }

        public string GetWsdlPath(ApiDescription apidesc)
        {
            string wsdlPath = null;

            if (apidesc.TryGetMethodInfo(out MethodInfo methodInfo))
            {
                if (methodInfo.DeclaringType == typeof(WsdlXml))
                {
                    // wsdl/vmain/v1 => /wsdl/v1
                    // wsdl/vvos/v1 => /wsdl/vos/v1
                    var re = apidesc.RelativePath;
                    if (re.StartsWith("wsdl/vmain/")) 
                        wsdlPath = $"/wsdl{re[10..]}";
                    else 
                        wsdlPath = $"/wsdl/{re[6..]}";
                }
            }

            return wsdlPath;
        }

        private OpenApiSchema CreateSoapEnvelope(string name, XmlPrefix xmlPrefix, OpenApiSchema schema, string wsdlPath)
        {
            if (schema.Xml == null) schema.Xml = new();

            schema.Xml.Namespace = new Uri($"http://{xmlPrefix.LocalDomain}{wsdlPath}");

            schema.Properties.Add($"xmlns:{xmlPrefix.XmlDefinition}", new OpenApiSchema
            {
                Xml = new OpenApiXml
                {
                    Name = $"xmlns:{xmlPrefix.XmlDefinition}",
                    Attribute = true
                },
                Example = new OpenApiString("http://www.w3.org/2001/XMLSchema")
            });
            schema.Properties.Add($"xmlns:{xmlPrefix.XmlInstance}", new OpenApiSchema
            {
                Xml = new OpenApiXml
                {
                    Name = $"xmlns:{xmlPrefix.XmlInstance}",
                    Attribute = true
                },
                Example = new OpenApiString("http://www.w3.org/2001/XMLSchema-instance")
            });

            return new OpenApiSchema
            {
                Type = "object",
                Xml = new OpenApiXml
                {
                    Name = "Envelope",
                    Prefix = "soap",
                    Namespace = new Uri(xmlPrefix.Namespaces["soap"])
                },
                Properties =
                {
                    {
                        "Body", new OpenApiSchema
                        {
                            Type = "object",
                            Xml = new OpenApiXml
                            {
                                Name = "Body",
                                Prefix = "soap"
                            },
                            Properties =
                            {
                                { name, schema }
                            }
                        }
                    }
                }
            };
        }

        private OpenApiSchema Build(string name, OpenApiSchema schema, Dictionary<string, OpenApiSchema> newSchemas, IDictionary<string, OpenApiSchema> existingSchemas)
        {
            if (schema == null) return null;

            OpenApiSchema newSchema;

            if (schema.Reference != null)
            {
                newSchema = CloneSchemaByReference(schema.Reference, false);

                var eid = schema.Reference.Id;
                var id = newSchema.Reference.Id;

                if (!newSchemas.ContainsKey(id) && existingSchemas.ContainsKey(eid))
                {
                    newSchemas.Add(id, Clone(name, existingSchemas[eid], newSchemas, existingSchemas));
                }
            }
            else
            {
                newSchema = Clone(name, schema, newSchemas, existingSchemas);
            }

            return newSchema;
        }

        private OpenApiSchema Clone(string name, OpenApiSchema schema, Dictionary<string, OpenApiSchema> newSchemas, IDictionary<string, OpenApiSchema> existingSchemas)
        {
            if (schema == null) return null;

            var type = DocumentationFilter.GetType(schema);

            // Create new schema based on the specified schema
            var newSchema = new OpenApiSchema
            {
                Type = schema.Type,
                Format = schema.Format,
                Description = schema.Description,
                Nullable = schema.Nullable,
                Items = schema.Type == "object" ? Clone("items", schema.Items, newSchemas, existingSchemas) : schema.Items,
                Example = schema.Example,
                Xml = new OpenApiXml
                {
                    Name = name.ToPascalCase(),
                    Prefix = xmlPrefix.Local
                }
            };

            if (type.IsDictionary())
            {
                //AdditionalProperties = Clone("additionalProperties", schema.AdditionalProperties, newSchemas, existingSchemas),
                //AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed,
                newSchema.Type = schema.AdditionalProperties.Type;
            }
            else
            {
            }

            foreach (var property in schema.Properties)
            {
                newSchema.Properties.Add(property.Key, Build(property.Key, property.Value, newSchemas, existingSchemas));
                //newSchema.AdditionalPropertiesAllowed = false;
            }

            // type attributes
            var typeAttributes = new Dictionary<string, OpenApiSchema>();

            if (type != null)
            {
                var xmlType = Xml.GetType(xmlPrefix, type);

                if (xmlType != null)
                {
                    typeAttributes.Add("type", new OpenApiSchema
                    {
                        Type = "string",
                        Xml = new OpenApiXml { Name = $"{xmlPrefix.XmlDefinition}:type", Attribute = true },
                        Example = new OpenApiString(xmlType)
                    });
                }

                if (type.IsArray)
                {
                    // schema.Type is array

                    var xmlArrayType = Xml.GetType(xmlPrefix, type.GetElementType());

                    typeAttributes.Add("arrayType", new OpenApiSchema
                    {
                        Type = "string",
                        Xml = new OpenApiXml { Name = $"{xmlPrefix.Local}:arrayType", Attribute = true },
                        Example = new OpenApiString(xmlArrayType)
                    });

                    newSchema.Xml.Wrapped = true;
                    newSchema.Items.Xml = new OpenApiXml
                    {
                        Name = "Item",
                        Prefix = xmlPrefix.Local
                    };
                }
                else if (type.IsList(out Type listType))
                {
                    // schema.Type is array

                    var xmlListType = Xml.GetType(xmlPrefix, listType);

                    typeAttributes.Add("listType", new OpenApiSchema
                    {
                        Type = "string",
                        Xml = new OpenApiXml { Name = $"{xmlPrefix.Local}:listType", Attribute = true },
                        Example = new OpenApiString(xmlListType)
                    });

                    newSchema.Xml.Wrapped = true;
                    newSchema.Items.Xml = new OpenApiXml
                    {
                        Name = "Item",
                        Prefix = xmlPrefix.Local
                    };
                }
                else if (type.IsDictionary(out Type keyType, out Type valueType))
                {
                    // schema.Type is object

                    var xmlKeyType = Xml.GetType(xmlPrefix, keyType);
                    var xmlValueType = Xml.GetType(xmlPrefix, valueType);

                    typeAttributes.Add("keyType", new OpenApiSchema
                    {
                        Type = "string",
                        Xml = new OpenApiXml { Name = $"{xmlPrefix.Local}:dictionaryKeyType", Attribute = true },
                        Example = new OpenApiString(xmlKeyType)
                    });

                    typeAttributes.Add("valueType", new OpenApiSchema
                    {
                        Type = "string",
                        Xml = new OpenApiXml { Name = $"{xmlPrefix.Local}:dictionaryValueType", Attribute = true },
                        Example = new OpenApiString(xmlValueType)
                    });

                    newSchema.Xml.Wrapped = true;

                    // because schema.Type is object, create schema for items
                    newSchema.Items = new OpenApiSchema
                    {
                        Xml = new OpenApiXml
                        {
                            Name = "Item",
                            Prefix = xmlPrefix.Local
                        }
                    };
                }
            }

            // HACK: OpenApi doesnt support attribute AND items together
            if (schema.Type != "object" || type.IsDictionary())
            {
                var wrapperSchema = new OpenApiSchema
                {
                    Type = "object",
                    Xml = new OpenApiXml
                    {
                        Name = newSchema.Xml.Name,
                        Prefix = newSchema.Xml.Prefix
                    }
                };

                foreach (var iod in typeAttributes)
                {
                    wrapperSchema.Properties.Add(iod);
                }

                wrapperSchema.Properties.Add("wrappedObject", newSchema);

                newSchema.Xml.Wrapped = false;
                newSchema.Xml.Name = "value";
                newSchema.Xml.Prefix = null;

                return wrapperSchema;
            }
            else
            {
                foreach (var iod in typeAttributes)
                {
                    newSchema.Properties.Add(iod);
                }

                return newSchema;
            }
        }

        private OpenApiSchema CloneSchemaByReference(OpenApiReference reference, bool envelope)
        {
            OpenApiSchema newschema = null;

            var referenceV3 = reference.ReferenceV3;

            if (referenceV3 != null)
            {
                var rv3s = referenceV3.Split(Collections.Slash, StringSplitOptions.RemoveEmptyEntries);

                if (rv3s.Length == 4)
                {
                    newschema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = $"SoapXml{(envelope ? "Envelope" : "")}{rv3s[3]}"
                        }
                    };
                }
            }

            return newschema;
        }

        private void ValueTypedSchemaConfig(string media, OpenApiMediaType mediaType, string wsdlPath)
        {
            var schema = mediaType.Schema;
            var type = DocumentationFilter.GetType(schema);

            if (media == "application/soap+xml")
            {              
                if (schema.Reference != null)
                {
                    mediaType.Schema = CloneSchemaByReference(schema.Reference, true);
                }
                else
                {
                    var xmlType = Xml.GetType(xmlPrefix, type);
                    if (xmlType != null)
                    {
                        var typeAttributes = new Dictionary<string, OpenApiSchema>();

                        typeAttributes.Add("type", new OpenApiSchema
                        {
                            Type = "string",
                            Xml = new OpenApiXml { Name = $"{xmlPrefix.XmlDefinition}:type", Attribute = true },
                            Example = new OpenApiString(xmlType)
                        });

                        var newSchema = new OpenApiSchema
                        {
                            Type = "object",
                            Xml = new OpenApiXml
                            {
                                Name = "Element",
                                Prefix = xmlPrefix.XmlDefinition
                            }
                        };

                        foreach (var iod in typeAttributes)
                        {
                            newSchema.Properties.Add(iod);
                        }

                        mediaType.Schema = CreateSoapEnvelope("element", xmlPrefix, newSchema, wsdlPath);
                    }
                }
            }            
        }

        #endregion
    }
}
