using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NoCacheAttribute : ResponseCacheAttribute
    {
        public NoCacheAttribute()
        {
            NoStore = true;
            Duration = 0;
            Location = ResponseCacheLocation.None;
        }
    }
}
