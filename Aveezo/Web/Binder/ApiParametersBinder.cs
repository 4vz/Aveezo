using Microsoft.AspNetCore.Mvc.Controllers; 
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Aveezo;

public class ApiParametersBinder : IModelBinder
{
    #region Fields

    #endregion

    #region Constructors

    public ApiParametersBinder()
    {

    }

    #endregion

    #region Operators

    #endregion

    #region Methods

    private Values<string> GetQueryStringValue(ModelBindingContext bindingContext, string name)
    {
        if (bindingContext.HttpContext.Request.Query.ContainsKey(name))
            return bindingContext.HttpContext.Request.Query[name];
        else
            return null;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var action = bindingContext.ActionContext.ActionDescriptor as ControllerActionDescriptor;

        List<(FieldAttribute, PropertyInfo)> fieldAttributes = new();

        // Get from referenced
        if (ApiService.IsResourceReturnType(action.MethodInfo, out Type resourceType))
        {
            foreach (var property in resourceType.GetProperties())
            {
                if (property.CanWrite && property.CanRead)
                {
                    if (!property.Has<HideAttribute>() && property.Has<FieldAttribute>(out var attr))
                    {
                        fieldAttributes.Add((attr, property));
                    }
                }
            }
        }

        bool total = false;
        bool paging = false;
        int limit = 0;
        int offset = -1;
        string after = null;
        Dictionary<string, (string, Values<string>)[]> queries = null;
        Dictionary<string, bool> sorts = null;

        List<string> fields = null;
        Dictionary<string, PropertyInfo> properties = null;
        Dictionary<string, FieldOptions> fieldOptions = null;

        List<string> parameters = new();

        // sorts
        Dictionary<string, bool> sortValues = null;
        var sortParam = bindingContext.ValueProvider.GetValue("sorts");
        parameters.Add("sorts");
        if (!(sortParam == ValueProviderResult.None || string.IsNullOrEmpty(sortParam.FirstValue)))
        {
            foreach (var param in sortParam.Values)
            {
                var sortParts = param.Split(Collections.Comma, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in sortParts)
                {
                    if (part.Length > 0)
                    {
                        string name = null;
                        int direction = 0;
                        if ((direction = part[0].IndexIn('+', '-')) > -1)
                            name = part[1..].ToLower();
                        else
                            name = part.ToLower();

                        if (sortValues == null)
                            sortValues = new();

                        if (!sortValues.ContainsKey(name))
                            sortValues.Add(name, direction != 0);
                    }
                }
            }
        }

        // fields
        List<string> fieldValues = null;
        var fieldParam = bindingContext.ValueProvider.GetValue("fields");
        parameters.Add("fields");
        if (!(fieldParam == ValueProviderResult.None || string.IsNullOrEmpty(fieldParam.FirstValue)))
        {
            foreach (var param in fieldParam.Values)
            {
                var fieldParts = param.Split(Collections.Comma, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in fieldParts)
                {
                    if (part.Length > 0)
                    {
                        if (fieldValues == null)
                            fieldValues = new();

                        if (!fieldValues.Contains(part))
                            fieldValues.Add(part);
                    }
                }
            }
        }


        foreach (var (attr, property) in fieldAttributes)
        {
            var name = attr.Name;
            var options = attr.Options;

            // queries
            if (options.HasFlag(FieldOptions.CanQuery))
            {
                parameters.Add(name);

                var queryValues = GetQueryStringValue(bindingContext, attr.Name);

                if (queryValues != null && !queryValues.IsEmpty)
                {
                    List<(string, Values<string>)> values = new();

                    foreach (var value in queryValues)
                    {
                        var valueLower = value.ToLower();

                        string qattr = null;
                        string qval = null;

                        if (valueLower.StartsWith("like:", value, out var like))
                        {
                            qattr = "like";
                            qval = like;
                        }
                        else if (valueLower.StartsWith("start:", value, out var start))
                        {
                            qattr = "start";
                            qval = start;
                        }
                        else if (valueLower.StartsWith("end:", value, out var end))
                        {
                            qattr = "end";
                            qval = end;
                        }
                        else if (valueLower.StartsWith("notlike:", value, out var notlike))
                        {
                            qattr = "notlike";
                            qval = notlike;
                        }
                        else if (valueLower.StartsWith("gt:", value, out var gt))
                        {
                            qattr = "gt";
                            qval = gt;
                        }
                        else if (valueLower.StartsWith("gte:", value, out var gte))
                        {
                            qattr = "gte";
                            qval = gte;
                        }
                        else if (valueLower.StartsWith("lt:", value, out var lt))
                        {
                            qattr = "lt";
                            qval = lt;
                        }
                        else if (valueLower.StartsWith("lte:", value, out var lte))
                        {
                            qattr = "lte";
                            qval = lte;
                        }
                        else if (valueLower.StartsWith("not:", value, out var not))
                        {
                            qattr = "not";
                            qval = not;
                        }
                        else
                        {
                            qval = value;
                        }

                        var multival = qval.Split(Collections.Comma, StringSplitOptions.RemoveEmptyEntries);
                        
                        if (multival.Length > 0)
                        {
                            values.Add((qattr, multival));
                        }
                        else
                        {
                            values.Add((qattr, qval));
                        }
                        
                    }

                    if (queries == null)
                        queries = new();

                    if (!queries.ContainsKey(attr.Name))
                        queries.Add(attr.Name, values.ToArray());
                }
            }

            var addFields = false;
            if (fieldValues != null)
            {
                // if theres fieldValues
                if (options.HasFlag(FieldOptions.FieldsOnly))
                    addFields = true;
                else
                {
                    if (fieldValues.Contains(name))
                        addFields = true;
                }
            }
            else
            {
                // if not
                if (options.HasFlag(FieldOptions.Default))
                    addFields = true;
            }

            if (addFields)
            {
                if (fields == null)
                {
                    fields = new();
                    properties = new();
                    //fieldOptions = new();
                }

                if (!fields.Contains(name))
                {
                    fields.Add(name);
                    properties.Add(name, property);
                    //fieldOptions.Add(name, options);
                }
            }

            if (fieldOptions == null)
                fieldOptions = new();

            fieldOptions.Add(name, options);


            if (sortValues != null)
            {
                // if theres sortvalues
                if (options.HasFlag(FieldOptions.CanSort))
                {
                    if (sorts == null)
                        sorts = new();

                    if (!sorts.ContainsKey(name))
                        sorts.Add(name, sorts[name]);
                }
            }
        }

        if (fieldValues != null)
        {
            if (fields != null)
            {
                List<string> undefinedFields = new();

                foreach (var v in fieldValues)
                {
                    if (!fields.Contains(v))
                        undefinedFields.Add(v);
                }

                if (undefinedFields.Count > 0)
                {
                    foreach (var field in undefinedFields)
                    {
                        bindingContext.ModelState.AddModelError("undefined_fields", $"noformat#{field}");
                    }

                }
            }
            else
            {
                foreach (var field in fieldValues)
                {
                    bindingContext.ModelState.AddModelError("undefined_fields", $"noformat#{field}");
                }
            }
        }
        if (sortValues != null)
        {
            if (sorts != null)
            {
                List<string> undefinedSorts = new();

                foreach (var (k, v) in sortValues)
                {
                    if (!sorts.ContainsKey(k))
                        undefinedSorts.Add(k);
                }

                if (undefinedSorts.Count > 0)
                {
                    foreach (var sort in undefinedSorts)
                    {
                        bindingContext.ModelState.AddModelError("undefined_sort", $"noformat#{sort}");
                    }
                }
            }
            else
            {
                foreach (var (sort, _) in sortValues)
                {
                    bindingContext.ModelState.AddModelError("undefined_sort", $"noformat#{sort}");
                }
            }
        }

        // paging
        if (ApiService.IsPagingResult(action.MethodInfo, out Type type))
        {
            paging = true;

            var limitParam = bindingContext.ValueProvider.GetValue("limit");
            parameters.Add("limit");
            if (!(limitParam == ValueProviderResult.None || string.IsNullOrEmpty(limitParam.FirstValue)) && int.TryParse(limitParam.FirstValue, out var limitParse) && limitParse > 0)
                limit = limitParse;

            var offsetParam = bindingContext.ValueProvider.GetValue("offset");
            parameters.Add("offset");
            if (!(offsetParam == ValueProviderResult.None || string.IsNullOrEmpty(offsetParam.FirstValue)) && int.TryParse(offsetParam.FirstValue, out var offsetParse) && offsetParse > -1)
                offset = offsetParse;

            var afterParam = bindingContext.ValueProvider.GetValue("after");
            parameters.Add("after");
            if (!(afterParam == ValueProviderResult.None || string.IsNullOrEmpty(afterParam.FirstValue)) && afterParam.FirstValue.IsBase64())
                after = afterParam.FirstValue;

            var totalParam = bindingContext.ValueProvider.GetValue("total");
            parameters.Add("total");
            if (!(totalParam == ValueProviderResult.None))
                total = true;
        }

        // nolinks
        var noLinks = false;
        var noLinksParam = bindingContext.ValueProvider.GetValue("nolinks");
        parameters.Add("nolinks");
        if (noLinksParam != ValueProviderResult.None)
            noLinks = true;

        List<string> notfounds = new();
        foreach (var (query, _) in bindingContext.HttpContext.Request.Query)
        {
            if (!parameters.Contains(query.ToLower()))
                notfounds.Add(query);
        }

        if (notfounds.Count > 0)
        {
            foreach (var notfound in notfounds)
            {
                bindingContext.ModelState.AddModelError("undefined_query", $"noformat#{notfound}");
            }
        }

        if (bindingContext.ModelState.IsValid)
        {
            bindingContext.Result = ModelBindingResult.Success(new ApiParameters
            {
                IsPaging = paging,
                Total = total,
                Properties = properties,
                FieldOptions = fieldOptions,
                Limit = limit, // limit=
                Offset = offset, // offset=
                After = after, // after=
                Queries = queries?.ToTupleArray(), // <name>=
                Sorts = sorts?.ToTupleArray(), // sorts=
                Fields = fields?.ToArray(), // fields=
                NoLinks = noLinks // nolinks
            });
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Statics

    #endregion
}


