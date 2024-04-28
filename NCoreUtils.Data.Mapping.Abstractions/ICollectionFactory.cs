using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace NCoreUtils.Data;

public interface ICollectionFactory
{
    Type ElementType { get; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type CollectionType { get; }

    Expression CreateNewExpression(IEnumerable<Expression> items);

    /// <summary>
    /// Creates construction expression from single parameter that must be enumerable sequence of items.
    /// </summary>
    /// <param name="items">Expression representing enumerable source.</param>
    Expression CreateNewExpression(Expression items);

    ICollectionBuilder CreateBuilder();
}