using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class PropFilterBinder : IModelBinder
    {
        #region Fields

        #endregion

        #region Constructors

        public PropFilterBinder()
        {

        }

        #endregion

        #region Operators

        #endregion

        #region Methods

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var instance = (IFilter)Activator.CreateInstance(bindingContext.ModelType);
            instance.Name = bindingContext.FieldName;

            var query = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);

            if (query.FirstValue != null)
            {
                List<(string, string)> values = new();

                foreach (var value in query.Values)
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
                instance.Values = values.ToArray();
            }

            bindingContext.Result = ModelBindingResult.Success(instance);

            return Task.CompletedTask;
        }

        #endregion

        #region Statics

        #endregion
    }
}
