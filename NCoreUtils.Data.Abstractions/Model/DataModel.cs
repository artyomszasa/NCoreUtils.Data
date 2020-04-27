using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NCoreUtils.Data.Build;

namespace NCoreUtils.Data.Model
{
    public class DataModel : Metadata
    {
        public IReadOnlyList<DataEntity> Entities { get; }

        public DataModel(DataModelBuilder builder)
            : base(builder.Metadata.ToImmutableDictionary())
        {
            Entities = builder.Entities.Select(e => e.Build()).ToArray();
        }
    }
}