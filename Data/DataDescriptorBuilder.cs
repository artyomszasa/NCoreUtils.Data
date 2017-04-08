using System;
using System.Collections.Generic;
using NCoreUtils.Reflection;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Used to build data description for a type.
    /// </summary>
    public sealed class DataDescriptorBuilder : CompositeDataBuilder
    {
        readonly List<FieldDescriptor> _fields = new List<FieldDescriptor>();
        /// <summary>
        /// Initializes and adds field definition to the builder.
        /// </summary>
        /// <param name="accessor">Target accessor.</param>
        /// <param name="init">Initialization routine.</param>
        /// <returns>The actual instance for chaining.</returns>
        public DataDescriptorBuilder AddField(IAccessor accessor, Action<FieldDescriptorBuilder> init)
        {
            var fieldBuilder = new FieldDescriptorBuilder(accessor);
            init?.Invoke(fieldBuilder);
            _fields.Add(fieldBuilder.Build());
            return this;
        }
    }
}