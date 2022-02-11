using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [ModelBinder(typeof(ApiParametersBinder))]
    public sealed class ApiParameters
    {
        #region Fields

        public bool IsPaging { get; init; } = false;

        public int Limit { get; init; } = 0;

        public int Offset { get; init; } = -1;

        public string After { get; init; } = null;

        public (string, bool)[] Sorts { get; init; } = null; 
        
        public (string, (string, string)[])[] Queries { get; init; } = null;

        public string[] Fields { get; init; } = null;

        public bool NoLinks { get; init; } = false;

        #endregion

        #region Statics

        public static bool IsPagingResult(MethodInfo methodInfo, out Type arrayType)
        {
            arrayType = null;
            if (methodInfo != null && !methodInfo.Has<NoPagingAttribute>() && methodInfo.ReturnType != null && methodInfo.ReturnType.IsAssignableToGenericType(typeof(Method<>), out Type[] rtype) && rtype[0].IsArray)
            {
                arrayType = rtype[0];
                return true;
            }
            else
                return false;
        }

        #endregion
    }


}
