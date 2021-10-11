
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PathAttribute : RouteAttribute
    {
        public PathAttribute(string template) : base($"/v{{version:apiVersion}}/{template.TrimStart('/').TrimEnd('/')}")
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PathNeutralAttribute : RouteAttribute, IApiVersionNeutral
    {
        public PathNeutralAttribute(string template) : base($"/{template.TrimStart('/').TrimEnd('/')}")
        {
        }
    }
}
