using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Aveezo
{
    public class ActionParametersBinder : IModelBinder
    {
        #region Fields

        #endregion

        #region Constructors

        public ActionParametersBinder()
        {

        }

        #endregion

        #region Operators

        #endregion

        #region Methods

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var res = new ActionParameters();
            var action = bindingContext.ActionContext.ActionDescriptor as ControllerActionDescriptor;

            var orpa = (string)bindingContext.HttpContext.Items["originalPath"];
            var copa = (string)bindingContext.HttpContext.Items["controllerPath"];

            res.FullPath = orpa.TrimEnd('/', '\\');
            res.Path = copa.TrimEnd('/', '\\');

            if (action.MethodInfo != null)
            {
                if (action.MethodInfo.Has<LinkAttribute>(out var la))
                {
                    res.Self = la.Self;
                }
            }

            bindingContext.Result = ModelBindingResult.Success(res);

            return Task.CompletedTask;
        }

        #endregion

        #region Statics

        #endregion
    }
}
