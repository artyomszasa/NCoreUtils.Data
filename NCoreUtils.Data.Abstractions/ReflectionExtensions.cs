using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data
{
    // FIXME: move to NCoreUtils.Extensions.Reflection
    public static class ReflectionExtensions
    {
        public static bool IsNullable(this Type type, [NotNullWhen(true)] out Type? elementType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
            elementType = default;
            return false;
        }

        public static bool IsDictionaryType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type,
            [NotNullWhen(true)] out Type? keyType,
            [NotNullWhen(true)] out Type? valueType)
        {
            if (type.IsInterface && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var gargs = type.GetGenericArguments();
                keyType = gargs[0];
                valueType = gargs[1];
                return true;
            }
            foreach (var itype in type.GetInterfaces())
            {
                if (itype.IsInterface && itype.IsConstructedGenericType && itype.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var gargs = itype.GetGenericArguments();
                    keyType = gargs[0];
                    valueType = gargs[1];
                    return true;
                }
            }
            keyType = default;
            valueType = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullable(this Type type)
            => type.IsNullable(out var _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDictionaryType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type)
            => type.IsDictionaryType(out var _, out var _);
    }
}