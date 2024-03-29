﻿
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PathAttribute : RouteAttribute
{
    public PathAttribute(string template) : base($"/v{{version:apiVersion}}/{template.TrimStart('/').TrimEnd('/')}")
    {
    }
}