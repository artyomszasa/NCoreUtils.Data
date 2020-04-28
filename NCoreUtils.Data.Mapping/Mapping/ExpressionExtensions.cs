using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Mapping
{
    public static class ExpressionExtensions
    {
        private interface IOriginSource
        {
            #if NETSTANDARD2_1
            bool TryGetOrigin(Expression instance, MemberInfo property, [NotNullWhen(true)] out Expression? origin);
            #else
            bool TryGetOrigin(Expression instance, MemberInfo property, out Expression origin);
            #endif
        }

        private sealed class CtorOriginSource : IOriginSource
        {
            private readonly Expression _instance;

            private readonly IReadOnlyList<PropertyMapping> _mappings;

            private readonly IReadOnlyList<Expression> _arguments;

            public CtorOriginSource(Expression instance, IReadOnlyList<PropertyMapping> mappings, IReadOnlyList<Expression> arguments)
            {
                _instance = instance;
                _mappings = mappings;
                _arguments = arguments;
            }

            #if NETSTANDARD2_1
            public bool TryGetOrigin(Expression instance, MemberInfo property, [NotNullWhen(true)] out Expression? origin)
            #else
            public bool TryGetOrigin(Expression instance, MemberInfo property, out Expression origin)
            #endif
            {
                if (_instance.Equals(instance))
                {
                    var index = _mappings.FindIndex(m => m.TargetProperty.Equals(property));
                    if (-1 != index)
                    {
                        origin = _arguments[index];
                        return true;
                    }
                }
                origin = default!;
                return false;
            }
        }

        private sealed class NewOriginSource : IOriginSource
        {
            private readonly Expression _instance;

            readonly IReadOnlyList<MemberInfo> _members;

            readonly IReadOnlyList<Expression> _arguments;

            public NewOriginSource(Expression instance, IReadOnlyList<MemberInfo> members, IReadOnlyList<Expression> arguments)
            {
                _instance = instance;
                _members = members;
                _arguments = arguments;
            }

            #if NETSTANDARD2_1
            public bool TryGetOrigin(Expression instance, MemberInfo property, [NotNullWhen(true)] out Expression? origin)
            #else
            public bool TryGetOrigin(Expression instance, MemberInfo property, out Expression origin)
            #endif
            {
                if (_instance.Equals(instance))
                {
                    var index = _members.FindIndex(m => m.Equals(property));
                    if (-1 != index)
                    {
                        origin = _arguments[index];
                        return true;
                    }
                }
                origin = default!;
                return false;
            }
        }

        private sealed class SimplifyVisitor : ExpressionVisitor
        {
            private readonly IOriginSource _originSource;

            public SimplifyVisitor(IOriginSource originSource)
            {
                _originSource = originSource ?? throw new ArgumentNullException(nameof(originSource));
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member is PropertyInfo property && _originSource.TryGetOrigin(node.Expression, property, out var origin))
                {
                    return origin;
                }
                return base.VisitMember(node);
            }
        }

        public static Expression<Func<TSource, TResult>> ChainSimplified<TSource, TInner, TResult>(
            this Expression<Func<TSource, TInner>> source,
            Expression<Func<TInner, TResult>> selector)
        {
            if (source.Body is CtorExpression ector)
            {
                var visitor = new SimplifyVisitor(new CtorOriginSource(selector.Parameters[0], ector.Ctor.Properties, ector.Arguments));
                return Expression.Lambda<Func<TSource, TResult>>(
                    visitor.Visit(selector.Body).SubstituteParameter(selector.Parameters[0], source.Body),
                    source.Parameters[0]
                );
            }
            if (source.Body is NewExpression enew && null != enew.Members && enew.Members.Count == enew.Arguments.Count)
            {
                var visitor = new SimplifyVisitor(new NewOriginSource(selector.Parameters[0], enew.Members, enew.Arguments));
                return Expression.Lambda<Func<TSource, TResult>>(
                    visitor.Visit(selector.Body).SubstituteParameter(selector.Parameters[0], source.Body),
                    source.Parameters[0]
                );
            }
            return Expression.Lambda<Func<TSource, TResult>>(
                selector.Body.SubstituteParameter(selector.Parameters[0], source.Body),
                source.Parameters[0]
            );
        }
    }
}