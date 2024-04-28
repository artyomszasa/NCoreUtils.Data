using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal sealed class DiagnosticData(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
{
    public DiagnosticDescriptor Descriptor { get; } = descriptor;

    public Location? Location { get; } = location;

    public object?[]? MessageArgs { get; } = messageArgs;

    public void Deconstruct(out DiagnosticDescriptor descriptor, out Location? location, out object?[]? messageArgs)
    {
        descriptor = Descriptor;
        location = Location;
        messageArgs = MessageArgs;
    }
}