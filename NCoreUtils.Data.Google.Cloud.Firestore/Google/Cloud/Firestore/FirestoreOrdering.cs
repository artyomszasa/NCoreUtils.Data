using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

public readonly struct FirestoreOrdering : IEquatable<FirestoreOrdering>
{
    private static readonly EqualityComparer<FieldPath> _pathComparer = EqualityComparer<FieldPath>.Default;

    public static bool operator==(FirestoreOrdering a, FirestoreOrdering b)
        => a.Equals(b);

    public static bool operator!=(FirestoreOrdering a, FirestoreOrdering b)
        => !a.Equals(b);

    public FieldPath Path { get; }

    public FirestoreOrderingDirection Direction { get; }

    public FirestoreOrdering(FieldPath path, FirestoreOrderingDirection direction)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Direction = direction;
    }

    public override bool Equals(object? obj)
        => obj is FirestoreOrdering other && Equals(other);

    public bool Equals(FirestoreOrdering other)
        => _pathComparer.Equals(Path, other.Path)
            && Direction == other.Direction;

    public override int GetHashCode()
        => HashCode.Combine(Path, Direction);

    public FirestoreOrdering Revert()
        => new(Path, Direction switch
        {
            FirestoreOrderingDirection.Ascending => FirestoreOrderingDirection.Descending,
            _ => FirestoreOrderingDirection.Ascending
        });

    public override string ToString()
        => $"{Path} {Direction}";
}