using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Aveezo
{
    public class ResourceFilter : IResourceFilter
    {
        #region Fields

        private IServiceProvider Provider { get; }

        private ApiOptions Options { get; }

        #endregion

        #region Constructors

        public ResourceFilter(IServiceProvider provider)
        {
            Provider = provider;
            Options = Provider.GetService<IOptions<ApiOptions>>().Value;
        }

        #endregion

        #region Methods

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var cancelled = false;

            var descr = context.ActionDescriptor as ControllerActionDescriptor;

            if (!descr.MethodInfo.Has<DisabledAttribute>())
            {
                if (descr.ControllerTypeInfo.Has<EnableIfAttribute>(out var attr) && attr.Key != null && attr.Value != null)
                {
                    var prif = typeof(ApiOptions).GetProperty(attr.Key);

                    if (prif != null)
                    {
                        var opval = prif.GetValue(Options);

                        if (Equals(prif.GetValue(Options), attr.Value))
                        {
                            // ok method is enabled


                        }
                        else cancelled = true;
                    }
                }
            }
            else cancelled = true;

            if (cancelled)
                context.Result = new NotFoundObjectResult(null);
            else
            {
                foreach (var p in descr.Parameters)
                {
                    var b = p.BindingInfo;

                    if (b.BindingSource == BindingSource.Query)
                    {
                        if (b.BinderModelName == null)
                        {
                            var sc = p.Name.ToSnakeCase();
                            b.BinderModelName = sc;
                        }
                    }
                }
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {

        }



        #endregion
    }

    
}
