using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Build;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreModel : DataModel
    {
        internal static readonly HashSet<Type> _primitiveTypes = new HashSet<Type>
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

        private readonly ConcurrentDictionary<Type, LambdaExpression> _initialSelectorCache = new ConcurrentDictionary<Type, LambdaExpression>();

        public IReadOnlyDictionary<Type, DataEntity> ByType { get; }

        public FirestoreModel(DataModelBuilder builder)
            : base(builder)
        {
            ByType = Entities.ToDictionary(e => e.EntityType);
        }

        protected Expression GetInitialSelector(Type type, Expression snapshot, ImmutableList<string> path)
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
                    if (_primitiveTypes.Contains(ptype))
                    {
                        if (entity.Key != null && entity.Key.Count == 1 && entity.Key[0].Property.Equals(p.TargetProperty))
                        {
                            // FIXME: keys on nested entities are not allowed...
                            return new FirestoreFieldExpression(snapshot, FieldPath.DocumentId, ptype);
                        }
                        return new FirestoreFieldExpression(snapshot, path.Add(pdata.Name), ptype);
                    }
                    if (CollectionBuilder.TryCreate(ptype, out var collectionBuilder))
                    {
                        return new FirestoreCollectionFieldExpression(snapshot, path.Add(pdata.Name), ptype, collectionBuilder);
                    }
                    // fallback to subentity
                    return GetInitialSelector(ptype, snapshot, path.Add(pdata.Name));
                })
            );
        }

        protected Expression<Func<DocumentSnapshot, T>> GetInitialSelectorNoCache<T>()
        {
            var arg = Expression.Parameter(typeof(DocumentSnapshot));
            return Expression.Lambda<Func<DocumentSnapshot, T>>(
                GetInitialSelector(typeof(T), arg, ImmutableList<string>.Empty),
                arg
            );
        }

        public Expression<Func<DocumentSnapshot, T>> GetInitialSelector<T>()
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