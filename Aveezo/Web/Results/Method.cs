using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;

using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aveezo
{
    public class Method<T> : IConvertToActionResult
    {
        #region Fields

        private IActionResult ActionResult { get; }

        #endregion

        #region Constructors

        protected Method(IActionResult result) => ActionResult = result;

        #endregion

        #region Operators

        public static implicit operator Method<T>(T result) => new(new ObjectResult(result));

        public static implicit operator Method<T>(MethodResult<T> result) => new(new ObjectResult(result));

        // return Result<T[]> dari Query
        public static implicit operator Method<T>(Method<T[]> resultArray)
        {
            if (resultArray.ActionResult is ObjectResult ores)
            {
                if (ores.Value is T[] tarray)
                    return tarray[0];
                else if (ores.Value is MethodResult<T> tpaging)
                    return tpaging;
            }
            else if (resultArray.ActionResult is StatusCodeResult scres)
                return scres;
            
            return new StatusCodeResult(503);
        }

        public static implicit operator Method<T>(ActionResult result) => new(result);

        public static implicit operator Method<T>(ObjectResult result) => new(result);

        public static implicit operator Method<T>(StatusCodeResult result) => new(result);

        #endregion

        #region Methods

        public IActionResult Convert()
        {
            return ActionResult;
        }

        #endregion
    }


} 

