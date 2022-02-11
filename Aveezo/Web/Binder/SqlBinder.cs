using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Aveezo
{
    public class SqlBinder : IModelBinder
    {
        #region Fields

        private IServiceProvider provider;

        #endregion

        #region Constructors

        public SqlBinder(IServiceProvider provider)
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

            if (action.MethodInfo.Has<SqlAttribute>(out var obj))
            {
                var name = obj[0].Name;

                var sql = provider.GetService<IDatabaseService>().Sql(name);

                if (sql)
                    bindingContext.Result = ModelBindingResult.Success(sql);
                else
                    bindingContext.ModelState.AddModelError("unavailable", $"Sql: {sql.LastException?.Exception?.Message}");
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Statics

        #endregion

    }
}
