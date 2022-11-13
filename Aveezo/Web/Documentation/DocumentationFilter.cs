using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class DocumentationFilter : ISchemaFilter, IOperationFilter, IDocumentFilter
    {
        class InternalTag : OpenApiTag { }
        
        class DisabledTag : InternalTag { }

        class PagingTag : InternalTag { }

        private void ModifySchema(OpenApiSchema schema, OpenApiParameter parameter, OpenApiOperation operation, OperationFilterContext context)
        {
            if (schema != null && schema.Reference != null)
            {
                var refid = schema.Reference.Id;

                if (refid != null && !schema.Reference.IsExternal)
                {
                    if (context.SchemaRepository.Schemas.ContainsKey(refid))
                    {
                        var referencedSchema = context.SchemaRepository.Schemas[refid];

                        if (referencedSchema.Description != null && referencedSchema.Description.StartsWith("aveezo:", out string tag))
                        {
                            if (tag == "result_array")
                            {
                                //referenced;chema.Description = null;

                                if (operation.Tags.Has(typeof(PagingTag)) && /*workaround*/referencedSchema.AdditionalProperties != null)
                                {
                                    referencedSchema.Type = "object";

                                    var pagingSchema = context.SchemaRepository.Schemas[referencedSchema.AdditionalProperties.Reference.Id];

                                    foreach (var (propertyKey, property) in pagingSchema.Properties)
                                    {
                                        referencedSchema.Properties.Add(propertyKey, property);
                                    }

                                    referencedSchema.Properties.Add("result", new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema
                                        {
                                            Reference = referencedSchema.Items.Reference
                                        }
                                    });

                                    referencedSchema.Items = null;
                                }

                                referencedSchema.AdditionalProperties = null;
                                referencedSchema.AdditionalPropertiesAllowed = false;
                            }
                        }
                    }
                }
            }
        }

        private void RemoveSchema(string id, OpenApiSchema schema, SchemaRepository repository, List<string> active, List<string> remove)
        {
            if (schema == null) return;

            if (id == null && schema.Reference != null)
                id = schema.Reference.Id;

            if (id != null)
            {
                if (repository.Schemas.ContainsKey(id))
                {
                    if (!active.Contains(id) && !remove.Contains(id))
                    {
                        remove.Add(id);

                        RemoveSchema(null, repository.Schemas[id], repository, active, remove);

                        RemoveSchema(null, schema.Items, repository, active, remove);

                        foreach (var (_, propertySchema) in schema.Properties)
                            RemoveSchema(null, propertySchema, repository, active, remove);
                         
                        RemoveSchema(null, schema.AdditionalProperties, repository, active, remove);
                    }

                }
            }
        }

        private void CheckSchema(OpenApiSchema schema, SchemaRepository repository, List<string> active, List<string> remove)
        {
            if (schema == null) return;

            // inside reference
            if (schema.Reference != null)
            {
                var id = schema.Reference.Id;
                if (repository.Schemas.ContainsKey(id))
                {
                    if (!active.Contains(id))
                        active.Add(id);
                    if (remove.Contains(id))
                        remove.Remove(id);

                    CheckSchema(repository.Schemas[id], repository, active, remove);
                }
            }

            // inside items
            CheckSchema(schema.Items, repository, active, remove);

            // remove hide
            var removeProperties = new List<string>();

            foreach (var (propertyKey, propertySchema) in schema.Properties)
            {
                if (propertySchema.Description == "aveezo:hide")
                    removeProperties.Add(propertyKey);

                CheckSchema(propertySchema, repository, active, remove);
            }

            foreach (var key in removeProperties)
                schema.Properties.Remove(key);


            // inside additional properties
            CheckSchema(schema.AdditionalProperties, repository, active, remove);
        }

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        { 
            var type = context.Type;     

            if (context.MemberInfo != null)
            {
                // if member
                var member = context.MemberInfo;

                if (member.MemberType == MemberTypes.Property)
                {
                    if (member.Has<HideAttribute>())
                        schema.Description = "aveezo:hide";
                }
            }
            else
            {
                // if class

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    type = type.GetGenericArguments()[0];

                    if (type.IsArray)
                    {
                        schema.Description = "aveezo:result_array";
                        schema.Type = "array";
                        schema.Items = context.SchemaGenerator.GenerateSchema(type.GetElementType(), context.SchemaRepository);
                        schema.AdditionalProperties = context.SchemaGenerator.GenerateSchema(typeof(IPagingResult), context.SchemaRepository);
                        schema.AdditionalPropertiesAllowed = true;
                    }
                    else
                    {
                        var newSchema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

                        schema.Reference = newSchema.Reference;
                    }
                }
                else if (type.IsEnum)
                {
                    if (type.Name == "InterfaceStatus")
                    {
                        schema.Description = "aveezo:enum";
                    }
                }
                else if (type.IsClass)
                {
                    schema.Description = "aveezo:object";
                }
            }

            var useCase = ExampleUseCase.None;
            object[] customExample = null;

            schema.Description = $"{schema.Description}(type={type.AssemblyQualifiedName})";
            if (context.MemberInfo != null)
            {

                if (context.MemberInfo.Has<Base64Attribute>()) useCase = ExampleUseCase.Base64;
                //lse if (context.MemberInfo.Has<ExampleAttribute>(out var ea) && ea.Example != null)
                //{
                //    customExample = ea.Example.Array();
                //    useCase = ExampleUseCase.Custom;
                //}
            }

            if (type.IsDictionary(out Type dictionaryKeyType, out Type dictionaryValueType))
            {
                var list = new OpenApiObject();

                string[] keys = null;
                IOpenApiAny[] values = null;

                if (useCase == ExampleUseCase.Custom && customExample.Length % 2 == 0)
                {
                    var keyObjects = new List<object>();
                    var valueObjects = new List<object>();

                    int ix = 0;
                    foreach (var o in customExample)
                    {
                        if (ix % 2 == 0)
                            keyObjects.Add(o);
                        else
                            valueObjects.Add(o);

                        ix++;
                    }

                    keys = DocumentationExample.CustomKeyExamples(dictionaryKeyType, keyObjects.ToArray());
                    values = DocumentationExample.CustomValueExamples(dictionaryValueType, valueObjects.ToArray());

                    if (values.Length > keys.Length)
                        values = values.Take(keys.Length).ToArray();
                    else if (values.Length < keys.Length)
                        keys = keys.Take(values.Length).ToArray();
                }
                else
                {
                    keys = DocumentationExample.KeyExamples(dictionaryKeyType, useCase);
                    values = DocumentationExample.ValueExamples(dictionaryValueType, useCase);
                }

                if (keys != null && values != null && keys.Length == values.Length)
                {
                    for (var i = 0; i < keys.Length; i++)
                        list.Add(keys[i], values[i]);

                    schema.Example = list;
                }
            }
            else if (type.IsList(out Type listValueType))
            {
                IOpenApiAny[] value;

                if (useCase == ExampleUseCase.Custom)
                    value = DocumentationExample.CustomValueExamples(listValueType, customExample);
                else
                    value = DocumentationExample.ValueExamples(listValueType, useCase);

                if (value != null)
                {
                    var list = new OpenApiArray();
                    list.AddRange(value);
                    schema.Example = list;
                }
            }
            else if (type.IsArray)
            {
                IOpenApiAny[] value;
                var arrayType = type.GetElementType();

                if (useCase == ExampleUseCase.Custom)
                    value = DocumentationExample.CustomValueExamples(arrayType, customExample);
                else
                {
                    if (arrayType == typeof(object))
                        value = new IOpenApiAny[] { DocumentationExample.ValueExamples(typeof(string), useCase)[0], DocumentationExample.ValueExamples(typeof(int))[0], DocumentationExample.ValueExamples(typeof(DateTime))[0] };
                    else
                    {
                        value = DocumentationExample.ValueExamples(arrayType, useCase);

                        if (value == null && arrayType is object) // some kind of object
                        {
                            //var newSchema = context.SchemaGenerator.GenerateSchema(arrayType, context.SchemaRepository);

                            //var a = new OpenApiS
                            //a.Values
                            //var a = new OpenApiString("lala");

                            //value = new IOpenApiAny[] { a, a, a };
                        }
                    }
                }

                if (value != null)
                {
                    var list = new OpenApiArray();
                    list.AddRange(value);
                    schema.Example = list;
                }
            }
            else if (context.MemberInfo != null)
            {
                if (useCase == ExampleUseCase.Custom)
                    schema.Example = DocumentationExample.CustomValueExamples(type, customExample)[0];
                else
                    schema.Example = DocumentationExample.ValueExample(type, useCase);
            }
            else
            {
                schema.Example = DocumentationExample.ValueExample(type);
            }
            //*/
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = context.MethodInfo;




            


            //return;

            if (method.Has<DisabledAttribute>())
                operation.Tags.Add(new DisabledTag());
            else if (ApiService.IsPagingResult(method, out _))
                operation.Tags.Add(new PagingTag());

            if (ApiService.IsResourceReturnType(method, out var type))
            {
                // find schema (for parameter description)
                Dictionary<string, string> fieldDescriptions = new();
                var sc = context.SchemaRepository.Schemas;

                if (type.Name == "Interface")
                {

                }

                if (sc.ContainsKey(type.Name))
                {
                    var sce = sc[type.Name];
                    foreach (var (pkey, psce) in sce.Properties)
                    {
                        if (!fieldDescriptions.ContainsKey(pkey))
                            fieldDescriptions.Add(pkey, psce.Description);
                    }
                }

                // create query parameters
                foreach (var prop in type.GetProperties())
                {
                    if (prop.Has<FieldAttribute>(out var fields))
                    {
                        var field = fields[0];

                        if (field.Options.HasFlag(FieldOptions.CanQuery))
                        {
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = field.Name,
                                In = ParameterLocation.Query,
                                Schema = context.SchemaGenerator.GenerateSchema(prop.PropertyType, context.SchemaRepository),
                                Description = fieldDescriptions.ContainsKey(field.Name) ? fieldDescriptions[field.Name] : null
                            });
                        }

                        
                    }
                }
            }
            
            foreach (var parameter in operation.Parameters)
                ModifySchema(parameter.Schema, parameter, operation, context);

            foreach (var (_, response) in operation.Responses)
                foreach (var (_, content) in response.Content)
                    ModifySchema(content.Schema, null, operation, context);
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var info = swaggerDoc.Info;

            if (info != null && info.Version != null)
            {
                var sp = info.Version.Split(Collections.HyphenMinus);
                if (sp.Length == 2)
                    info.Version = sp[0];
            }

            var newPaths = new Dictionary<string, OpenApiPathItem>(); 
            var removePaths = new List<string>();


            var activeSchemas = new List<string>();
            var removeSchemas = new List<string>();

            foreach (var (path, pathItem) in swaggerDoc.Paths)
            {
                var removePath = false;
                var removeOperations = new List<OperationType>();
                var removeParameters = new List<OpenApiParameter>();

                foreach (var (operationKey, operation) in pathItem.Operations)
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Schema?.Reference != null)
                        {
                            var referencedSchema = context.SchemaRepository.Schemas[parameter.Schema.Reference.Id];

                            if (referencedSchema.Description.StartsWith("aveezo:object"))
                                removeParameters.Add(parameter);
                        }
                    }

                    foreach (var parameter in removeParameters)
                        operation.Parameters.Remove(parameter);

                    if (operation.Tags.Has(typeof(DisabledTag)))
                    {
                        removeOperations.Add(operationKey);

                        foreach (var parameter in operation.Parameters)
                            RemoveSchema(null, parameter.Schema, context.SchemaRepository, activeSchemas, removeSchemas);

                        if (operation.RequestBody != null)
                            foreach (var (contentKey, contentBody) in operation.RequestBody.Content)
                                RemoveSchema(null, contentBody.Schema, context.SchemaRepository, activeSchemas, removeSchemas);

                        foreach (var (responseKey, response) in operation.Responses)
                        {
                            foreach (var (contentKey, content) in response.Content)
                                RemoveSchema(null, content.Schema, context.SchemaRepository, activeSchemas, removeSchemas);
                        }
                    }
                    else
                    {
                        foreach (var parameter in operation.Parameters)
                            CheckSchema(parameter.Schema, context.SchemaRepository, activeSchemas, removeSchemas);

                        if (operation.RequestBody != null)
                            foreach (var (contentKey, contentBody) in operation.RequestBody.Content)
                                CheckSchema(contentBody.Schema, context.SchemaRepository, activeSchemas, removeSchemas);

                        foreach (var (responseKey, response) in operation.Responses)
                        {
                            foreach (var (contentKey, content) in response.Content)
                                CheckSchema(content.Schema, context.SchemaRepository, activeSchemas, removeSchemas);
                        }
                    }

                    // remove internal tags
                    foreach (var tag in operation.Tags.Keep(s => s is InternalTag))
                        operation.Tags.Remove(tag);
                }  

                foreach (var operationKey in removeOperations)
                    pathItem.Operations.Remove(operationKey);

                if (pathItem.Operations.Count == 0)
                    removePath = true;

                if (!removePath)
                {
                    if (path.StartsWith("/v") && path.Length >= 5)
                    {
                        var fslash = path.IndexOf('/', 1);

                        if (fslash > -1 && path[fslash + 1] == 'v')
                        {
                            var schema = path[2..fslash];
                            var vslash = path.IndexOf('/', fslash + 1);

                            string remaining = null;
                            string vers = null;

                            if (vslash > -1)
                            {
                                vers = path.Substring(fslash + 1, vslash - fslash - 1);
                                remaining = path[vslash..];
                            }
                            else
                            {
                                vers = path[(fslash + 1)..];
                            }

                            string url = null;

                            if (schema == "main")
                                url = $"/{vers}{remaining}";
                            else
                                url = $"/{schema.Replace("ooo", "v")}/{vers}{remaining}";

                            newPaths.Add(url, pathItem);
                            removePath = true;
                        }
                    }
                    else if (path.StartsWith("/wsdl"))
                        removePath = true;
                }

                if (removePath)
                    removePaths.Add(path);
            }

            foreach (var remove in removePaths)
                swaggerDoc.Paths.Remove(remove);

            foreach (var add in newPaths)
                swaggerDoc.Paths.Add(add.Key, add.Value);

            // clean up schema from Filter<>
            foreach (var (key, schema) in context.SchemaRepository.Schemas)
            {
                if (schema.Description != null && schema.Description.StartsWith("aveezo:"))
                    RemoveSchema(key, schema, context.SchemaRepository, activeSchemas, removeSchemas);
                else if (key == "IPagingResult") // paging 
                    removeSchemas.Add("IPagingResult");
                else if (key == "StringStringValueTuple") // filter
                    removeSchemas.Add("StringStringValueTuple");
            }

            // remove schemas
            foreach (var key in removeSchemas)
                context.SchemaRepository.Schemas.Remove(key);

        }

        #region Statics

        public static Type GetType(OpenApiSchema schema)
        {
            var description = schema.Description;

            Type type = null;

            if (description != null)
            {
                int idx;

                if ((idx = description.IndexOf("(type=")) > -1)
                {
                    var ockx = description[idx..].Trim('(', ')').Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

                    if (ockx.Length == 2)
                    {
                        type = Type.GetType($"{ockx[1]}");
                    }

                    var newDescription = description.Substring(0, idx);
                    if (newDescription == string.Empty) newDescription = null;
                    schema.Description = newDescription;
                }
            }

            return type;
        }

        #endregion
    }
}
