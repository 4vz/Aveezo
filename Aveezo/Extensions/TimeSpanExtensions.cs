using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Aveezo
{
    public static class TimeSpanExtensions
    {
        public static string ToISO8601(this TimeSpan value) => XmlConvert.ToString(value);
    }
}
