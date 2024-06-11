using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Build
{
    public class DataModelBuilder : MetadataBuilder
    {
        private readonly Dictionary<Type, DataEntityBuilder> _entities = new();

        public IReadOnlyCollection<DataEntityBuilder> Entities => _entities.Values;

        public new DataModelBuilder SetMetadata(string key, object? value)
        {
            base.SetMetadata(key, value);
            return this;
        }

        [RequiresUnreferencedCode(S.MethodUsesReflection)]
        [RequiresDynamicCode(S.MethodUsesReflection)]
        public DataEntityBuilder Entity([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type entityType)
        {
            var gmEntity = typeof(DataModelBuilder)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.IsGenericMethodDefinition && m.Name == nameof(Entity));
            return (DataEntityBuilder)gmEntity.MakeGenericMethod(entityType).Invoke(this, Array.Empty<object>())!;
        }

        [RequiresDynamicCode(S.MethodUsesReflection)]
        public DataEntityBuilder<T> Entity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
        {
            if (_entities.TryGetValue(typeof(T), out var boxed))
            {
                return (DataEntityBuilder<T>)boxed;
            }
            var builder = new DataEntityBuilder<T>();
            _entities[typeof(T)] = builder;
            return builder;
        }

        [RequiresDynamicCode(S.MethodUsesReflection)]
        [RequiresUnreferencedCode(S.MethodUsesReflection)]
        public DataModelBuilder Entity(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type entityType,
            Action<DataEntityBuilder> configure)
        {
            configure(Entity(entityType));
            return this;
        }

        [RequiresDynamicCode(S.MethodUsesReflection)]
        public DataModelBuilder Entity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Action<DataEntityBuilder<T>> configure)
        {
            configure(Entity<T>());
            return this;
        }

        public DataModelBuilder AddEntityBuilder<T>(T builder)
            where T : DataEntityBuilder
        {
            _entities.Add(builder.EntityType, builder);
            return this;
        }
    }
}