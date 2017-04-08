using System;
using System.Collections.Immutable;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Contains definition for single type.
    /// </summary>
    public class DataDescriptor : CompositeData
    {
        /// <summary>
        /// Associated field descriptions.
        /// </summary>
        /// <returns></returns>
        public ImmutableArray<FieldDescriptor> Fields { get; private set; }
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.DataDescriptor" />.
        /// </summary>
        /// <param name="factories">Partial data factories.</param>
        /// <param name="fields">Field descriptions.</param>
        public DataDescriptor(ImmutableDictionary<Type, Func<ICompositeData, IPartialData>> factories, ImmutableArray<FieldDescriptor> fields)
            : base(factories)
        {
            Fields = fields;
        }
    }
}