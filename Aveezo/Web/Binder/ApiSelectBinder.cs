using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Aveezo
{
    public class ApiSelectBinder : IModelBinder
    {
        #region Fields

        private IServiceProvider provider;

        #endregion

        #region Constructors

        public ApiSelectBinder(IServiceProvider provider)
        {
            this.provider = provider;
        }

        #endregion

        #region Methods

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var action = bindingContext.ActionContext.ActionDescriptor as ControllerActionDescriptor;
            var method = action.MethodInfo;

            if (method.Has<SqlAttribute>(out var sqlAttributes))
            {
                var sql = provider.GetService<IDatabaseService>().Sql(sqlAttributes[0].Name);

                if (sql)
                {
                    if (ApiService.IsResourceReturnType(method, out Type type))
                    {
                        if (Activator.CreateInstance(type) is Resource resource)
                        {
                            Func<Parameters, SqlSelect> sqlSelect = parameters => resource.Select(sql, parameters);

                            bindingContext.Result = ModelBindingResult.Success(new ApiSelect()
                            {
                                SqlSelect = sqlSelect
                            });
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Statics



        #endregion

    }
}
