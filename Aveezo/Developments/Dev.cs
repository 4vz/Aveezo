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
            return Watch(stopwatch, 0);
        }

        public static string Watch(Stopwatch stopwatch, double substract)
        {
            return Watch(stopwatch, substract, out _);
        }

        public static string Watch(Stopwatch stopwatch, double substract, out double totalMs)
        {
            totalMs = stopwatch.Elapsed.TotalMilliseconds;
            var netMs = totalMs - substract;
            stopwatch.Restart();
            return $"Watch-Stop: {netMs} ms";
        }
    }
}
