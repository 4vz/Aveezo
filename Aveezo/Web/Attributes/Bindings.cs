using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class BodyAttribute : FromBodyAttribute
    {
    }

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

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class ArgumentAttribute : FromRouteAttribute
    {
        public ArgumentAttribute(string name)
        {
            Name = name;
        }
    }

}
