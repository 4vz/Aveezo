using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

[PathNeutral("/util")]
public class Util : Api
{
    public Util(IServiceProvider i) : base(i) { }

    [Get("urlbase64/{guid}")]
    public Method<string> UrlBase64(Guid guid)
    {
        return Base64.UrlEncode(guid);
    }
}
