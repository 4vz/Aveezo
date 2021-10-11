using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class Is
    {
        public static bool Null<T>(params T[] objects) where T : class
        {
            foreach (var obj in objects) if (obj == null) return true;
            return false;
        }
    }
}
