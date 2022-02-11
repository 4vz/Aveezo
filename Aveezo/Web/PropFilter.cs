using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Aveezo
{
    public interface IFilter
    {
        string Name { get; set; }

        (string, string)[] Values { get; set; }
    }

    [ModelBinder(typeof(PropFilterBinder))]
    public class PropFilter<T> : IFilter
    {
        #region Fields

        public string Name { get; set; }

        public (string, string)[] Values { get; set; }

        #endregion

        #region Constructors

        public PropFilter()
        {
        }

        #endregion
    }

}
