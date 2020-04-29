using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions
{
    public class FirestoreFieldExpression : Expression, IExtensionExpression
    {
        private static readonly MethodInfo _gmValue = typeof(DocumentSnapshot)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "GetValue" && m.IsGenericMethodDefinition && SingleParameter(m, typeof(FieldPath)));

        private static bool SingleParameter(MethodInfo method, Type type)
            => method.GetParameters() switch
            {
                var ps when ps.Length == 1 && ps[0].ParameterType == type => true,
                _ => false
            };

        public override bool CanReduce => true;

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type { get; }

        public ImmutableList<string>? RawPath { get; }

        public FieldPath Path { get; }

        public Expression Instance { get; }

        private FirestoreFieldExpression(Expression instance, ImmutableList<string>? rawPath, FieldPath path, Type type)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            RawPath = rawPath;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            if (instance.Type != typeof(DocumentSnapshot))
            {
                throw new InvalidOperationException("Firestore field expression can only be created for document snapshots.");
            }
        }

        public FirestoreFieldExpression(Expression instance, ImmutableList<string> rawPath, Type type)
            : this(
                instance,
                rawPath ?? throw new ArgumentNullException(nameof(rawPath), "For special paths use overloaded constructor."),
                new FieldPath(rawPath.ToArray()),
                type)
        { }

        public FirestoreFieldExpression(Expression instance, FieldPath specialPath, Type type)
            : this(instance, default, specialPath, type)
        { }

        public override Expression Reduce()
        {
            if (Path.Equals(FieldPath.DocumentId))
            {
                // FIXME: cache property
                return Property(Instance, "Id");
            }
            return Call(
                Instance,
                _gmValue.MakeGenericMethod(Type),
                Constant(Path)
            );
        }

        public override string ToString()
            => $"{Instance}[{Path}]";

        public Expression AcceptNoReduce(ExpressionVisitor visitor)
        {
            var newInstance = visitor.Visit(Instance);
            return ReferenceEquals(newInstance, Instance)
                ? this
                : new FirestoreFieldExpression(newInstance, RawPath, Path, Type);
        }
    }
}