using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal class GenerationException(DiagnosticData error) : Exception
{
    public DiagnosticData Error { get; } = error;

    public GenerationException(DiagnosticDescriptor descriptor, Location? location = default, params object?[]? messageArgs)
        : this(new(descriptor, location, messageArgs))
    { }
}