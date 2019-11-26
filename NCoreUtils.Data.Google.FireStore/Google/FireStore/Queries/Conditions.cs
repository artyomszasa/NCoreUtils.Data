using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public static class Conditions
    {
        struct PathOrValue
        {
            public static PathOrValue CreatePath(string path)
            {
                if (path is null)
                {
                    throw new System.ArgumentNullException(nameof(path));
                }
                return new PathOrValue(path, default);
            }

            public static PathOrValue CreateValue(object value) => new PathOrValue(null, value);

            public bool IsPath => !(Path is null);

            public string Path { get; }

            public object Value { get; }

            PathOrValue(string path, object value)
            {
                Path = path;
                Value = value;
            }
        }

        static readonly ImmutableDictionary<Condition.Op, Condition.Op> _reverse = new Dictionary<Condition.Op, Condition.Op>
        {
            { Condition.Op.EqualTo, Condition.Op.EqualTo },
            { Condition.Op.GreaterThan, Condition.Op.LessThan },
            { Condition.Op.GreaterThanOrEqualTo, Condition.Op.LessThanOrEqualTo },
            { Condition.Op.LessThan, Condition.Op.GreaterThan },
            { Condition.Op.LessThanOrEqualTo, Condition.Op.GreaterThanOrEqualTo },
        }.ToImmutableDictionary();

        static readonly MethodInfo _gmContains = GetMethod<IEnumerable<int>, int, bool>(Enumerable.Contains).GetGenericMethodDefinition();

        static void CreateCondition(Condition.Op operation, (PathOrValue Left, PathOrValue Right) args, ref TinyList<Condition> conditions)
        {
            ref readonly PathOrValue left = ref args.Left;
            ref readonly PathOrValue right = ref args.Right;
            if (left.IsPath)
            {
                if (right.IsPath)
                {
                    throw new InvalidOperationException("Only path ~ object comparison is supported.");
                }
                conditions.Add(new Condition(left.Path, operation, right.Value));
            }
            else
            {
                if (right.IsPath)
                {
                    conditions.Add(new Condition(right.Path, Reverse(operation), left.Value));
                }
                else
                {
                    throw new InvalidOperationException("Only path ~ object comparison is supported.");
                }
            }
        }

        static void ExtractPath(ParameterExpression arg, TypeMapping mapping, ref TinyList<string> path, Expression expression)
        {
            if (expression.Equals(arg))
            {
                return;
            }
            if (expression is MemberExpression expr && expr.Member is PropertyInfo property)
            {
                ref readonly TypeDescriptor typeDescriptor = ref mapping(property.DeclaringType);
                if (typeDescriptor.TryGetPropertyName(property, out var name))
                {
                    ExtractPath(arg, mapping, ref path, expr.Expression);
                    path.Add(name);
                    return;
                }
            }
            throw new InvalidOperationException($"Unable to resolve subpath: {expression}.");
        }

        static PathOrValue ExtractPathOrValue(ParameterExpression arg, TypeMapping mapping, Expression expression)
        {
            if (expression.TryExtractConstant(out var value))
            {
                return PathOrValue.CreateValue(value);
            }
            var path = new TinyList<string>();
            ExtractPath(arg, mapping, ref path, expression);
            if (0 == path.Count)
            {
                throw new InvalidOperationException($"Empty path resolved for {expression}");
            }
            return PathOrValue.CreatePath(path.Join("."));
        }

        static (PathOrValue Left, PathOrValue Right) ExtractPathOrValue(ParameterExpression arg, TypeMapping mapping, BinaryExpression expression)
        {
            return (
                ExtractPathOrValue(arg, mapping, expression.Left),
                ExtractPathOrValue(arg, mapping, expression.Right)
            );
        }

        static void ExtractConditions(ParameterExpression arg, TypeMapping mapping, ref TinyList<Condition> conditions, Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression bin:
                    switch (bin.NodeType)
                    {
                        case ExpressionType.AndAlso:
                            ExtractConditions(arg, mapping, ref conditions, bin.Left);
                            ExtractConditions(arg, mapping, ref conditions, bin.Right);
                            break;
                        case ExpressionType.Equal:
                            CreateCondition(Condition.Op.EqualTo, ExtractPathOrValue(arg, mapping, bin), ref conditions);
                            break;
                        case ExpressionType.LessThan:
                            CreateCondition(Condition.Op.LessThan, ExtractPathOrValue(arg, mapping, bin), ref conditions);
                            break;
                        case ExpressionType.LessThanOrEqual:
                            CreateCondition(Condition.Op.LessThanOrEqualTo, ExtractPathOrValue(arg, mapping, bin), ref conditions);
                            break;
                        case ExpressionType.GreaterThan:
                            CreateCondition(Condition.Op.GreaterThan, ExtractPathOrValue(arg, mapping, bin), ref conditions);
                            break;
                        case ExpressionType.GreaterThanOrEqual:
                            CreateCondition(Condition.Op.GreaterThanOrEqualTo, ExtractPathOrValue(arg, mapping, bin), ref conditions);
                            break;
                        default:
                            throw new NotSupportedException($"Not supported expression {expression}.");
                    }
                    break;
                case MethodCallExpression methodCall when (methodCall.Method.IsGenericMethod && methodCall.Method.GetGenericMethodDefinition().Equals(_gmContains)):
                    CreateCondition(
                        Condition.Op.ArrayContains,
                        (ExtractPathOrValue(arg, mapping, methodCall.Arguments[0]), ExtractPathOrValue(arg, mapping, methodCall.Arguments[1])),
                        ref conditions);
                    break;
                default:
                    throw new NotSupportedException($"Not supported expression {expression}.");
            }
        }

        static MethodInfo GetMethod<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func) => func.Method;

        static Condition.Op Reverse(Condition.Op op)
            => _reverse.TryGetValue(op, out var res) ? res : throw new InvalidOperationException($"Unable to reverse operation {op}.");

        public static void ExtractConditions(this LambdaExpression expression, TypeMapping mapping, ref TinyList<Condition> conditions)
        {
            try
            {
                ExtractConditions(expression.Parameters[0], mapping, ref conditions, expression.Body);
            }
            catch (Exception exn)
            {
                throw new InvalidOperationException($"Unable to extract conditions from {expression}", exn);
            }
        }
    }
}