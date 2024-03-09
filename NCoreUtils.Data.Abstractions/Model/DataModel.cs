using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NCoreUtils.Data.Build;

namespace NCoreUtils.Data.Model;

public class DataModel(DataModelBuilder builder) : Metadata(builder.Metadata.ToImmutableDictionary())
{
    public IReadOnlyList<DataEntity> Entities { get; } = builder.Entities.Select(e => e.Build()).ToArray();
}