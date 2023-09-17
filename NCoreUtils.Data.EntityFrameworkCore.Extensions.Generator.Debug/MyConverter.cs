using System.Text.Json.Serialization;

namespace NCoreUtils.Data;

[JsonSerializable(typeof(IReadOnlyList<MyData>))]
internal partial class MyDataSerializerContext : JsonSerializerContext { }

[AsJsonStringConverter(typeof(IReadOnlyList<MyData>), typeof(MyDataSerializerContext))]
public partial class MyConverter { }