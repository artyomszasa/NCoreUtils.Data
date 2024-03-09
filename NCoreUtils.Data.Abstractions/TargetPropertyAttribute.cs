using System;

namespace NCoreUtils.Data;

/// <summary>
/// Allows specifying "real" property then some interface defined property has explicit implementation. In this
/// case target property is used when creating queries.
/// </summary>
/// <remarks>
/// Initializes new instance of <see cref="TargetPropertyAttribute" /> with the specified
/// "real" property name.
/// </remarks>
/// <param name="propertyName">
/// Name of the property to be used in expressions instead of the actual property.
/// </param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class TargetPropertyAttribute(string propertyName) : Attribute
{
    /// <summary>
    /// Name of the "real" property.
    /// </summary>
    public string PropertyName { get; } = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
}