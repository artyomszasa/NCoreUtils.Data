using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal static class DiagnosticDescriptors
{
    public static DiagnosticDescriptor IncompatibleLanguageVersion { get; } = new DiagnosticDescriptor(
        id: "NUD0001",
        title: "Incompatible C# version.",
        messageFormat: "Must target at least C# language version 10 to use builder generator (target version is {0}).",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidContextSymbol { get; } = new DiagnosticDescriptor(
        id: "NUD0002",
        title: "Invalid symbol.",
        messageFormat: "Target context symbol is not a named symbol.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidContextNode { get; } = new DiagnosticDescriptor(
        id: "NUD0003",
        title: "Invalid node.",
        messageFormat: "Target context node is not a type declaration node.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidEntitySymbol { get; } = new DiagnosticDescriptor(
        id: "NUD0004",
        title: "Invalid entity symbol.",
        messageFormat: "Argument passed to the DataEntityAttribute is not a valid named type symbol.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor DataPropertyBuilderMissing { get; } = new DiagnosticDescriptor(
        id: "NUD0005",
        title: "Missing NCoreUtils.Data.Abstractions reference?",
        messageFormat: "NCoreUtils.Data.Build.DataPropertyBuilder cannot be resolved. In order to generate data definitions NCoreUtils.Data.Abstractions must be referenced. Consider adding package reference to the project.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor FirestoreReferenceMissing { get; } = new DiagnosticDescriptor(
        id: "NUD0006",
        title: "Missing NCoreUtils.Data.Google.Cloud.Firestore reference.",
        messageFormat: "In order to generate Firestore decorations NCoreUtils.Data.Google.Cloud.Firestore must be referenced. Consider adding package reference to the project.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor NameFactoryGetTypeNameMissing { get; } = new DiagnosticDescriptor(
        id: "NUD0007",
        title: "Missing usable GetName method.",
        messageFormat: "{0} must define GetName(System.Type type) method to be used as name factory.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor NameFactoryGetPropertyNameMissing { get; } = new DiagnosticDescriptor(
        id: "NUD0008",
        title: "Missing usable GetName method.",
        messageFormat: "{0} must define GetName(System.Reflection.PropertyInfo property) method to be used as name factory.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor CollectionGetEnumeratorMissing { get; } = new DiagnosticDescriptor(
        id: "NUD0009",
        title: "Missing usable GetEnumerator method.",
        messageFormat: "{0} must define GetEnumerator() method to be used as collection.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor NoMutableCollectionFound { get; } = new DiagnosticDescriptor(
        id: "NUD0010",
        title: "No mutable collection factory found for immutable collection.",
        messageFormat: "No mutable collection factory found for immutable collection {0}.",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor UnexpectedError { get; } = new DiagnosticDescriptor(
        id: "NUD0000",
        title: "Unexpected error occured.",
        messageFormat: "{0}: {1} | {2}",
        category: "CodeGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}