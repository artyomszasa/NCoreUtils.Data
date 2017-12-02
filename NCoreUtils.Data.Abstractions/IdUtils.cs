using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Busmiess key manupulation helper.
    /// </summary>
    public static class IdUtils
    {
        static class GenericIdCheck<T>
        {
            static readonly Func<T, bool> _check;

            static GenericIdCheck()
            {
                _check = _idChecks.TryGetValue(typeof(T), out var check) ? (Func<T, bool>)check : throw new InvalidOperationException($"No invalid id check defined for {typeof(T).FullName}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            public static bool IsValid(T id) => _check(id);
        }

        static readonly ImmutableDictionary<Type, Delegate> _idChecks = new Dictionary<Type, Delegate>
        {
            { typeof(sbyte), new Func<sbyte, bool>(id => id > 0) },
            { typeof(short), new Func<short, bool>(id => id > 0) },
            { typeof(int), new Func<int, bool>(id => id > 0) },
            { typeof(long), new Func<long, bool>(id => id > 0) },
            { typeof(string), new Func<string, bool>(id => id != null) },
        }.ToImmutableDictionary();

        /// <summary>
        /// Checks whether the specified business key is valid.
        /// </summary>
        /// <param name="id">Business key to check.</param>
        /// <returns>
        /// <c>true</c> if business key is valid, <c>false</c> otherwise.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool IsValidId<T>(T id) => GenericIdCheck<T>.IsValid(id);

        /// <summary>
        /// Checks whether the specified data entity business key is valid i.e. has been assigned.
        /// </summary>
        /// <param name="entity">Data entity to check the business key of.</param>
        /// <returns>
        /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool HasValidId(this IHasId<short> entity) => entity.Id > 0;

        /// <summary>
        /// Checks whether the specified data entity business key is valid i.e. has been assigned.
        /// </summary>
        /// <param name="entity">Data entity to check the business key of.</param>
        /// <returns>
        /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool HasValidId(this IHasId<int> entity) => entity.Id > 0;

        /// <summary>
        /// Checks whether the specified data entity business key is valid i.e. has been assigned.
        /// </summary>
        /// <param name="entity">Data entity to check the business key of.</param>
        /// <returns>
        /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool HasValidId(this IHasId<long> entity) => entity.Id > 0L;

        /// <summary>
        /// Checks whether the specified data entity business key is valid i.e. has been assigned.
        /// </summary>
        /// <param name="entity">Data entity to check the business key of.</param>
        /// <returns>
        /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool HasValidId<T>(this IHasId<T> entity) => IsValidId<T>(entity.Id);

        /// <summary>
        /// Attempts to get business key type from data entity type.
        /// </summary>
        /// <param name="entityType">Data entity type.</param>
        /// <param name="idType">Variable to store business key type.</param>
        /// <returns>
        /// <c>true</c> if the specified type implements <see cref="NCoreUtils.Data.IHasId{T}" /> and the business
        /// key type has been stored into <paramref name="idType" />, <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetIdType(Type entityType, out Type idType)
        {
            foreach (var interfaceType in entityType.GetInterfaces())
            {
                if (interfaceType.IsConstructedGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IHasId<>))
                {
                    idType = interfaceType.GenericTypeArguments[0];
                    return true;
                }
            }
            if (entityType.IsConstructedGenericType && entityType.GetGenericTypeDefinition() == typeof(IHasId<>))
            {
                idType = entityType.GenericTypeArguments[0];
                return true;
            }
            idType = default(Type);
            return false;
        }

        /// <summary>
        /// Attempts to get interface mapping between the specified data entity type and
        /// <see cref="NCoreUtils.Data.IHasId{T}" />.
        /// </summary>
        /// <param name="entityType">Data entity type.</param>
        /// <param name="interfaceMapping">Variable to store interface mapping.</param>
        /// <returns>
        /// <c>true</c> if the specified type implements <see cref="NCoreUtils.Data.IHasId{T}" /> and the mapping
        /// has been stored into <paramref name="interfaceMapping" />, <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetInterfaceMap(Type entityType, out InterfaceMapping interfaceMapping)
        {
            if (!TryGetIdType(entityType, out var idType))
            {
                interfaceMapping = default(InterfaceMapping);
                return false;
            }
            var interfaceType = typeof(IHasId<>).MakeGenericType(idType);
            interfaceMapping = entityType.GetInterfaceMap(interfaceType);
            return true;
        }

        /// <summary>
        /// Attempts to get business key property defined by <see cref="NCoreUtils.Data.IHasId{T}" /> from data
        /// entity type. On success interface implementation property is returned even if the interface is implemented
        /// explicitly. To get property that can be used in linq expressions use
        /// <see cref="M:NCoreUtils.Data.TryGetRealIdProperty" /> instead.
        /// </summary>
        /// <param name="entityType">Data entity type.</param>
        /// <param name="property">Variable to store property.</param>
        /// <returns>
        /// <c>true</c> if the specified type implements <see cref="NCoreUtils.Data.IHasId{T}" /> and the property
        /// has been stored into <paramref name="property" />, <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetIdProperty(Type entityType, out PropertyInfo property)
        {
            if (!TryGetInterfaceMap(entityType, out var interfaceMapping))
            {
                property = default(PropertyInfo);
                return false;
            }
            var index = Array.FindIndex(interfaceMapping.InterfaceMethods, method => StringComparer.InvariantCultureIgnoreCase.Equals("get_Id", method.Name));
            var getter = interfaceMapping.TargetMethods[index];
            var prop = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(p => p.CanRead && null != p.GetMethod && p.GetMethod.Equals(getter));
            if (null == prop)
            {
                property = default(PropertyInfo);
                return false;
            }
            property = prop;
            return false;
        }

        /// <summary>
        /// Attempts to get business key property defined by <see cref="NCoreUtils.Data.IHasId{T}" /> from data
        /// entity type. On success linq expression safe property is returned. To get interface implementation property
        /// use <see cref="M:NCoreUtils.Data.TryGetIdProperty" /> instead.
        /// </summary>
        /// <param name="entityType">Data entity type.</param>
        /// <param name="property">Variable to store property.</param>
        /// <returns>
        /// <c>true</c> if the specified type implements <see cref="NCoreUtils.Data.IHasId{T}" /> and the property
        /// has been stored into <paramref name="property" />, <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetRealIdProperty(Type entityType, out PropertyInfo property)
        {
            if (TryGetIdProperty(entityType, out var interfaceProperty))
            {
                var attr = interfaceProperty.GetCustomAttribute<TargetPropertyAttribute>();
                property = null == attr ? interfaceProperty : entityType.GetProperty(attr.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return true;
            }
            property = default(PropertyInfo);
            return true;
        }

        /// <summary>
        /// Created business key equality predicate for the specified value.
        /// </summary>
        /// <param name="value">Value of the business key.</param>
        /// <returns>Predicate expression.</returns>
        public static Expression<Func<TEntity, bool>> CreateIdEqualsPredicate<TEntity, TId>(TId value)
            where TEntity : IHasId<TId>
        {
            Expression<Func<TEntity, TId>> idAccess = e => e.Id;
            var expressionParameter = Expression.Parameter(typeof(TEntity));
            return LinqExtensions.ReplaceExplicitProperties(Expression.Lambda<Func<TEntity, bool>>(
                Expression.Equal(
                    idAccess.Body.SubstituteParameter(idAccess.Parameters[0], expressionParameter),
                    Expression.Constant(value)
                ),
                expressionParameter
            ));
        }
    }
}