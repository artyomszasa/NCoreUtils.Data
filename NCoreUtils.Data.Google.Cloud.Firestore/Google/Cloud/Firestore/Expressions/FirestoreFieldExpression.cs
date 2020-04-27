using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Expressions
{
    public class FirestoreFieldExpression : Expression
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

        public FieldPath Path { get; }

        public Expression Instance { get; }

        public FirestoreFieldExpression(Expression instance, FieldPath path, Type type)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            if (instance.Type != typeof(DocumentSnapshot))
            {
                throw new InvalidOperationException("Firestore field expression can only be created for document snapshots.");
            }
        }

        public override Expression Reduce()
            => Call(
                Instance,
                _gmValue.MakeGenericMethod(Type),
                Constant(Path)
            );

        public override string ToString()
            => $"{Instance}[{Path}]";
    }
}