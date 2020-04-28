using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using NCoreUtils.Data.Mapping;

namespace NCoreUtils.Data
{
    public class CtorExpression : Expression
    {
        public Ctor Ctor { get; }

        public ReadOnlyCollection<Expression> Arguments { get; }

        public override bool CanReduce => true;

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => Ctor.Type;

        public CtorExpression(Ctor ctor, IEnumerable<Expression> arguments)
        {
            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }
            Ctor = ctor ?? throw new ArgumentNullException(nameof(ctor));
            Arguments = arguments.ToReadOnlyCollection();
            if (Arguments.Count != Ctor.Properties.Count)
            {
                throw new InvalidOperationException($"Argument count mismatch: expected {Ctor.Properties.Count}, {Arguments.Count} supplied.");
            }
            for (var i = 0; i < Arguments.Count; ++i)
            {
                Ctor.Properties[i].Match(
                    byCtorParameter => {
                        if (!byCtorParameter.By.ParameterType.IsAssignableFrom(Arguments[i].Type))
                        {
                            throw new InvalidOperationException($"Argument {i} type mismatch: {byCtorParameter.By.ParameterType} is not assignable from {Arguments[i].Type}.");
                        }
                    },
                    bySetter => {
                        if (!bySetter.TargetProperty.PropertyType.IsAssignableFrom(Arguments[i].Type))
                        {
                            throw new InvalidOperationException($"Argument {i} type mismatch: {bySetter.TargetProperty.PropertyType} is not assignable from {Arguments[i].Type}.");
                        }
                    }
                );
            }
        }

        public override Expression Reduce()
        {
            if (Ctor.Properties.All(mapping => mapping.IsByCtorParameter))
            {
                return New(Ctor.Constructor, Arguments);
            }
            var pairs = Ctor.Properties.Zip(Arguments, (p, a) => (p, a));
            return MemberInit(
                New(
                    Ctor.Constructor,
                    pairs
                        .Where(pair => pair.p.IsByCtorParameter)
                        .Select(pair => pair.a)
                ),
                pairs
                    .Where(pair => pair.p.IsBySetter)
                    .Select(pair => Bind(pair.p.TargetProperty, pair.a))
            );
        }

        public override string ToString()
        #if NETSTANDARD2_1
            => $"construct:{Type}({string.Join(',', Arguments)})";
        #else
            => $"construct:{Type}({string.Join(",", Arguments)})";
        #endif

        // protected override Expression VisitChildren(ExpressionVisitor visitor)
        //     => new CtorExpression(Ctor, visitor.Visit(Arguments));
    }
}