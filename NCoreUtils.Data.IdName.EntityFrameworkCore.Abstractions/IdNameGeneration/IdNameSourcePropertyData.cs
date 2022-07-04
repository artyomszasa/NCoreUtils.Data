using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace NCoreUtils.Data.IdNameGeneration;

public class IdNameSourcePropertyData
{
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    [UnconditionalSuppressMessage("Trimming", "IL2068", Justification = "Only used by JSON serializer.")]
    private static string SuppressTrimWarning(string typeName)
        => typeName;

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
    public IdNameSourcePropertyData(
        string typeName,
        string sourcePropertyName,
        string[] additionalPropertyNames)
    {
        TypeName = SuppressTrimWarning(typeName) ?? throw new ArgumentNullException(nameof(typeName));
        SourcePropertyName = sourcePropertyName;
        AdditionalPropertyNames = additionalPropertyNames;
    }
}