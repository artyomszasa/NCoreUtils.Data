using System;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Allows specifying "real" property then some interface defined property has explicit implementation. In this
    /// case target property is used when creating queries.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class TargetPropertyAttribute : Attribute
    {
        /// <summary>
        /// Name of the "real" property.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Initializes new instance of <see cref="NCoreUtils.Data.TargetPropertyAttribute" /> with the specified
        /// "real" property name.
        /// </summary>
        /// <param name="propertyName">
        /// Name of the property to be used in expressions instead of the actual property.
        /// </param>
        public TargetPropertyAttribute(string propertyName)
            => PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    }
}