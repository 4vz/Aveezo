using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            return "Dev.Watch START";
        }

        public static string Watch(Stopwatch stopwatch)
        {
            var elapsed = stopwatch.Elapsed;
            stopwatch.Restart();
            return $"Dev.Watch WATCH Elapsed: {elapsed.TotalMilliseconds} ms";
        }

    }
}
