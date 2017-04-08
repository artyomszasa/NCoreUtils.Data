using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NCoreUtils.Reflection;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Contains immutable description for the single field.
    /// </summary>
    public class FieldDescriptor : CompositeData
    {
        /// <summary>
        /// Related accessor.
        /// </summary>
        public IAccessor Accessor { get; private set; }
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.FieldDescriptor" />.
        /// </summary>
        /// <param name="factories">Partial data factories.</param>
        /// <param name="accessor">Related accessor accessor.</param>
        public FieldDescriptor(ImmutableDictionary<Type, Func<ICompositeData, IPartialData>> factories, IAccessor accessor)
            : base(factories)
        {
            RuntimeAssert.ArgumentNotNull(factories, nameof(factories));
            RuntimeAssert.ArgumentNotNull(accessor, nameof(accessor));
            Accessor = accessor;
        }
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.FieldDescriptor" />.
        /// </summary>
        /// <param name="factories">Partial data factories.</param>
        /// <param name="accessor">Related accessor accessor.</param>
        public FieldDescriptor(IEnumerable<KeyValuePair<Type, Func<ICompositeData, IPartialData>>> factories, IAccessor accessor)
            : this(factories.ToImmutableDictionary(), accessor)
        { }
    }
}