using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data;

/// <summary>
/// Provides data entity related rextensions for query objects.
/// </summary>
public static class LinqExtensions
{
    sealed class ExplicitPropertyVisitor : ExpressionVisitor
    {
        public static ExplicitPropertyVisitor SharedInstance { get; } = new ExplicitPropertyVisitor();

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Both method and property is preserved if related branch is reached.")]
        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Both method and property is preserved if related branch is reached.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Both method and property is preserved if related branch is reached.")]
        [UnconditionalSuppressMessage("Trimming", "IL2103", Justification = "Both method and property is preserved if related branch is reached.")]
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is not null && node.Expression.NodeType == ExpressionType.Convert
                && node.Member is PropertyInfo propertyInfo && propertyInfo.CanRead
                && propertyInfo.GetMethod is not null
                && propertyInfo.DeclaringType is not null && propertyInfo.DeclaringType.IsInterface)
            {
                var unaryExpression = (UnaryExpression)node.Expression;
                if (null == unaryExpression.Method)
                {
                    var realExpression = unaryExpression.Operand;
                    var dynamicType = realExpression.Type;
                    var mapping = dynamicType.GetInterfaceMap(propertyInfo.DeclaringType);
                    var index = Array.FindIndex(mapping.InterfaceMethods, m => m.Equals(propertyInfo.GetMethod));
                    var implementationMethod = mapping.TargetMethods[index];
                    var realPropertyInfo = dynamicType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                        .FirstOrDefault(p => p.CanRead && null != p.GetMethod && p.GetMethod.Equals(implementationMethod));
                    TargetPropertyAttribute? attribute;
                    if (null == realPropertyInfo)
                    {
                        // In F# no implementation property is created --> TargetPropertyAttribute placed on method
                        attribute = implementationMethod.GetCustomAttribute<TargetPropertyAttribute>();
                    }
                    else
                    {
                        attribute = realPropertyInfo.GetCustomAttribute<TargetPropertyAttribute>();
                    }
                    if (null != attribute)
                    {
                        var targetPropertyInfo = dynamicType.GetProperty(attribute.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (null == targetPropertyInfo)
                        {
                            throw new InvalidOperationException($"{dynamicType.FullName} has no property with name {attribute.PropertyName}.");
                        }
                        if (!node.Type.IsAssignableFrom(targetPropertyInfo.PropertyType))
                        {
                            throw new InvalidOperationException($"{dynamicType.FullName}.{attribute.PropertyName} is not compatible with {dynamicType.FullName}.{realPropertyInfo?.Name}.");
                        }
                        return Expression.Property(Visit(realExpression), targetPropertyInfo);
                    }
                    return Expression.Property(Visit(realExpression), implementationMethod);
                }
            }
            if (node.Expression != null && node.Member is PropertyInfo ownProperty && ownProperty.CanRead && null != ownProperty.GetMethod)
            {
                if (ownProperty.DeclaringType is not null && ownProperty.DeclaringType.IsInterface)
                {
                    var dynamicType = node.Expression.Type;
                    var mapping = dynamicType.GetInterfaceMap(ownProperty.DeclaringType);
                    var index = Array.FindIndex(mapping.InterfaceMethods, m => m.Equals(ownProperty.GetMethod));
                    var implementationMethod = mapping.TargetMethods[index];
                    ownProperty = dynamicType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                        .First(p => p.CanRead && null != p.GetMethod && p.GetMethod.Equals(implementationMethod));
                }
                var attribute = ownProperty.GetCustomAttribute<TargetPropertyAttribute>();
                if (null != attribute)
                {
                    var targetPropertyInfo = ownProperty.DeclaringType?.GetProperty(attribute.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var declaringTypeName = ownProperty.DeclaringType switch
                    {
                        null => "<<unknown>>",
                        var ty => ty.FullName
                    };
                    if (null == targetPropertyInfo)
                    {
                        throw new InvalidOperationException($"{declaringTypeName} has no property with name {attribute.PropertyName}.");
                    }
                    if (!node.Type.IsAssignableFrom(targetPropertyInfo.PropertyType))
                    {
                        throw new InvalidOperationException($"{declaringTypeName}.{attribute.PropertyName} is not compatible with {declaringTypeName}.{ownProperty.Name}.");
                    }
                    return Expression.Property(Visit(node.Expression), targetPropertyInfo);
                }
                return Expression.Property(Visit(node.Expression), ownProperty);
            }
            return base.VisitMember(node);
        }
    }

    /// <summary>
    /// Replaces all explicit properties with "real" properties within an expression.
    /// </summary>
    /// <param name="expression">Source expression.</param>
    /// <returns></returns>
    public static Expression<TDelegate> ReplaceExplicitProperties<TDelegate>(this Expression<TDelegate> expression)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(expression);
#else
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }
#endif
        return (Expression<TDelegate>)ExplicitPropertyVisitor.SharedInstance.Visit(expression);
    }

    /// <summary>
    /// Filters query of data entities that implements <see cref="NCoreUtils.Data.IHasId{T}" /> so that the result
    /// query contains only public data entities.
    /// </summary>
    /// <param name="source">Source query.</param>
    /// <returns>Result query.</returns>
#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be handaled by the provider.")]
#endif
    public static IQueryable<T> FilterPublic<T>(this IQueryable<T> source)
        where T : IHasState
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
#endif
        return source.Where(ReplaceExplicitProperties<Func<T, bool>>(entity => entity.State == State.Public));
    }

    /// <summary>
    /// Filters query of data entities that implements <see cref="NCoreUtils.Data.IHasId{T}" /> so that the result
    /// query contains only non-public data entities.
    /// </summary>
    /// <param name="source">Source query.</param>
    /// <returns>Result query.</returns>
#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be handaled by the provider.")]
#endif
    public static IQueryable<T> FilterNotPublic<T>(this IQueryable<T> source)
        where T : IHasState
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
#endif
        return source.Where(ReplaceExplicitProperties<Func<T, bool>>(entity => entity.State == State.NotPublic));
    }

    /// <summary>
    /// Filters query of data entities that implements <see cref="NCoreUtils.Data.IHasId{T}" /> so that the result
    /// query contains only deleted data entities.
    /// </summary>
    /// <param name="source">Source query.</param>
    /// <returns>Result query.</returns>
#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be handaled by the provider.")]
#endif
    public static IQueryable<T> FilterDeleted<T>(this IQueryable<T> source)
        where T : IHasState
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
#endif
        return source.Where(ReplaceExplicitProperties<Func<T, bool>>(entity => entity.State == State.Deleted));
    }

    /// <summary>
    /// Filters query of data entities that implements <see cref="NCoreUtils.Data.IHasId{T}" /> so that the result
    /// query contains only not-deleted data entities.
    /// </summary>
    /// <param name="source">Source query.</param>
    /// <returns>Result query.</returns>
#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be handaled by the provider.")]
#endif
    public static IQueryable<T> FilterNotDeleted<T>(this IQueryable<T> source)
        where T : IHasState
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(source);
#else
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
#endif
        return source.Where(ReplaceExplicitProperties<Func<T, bool>>(entity => entity.State != State.Deleted));
    }
}