using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;

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
            var action = context.ActionDescriptor as ControllerActionDescriptor;

            if (context.Result == null)
            {
                // Invalid Model State error result
                if (context.ModelState.IsValid == false)
                {
                    if (context.ModelState.ContainsKey("unavailable"))
                    {
                        string message = null;

                        if (context.ModelState["unavailable"].Errors.Count > 0)
                        {
                            message = context.ModelState["unavailable"].Errors[0].ErrorMessage;
                        }

                        context.Result = Api.Unavailable(message);

                    }
                    else
                    {
                        var statusCode = 400; // by default it is 400 bad request
                        var details = new Dictionary<string, string[]>();

                        Type modelTypeForJsonNaming = null;

                        if (!context.HttpContext.IsSoapXml())
                        {
                            var pars = context.ActionDescriptor.Parameters;
                            if (pars.Count > 0)
                                modelTypeForJsonNaming = pars[0].ParameterType;
                        }

                        foreach ((var key, var value) in context.ModelState)
                        {
                            if (value.Errors.Count > 0)
                            {
                                string rkey = null;

                                if (key.StartsWith("$."))
                                {
                                    rkey = key[2..];
                                }
                                else if (modelTypeForJsonNaming != null)
                                {
                                    if (modelTypeForJsonNaming.GetMember(key).Has<JsonPropertyNameAttribute>(out var tdd))
                                        rkey = tdd[0].Name;
                                    else
                                        rkey = key.ToSnakeCase();
                                }

                                var detailStatuses = new List<string>();
                                var detailMessages = new List<string>();

                                foreach (var error in value.Errors)
                                {
                                    detailMessages.Add(rkey != null ? error.ErrorMessage.Replace(key, rkey).Trim() : error.ErrorMessage.Trim());
                                }

                                details.Add(rkey ?? key, detailMessages.ToArray());
                            }
                        }

                        var errorResult = new ErrorResult(statusCode, "request_validation", "VALIDATIONS_FAILED", "one or more validations have been failed");
                        errorResult.Error.Details = details;
                        context.Result = new ObjectResult(errorResult) { StatusCode = statusCode };
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }

        #endregion
    }

 

}
