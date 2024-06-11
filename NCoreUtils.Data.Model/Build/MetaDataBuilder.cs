using System.Collections.Generic;

namespace NCoreUtils.Data.Build;

public class MetadataBuilder
{
    public Dictionary<string, object?> Metadata { get; } = [];

    public void SetMetadata(string key, object? value)
        => Metadata[key] = value;

    public object? GetMetadata(string key)
        => Metadata.TryGetValue(key, out var data) ? data : default;
}