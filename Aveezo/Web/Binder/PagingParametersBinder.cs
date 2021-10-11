using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class PagingParametersBinder : IModelBinder
    {
        #region Fields

        #endregion

        #region Constructors

        public PagingParametersBinder()
        {

        }

        #endregion

        #region Operators

        #endregion

        #region Methods

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var action = bindingContext.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            if (action.MethodInfo != null && action.MethodInfo.Has<PagingAttribute>())
            {
                int limit = -1;
                int offset = 0;
                string after = null;
                string[] sorts = null;

                var limitParam = bindingContext.ValueProvider.GetValue("limit");
                if (!(limitParam == ValueProviderResult.None || string.IsNullOrEmpty(limitParam.FirstValue)) && int.TryParse(limitParam.FirstValue, out var limitParse) && limitParse >= 0)
                    limit = limitParse;

                var offsetParam = bindingContext.ValueProvider.GetValue("offset");
                if (!(offsetParam == ValueProviderResult.None || string.IsNullOrEmpty(offsetParam.FirstValue)) && int.TryParse(offsetParam.FirstValue, out var offsetParse) && offsetParse >= 0)
                    offset = offsetParse;

                var afterParam = bindingContext.ValueProvider.GetValue("after");
                if (!(afterParam == ValueProviderResult.None || string.IsNullOrEmpty(afterParam.FirstValue)) && afterParam.FirstValue.IsBase64())
                    after = afterParam.FirstValue;

                var sortParam = bindingContext.ValueProvider.GetValue("sort");
                if (!(sortParam == ValueProviderResult.None || string.IsNullOrEmpty(sortParam.FirstValue)))
                {
                    var sortList = new List<string>();

                    foreach (var sort in sortParam.Values)
                    {
                        var sortx = sort.Split(Collections.Comma, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var sorty in sortx)
                        {
                            if (sorty.Length > 0)
                                sortList.Add(sorty);
                        }
                    }

                    sorts = sortList.ToArray();
                }

                bindingContext.Result = ModelBindingResult.Success(new PagingParameters
                {
                    Limit = limit,
                    Offset = offset,
                    After = after,
                    Sorts = sorts
                });
            }


            return Task.CompletedTask;
        }

        #endregion

        #region Statics

        #endregion
    }
}
