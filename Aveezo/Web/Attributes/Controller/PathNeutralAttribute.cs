
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PathNeutralAttribute : RouteAttribute, IApiVersionNeutral
{
    public PathNeutralAttribute(string template) : base($"/{template.TrimStart('/').TrimEnd('/')}")
    {
    }
}
