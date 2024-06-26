using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data;

/// <summary>
/// Busmiess key manupulation helper.
/// </summary>
public static class IdUtils
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private sealed class IdBox<T>(T value)
    {
        public T Value = value;
    }

    private static class GenericIdCheck<T>
    {
        private static readonly Func<T, bool> _check;

        [ExcludeFromCodeCoverage]
        static GenericIdCheck()
        {
            _check = _idChecks.TryGetValue(typeof(T), out var check) ? (Func<T, bool>)check : throw new InvalidOperationException($"No invalid id check defined for {typeof(T).FullName}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool IsValid(T id) => _check(id);
    }

    private static readonly ImmutableDictionary<Type, Delegate> _idChecks = new Dictionary<Type, Delegate>
    {
        { typeof(Guid), new Func<Guid, bool>(IsValidId) },
        { typeof(byte), new Func<byte, bool>(IsValidId) },
        { typeof(sbyte), new Func<sbyte, bool>(IsValidId) },
        { typeof(short), new Func<short, bool>(IsValidId) },
        { typeof(int), new Func<int, bool>(IsValidId) },
        { typeof(long), new Func<long, bool>(IsValidId) },
        { typeof(string), new Func<string, bool>(IsValidId) },
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
    public static bool IsValidId(Guid id) => id != Guid.Empty;

    /// <summary>
    /// Checks whether the specified business key is valid.
    /// </summary>
    /// <param name="id">Business key to check.</param>
    /// <returns>
    /// <c>true</c> if business key is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool IsValidId(byte id) => id > 0;

    /// <summary>
    /// Checks whether the specified business key is valid.
    /// </summary>
    /// <param name="id">Business key to check.</param>
    /// <returns>
    /// <c>true</c> if business key is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool IsValidId(sbyte id) => id > 0;

    /// <summary>
    /// Checks whether the specified business key is valid.
    /// </summary>
    /// <param name="id">Business key to check.</param>
    /// <returns>
    /// <c>true</c> if business key is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool IsValidId(short id) => id > 0;

    /// <summary>
    /// Checks whether the specified business key is valid.
    /// </summary>
    /// <param name="id">Business key to check.</param>
    /// <returns>
    /// <c>true</c> if business key is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool IsValidId(int id) => id > 0;

    /// <summary>
    /// Checks whether the specified business key is valid.
    /// </summary>
    /// <param name="id">Business key to check.</param>
    /// <returns>
    /// <c>true</c> if business key is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool IsValidId(long id) => id > 0;

    /// <summary>
    /// Checks whether the specified business key is valid.
    /// </summary>
    /// <param name="id">Business key to check.</param>
    /// <returns>
    /// <c>true</c> if business key is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool IsValidId(string? id) => !string.IsNullOrEmpty(id);

    /// <summary>
    /// Checks whether the specified business key is valid.
    /// </summary>
    /// <param name="id">Business key to check.</param>
    /// <returns>
    /// <c>true</c> if business key is valid, <c>false</c> otherwise.
    /// </returns>
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
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
    public static bool HasValidId(this IHasId<Guid> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Checks whether the specified data entity business key is valid i.e. has been assigned.
    /// </summary>
    /// <param name="entity">Data entity to check the business key of.</param>
    /// <returns>
    /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool HasValidId(this IHasId<byte> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Checks whether the specified data entity business key is valid i.e. has been assigned.
    /// </summary>
    /// <param name="entity">Data entity to check the business key of.</param>
    /// <returns>
    /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool HasValidId(this IHasId<sbyte> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Checks whether the specified data entity business key is valid i.e. has been assigned.
    /// </summary>
    /// <param name="entity">Data entity to check the business key of.</param>
    /// <returns>
    /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool HasValidId(this IHasId<short> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Checks whether the specified data entity business key is valid i.e. has been assigned.
    /// </summary>
    /// <param name="entity">Data entity to check the business key of.</param>
    /// <returns>
    /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool HasValidId(this IHasId<int> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Checks whether the specified data entity business key is valid i.e. has been assigned.
    /// </summary>
    /// <param name="entity">Data entity to check the business key of.</param>
    /// <returns>
    /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool HasValidId(this IHasId<long> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Checks whether the specified data entity business key is valid i.e. has been assigned.
    /// </summary>
    /// <param name="entity">Data entity to check the business key of.</param>
    /// <returns>
    /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool HasValidId(this IHasId<string> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Checks whether the specified data entity business key is valid i.e. has been assigned.
    /// </summary>
    /// <param name="entity">Data entity to check the business key of.</param>
    /// <returns>
    /// <c>true</c> if business key of the specified data entity is valid, <c>false</c> otherwise.
    /// </returns>
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    [DebuggerStepThrough]
    public static bool HasValidId<T>(this IHasId<T> entity) => IsValidId(entity.Id);

    /// <summary>
    /// Attempts to get business key type from data entity type.
    /// </summary>
    /// <param name="entityType">Data entity type.</param>
    /// <param name="idType">Variable to store business key type.</param>
    /// <returns>
    /// <c>true</c> if the specified type implements <see cref="NCoreUtils.Data.IHasId{T}" /> and the business
    /// key type has been stored into <paramref name="idType" />, <c>false</c> otherwise.
    /// </returns>
    public static bool TryGetIdType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type entityType,
        [NotNullWhen(true)] out Type? idType)
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
        idType = default;
        return false;
    }

#if NET7_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Common types are handled, other types should be preserved by the root type annotations.")]
#else
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Interface types are preserved due to the root type annotation.")]
#endif
    private static Type GetOrMakeGetericIHasIdType(Type idType)
    {
        if (idType == typeof(int))
        {
            return typeof(IHasId<int>);
        }
        if (idType == typeof(string))
        {
            return typeof(IHasId<string>);
        }
        if (idType == typeof(Guid))
        {
            return typeof(IHasId<Guid>);
        }
        if (idType == typeof(long))
        {
            return typeof(IHasId<long>);
        }
        if (idType == typeof(short))
        {
            return typeof(IHasId<short>);
        }
        if (idType == typeof(byte))
        {
            return typeof(IHasId<byte>);
        }
        if (idType == typeof(sbyte))
        {
            return typeof(IHasId<sbyte>);
        }
        return typeof(IHasId<>).MakeGenericType(idType);
    }

    /// <summary>
    /// Attempts to get interface mapping between the specified data entity type and
    /// <see cref="IHasId{T}" />.
    /// </summary>
    /// <param name="entityType">Data entity type.</param>
    /// <param name="interfaceMapping">Variable to store interface mapping.</param>
    /// <returns>
    /// <c>true</c> if the specified type implements <see cref="IHasId{T}" /> and the mapping
    /// has been stored into <paramref name="interfaceMapping" />, <c>false</c> otherwise.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IHasId<Guid>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IHasId<byte>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IHasId<sbyte>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IHasId<short>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IHasId<int>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IHasId<long>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IHasId<string>))]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Common types are handled, other types should be preserved by the root type annotations.")]
    public static bool TryGetInterfaceMap(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
            Type entityType,
        out InterfaceMapping interfaceMapping)
    {
        if (!TryGetIdType(entityType, out var idType))
        {
            interfaceMapping = default;
            return false;
        }
        var interfaceType = GetOrMakeGetericIHasIdType(idType);
        interfaceMapping = entityType.GetInterfaceMap(interfaceType);
        return true;
    }

    /// <summary>
    /// Attempts to get business key property defined by <see cref="IHasId{T}" /> from data
    /// entity type. On success interface implementation property is returned even if the interface is implemented
    /// explicitly. To get property that can be used in linq expressions use
    /// <see cref="TryGetRealIdProperty" /> instead.
    /// </summary>
    /// <param name="entityType">Data entity type.</param>
    /// <param name="property">Variable to store property.</param>
    /// <returns>
    /// <c>true</c> if the specified type implements <see cref="IHasId{T}" /> and the property
    /// has been stored into <paramref name="property" />, <c>false</c> otherwise.
    /// </returns>
    public static bool TryGetIdProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            Type entityType,
        [NotNullWhen(true)] out PropertyInfo? property)
    {
        if (!TryGetInterfaceMap(entityType, out var interfaceMapping))
        {
            property = default;
            return false;
        }
        var index = Array.FindIndex(interfaceMapping.InterfaceMethods, method => StringComparer.InvariantCultureIgnoreCase.Equals("get_Id", method.Name));
        var getter = interfaceMapping.TargetMethods[index];
        var prop = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(p => p.CanRead && null != p.GetMethod && p.GetMethod.Equals(getter));
        property = prop;
        return prop != null;
    }

    /// <summary>
    /// Attempts to get business key property defined by <see cref="IHasId{T}" /> from data
    /// entity type. On success linq expression safe property is returned. To get interface implementation property
    /// use <see cref="TryGetIdProperty" /> instead.
    /// </summary>
    /// <param name="entityType">Data entity type.</param>
    /// <param name="property">Variable to store property.</param>
    /// <returns>
    /// <c>true</c> if the specified type implements <see cref="IHasId{T}" /> and the property
    /// has been stored into <paramref name="property" />, <c>false</c> otherwise.
    /// </returns>
    public static bool TryGetRealIdProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.NonPublicProperties
            | DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
            Type entityType,
        [NotNullWhen(true)] out PropertyInfo? property)
    {
        if (TryGetIdProperty(entityType, out var interfaceProperty))
        {
            var attr = interfaceProperty.GetCustomAttribute<TargetPropertyAttribute>();
            if (attr is null)
            {
                property = interfaceProperty;
                return true;
            }
            var realProperty = entityType.GetProperty(attr.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (realProperty is null)
            {
                property = default;
                return false;
            }
            property = realProperty;
            return true;
        }
        property = default;
        return false;
    }

    /// <summary>
    /// Created business key equality predicate for the specified value.
    /// </summary>
    /// <param name="value">Value of the business key.</param>
    /// <returns>Predicate expression.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "IdBox<> has necessary attributes.")]
    public static Expression<Func<TEntity, bool>> CreateIdEqualsPredicate<TEntity, TId>(TId value)
        where TEntity : IHasId<TId>
    {
        Expression<Func<TEntity, TId>> idAccess = e => e.Id;
        // see: https://github.com/aspnet/EntityFrameworkCore/issues/8909
        // see: https://github.com/aspnet/EntityFrameworkCore/issues/10535
        // instead of constant member access expression must be generated to avoid cache issues.
        var idBox = new IdBox<TId>(value);
        var idExpression = Expression.Field(Expression.Constant(idBox), nameof(IdBox<int>.Value));
        var expressionParameter = Expression.Parameter(typeof(TEntity));
        return LinqExtensions.ReplaceExplicitProperties(Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(
                idAccess.Body.SubstituteParameter(idAccess.Parameters[0], expressionParameter),
                idExpression
            ),
            expressionParameter
        ));
    }
}