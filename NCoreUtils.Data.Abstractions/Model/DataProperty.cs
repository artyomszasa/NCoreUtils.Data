using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using NCoreUtils.Data.Build;

namespace NCoreUtils.Data.Model
{
    public abstract class DataProperty : Metadata
    {
        public string Name => TryGetValue(CommonMetadata.Name, out var boxed) && boxed is string name
            ? name
            : Property.Name;

        public int? MinLength => TryGetValue(CommonMetadata.MinLength, out var boxed) && boxed is int minLength
            ? minLength
            : new int?();

        public int? MaxLength => TryGetValue(CommonMetadata.MaxLength, out var boxed) && boxed is int maxLength
            ? maxLength
            : new int?();

        public bool? Required => TryGetValue(CommonMetadata.Required, out var boxed) && boxed is bool required
            ? required
            : new bool?();

        public bool? Unicode => TryGetValue(CommonMetadata.Unicode, out var boxed) && boxed is bool unicode
            ? unicode
            : new bool?();

        public PropertyInfo Property { get; }

        protected DataProperty(PropertyInfo property, IReadOnlyDictionary<string, object?> data)
            : base(data)
            => Property = property ?? throw new ArgumentNullException(nameof(property));

        public bool TryGetDefaultValue(out object? value)
            => TryGetValue(CommonMetadata.DefaultValue, out value);
    }

    public class DataProperty<T> : DataProperty
    {
        public DataProperty(DataPropertyBuilder<T> builder)
            : base(builder.Property, builder.Metadata.ToImmutableDictionary())
        { }
    }
}