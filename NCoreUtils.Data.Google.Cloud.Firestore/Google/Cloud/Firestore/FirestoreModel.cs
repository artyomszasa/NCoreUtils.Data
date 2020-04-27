using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NCoreUtils.Data.Build;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public class FirestoreModel : DataModel
    {
        public IReadOnlyDictionary<Type, DataEntity> ByType { get; }

        public FirestoreModel(DataModelBuilder builder)
            : base(builder)
        {
            ByType = Entities.ToDictionary(e => e.EntityType);
        }

        public bool TryGetDataEntity(Type type, [NotNullWhen(true)] out DataEntity? entity)
            => ByType.TryGetValue(type, out entity);
    }
}