using System;
using System.Collections.Generic;

using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class MemberInfoExtensions
    {
        public static bool Has<T>(this MemberInfo info) where T : Attribute => info.Has(out Values<T> _, false);

        public static bool Has<T>(this MemberInfo info, bool inherit) where T : Attribute => info.Has(out Values<T> _, inherit);

        public static bool Has<T>(this MemberInfo info, out Values<T> attributes) where T : Attribute => info.Has(out attributes, false);

        public static bool Has<T>(this MemberInfo info, out Values<T> attributes, bool inherit) where T : Attribute
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var list = new List<T>();

            foreach (var attribute in info.GetCustomAttributes(inherit))
            {
                if (attribute is T at)
                {
                    list.Add(at);
                }
            }

            if (list.Count > 0)
                attributes = list.ToArray();
            else
                attributes = null;

            return attributes != null;
        }

        public static bool HasGenericAttributes<T>(this MemberInfo info, out Values<GenericAttribute> attributes) where T : Attribute => info.HasGenericAttributes<T>(out attributes, false);

        public static bool HasGenericAttributes<T>(this MemberInfo info, out Values<GenericAttribute> attributes, bool inherit) where T : Attribute
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var list = new List<GenericAttribute>();

            var hasType = typeof(T);
            var hasGenericType = hasType.IsGenericType ? hasType.GetGenericTypeDefinition() : null;
            var hasGenericTypeArguments = hasType.IsGenericType ? hasType.GetGenericArguments() : null;

            foreach (var attribute in info.GetCustomAttributes(inherit))
            {
                if (attribute is T at)
                {
                    list.Add(new GenericAttribute
                    {
                        Type = hasType,
                        Instance = at
                    });;
                }
                else
                {
                    // workaround for generic type attribute
                    var attributeType = attribute.GetType();

                    if (hasGenericType != null && attributeType.IsGenericType && attributeType.IsAssignableToGenericType(hasGenericType))
                    {
                        var hasGenericTypeFirstType = hasGenericTypeArguments[0];
                        var attributeGenericTypeFirstType = attributeType.GetGenericArguments()[0];

                        if (attributeGenericTypeFirstType.IsAssignableTo(hasGenericTypeFirstType))
                        {
                            var i = attribute as Attribute;

                            list.Add(new GenericAttribute
                            {
                                Type = i.GetType(),
                                Instance = i
                            });
                        }
                    }
                }
            }

            if (list.Count > 0)
                attributes = list.ToArray();
            else
                attributes = null;

            return attributes != null;
        }

        public static bool Has<T>(this MemberInfo[] infos) where T : Attribute => infos.Has(out Values<T> _, false);

        public static bool Has<T>(this MemberInfo[] infos, bool inherit) where T : Attribute => infos.Has(out Values<T> _, inherit);

        public static bool Has<T>(this MemberInfo[] infos, out Values<T> attribute) where T : Attribute => infos.Has(out attribute, false);

        public static bool Has<T>(this MemberInfo[] infos, out Values<T> attribute, bool inherit) where T : Attribute
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

    public class GenericAttribute : Attribute
    {
        public Attribute Instance { get; init; }

        public Type Type { get; init; }
    }
}
