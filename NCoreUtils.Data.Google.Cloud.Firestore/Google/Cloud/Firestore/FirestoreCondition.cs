using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public struct FirestoreCondition : IEquatable<FirestoreCondition>
    {
        public enum Op
        {
            NoOp = 0,
            ArrayContains = 1,
            EqualTo = 2,
            GreaterThan = 3,
            GreaterThanOrEqualTo = 4,
            LessThan = 5,
            LessThanOrEqualTo = 6
        }

        private static readonly EqualityComparer<FieldPath> _pathComparer = EqualityComparer<FieldPath>.Default;

        private static readonly string[] _opNames = new []
        {
            "?",
            "@",
            "=",
            ">",
            ">=",
            "<",
            "<="
        };

        public static bool operator==(FirestoreCondition a, FirestoreCondition b)
            => a.Equals(b);

        public static bool operator!=(FirestoreCondition a, FirestoreCondition b)
            => !a.Equals(b);

        public FieldPath Path { get; }

        public Op Operation { get; }

        public object Value { get; }

        public FirestoreCondition(FieldPath path, Op operation, object value)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Operation = operation;
            Value = value;
        }

        public bool Equals(FirestoreCondition other)
            => _pathComparer.Equals(Path, other.Path)
                && Operation == other.Operation
                && (Value?.Equals(other.Value) ?? false);

        public override bool Equals(object obj) => obj is FirestoreCondition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Path, Operation, Value);

        public override string ToString() => $"{{{Path} {_opNames[(int)Operation]} {Value}}}";
    }
}