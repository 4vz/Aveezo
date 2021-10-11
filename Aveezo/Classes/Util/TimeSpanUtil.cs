using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Aveezo
{
    public static class TimeSpanUtil
    {
        public static TimeSpan Parse(string iso8601value)
        {
            var timeSpan = XmlConvert.ToTimeSpan(iso8601value);

            return timeSpan;
        }
    }
}
