using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal
{
    public class NestedDocumentSnapshotByKeyComparer : IComparer<DocumentSnapshot>
    {
        private static readonly Value _nullValue = new Value { NullValue = global::Google.Protobuf.WellKnownTypes.NullValue.NullValue };

        private readonly IComparer<DocumentSnapshot> _outerComparer;

        private readonly FieldPath _path;

        private readonly bool _isDescending;

        public NestedDocumentSnapshotByKeyComparer(
            IComparer<DocumentSnapshot> outerComparer,
            FieldPath path,
            bool isDescending)
        {
            _outerComparer = outerComparer ?? throw new ArgumentNullException(nameof(outerComparer));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _isDescending = isDescending;
        }

        public int Compare(DocumentSnapshot x, DocumentSnapshot y)
        {
            var outerResult = _outerComparer.Compare(x, y);
            if (outerResult != 0)
            {
                return outerResult;
            }
            var a = !(x is null) && x.TryGetValue<Value>(_path, out var v) ? v : _nullValue;
            var b = !(y is null) && y.TryGetValue<Value>(_path, out v) ? v : _nullValue;
            var res = ValueComparer.Instance.Compare(a, b);
            return _isDescending ? res * -1 : res;
        }
    }
}