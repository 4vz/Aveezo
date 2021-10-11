using System;
using System.Collections.Generic;
using System.Text;

namespace Aveezo
{
    public enum SshConnectionFailReason
    {
        None,
        Unknown,
        TimeOut,
        AuthenticationFailed,
        HostUnreachable,
        HostUnknown
    }

}
