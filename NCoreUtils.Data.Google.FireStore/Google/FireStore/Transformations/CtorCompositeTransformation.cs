using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public static class CtorCompositeTransformation
    {
        public static CompositeTransformation FromExpression(NewExpression expression, ParameterExpression rootArg, IReadOnlyDictionary<PropertyInfo, IValueSource> mapping)
        {
            if (null == expression.Members || expression.Arguments.Count != expression.Members.Count)
            {
                throw new InvalidOperationException("NewExpression must contain member mapping information.");
            }
            var newSources = ImmutableArray.CreateBuilder<IValueSource>(expression.Arguments.Count);
            var newMapping = ImmutableDictionary.CreateBuilder<PropertyInfo, IValueSource>();
            foreach (var data in expression.Arguments.Zip(expression.Members, (arg, mem) => (arg, mem)))
            {
                var arg = data.arg;
                var mem = data.mem;
                var src = ValueSources.FromExpression(arg, rootArg, mapping);
                newSources.Add(src);
                newMapping.Add((PropertyInfo)mem, src);
            }
            return (CompositeTransformation)Activator.CreateInstance(typeof(CtorCompositeTransformation<>).MakeGenericType(expression.Type), new object[] { newSources, newMapping, expression.Constructor });
        }
    }
}