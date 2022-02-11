using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public class AuthJwtPayload
{
    [DataMember(Name = "jti")]
    public string Id { get; set; }

    [DataMember(Name = "sco")]
    public string Scope { get; set; }

    [DataMember(Name = "aud")]
    public string ClientId { get; set; }

    [DataMember(Name = "par")]
    public string Parameters { get; set; }
}
