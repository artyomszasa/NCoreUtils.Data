using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            LessThanOrEqualTo = 6,
            ArrayContainsAny = 7,
            In = 8,
            AlwaysFalse = 9
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
            "<=",
            "@@",
            "in"
        };

        public static FirestoreCondition AlwaysFalse { get; } = new FirestoreCondition(default!, Op.AlwaysFalse, default!);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SafeEq(object a, object b)
        {
            if (a is null)
            {
                return b is null;
            }
            if (b is null)
            {
                return false;
            }
            return a.Equals(b);
        }

        private static bool SequenceEqual(IEnumerable a, IEnumerable b)
        {
            if (a is null)
            {
                return b is null;
            }
            if (b is null)
            {
                return false;
            }
            var ae = a.GetEnumerator();
            var be = b.GetEnumerator();
            while (true)
            {
                var anext = ae.MoveNext();
                var bnext = be.MoveNext();
                if (anext)
                {
                    if (bnext)
                    {
                        if (!SafeEq(ae.Current, be.Current))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return !bnext;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(FirestoreCondition a, FirestoreCondition b)
            => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(FirestoreCondition a, FirestoreCondition b)
            => !a.Equals(b);

        public FieldPath Path { get; }

        public Op Operation { get; }

        public object Value { get; }

        public FirestoreCondition(FieldPath path, Op operation, object value)
        {
            if (operation != Op.AlwaysFalse)
            {
                if (path is null)
                {
                    throw new ArgumentNullException(nameof(path));
                }
            }
            Path = path;
            Operation = operation;
            Value = value;
        }

        public bool Equals(FirestoreCondition other)
        {
            if (Operation == Op.AlwaysFalse)
            {
                return other.Operation == Op.AlwaysFalse;
            }
            if (Operation == Op.ArrayContainsAny)
            {
                return other.Operation == Op.ArrayContainsAny
                    && _pathComparer.Equals(Path, other.Path)
                    && SequenceEqual((IEnumerable)Value, (IEnumerable)other.Value);
            }
            return Operation == other.Operation
                && _pathComparer.Equals(Path, other.Path)
                && (Value?.Equals(other.Value) ?? false);
        }

        public override bool Equals(object obj) => obj is FirestoreCondition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Path, Operation, Value);

        private string StringifyValue(object value)
            => value switch
            {
                null => string.Empty,
                string s => s,
                IEnumerable enumerable => $"[{string.Join(", ", enumerable.Cast<object>().Select(StringifyValue))}]",
                var o => o.ToString()
            };

        public override string ToString()
            => Operation < Op.AlwaysFalse
                ? $"{{{Path} {_opNames[(int)Operation]} {StringifyValue(Value)}}}"
                : "FALSE";
    }
}