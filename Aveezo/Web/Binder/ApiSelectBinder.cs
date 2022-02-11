using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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

        #region Operators


        #endregion

        #region Methods

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var action = bindingContext.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            if (action.MethodInfo.Has<SqlAttribute>(out var sqlAttributes))
            {
                if (action.MethodInfo.HasGenericAttributes<ResourceAttribute<Resource>>(out var resourceAttributes))
                {
                    var sql = provider.GetService<IDatabaseService>().Sql(sqlAttributes[0].Name);

                    if (sql)
                    {
                        var restype = resourceAttributes[0].Type.GetGenericArguments()[0];

                        if (Activator.CreateInstance(restype) is Resource resource)
                        {
                            Func<object[], SqlSelect> sqlSelect = parameters => resource.Select(sql, parameters);

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
