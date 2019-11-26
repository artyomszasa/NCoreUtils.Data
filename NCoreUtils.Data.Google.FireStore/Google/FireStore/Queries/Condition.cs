using System;

namespace NCoreUtils.Data.Google.FireStore.Queries
{
    public struct Condition : IEquatable<Condition>
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

        static readonly string[] _opNames = new []
        {
            "?",
            "@",
            "=",
            ">",
            ">=",
            "<",
            "<="
        };

        public string Path { get; }

        public Op Operation { get; }

        public object Value { get; }

        public Condition(string path, Op operation, object value)
        {
            Path = path;
            Operation = operation;
            Value = value;
        }

        public bool Equals(Condition other)
            => Path == other.Path
                && Operation == other.Operation
                && (Value?.Equals(other.Value) ?? false);

        public override bool Equals(object obj) => obj is Condition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Path, Operation, Value);

        public override string ToString() => $"{{{Path} {_opNames[(int)Operation]} {Value}}}";
    }
}