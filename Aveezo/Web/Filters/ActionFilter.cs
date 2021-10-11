using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Aveezo
{
    public class ActionFilter : IActionFilter
    {
        #region Fields

        private IServiceProvider Provider { get; }

        private ApiOptions Options { get; }

        #endregion

        #region Constructors

        public ActionFilter(IServiceProvider provider)
        {
            Provider = provider;
            Options = Provider.GetService<IOptions<ApiOptions>>().Value;
        }

        #endregion

        #region Methods

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // check 
            if (context.Result == null && !context.ModelState.IsValid)
            {
                int statusCode = 400; // by default it is 400 bad request

                Type modelTypeForJsonNaming = null;

                if (!context.HttpContext.IsSoapXml())
                {
                    var pars = context.ActionDescriptor.Parameters;

                    if (pars.Count > 0)
                    {
                        modelTypeForJsonNaming = pars[0].ParameterType;
                    }
                }

                var entries = new List<ErrorResultEntry>();

                foreach ((var key, var value) in context.ModelState)
                {
                    if (value.Errors.Count > 0)
                    {
                        string rkey = null;

                        if (key.StartsWith("$."))
                        {
                            rkey = key.Substring(2);
                        }
                        else if (modelTypeForJsonNaming != null && modelTypeForJsonNaming.GetMember(key).Has<JsonPropertyNameAttribute>(out var tdd))
                        {
                            rkey = tdd.Name;
                        }

                        var list = new List<string>();

                        foreach (var error in value.Errors)
                        {
                            var message = error.ErrorMessage;

                            if (message != null && message.Length > 3 && message.StartsWith("###"))
                            {
                                statusCode = 503;
                                message = message.Substring(3);
                            }

                            if (rkey != null)
                                list.Add(message.Replace(key, rkey));
                            else
                                list.Add(message);
                        }

                        entries.Add(new ErrorResultEntry { Source = rkey ?? key, Errors = list.ToArray() });
                    }
                }

                context.Result = new ObjectResult(new ErrorResult { Entries = entries.ToArray() }) { StatusCode = statusCode };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        #endregion
    }

 

}
