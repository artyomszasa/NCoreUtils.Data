using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        private static readonly MethodInfo _mHasFlag = GetMethod<Enum, bool>(default(NumberStyles).HasFlag);

        private static MethodInfo GetMethod<TArg, TResult>(Func<TArg, TResult> func) => func.Method;

        private static MethodInfo GetMethod<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func) => func.Method;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNumericType(Type type)
        {
            if (type.IsEnum)
            {
                return false;
            }
            var code = Type.GetTypeCode(type);
            return code >= TypeCode.SByte && code <= TypeCode.UInt64;
        }


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

        private object PrepareEnumValue(object value, Type enumType)
        {
            // value can be either enum or numeric representation --> unify value
            var v = value.GetType() == enumType ? value : Enum.ToObject(enumType, value);
            return (Model.Configuration.ConversionOptions ?? FirestoreConversionOptions.Default).EnumHandling switch
            {
                FirestoreEnumHandling.AlwaysAsString => enumType.IsDefined(typeof(FlagsAttribute), true)
                    ? FirestoreConvert.EnumToFlagsString(enumType, v)
                    : v.ToString(),
                FirestoreEnumHandling.AsNumberOrNumberArray => ((IConvertible)v).ToInt64(CultureInfo.InvariantCulture),
                FirestoreEnumHandling.AsSingleNumber => ((IConvertible)v).ToInt64(CultureInfo.InvariantCulture),
                FirestoreEnumHandling.AsStringOrStringArray => v.ToString(),
                _ => throw new InvalidOperationException("should never happen")
            };
        }

        private PathOrValue HandleEnumValues(PathOrValue source, Type expressionType)
        {
            if (source.IsPath)
            {
                return source;
            }
            if (expressionType.IsEnum)
            {
                // source is value which has enum type.
                if (source.Value is null)
                {
                    // no conversion for null values
                    return source;
                }
                return PathOrValue.CreateValue(PrepareEnumValue(source.Value, expressionType));
            }
            if (CollectionFactory.IsCollection(expressionType, out var elementType) && elementType.IsEnum)
            {
                var targetElementType = (Model.Configuration.ConversionOptions ?? FirestoreConversionOptions.Default).EnumHandling switch
                {
                    FirestoreEnumHandling.AlwaysAsString => typeof(string),
                    FirestoreEnumHandling.AsNumberOrNumberArray => typeof(long),
                    FirestoreEnumHandling.AsSingleNumber => typeof(long),
                    FirestoreEnumHandling.AsStringOrStringArray => typeof(string),
                    _ => throw new InvalidOperationException("should never happen")
                };
                // source is a colection which elements has enum type.
                if (source.Value is null || !CollectionFactory.TryCreate(typeof(IReadOnlyList<>).MakeGenericType(targetElementType), out var factory))
                {
                    // no conversion for null values
                    return source;
                }
                var builder = factory.CreateBuilder();
                foreach (var item in (System.Collections.IEnumerable)source.Value)
                {
                    builder.Add(PrepareEnumValue(item, elementType));
                }
                return PathOrValue.CreateValue(builder.Build());
            }
            // not an enum...
            return source;
        }

        private PathOrValue ExtractPathOrValue(ParameterExpression arg, Expression expression, out Type? memberType)
        {
            if (expression.TryExtractConstant(out var value))
            {
                memberType = value?.GetType();
                return HandleEnumValues(PathOrValue.CreateValue(value!), expression.Type);
            }
            if (TryResolvePath(expression, arg, out var path, out memberType))
            {
                return PathOrValue.CreatePath(path);
            }
            if (expression is UnaryExpression u && u.NodeType == ExpressionType.Convert)
            {
                return HandleEnumValues(ExtractPathOrValue(arg, u.Operand, out memberType), expression.Type);
            }
            if (expression is NewArrayExpression arrayExpr)
            {
                memberType = arrayExpr.Type.GetElementType();
                var arrayValue = Array.CreateInstance(memberType, arrayExpr.Expressions.Count);
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
                    return HandleEnumValues(PathOrValue.CreateValue(arrayValue), expression.Type);
                }
            }
            throw new InvalidOperationException($"Unable to resolve path {expression}.");
        }

        private PathOrValue ExtractPathOrValue(ParameterExpression arg, Expression expression)
            => ExtractPathOrValue(arg, expression, out var _);

        private (PathOrValue Left, PathOrValue Right) ExtractPathOrValue(ParameterExpression arg, Expression leftSource, Expression rightSource)
        {
            var left = ExtractPathOrValue(arg, leftSource, out var tyLeft);
            var right = ExtractPathOrValue(arg, rightSource, out var tyRight);
            // consolidate types
            // if (tyLeft == tyRight)
            // {
            //     return (left, right);
            // }
            if (!(tyLeft is null) && tyLeft.IsEnum && (tyRight is null || IsNumericType(tyRight) || rightSource.Type.IsArray))
            {
                return (HandleEnumValues(left, tyLeft), HandleEnumValues(right, tyLeft));
            }
            if (!(tyRight is null) && tyRight.IsEnum && (tyLeft is null || IsNumericType(tyLeft) || leftSource.Type.IsArray))
            {
                return (HandleEnumValues(left, tyRight), HandleEnumValues(right, tyRight));
            }
            return (left, right);
        }

        private (PathOrValue Left, PathOrValue Right) ExtractPathOrValue(ParameterExpression arg, BinaryExpression expression)
            => ExtractPathOrValue(arg, expression.Left, expression.Right);

        private void ExtractConditions(ParameterExpression arg, List<FirestoreCondition> conditions, Expression expression)
        {
            switch (expression)
            {
                case UnaryExpression un:
                    if (un.NodeType == ExpressionType.Not && TryResolvePath(un.Operand, arg, out var upath, out var _))
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
                case MethodCallExpression methodCall when methodCall.Method == _mHasFlag:
                    if (!methodCall.Object.Type.IsDefined(typeof(FlagsAttribute), true))
                    {
                        throw new InvalidOperationException($"HasFlag cannot be used on non-flags type {methodCall.Object.Type}.");
                    }
                    var options = Model.ConversionOptions ?? FirestoreConversionOptions.Default;
                    if (options.EnumHandling == FirestoreEnumHandling.AlwaysAsString || options.EnumHandling == FirestoreEnumHandling.AsSingleNumber)
                    {
                        throw new InvalidOperationException($"To use HasFlag enum handling must be set to either {FirestoreEnumHandling.AsNumberOrNumberArray} or {FirestoreEnumHandling.AsStringOrStringArray}");
                    }
                    CreateCondition(
                        FirestoreCondition.Op.ArrayContains,
                        (ExtractPathOrValue(arg, methodCall.Object), ExtractPathOrValue(arg, methodCall.Arguments[0])),
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
                        if (TryResolvePath(expression, arg, out var path, out var _))
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