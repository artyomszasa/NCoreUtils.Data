using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NCoreUtils.Data.Mapping
{
    /// <summary>
    /// <c>IReadOnlyList&lt;T&gt;</c> builder. Uses <c>List&lt;T&gt;</c>.
    /// </summary>
    public class ReadOnlyListBuilder : CollectionBuilder
    {
        private readonly MutableCollectionBuilder _listBuilder;

        internal ReadOnlyListBuilder(Type elementType, Type collectionType)
            : base(elementType, collectionType)
        {
            if (!collectionType.Equals(typeof(IReadOnlyList<>).MakeGenericType(elementType)))
            {
                throw new InvalidOperationException($"Invalid collection type: {collectionType}.");
            }
            _listBuilder = new MutableCollectionBuilder(elementType, typeof(List<>).MakeGenericType(elementType));
        }

        public override Expression CreateNewExpression(IEnumerable<Expression> items)
            => _listBuilder.CreateNewExpression(items);

        public override Expression CreateNewExpression(Expression items)
            => _listBuilder.CreateNewExpression(items);
    }
}