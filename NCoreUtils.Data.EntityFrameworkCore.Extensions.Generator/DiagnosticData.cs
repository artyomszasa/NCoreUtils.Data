using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal sealed class DiagnosticData(DiagnosticDescriptor descriptor, Location? location, object?[]? messageArgs)
{
    public DiagnosticDescriptor Descriptor { get; } = descriptor;

    public Location? Location { get; } = location;

    public object?[]? MessageArgs { get; } = messageArgs;
}