using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class QueryAttribute : FromQueryAttribute
{
    public QueryAttribute(string name)
    {
        Name = name;
    }

    public QueryAttribute()
    {
    }
}

