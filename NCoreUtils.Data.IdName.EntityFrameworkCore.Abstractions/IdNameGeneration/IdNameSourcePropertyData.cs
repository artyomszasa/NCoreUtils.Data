using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace NCoreUtils.Data.IdNameGeneration;

public class IdNameSourcePropertyData
{
    public static IdNameSourcePropertyData Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] string typeName,
        string sourcePropertyName,
        string[] additionalPropertyNames)
        => new(typeName, sourcePropertyName, additionalPropertyNames);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public string TypeName { get; }

    public string SourcePropertyName { get; }

    public string[] AdditionalPropertyNames { get; }

    [JsonConstructor]
    internal IdNameSourcePropertyData(
        string typeName,
        string sourcePropertyName,
        string[] additionalPropertyNames)
    {
        TypeName = typeName ?? throw new System.ArgumentNullException(nameof(typeName));
        SourcePropertyName = sourcePropertyName;
        AdditionalPropertyNames = additionalPropertyNames;
    }
}