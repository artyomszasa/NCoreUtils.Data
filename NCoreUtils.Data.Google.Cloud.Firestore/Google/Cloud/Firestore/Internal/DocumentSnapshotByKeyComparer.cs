using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public class DocumentSnapshotByKeyComparer : IComparer<DocumentSnapshot>
    {
        private static readonly Value _nullValue = new() { NullValue = global::Google.Protobuf.WellKnownTypes.NullValue.NullValue };

        public static DocumentSnapshotByKeyComparer ById { get; }
            = new DocumentSnapshotByKeyComparer(FieldPath.DocumentId, false);

        private readonly FieldPath _path;

        private readonly bool _isDescending;

        public DocumentSnapshotByKeyComparer(FieldPath path, bool isDescending)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _isDescending = isDescending;
        }

        public int Compare(DocumentSnapshot? x, DocumentSnapshot? y)
        {
            var a = x is not null && x.TryGetValue<Value>(_path, out var v) ? v : _nullValue;
            var b = y is not null && y.TryGetValue<Value>(_path, out v) ? v : _nullValue;
            var res = ValueComparer.Instance.Compare(a, b);
            return _isDescending ? res * -1 : res;
        }
    }
}