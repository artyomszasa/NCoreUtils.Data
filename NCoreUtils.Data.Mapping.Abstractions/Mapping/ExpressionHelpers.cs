using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Mapping;

public static class Helpers
{
    public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
    {
        if (source is IList<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }
        return new ReadOnlyCollection<T>(source.ToList());
    }

    public static Expression CreateNewCollectionExpressionWithEnumerableInitialization(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type enumeratorType,
        MethodInfo addMethod,
        MethodInfo? addRangeMethod,
        MethodInfo getEnumeratorMethod,
        MethodInfo moveNextMethod,
        PropertyInfo currentProperty,
        Expression items
    )
    {
        var evar = Expression.Variable(collectionType);
        var evarinit = Expression.Assign(evar, Expression.New(collectionType));
        var retval = Expression.Label(collectionType);
        var @return = Expression.Label(retval, Expression.Constant(null, collectionType));
        if (addRangeMethod is null)
        {
            var eenumerator = Expression.Variable(enumeratorType);
            var eenumeratorinit = Expression.Assign(
                eenumerator,
                Expression.Call(items, getEnumeratorMethod)
            );
            var @break = Expression.Label("breakloop");
            return Expression.Block(
                [evar],
                evarinit,
                Expression.Block(
                    [eenumerator],
                    eenumeratorinit,
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.Call(eenumerator, moveNextMethod),
                            Expression.Call(evar, addMethod, Expression.Property(eenumerator, currentProperty)),
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
            [evar],
            evarinit,
            Expression.Call(evar, addRangeMethod, items),
            Expression.Return(retval, evar),
            @return
        );
    }
}