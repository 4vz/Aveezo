using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class Dev
    {
        public static string Watch(out Stopwatch stopwatch)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            return "Watch-Start";
        }

        public static string Watch(Stopwatch stopwatch)
        {
            var elapsed = stopwatch.Elapsed;
            stopwatch.Restart();
            return $"Watch-Stop: {elapsed.TotalMilliseconds} ms";
        }

    }
}
