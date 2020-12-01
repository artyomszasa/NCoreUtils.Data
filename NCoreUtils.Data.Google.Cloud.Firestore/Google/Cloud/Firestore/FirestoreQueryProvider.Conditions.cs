using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public partial class FirestoreQueryProvider
    {
        private struct PathOrValue
        {
            public static PathOrValue CreatePath(FieldPath path)
            {
                if (path is null)
                {
                    throw new ArgumentNullException(nameof(path));
                }
                return new PathOrValue(path, default!);
            }

            public static PathOrValue CreateValue(object value) => new PathOrValue(null!, value);

            public bool IsPath => !(Path is null);

            public FieldPath Path { get; }

            public object Value { get; }

            PathOrValue(FieldPath path, object value)
            {
                Path = path;
                Value = value;
            }
        }

        private static readonly ImmutableDictionary<FirestoreCondition.Op, FirestoreCondition.Op> _conditionReverseMap = new Dictionary<FirestoreCondition.Op, FirestoreCondition.Op>
        {
            { FirestoreCondition.Op.EqualTo, FirestoreCondition.Op.EqualTo },
            { FirestoreCondition.Op.GreaterThan, FirestoreCondition.Op.LessThan },
            { FirestoreCondition.Op.GreaterThanOrEqualTo, FirestoreCondition.Op.LessThanOrEqualTo },
            { FirestoreCondition.Op.LessThan, FirestoreCondition.Op.GreaterThan },
            { FirestoreCondition.Op.LessThanOrEqualTo, FirestoreCondition.Op.GreaterThanOrEqualTo },
            { FirestoreCondition.Op.ArrayContains, FirestoreCondition.Op.In },
            { FirestoreCondition.Op.In, FirestoreCondition.Op.ArrayContains },
        }.ToImmutableDictionary();

        private static readonly MethodInfo _gmContains = GetMethod<IEnumerable<int>, int, bool>(Enumerable.Contains).GetGenericMethodDefinition();

        private static readonly MethodInfo _gmContainsAny = GetMethod<IEnumerable<int>, IEnumerable<int>, bool>(FirestoreQueryableExtensions.ContainsAny).GetGenericMethodDefinition();

        private static MethodInfo GetMethod<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func) => func.Method;

        private static FirestoreCondition.Op Reverse(FirestoreCondition.Op op)
            => _conditionReverseMap.TryGetValue(op, out var res) ? res : throw new InvalidOperationException($"Unable to reverse operation {op}.");

        private static void CreateCondition(FirestoreCondition.Op operation, (PathOrValue Left, PathOrValue Right) args, List<FirestoreCondition> conditions)
        {
            ref readonly PathOrValue left = ref args.Left;
            ref readonly PathOrValue right = ref args.Right;
            if (left.IsPath)
            {
                if (right.IsPath)
                {
                    throw new InvalidOperationException("Only path ~ object comparison is supported.");
                }
                conditions.Add(new FirestoreCondition(left.Path, operation, right.Value));
            }
            else
            {
                if (right.IsPath)
                {
                    conditions.Add(new FirestoreCondition(right.Path, Reverse(operation), left.Value));
                }
                else
                {
                    throw new InvalidOperationException("Only path ~ object comparison is supported.");
                }
            }
        }

        private PathOrValue ExtractPathOrValue(ParameterExpression arg, Expression expression)
        {
            if (expression.TryExtractConstant(out var value))
            {
                return PathOrValue.CreateValue(value);
            }
            if (TryResolvePath(expression, arg, out var path))
            {
                return PathOrValue.CreatePath(path);
            }
            if (expression is UnaryExpression u && u.NodeType == ExpressionType.Convert)
            {
                return ExtractPathOrValue(arg, u.Operand);
            }
            if (expression is NewArrayExpression arrayExpr)
            {
                var arrayValue = Array.CreateInstance(arrayExpr.Type.GetElementType(), arrayExpr.Expressions.Count);
                var i = 0;
                foreach (var expr in arrayExpr.Expressions)
                {
                    if (expr.TryExtractConstant(out var item))
                    {
                        arrayValue.SetValue(item, i++);
                    }
                    else
                    {
                        break;
                    }
                }
                if (i == arrayValue.Length)
                {
                    return PathOrValue.CreateValue(arrayValue);
                }
            }
            throw new InvalidOperationException($"Unable to resolve path {expression}.");
        }

        private (PathOrValue Left, PathOrValue Right) ExtractPathOrValue(ParameterExpression arg, BinaryExpression expression)
        {
            return (
                ExtractPathOrValue(arg, expression.Left),
                ExtractPathOrValue(arg, expression.Right)
            );
        }

        private void ExtractConditions(ParameterExpression arg, List<FirestoreCondition> conditions, Expression expression)
        {
            switch (expression)
            {
                case UnaryExpression un:
                    if (un.NodeType == ExpressionType.Not && TryResolvePath(un.Operand, arg, out var upath))
                    {
                        conditions.Add(new FirestoreCondition(upath, FirestoreCondition.Op.EqualTo, false));
                        break;
                    }
                    throw new NotSupportedException($"Not supported expression {expression}.");
                case BinaryExpression bin:
                    switch (bin.NodeType)
                    {
                        case ExpressionType.AndAlso:
                            ExtractConditions(arg, conditions, bin.Left);
                            ExtractConditions(arg, conditions, bin.Right);
                            break;
                        case ExpressionType.Equal:
                            CreateCondition(FirestoreCondition.Op.EqualTo, ExtractPathOrValue(arg, bin), conditions);
                            break;
                        case ExpressionType.LessThan:
                            CreateCondition(FirestoreCondition.Op.LessThan, ExtractPathOrValue(arg, bin), conditions);
                            break;
                        case ExpressionType.LessThanOrEqual:
                            CreateCondition(FirestoreCondition.Op.LessThanOrEqualTo, ExtractPathOrValue(arg, bin), conditions);
                            break;
                        case ExpressionType.GreaterThan:
                            CreateCondition(FirestoreCondition.Op.GreaterThan, ExtractPathOrValue(arg, bin), conditions);
                            break;
                        case ExpressionType.GreaterThanOrEqual:
                            CreateCondition(FirestoreCondition.Op.GreaterThanOrEqualTo, ExtractPathOrValue(arg, bin), conditions);
                            break;
                        default:
                            throw new NotSupportedException($"Not supported expression {expression}.");
                    }
                    break;
                case MethodCallExpression methodCall when methodCall.Method.IsGenericMethod && methodCall.Method.GetGenericMethodDefinition().Equals(_gmContains):
                    CreateCondition(
                        FirestoreCondition.Op.ArrayContains,
                        (ExtractPathOrValue(arg, methodCall.Arguments[0]), ExtractPathOrValue(arg, methodCall.Arguments[1])),
                        conditions);
                    break;
                case MethodCallExpression methodCall when methodCall.Method.IsGenericMethod && methodCall.Method.GetGenericMethodDefinition().Equals(_gmContainsAny):
                    CreateCondition(
                        FirestoreCondition.Op.ArrayContainsAny,
                        (ExtractPathOrValue(arg, methodCall.Arguments[0]), ExtractPathOrValue(arg, methodCall.Arguments[1])),
                        conditions);
                    break;
                default:
                    if (expression.Type == typeof(bool))
                    {
                        if (expression.TryExtractConstant(out var boxed))
                        {
                            var boolValue = (bool)boxed;
                            if (!boolValue)
                            {
                                conditions.Add(FirestoreCondition.AlwaysFalse);
                            }
                            break;
                        }
                        if (TryResolvePath(expression, arg, out var path))
                        {
                            conditions.Add(new FirestoreCondition(path, FirestoreCondition.Op.EqualTo, true));
                            break;
                        }
                    }
                    throw new NotSupportedException($"Not supported expression {expression}.");
            }
        }

        protected List<FirestoreCondition> ExtractConditions(LambdaExpression expression)
        {
            try
            {
                var conditions = new List<FirestoreCondition>();
                ExtractConditions(expression.Parameters[0], conditions, expression.Body);
                return conditions;
            }
            catch (Exception exn)
            {
                throw new InvalidOperationException($"Unable to extract conditions from {expression}.", exn);
            }
        }
    }
}