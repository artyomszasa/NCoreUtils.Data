using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions
{
    public class FirestoreFieldExpression : Expression, IExtensionExpression
    {
        private static readonly MethodInfo _mGetValue;

        private static readonly MethodInfo _mContainsField;

        private static readonly MethodInfo _gmConvertFromValue;

        static FirestoreFieldExpression()
        {
            Expression<Func<DocumentSnapshot, FieldPath, Value>> e0 = (doc, name) => doc.GetValue<Value>(name);
            _mGetValue = ((MethodCallExpression)e0.Body).Method;
            Expression<Func<DocumentSnapshot, FieldPath, bool>> e1 = (doc, name) => doc.ContainsField(name);
            _mContainsField = ((MethodCallExpression)e1.Body).Method;
            Expression<Func<FirestoreConverter, Value, int>> e2 = (converter, value) => converter.ConvertFromValue<int>(value);
            _gmConvertFromValue = ((MethodCallExpression)e2.Body).Method.GetGenericMethodDefinition();
        }

        public override bool CanReduce => true;

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type { get; }

        public ImmutableList<string>? RawPath { get; }

        public FieldPath Path { get; }

        public Expression Instance { get; }

        public FirestoreConverter Converter { get; }

        private FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string>? rawPath, FieldPath path, Type type)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Converter = converter ?? throw new ArgumentNullException(nameof(converter));
            RawPath = rawPath;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            if (instance.Type != typeof(DocumentSnapshot))
            {
                throw new InvalidOperationException("Firestore field expression can only be created for document snapshots.");
            }
        }

        public FirestoreFieldExpression(FirestoreConverter converter, Expression instance, ImmutableList<string> rawPath, Type type)
            : this(
                converter,
                instance,
                rawPath ?? throw new ArgumentNullException(nameof(rawPath), "For special paths use overloaded constructor."),
                new FieldPath(rawPath.ToArray()),
                type)
        { }

        public FirestoreFieldExpression(FirestoreConverter converter, Expression instance, FieldPath specialPath, Type type)
            : this(converter, instance, default, specialPath, type)
        { }

        protected virtual Expression Reduce(Type targetType)
        {
            var cpath = Constant(Path);
            return Call(
                Constant(Converter),
                _gmConvertFromValue.MakeGenericMethod(targetType),
                Condition(
                    Call(Instance, _mContainsField, cpath),
                    Call(Instance, _mGetValue, Constant(Path)),
                    Constant(new Value { NullValue = default })
                )
            );
        }

        public override Expression Reduce()
        {
            if (Path.Equals(FieldPath.DocumentId))
            {
                // FIXME: cache property
                return Property(Instance, "Id");
            }
            return Reduce(Type);
        }

        public override string ToString()
            => $"{Instance}[{Path}]";

        public Expression AcceptNoReduce(ExpressionVisitor visitor)
        {
            var newInstance = visitor.Visit(Instance);
            return ReferenceEquals(newInstance, Instance)
                ? this
                : new FirestoreFieldExpression(Converter, newInstance, RawPath, Path, Type);
        }
    }
}