using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [ModelBinder(typeof(ActionParametersBinder))]
    public class ActionParameters
    {
        #region Fields

        public string FullPath { get; set; }

        public string Path { get; set; }

        public string Self { get; set; }

        #endregion
    }


}
