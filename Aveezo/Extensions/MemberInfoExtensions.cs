using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class MemberInfoExtensions
    {
        public static bool Has<T>(this MemberInfo info) where T : Attribute => info.Has(out T _, false);

        public static bool Has<T>(this MemberInfo info, bool inherit) where T : Attribute => info.Has(out T _, inherit);

        public static bool Has<T>(this MemberInfo info, out T attribute) where T : Attribute => info.Has(out attribute, false);

        public static bool Has<T>(this MemberInfo info, out T attribute, bool inherit) where T : Attribute
        {
            if (info == null) throw new ArgumentNullException("info");

            attribute = null;

            foreach (var item in info.GetCustomAttributes(inherit))
            {
                if (item is T at)
                {
                    attribute = at;
                    break;
                }
            }
            return attribute != null;
        }

        public static bool Has<T>(this MemberInfo[] infos) where T : Attribute => infos.Has(out T _, false);

        public static bool Has<T>(this MemberInfo[] infos, bool inherit) where T : Attribute => infos.Has(out T _, inherit);

        public static bool Has<T>(this MemberInfo[] infos, out T attribute) where T : Attribute => infos.Has(out attribute, false);

        public static bool Has<T>(this MemberInfo[] infos, out T attribute, bool inherit) where T : Attribute
        {
            if (infos != null && infos.Length > 0)
            {
                return infos[0].Has(out attribute, inherit);
            }
            else
            {
                attribute = null;
                return false;
            }
        }
    }
}
