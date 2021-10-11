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

namespace Aveezo
{
    public class AuthorizationFilter : IAuthorizationFilter
    {
        #region Fields

        private IServiceProvider Provider { get; }

        private ApiOptions Options { get; }

        #endregion

        #region Constructors

        public AuthorizationFilter(IServiceProvider provider)
        {
            Provider = provider;
            Options = Provider.GetService<IOptions<ApiOptions>>().Value;
        }

        #endregion

        #region Fields

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            
        }

        #endregion
    }
}
