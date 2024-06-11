using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NCoreUtils.Data.Build;

namespace NCoreUtils.Data.Model;

public abstract class DataEntity : Metadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type _entityType;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type EntityType => _entityType;

    public IReadOnlyList<DataProperty> Properties { get; }

    public string Name => TryGetValue(CommonMetadata.Name, out var boxed) && boxed is string name
        ? name
        : EntityType.Name;

    public IReadOnlyList<DataProperty>? Key { get; }

    protected DataEntity(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type entityType,
        IReadOnlyList<DataProperty> properties,
        IReadOnlyDictionary<string, object?> data)
        : base(data)
    {
        _entityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        if (TryGetValue(CommonMetadata.Key, out var boxed) && boxed is PropertyInfo[] keyProperties)
        {
            Key = keyProperties
                .Select(p => Properties.FirstOrDefault(e => e.Property == p) ?? throw new InvalidOperationException($"Key property {p} not defined in properties of {_entityType}."))
                .ToArray();
        }
    }
}

public class DataEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : DataEntity
{
    internal DataEntity(DataEntityBuilder<T> builder)
        : base(
            typeof(T),
            builder.Properties.Values.Select(p => p.Build()).ToArray(),
            builder.Metadata.ToImmutableDictionary()
        )
    { }
}