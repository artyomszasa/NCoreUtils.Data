using System.Text.Json.Serialization;

namespace NCoreUtils.Data.IdNameGeneration;

[JsonSerializable(typeof(IdNameSourcePropertyData))]
public partial class IdNameGenerationAnnotationSerializationContext : JsonSerializerContext
{ }