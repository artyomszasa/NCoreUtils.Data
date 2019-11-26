using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public abstract class CompositeTransformation : ITransformation
    {
        public static CompositeTransformation FromExpression(Expression expression, ParameterExpression rootArg, IReadOnlyDictionary<PropertyInfo, IValueSource> mapping)
        {
            if (expression is NewExpression newExpression)
            {
                return CtorCompositeTransformation.FromExpression(newExpression, rootArg, mapping);
            }
            var (body, sources) = EmplaceSourcesVisitor.Visit(expression, mapping);
            var converterExpression = Expression.Lambda(body, rootArg);
            var converter = converterExpression.Compile();
            return CustomCompositeTransformation.FromDelegate(sources, converter);
        }

        Type IValueSource.Type => ResultType;

        IEnumerable<IValueSource> ITransformation.Sources => Sources;

        protected abstract Type ResultType { get; }

        public IReadOnlyList<IValueSource> Sources { get; }

        public IReadOnlyDictionary<PropertyInfo, IValueSource> Mapping { get; }

        object IValueSource.GetValue(object instance) => DoConvert(instance);

        protected CompositeTransformation(IReadOnlyList<IValueSource> sources, IReadOnlyDictionary<PropertyInfo, IValueSource> mapping)
        {
            Sources = sources ?? throw new ArgumentNullException(nameof(sources));
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }

        protected object[] PopulateSources(object source)
        {
            return Sources.MapToArray(extractor => extractor.GetValue(source));
        }

        public abstract object DoConvert(object source);
    }
}