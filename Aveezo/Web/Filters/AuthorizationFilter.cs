using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aveezo
{
    public class AuthorizationFilter : IAuthorizationFilter
    {
        #region Fields

        private IServiceProvider Provider { get; }

        private ApiOptions Options { get; }

        private IAuthService Auth { get; }

        #endregion

        #region Constructors

        public AuthorizationFilter(IServiceProvider provider)
        {
            Provider = provider;
            Options = Provider.GetService<IOptions<ApiOptions>>().Value;
            Auth = Provider.GetService<IAuthService>();
        }

        #endregion

        #region Fields

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            ObjectResult result = null;
            var desc = context.ActionDescriptor as ControllerActionDescriptor;

            if (desc != null)
            {
                var methodInfo = desc.MethodInfo;
                var controllerInfo = desc.ControllerTypeInfo;
                var noAuth = false;
                var level = -1;

                if (methodInfo.Has<NoAuthAttribute>())
                    noAuth = true;
                else if (methodInfo.Has<AuthAttribute>(out var mat))
                    level = mat[0].Level;
                else if (controllerInfo.Has<NoAuthAttribute>())
                    noAuth = true;
                else if (controllerInfo.Has<AuthAttribute>(out var cat))
                    level = cat[0].Level;
                else
                    level = 0;

                if (!noAuth)
                {
                    var statusCode = 503;
                    ErrorResult errorResult = null;

                    if (level >= 0)
                    {
                        var auth = context.HttpContext.Request.Headers.Authorization;

                        if (auth.Count > 0 && auth[0].Length > 0 && auth[0].StartsWith("Bearer "))
                        {
                            var token = auth[0][7..];

                            if (token.Length > 0)
                            {
                                if (Auth.Validate(token, out var scopes, out var parameters))
                                {
                                    context.HttpContext.Items.Add("auth_scopes", scopes);
                                    context.HttpContext.Items.Add("auth_parameters", parameters);
                                }
#if !DEBUG
                                else
                                    result = Api.Forbidden("api_authorization", "FAILED", "Authorization has been failed");
                            }
                            else
                                result = Api.Forbidden("api_authorization", "INVALID_TOKEN", "Invalid specified access token");
                        }
                        else
                            result = Api.Forbidden("api_authorization", "TOKEN_REQUIRED", "Unauthorized");
#else
                            }
                        }
#endif
                    }
                    else
                    {
                        result = Api.Unavailable("Error level < -1");
                    }
                }
            }

            context.Result = result;
        }


#endregion
    }
}
