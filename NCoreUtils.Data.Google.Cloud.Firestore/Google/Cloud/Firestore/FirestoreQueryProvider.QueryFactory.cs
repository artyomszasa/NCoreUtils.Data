using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Google.Cloud.Firestore;
using NCoreUtils.Data.Google.Cloud.Firestore.Expressions;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider
    {
        private static readonly HashSet<Type> _primitiveTypes = new HashSet<Type>
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
            typeof(string)
        };

        protected virtual Expression GetInitialSelector(Type type, Expression snapshot, ImmutableList<string> path)
        {
            // FIXME: Polymorphism...
            if (!Model.TryGetDataEntity(type, out var entity))
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
                        return new FirestoreFieldExpression(snapshot, path.ToFieldPath(pdata.Name), ptype);
                    }
                    // FIXME: implement collections...
                    // fallback to subentity
                    return GetInitialSelector(ptype, snapshot, path.Add(pdata.Name));
                })
            );
        }

        protected Expression<Func<DocumentSnapshot, T>> GetInitialSelector<T>()
        {
            var arg = Expression.Parameter(typeof(DocumentSnapshot));
            return Expression.Lambda<Func<DocumentSnapshot, T>>(
                GetInitialSelector(typeof(T), arg, ImmutableList<string>.Empty),
                arg
            );
        }

        public FirestoreQuery<T> CreateQueryable<T>()
        {
            if (!Model.TryGetDataEntity(typeof(T), out var entity))
            {
                throw new InvalidOperationException($"Unable to create initial selector for type {typeof(T)} as it is not registered as entity.");
            }
            return new FirestoreQuery<T>(
                this,
                entity.Name,
                GetInitialSelector<T>(),
                ImmutableList<FirestoreCondition>.Empty,
                ImmutableList<FirestoreOrdering>.Empty,
                0,
                default
            );
        }
    }
}