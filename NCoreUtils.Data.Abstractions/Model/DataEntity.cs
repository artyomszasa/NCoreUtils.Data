using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using NCoreUtils.Data.Build;

namespace NCoreUtils.Data.Model
{
    public abstract class DataEntity : Metadata
    {
        public Type EntityType { get; }

        public IReadOnlyList<DataProperty> Properties { get; }

        public string Name => TryGetValue(CommonMetadata.Name, out var boxed) && boxed is string name
            ? name
            : EntityType.Name;

        public IReadOnlyList<DataProperty>? Key { get; }

        protected DataEntity(
            Type entityType,
            IReadOnlyList<DataProperty> properties,
            IReadOnlyDictionary<string, object?> data)
            : base(data)
        {
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            if (TryGetValue(CommonMetadata.Key, out var boxed) && boxed is PropertyInfo[] keyProperties)
            {
                Key = keyProperties
                    .Select(p => Properties.FirstOrDefault(e => e.Property == p) ?? throw new InvalidOperationException($"Key property {p} not defined in properties of {entityType}."))
                    .ToArray();
            }
        }
    }

    public class DataEntity<T> : DataEntity
    {
        internal DataEntity(DataEntityBuilder<T> builder)
            : base(
                typeof(T),
                builder.Properties.Values.Select(p => p.Build()).ToArray(),
                builder.Metadata.ToImmutableDictionary()
            )
        { }
    }
}