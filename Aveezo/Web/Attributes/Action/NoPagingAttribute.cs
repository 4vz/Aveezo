﻿using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class NoPagingAttribute : Attribute
{

}
