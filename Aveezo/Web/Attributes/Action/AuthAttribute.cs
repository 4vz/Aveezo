
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthAttribute : Attribute
{
    #region Fields

    public int Level { get; } = 0;

    #endregion

    #region Constructors

    public AuthAttribute(int level)
    {
        if (level > 0)
            Level = level;
        else if (level == 0)
            throw new Exception("Auth(0) is enabled by default");
        else
            throw new ArgumentOutOfRangeException(nameof(level));
    }

    #endregion
}
