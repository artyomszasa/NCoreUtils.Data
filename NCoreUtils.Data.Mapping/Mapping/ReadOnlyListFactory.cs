using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NCoreUtils.Data.Mapping
{
    /// <summary>
    /// <c>IReadOnlyList&lt;T&gt;</c> builder. Uses <c>List&lt;T&gt;</c>.
    /// </summary>
    public class ReadOnlyListFactory : CollectionFactory
    {
        private readonly MutableCollectionFactory _listBuilder;

        internal ReadOnlyListFactory(Type elementType, Type collectionType)
            : base(elementType, collectionType)
        {
            if (!collectionType.Equals(typeof(IReadOnlyList<>).MakeGenericType(elementType)))
            {
                throw new InvalidOperationException($"Invalid collection type: {collectionType}.");
            }
            _listBuilder = new MutableCollectionFactory(elementType, typeof(List<>).MakeGenericType(elementType));
        }

        public override ICollectionBuilder CreateBuilder()
            => (ICollectionBuilder)Activator.CreateInstance(
                typeof(ReadOnlyListBuilder<>).MakeGenericType(_listBuilder.ElementType),
                true
            );

        public override Expression CreateNewExpression(IEnumerable<Expression> items)
            => _listBuilder.CreateNewExpression(items);

        public override Expression CreateNewExpression(Expression items)
            => _listBuilder.CreateNewExpression(items);
    }
}