using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public sealed class ReflectionFieldExpressionFactory : IFirestoreFieldExpressionFactory
{
    private abstract class FactoryImpl
    {
        private static readonly MethodInfo _gmCreate
            = ((MethodCallExpression)((Expression<Func<ImmutableList<string>, FactoryImpl<int>>>)(rawPath => Create<int>(rawPath))).Body)
                .Method
                .GetGenericMethodDefinition();

        private static FactoryImpl<T> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ImmutableList<string> rawPath)
            => new(rawPath);

        public static FactoryImpl Create(Type propertyType, ImmutableList<string> rawPath)
            => (FactoryImpl)_gmCreate.MakeGenericMethod(propertyType).Invoke(null, [rawPath])!;

        public abstract FirestoreFieldExpression CreateFieldExpression(FirestoreConverter converter, Expression snapshot);
    }

    private sealed class FactoryImpl<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ImmutableList<string> rawPath) : FactoryImpl
    {
        public override FirestoreFieldExpression CreateFieldExpression(FirestoreConverter converter, Expression snapshot)
            => new FirestoreFieldExpression<T>(converter, snapshot, rawPath);
    }

    private sealed class IdFactoryImpl : FactoryImpl
    {
        public static IdFactoryImpl Singleton { get; } = new();

        private IdFactoryImpl() { }

        public override FirestoreFieldExpression CreateFieldExpression(FirestoreConverter converter, Expression snapshot)
            => new FirestoreFieldExpression<string>(converter, snapshot, FieldPath.DocumentId);
    }

    private ConcurrentDictionary<DataProperty, FactoryImpl> FactoryCache { get; } = [];

    public FirestoreFieldExpression Create(DataEntity entity, DataProperty property, FirestoreConverter converter, Expression snapshot)
    {
        if (!FactoryCache.TryGetValue(property, out var factory))
        {
            if (entity.Key != null && entity.Key.Count == 1 && entity.Key[0].Property == property.Property)
            {
                if (property.Property.PropertyType != typeof(string))
                {
                    throw new InvalidOperationException($"Key property ({property.Property}) of {entity.EntityType} must have string type to be used as firestore stored entity.");
                }
                factory = IdFactoryImpl.Singleton;
            }
            else
            {
#if NET8_0_OR_GREATER
                factory = FactoryImpl.Create(property.Property.PropertyType, [property.Name]);
#else
                factory = FactoryImpl.Create(property.Property.PropertyType, ImmutableList.Create(property.Name));
#endif
            }
            FactoryCache.TryAdd(property, factory);
        }
        return factory.CreateFieldExpression(converter, snapshot);
    }
}