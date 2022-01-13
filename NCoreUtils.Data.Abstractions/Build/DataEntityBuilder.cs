using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Build
{
    public abstract class DataEntityBuilder : MetadataBuilder
    {
        public Type EntityType { get; }

        public IReadOnlyDictionary<PropertyInfo, DataPropertyBuilder> Properties { get; }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataPropertyBuilder<>))]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic dependency should handle preserving.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Dynamic dependency should handle preserving.")]
        public DataEntityBuilder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type entityType)
        {
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            var properties = new Dictionary<PropertyInfo, DataPropertyBuilder>();
            foreach (var property in EntityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                properties.Add(property, (DataPropertyBuilder)Activator.CreateInstance(typeof(DataPropertyBuilder<>).MakeGenericType(property.PropertyType), new object[] { property })!);
            }
            Properties = properties;
        }

        internal abstract DataEntity Build();

        public DataPropertyBuilder Property(PropertyInfo property)
            => Properties.TryGetValue(property, out var builder) ? builder : throw new InvalidOperationException($"Invalid property {property}.");

        public DataPropertyBuilder Property(string propertyName)
        {
            var property = Properties.Keys.FirstOrDefault(p => p.Name == propertyName) ?? throw new InvalidOperationException($"No property with name {propertyName} found for {EntityType}.");
            return Property(property);
        }

        public new DataEntityBuilder SetMetadata(string key, object? value)
        {
            base.SetMetadata(key, value);
            return this;
        }

        public DataEntityBuilder SetName(string? value)
            => SetMetadata(CommonMetadata.Name, value);

        public DataEntityBuilder SetKey(PropertyInfo[] properties)
            => SetMetadata(CommonMetadata.Key, properties);
    }

    public class DataEntityBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : DataEntityBuilder
    {
        public DataEntityBuilder() : base(typeof(T)) { }

        internal override DataEntity Build()
            => new DataEntity<T>(this);

        public new DataEntityBuilder<T> SetMetadata(string key, object? value)
        {
            base.SetMetadata(key, value);
            return this;
        }

        public new DataEntityBuilder<T> SetName(string? value)
            => SetMetadata(CommonMetadata.Name, value);

        public new DataEntityBuilder<T> SetKey(PropertyInfo[] properties)
            => SetMetadata(CommonMetadata.Key, properties);

        public DataEntityBuilder<T> SetKey(Expression<Func<T, object>> selector)
            => SetKey(selector.ExtractProperties(true).ToArray());

        public DataPropertyBuilder<TProp> Property<TProp>(Expression<Func<T, TProp>> selector)
            => (DataPropertyBuilder<TProp>)Property(selector.ExtractProperty());
    }
}