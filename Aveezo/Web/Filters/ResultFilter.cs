using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

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
            else if (context.Result is ObjectResult objectResult)
            {
                if (objectResult.Value is ErrorResult errorResult)
                {
                    if (errorResult.Error != null)
                    {
                        var error = errorResult.Error;

                        error.Message = FormatMessage(error.Message, context);

                        if (error.Details != null)
                        {
                            foreach (var (key, details) in error.Details)
                            {
                                if (details != null)
                                {
                                    var nds = new List<string>();
                                    foreach (var detail in details)
                                    {
                                        nds.Add(FormatMessage(detail, context));
                                    }
                                    error.Details[key] = nds.ToArray();
                                }
                            }
                        }
                    }
                }
            }
        }

        public string FormatMessage(string message, ResultExecutingContext context)
        {
            var parts = message.Split('#', 2);
            var noformat = false;

            if (parts.Length == 2)
            {
                var heads = parts[0];
                message = parts[1];                

                foreach (var head in heads.Split(','))
                {
                    if (head == "noformat")
                        noformat = true;
                }
            }

            if (!noformat)
                message = Document.Format(message);

            CaseStyle style;

            if (context.HttpContext.IsSoapXml())
                style = CaseStyle.CamelCase;
            else
                style = CaseStyle.LowerSnakeCase;

            message.Find(@"\[\w+\]", s =>
            {
                var d = s[1..^1];

                string tg = d.ToCase(style);

                message = message.Replace(s, tg);
            });

            return message;
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {

        }

        #endregion
    }
}
