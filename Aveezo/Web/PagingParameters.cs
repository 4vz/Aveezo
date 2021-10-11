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
    [ModelBinder(typeof(PagingParametersBinder))]
    public class PagingParameters
    {
        #region Fields

        public int Limit { get; set; }

        public int Offset { get; set; }

        public string After { get; set; }

        public string[] Sorts { get; set; }

        #endregion
    }


}
