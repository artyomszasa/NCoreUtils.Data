using NCoreUtils.Reflection;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Mutable composite data builder that can be converted into immutable
    /// <see cref="T:NCoreUtils.Data.FieldDescriptor" />.
    /// </summary>
    public sealed class FieldDescriptorBuilder : CompositeDataBuilder
    {
        /// <summary>
        /// Related accessor.
        /// </summary>
        public IAccessor Accessor { get; private set; }
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.FieldDescriptorBuilder" />.
        /// </summary>
        /// <param name="accessor">Related accessor.</param>
        public FieldDescriptorBuilder(IAccessor accessor)
        {
            RuntimeAssert.ArgumentNotNull(accessor, nameof(accessor));
            Accessor = accessor;
        }
        /// <summary>
        /// Builds new instance of <see cref="T:NCoreUtils.Data.FieldDescriptor" /> from defined related accessor and
        /// partial data added to the actual instance.
        /// </summary>
        /// <returns>Newly created instance of <see cref="T:NCoreUtils.Data.FieldDescriptor" />.</returns>
        public FieldDescriptor Build() => new FieldDescriptor(Factories, Accessor);
    }
}