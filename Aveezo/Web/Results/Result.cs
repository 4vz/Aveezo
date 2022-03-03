using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;

using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aveezo
{
    public class Result<T> : IConvertToActionResult
    {
        #region Fields

        private IActionResult ActionResult { get; }

        #endregion

        #region Constructors

        protected Result(IActionResult result) => ActionResult = result;

        #endregion

        #region Operators

        public static implicit operator Result<T>(T result) => new(new ObjectResult(result));

        public static implicit operator Result<T>(PagingResult<T> result) => new(new ObjectResult(result));

        // return Result<T[]> dari Query
        public static implicit operator Result<T>(Result<T[]> resultArray)
        {
            if (resultArray.ActionResult is ObjectResult ores)
            {
                if (ores.Value is T[] tarray)
                    return tarray[0];
                else if (ores.Value is PagingResult<T> tpaging)
                    return tpaging;
            }
            else if (resultArray.ActionResult is StatusCodeResult scres)
                return scres;
            
            return new StatusCodeResult(503);
        }

        public static implicit operator Result<T>(ActionResult result) => new(result);

        public static implicit operator Result<T>(ObjectResult result) => new(result);

        public static implicit operator Result<T>(StatusCodeResult result) => new(result);

        #endregion

        #region Methods

        public IActionResult Convert()
        {
            return ActionResult;
        }

        #endregion
    }


} 

