using System;
using System.Reflection;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Build
{
    public abstract class DataPropertyBuilder : MetadataBuilder
    {
        public PropertyInfo Property { get; }

        public DataPropertyBuilder(PropertyInfo property)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        internal abstract DataProperty Build();

        public new DataPropertyBuilder SetMetadata(string key, object? value)
        {
            base.SetMetadata(key, value);
            return this;
        }

        public DataPropertyBuilder SetName(string? value)
            => SetMetadata(CommonMetadata.Name, value);

        public DataPropertyBuilder SetMinLength(int? value)
            => SetMetadata(CommonMetadata.MinLength, value);

        public DataPropertyBuilder SetMaxLength(int? value)
            => SetMetadata(CommonMetadata.MaxLength, value);

        public DataPropertyBuilder SetRequired(bool value = true)
            => SetMetadata(CommonMetadata.Required, value);

        public DataPropertyBuilder SetUnicode(bool value = true)
            => SetMetadata(CommonMetadata.Unicode, value);
    }

    public class DataPropertyBuilder<T> : DataPropertyBuilder
    {
        public DataPropertyBuilder(PropertyInfo property) : base(property) { }

        public new DataPropertyBuilder<T> SetMetadata(string key, object? value)
        {
            base.SetMetadata(key, value);
            return this;
        }

        internal override DataProperty Build() => new DataProperty<T>(this);

        public new DataPropertyBuilder<T> SetName(string? value)
            => SetMetadata(CommonMetadata.Name, value);

        public new DataPropertyBuilder<T> SetMinLength(int? value)
            => SetMetadata(CommonMetadata.MinLength, value);

        public new DataPropertyBuilder<T> SetMaxLength(int? value)
            => SetMetadata(CommonMetadata.MaxLength, value);

        public new DataPropertyBuilder<T> SetRequired(bool value = true)
            => SetMetadata(CommonMetadata.Required, value);

        public new DataPropertyBuilder<T> SetUnicode(bool value = true)
            => SetMetadata(CommonMetadata.Unicode, value);
    }
}