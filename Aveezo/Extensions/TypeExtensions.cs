using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class TypeExtensions
    {
        public static bool Has<T>(this Type type) where T : Attribute => type.Has(out T _, false);

        public static bool Has<T>(this Type type, bool inherit) where T : Attribute => type.Has(out T _, inherit);

        public static bool Has<T>(this Type type, out T attribute) where T : Attribute => type.Has(out attribute, false);

        public static bool Has<T>(this Type type, out T attribute, bool inherit) where T : Attribute
        {
            attribute = null;

            foreach (var item in type.GetCustomAttributes(inherit))
            {
                if (item is T at)
                {
                    attribute = at;
                    break;
                }
            }
            return attribute != null;
        }        

        public static bool IsDictionary(this Type type, out Type key, out Type value)
        {
            key = null;
            value = null;

            if (type == null) return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                key = type.GetGenericArguments()[0];
                value = type.GetGenericArguments()[1];
                return true;
            }
            else
                return false;
        }

        public static bool IsDictionary(this Type type) => type.IsDictionary(out _, out _);

        public static bool IsDictionaryWithKeyType(this Type type, Type key) => type.IsDictionary(out Type keyType, out _) && keyType == key;

        public static bool IsDictionaryWithValueType(this Type type, Type value) => type.IsDictionary(out _, out Type valueType) && valueType == value;

        public static bool IsDictionary(this Type type, Type key, Type value) => type.IsDictionaryWithKeyType(key) && type.IsDictionaryWithValueType(value);

        public static bool IsList(this Type type, out Type value)
        {
            value = null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                value = type.GetGenericArguments()[0];
                return true;
            }
            else
                return false;
        }

        public static bool IsString(this Type type) => type == typeof(string);

        public static bool IsIntegral(this Type type) =>
            type == typeof(sbyte) ||
            type == typeof(byte) ||
            type == typeof(short) ||
            type == typeof(ushort) ||
            type == typeof(int) ||
            type == typeof(uint) ||
            type == typeof(long) ||
            type == typeof(ulong) ||
            type == typeof(char);

        public static bool IsFloatingPoint(this Type type) =>
            type == typeof(float) ||
            type == typeof(double);

        public static bool IsNumeric(this Type type) =>
            IsIntegral(type) ||
            IsFloatingPoint(type) ||
            type == typeof(decimal);

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType) => givenType.IsAssignableToGenericType(genericType, out var _);

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType, out Type[] typeArguments)
        {
            typeArguments = null;
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                {
                    typeArguments = givenType.GetGenericArguments();
                    return true;
                }
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                typeArguments = givenType.GetGenericArguments();
                return true;
            }

            Type baseType = givenType.BaseType;
            if (baseType == null)
            {
                return false;
            }
            else
            {
                return IsAssignableToGenericType(baseType, genericType, out typeArguments);
            }
        }

    }
}
