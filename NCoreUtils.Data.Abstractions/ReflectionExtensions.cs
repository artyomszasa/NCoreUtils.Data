using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data
{
    // FIXME: move to NCoreUtils.Extensions.Reflection
    public static class ReflectionExtensions
    {
        #if NETSTANDARD2_1
        public static bool IsNullable(this Type type, [NotNullWhen(true)] out Type? elementType)
        #else
        public static bool IsNullable(this Type type, out Type elementType)
        #endif
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
            #if NETSTANDARD2_1
            elementType = default;
            #else
            elementType = default!;
            #endif
            return false;
        }

        #if NETSTANDARD2_1
        public static bool IsDictionaryType(this Type type, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valueType)
        #else
        public static bool IsDictionaryType(this Type type, out Type keyType, out Type valueType)
        #endif
        {
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var gargs = type.GetGenericArguments();
                keyType = gargs[0];
                valueType = gargs[1];
                return true;
            }
            foreach (var itype in type.GetInterfaces())
            {
                if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var gargs = type.GetGenericArguments();
                    keyType = gargs[0];
                    valueType = gargs[1];
                    return true;
                }
            }
            #if NETSTANDARD2_1
            keyType = default;
            valueType = default;
            #else
            keyType = default!;
            valueType = default!;
            #endif
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullable(this Type type)
            => type.IsNullable(out var _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDictionaryType(this Type type)
            => type.IsDictionaryType(out var _, out var _);
    }
}