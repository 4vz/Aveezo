using System;
using System.Collections.Generic;

using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aveezo
{
    public class NamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToSnakeCase();
    }
}
