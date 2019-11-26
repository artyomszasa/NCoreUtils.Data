using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public class EmplaceSourcesVisitor : ExpressionVisitor
    {
        public class ObjectReferenceEqualityComparer<T> : EqualityComparer<T>
            where T : class
        {
            public override bool Equals(T x, T y) => ReferenceEquals(x, y);

            public override int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        static readonly ObjectReferenceEqualityComparer<IValueSource> _sourceEqualityComparer = new ObjectReferenceEqualityComparer<IValueSource>();

        static readonly MethodInfo _gEmplaceValueSource = typeof(EmplaceSourcesVisitor).GetMethod(nameof(DoEmplaceValueSource), BindingFlags.NonPublic | BindingFlags.Static);

        static Expression DoEmplaceValueSource<T>(ParameterExpression arg, IValueSource<T> source)
        {
            Expression<Func<object, T>> func = o => source.GetValue(o);
            return func.Body.SubstituteParameter(func.Parameters[0], arg);
        }

        static Expression EmplaceValueSource(ParameterExpression arg, IValueSource source)
        {
            return (Expression)_gEmplaceValueSource.MakeGenericMethod(source.Type).Invoke(null, new object[] { arg, source });
        }

        public static (Expression Expression, IReadOnlyList<IValueSource> UserSources) Visit(Expression expression, IReadOnlyDictionary<PropertyInfo, IValueSource> mapping)
        {
            var visitor = new EmplaceSourcesVisitor(mapping);
            var expr = visitor.Visit(expression);
            return (expr, visitor._usedSources.ToList());
        }

        readonly HashSet<IValueSource> _usedSources = new HashSet<IValueSource>(_sourceEqualityComparer);

        public IReadOnlyDictionary<PropertyInfo, IValueSource> Mapping { get; }

        public EmplaceSourcesVisitor(IReadOnlyDictionary<PropertyInfo, IValueSource> mapping)
        {
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression arg && node.Member is PropertyInfo property && Mapping.TryGetValue(property, out var source))
            {
                _usedSources.Add(source);
                return EmplaceValueSource(arg, source);
            }
            return base.VisitMember(node);
        }
    }
}