using System.Text.Json.Serialization;

namespace NCoreUtils.Data;

[JsonSerializable(typeof(IReadOnlyList<MyData>))]
internal partial class MyDataSerializerContext : JsonSerializerContext { }

[AsJsonStringConverter(typeof(IReadOnlyList<MyData>), typeof(MyDataSerializerContext), Nullable = false)]
public partial class MyConverter { }