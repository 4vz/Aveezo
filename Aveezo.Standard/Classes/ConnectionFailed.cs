using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public enum ConnectionFailReason
    {
        None,
        Unknown,
        TimeOut,
        AuthenticationFailed,
        HostUnknown
    }

}
