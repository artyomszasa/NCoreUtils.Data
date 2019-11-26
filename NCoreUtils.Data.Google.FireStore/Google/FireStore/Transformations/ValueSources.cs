using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public static class ValueSources
    {
        public static IValueSource FromExpression(Expression expression, ParameterExpression rootArg, IReadOnlyDictionary<PropertyInfo, IValueSource> mapping)
        {
            // Constant
            if (expression.TryExtractConstant(out var contantValue))
            {
                return ConstantSource.FromValue(contantValue, expression.Type);
            }
            // Mapped property access
            if (expression is MemberExpression mexpr && mexpr.Member is PropertyInfo property && mexpr.Expression is ParameterExpression arg)
            {
                if (arg != rootArg)
                {
                    throw new InvalidOperationException($"Unmapped parameter of type {arg.Type} in {expression}.");
                }
                if (!mapping.TryGetValue(property, out var source))
                {
                    throw new InvalidOperationException($"Unmapped property {property.DeclaringType.FullName}.{property.Name} in {expression}.");
                }
                return source;
            }
            // Generic expression
            return CompositeTransformation.FromExpression(expression, rootArg, mapping);
        }
    }
}