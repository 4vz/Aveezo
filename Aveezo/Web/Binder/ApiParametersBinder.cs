﻿using Microsoft.AspNetCore.Mvc.Controllers; 
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aveezo
{
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

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var action = bindingContext.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            List<FieldAttribute> fieldAttributes = new();

            // Get from referenced
            if (action.MethodInfo.HasGenericAttributes<ResourceAttribute<Resource>>(out var attributes))
            {
                var resourceType = attributes[0].Type.GetGenericArguments()[0];

                foreach (var property in resourceType.GetProperties())
                {
                    if (property.CanWrite && property.CanRead)
                    {
                        if (!property.Has<HideAttribute>() && property.Has<FieldAttribute>(out var propertyAttributes))
                        {
                            fieldAttributes.AddRange(propertyAttributes);
                        }
                    }
                }
            }

            bool paging = false;
            int limit = 0;
            int offset = -1;
            string after = null;
            Dictionary<string, (string, string)[]> queries = null;
            Dictionary<string, bool> sorts = null;
            List<string> fields = null;

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

            foreach (var attr in fieldAttributes)
            {
                var name = attr.Name;
                var options = attr.Options;

                // queries
                if (options.HasFlag(FieldOptions.CanQuery))
                {
                    parameters.Add(name);

                    var param = bindingContext.ValueProvider.GetValue(attr.Name);

                    if (!string.IsNullOrEmpty(param.FirstValue))
                    {
                        List<(string, string)> values = new();

                        foreach (var value in param.Values)
                        {
                            var valueLower = value.ToLower();

                            if (valueLower.StartsWith("like:", value, out var like)) values.Add(("like", like));
                            else if (valueLower.StartsWith("start:", value, out var start)) values.Add(("start", start));
                            else if (valueLower.StartsWith("end:", value, out var end)) values.Add(("end", end));
                            else if (valueLower.StartsWith("notlike:", value, out var notlike)) values.Add(("notlike", notlike));
                            else if (valueLower.StartsWith("gt:", value, out var gt)) values.Add(("gt", gt));
                            else if (valueLower.StartsWith("gte:", value, out var gte)) values.Add(("gte", gte));
                            else if (valueLower.StartsWith("lt:", value, out var lt)) values.Add(("lt", lt));
                            else if (valueLower.StartsWith("lte:", value, out var lte)) values.Add(("lte", lte));
                            else if (valueLower.StartsWith("not:", value, out var not)) values.Add(("not", not));
                            else values.Add((null, value));
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
                        fields = new();

                    if (!fields.Contains(name))
                        fields.Add(name);
                }

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
            if (ApiParameters.IsPagingResult(action.MethodInfo, out Type type))
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
}