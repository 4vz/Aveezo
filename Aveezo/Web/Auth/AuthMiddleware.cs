using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Aveezo
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate next;

        public AuthMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, IServiceProvider provider)
        {
            var authService = provider.GetService<IAuthService>();

            var authHeader = context.Request.Headers["Authorization"];

            if (authHeader.Count > 0 && authHeader[0].Length > 0 && authHeader[0].StartsWith("Bearer "))
            {
                var token = authHeader[0].Substring(7);

                if (token.Length > 0)
                {
                    if (authService.Validate(token))
                    {
                        context.Response.Headers.Add("Token", "Valid");
                    }
                    else
                    {
                        context.Response.Headers.Add("Token", "Expired");
                    }
                }
            }

            await next(context);
        }
    }
}
