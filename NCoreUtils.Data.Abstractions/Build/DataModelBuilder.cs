using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Build
{
    public class DataModelBuilder : MetadataBuilder
    {
        private static readonly MethodInfo _gmEntity = typeof(DataModelBuilder)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.IsGenericMethodDefinition && m.Name == nameof(Entity));

        private readonly Dictionary<Type, DataEntityBuilder> _entities = new Dictionary<Type, DataEntityBuilder>();

        public IReadOnlyCollection<DataEntityBuilder> Entities => _entities.Values;

        public new DataModelBuilder SetMetadata(string key, object? value)
        {
            base.SetMetadata(key, value);
            return this;
        }

        public DataEntityBuilder Entity(Type entityType)
        {
            return (DataEntityBuilder)_gmEntity.MakeGenericMethod(entityType).Invoke(this, new object[0]);
        }

        public DataEntityBuilder<T> Entity<T>()
        {
            if (_entities.TryGetValue(typeof(T), out var boxed))
            {
                return (DataEntityBuilder<T>)boxed;
            }
            var builder = new DataEntityBuilder<T>();
            _entities[typeof(T)] = builder;
            return builder;
        }

        public DataModelBuilder Entity(Type entityType, Action<DataEntityBuilder> configure)
        {
            configure(Entity(entityType));
            return this;
        }

        public DataModelBuilder Entity<T>(Action<DataEntityBuilder<T>> configure)
        {
            configure(Entity<T>());
            return this;
        }
    }
}