using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal static class DiagnosticDescriptors
{
    public static DiagnosticDescriptor IncompatibleLanguageVersion { get; } = new DiagnosticDescriptor(
        id: "NUEF0001",
        title: "Incompatible C# version.",
        messageFormat: "Must target at least C# language version 10 to use builder generator (target version is {0}).",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidTargetType { get; } = new DiagnosticDescriptor(
        id: "NUEF0002",
        title: "Invalid target type.",
        messageFormat: "Target must be a valid type.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidSerializerContextType { get; } = new DiagnosticDescriptor(
        id: "NUEF0003",
        title: "Invalid serializer context type.",
        messageFormat: "Serializer context must be a valid type.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor EFRelationalReferenceRequired { get; } = new DiagnosticDescriptor(
        id: "NUEF0004",
        title: "Missing required reference.",
        messageFormat: "Project must reference Microsoft.EntityFrameworkCore.Relational assembly.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor UnexpectedError { get; } = new DiagnosticDescriptor(
        id: "NUEF0000",
        title: "Unexpected error occured.",
        messageFormat: "{0}: {1} | {2}",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}