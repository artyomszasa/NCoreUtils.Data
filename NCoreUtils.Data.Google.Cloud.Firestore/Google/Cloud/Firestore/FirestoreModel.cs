using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data.Build;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreModel : DataModel
    {
        internal static readonly HashSet<Type> _primitiveTypes = new()
        {
            typeof(bool),
            typeof(byte[]),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(float),
            typeof(double),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(long),
            typeof(string),
            // nullable
            typeof(bool?),
            typeof(DateTime?),
            typeof(DateTimeOffset?),
            typeof(float?),
            typeof(double?),
            typeof(byte?),
            typeof(short?),
            typeof(ushort?),
            typeof(int?),
            typeof(long?),
        };

        private readonly ConcurrentDictionary<Type, LambdaExpression> _initialSelectorCache = new();

        public IFirestoreConfiguration Configuration { get; }

        public FirestoreConversionOptions ConversionOptions { get; }

        public ILoggerFactory LoggerFactory { get; }

        public IReadOnlyDictionary<Type, DataEntity> ByType { get; }

        public FirestoreConverter Converter { get; }

        public FirestoreModel(IFirestoreConfiguration configuration, ILoggerFactory loggerFactory, DataModelBuilder builder)
            : base(builder)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ConversionOptions = configuration.ConversionOptions ?? FirestoreConversionOptions.Default;
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            ByType = Entities.ToDictionary(e => e.EntityType);
            Converter = new FirestoreConverter(LoggerFactory.CreateLogger<FirestoreConverter>(), ConversionOptions, this);
        }

        protected Expression GetInitialSelector(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
            Expression snapshot)
        {
            // FIXME: Polymorphism...
            if (!TryGetDataEntity(type, out var entity))
            {
                throw new InvalidOperationException($"Unable to create initial selector for type {type} as it is not registered as entity.");
            }
            var ctor = Ctor.GetCtor(type);
            return new CtorExpression(
                ctor,
                ctor.Properties.Select(p =>
                {
                    var ptype = p.TargetProperty.PropertyType;
                    var pdata = entity.Properties.First(e => e.Property == p.TargetProperty);
                    if (entity.Key != null && entity.Key.Count == 1 && entity.Key[0].Property.Equals(p.TargetProperty))
                    {
                        // FIXME: keys on nested entities are not allowed...
                        return new FirestoreFieldExpression(Converter, snapshot, FieldPath.DocumentId, ptype);
                    }
                    return new FirestoreFieldExpression(Converter, snapshot, ImmutableList.Create(pdata.Name), ptype);
                })
            );
        }

        protected Expression<Func<DocumentSnapshot, T>> GetInitialSelectorNoCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        {
            var arg = Expression.Parameter(typeof(DocumentSnapshot));
            return Expression.Lambda<Func<DocumentSnapshot, T>>(
                GetInitialSelector(typeof(T), arg),
                arg
            );
        }

        public Expression<Func<DocumentSnapshot, T>> GetInitialSelector<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        {
            if (_initialSelectorCache.TryGetValue(typeof(T), out var boxed))
            {
                return (Expression<Func<DocumentSnapshot, T>>)boxed;
            }
            return (Expression<Func<DocumentSnapshot, T>>)_initialSelectorCache.GetOrAdd(typeof(T), _ => GetInitialSelectorNoCache<T>());
        }

        public bool TryGetDataEntity(Type type, [NotNullWhen(true)] out DataEntity? entity)
            => ByType.TryGetValue(type, out entity);
    }
}