using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Aveezo
{
    public class ResultFilter : IResultFilter
    {
        #region Fields

        private IServiceProvider Provider { get; }

        private ApiOptions Options { get; }

        #endregion

        #region Constructors

        public ResultFilter(IServiceProvider provider)
        {
            Provider = provider;
            Options = Provider.GetService<IOptions<ApiOptions>>().Value;
        }

        #endregion

        #region Methods

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is StatusCodeResult statusCodeResult)
            {
                context.Result = new ObjectResult(null)
                {
                    StatusCode = statusCodeResult.StatusCode
                };
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        #endregion
    }
}
