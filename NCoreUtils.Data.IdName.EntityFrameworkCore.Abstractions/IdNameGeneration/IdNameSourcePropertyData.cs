using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.IdNameGeneration;

public class IdNameSourcePropertyData
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public string TypeName { get; }

    public string SourcePropertyName { get; }

    public string[] AdditionalPropertyNames { get; }

    public IdNameSourcePropertyData(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] string typeName,
        string sourcePropertyName,
        string[] additionalPropertyNames)
    {
        TypeName = typeName ?? throw new System.ArgumentNullException(nameof(typeName));
        SourcePropertyName = sourcePropertyName;
        AdditionalPropertyNames = additionalPropertyNames;
    }
}