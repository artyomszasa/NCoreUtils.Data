using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Mapping
{
    public class MutableCollectionFactory : CollectionFactory
    {
        public MethodInfo AddMethod { get; }

        public MethodInfo? AddRangeMethod { get; }

        internal MutableCollectionFactory(Type elementType, Type collectionType)
            : base(elementType, collectionType)
        {
            if (!typeof(ICollection<>).MakeGenericType(elementType).IsAssignableFrom(collectionType))
            {
                throw new InvalidOperationException($"{typeof(ICollection<>).MakeGenericType(elementType)} is not assignable from {collectionType.MakeGenericType(elementType)}");
            }
            AddMethod = CollectionType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new [] { elementType}, null);
            if (AddMethod is null)
            {
                throw new InvalidOperationException($"No suitable Add method found for collection type {collectionType} and element type {elementType}.");
            }
            AddRangeMethod = CollectionType.GetMethod("AddRange", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(IEnumerable<>).MakeGenericType(elementType) }, null);
        }

        public override Expression CreateNewExpression(IEnumerable<Expression> items)
            => Expression.ListInit(
                Expression.New(CollectionType),
                items.Select(item => Expression.ElementInit(AddMethod, item))
            );

        public override Expression CreateNewExpression(Expression items)
        {
            var evar = Expression.Variable(CollectionType);
            var evarinit = Expression.Assign(evar, Expression.New(CollectionType));
            var retval = Expression.Label(CollectionType);
            var @return = Expression.Label(retval, Expression.Constant(null, CollectionType));
            if (AddRangeMethod is null)
            {
                var eenumerator = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(ElementType));
                var eenumeratorinit = Expression.Assign(
                    eenumerator,
                    Expression.Call(items, typeof(IEnumerable<>).MakeGenericType(ElementType).GetMethod("GetEnumerator"))
                );
                var moveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext");
                var current = eenumerator.Type.GetProperty("Current");
                var @break = Expression.Label("breakloop");
                return Expression.Block(
                    new [] { evar },
                    evarinit,
                    Expression.Block(
                        new [] { eenumerator },
                        eenumeratorinit,
                        Expression.Loop(
                            Expression.IfThenElse(
                                Expression.Call(eenumerator, moveNext),
                                Expression.Call(evar, AddMethod, Expression.Property(eenumerator, current)),
                                Expression.Break(@break)
                            ),
                            @break
                        )
                    ),
                    Expression.Return(retval, evar),
                    @return
                );
            }

            return Expression.Block(
                new [] { evar },
                evarinit,
                Expression.Call(evar, AddRangeMethod, items),
                Expression.Return(retval, evar),
                @return
            );
        }

        public override ICollectionBuilder CreateBuilder()
            => (ICollectionBuilder)Activator.CreateInstance(
                typeof(MutableCollectionBuilder<,>).MakeGenericType(CollectionType, ElementType),
                true
            );
    }
}