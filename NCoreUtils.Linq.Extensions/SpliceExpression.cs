using System;
using System.Linq.Expressions;

namespace NCoreUtils.Linq
{
    abstract class SpliceExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Extension;

        public string Name { get; }

        protected SpliceExpression(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}