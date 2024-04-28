using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Data;

internal class EntityTarget(INamedTypeSymbol type, INamedTypeSymbol? nameFactory, bool hasNoKey)
{
    private TypeWrapper _type = new(type);

    public ref TypeWrapper Type => ref _type;

    public NameFactoryInfo? NameFactory { get; } = nameFactory is null ? default : new NameFactoryInfo(nameFactory);

    public bool HasNoKey { get; } = hasNoKey;
}

internal class ContextTarget(
    SemanticModel semanticModel,
    TypeDeclarationSyntax syntax,
    INamedTypeSymbol contextType,
    ImmutableArray<EntityTarget> entities,
    INamedTypeSymbol? defaultNameFactory,
    bool generateFirestoreDecorators)
{
    private TypeWrapper _contextType = new(contextType);

    public SemanticModel SemanticModel { get; } = semanticModel;

    public TypeDeclarationSyntax Syntax { get; } = syntax;

    public ImmutableArray<EntityTarget> Entities { get; } = entities;

    public ref TypeWrapper ContextType => ref _contextType;

    public NameFactoryInfo? DefaultNameFactory { get; } = defaultNameFactory is null ? default : new NameFactoryInfo(defaultNameFactory);

    public bool GenerateFirestoreDecorators { get; } = generateFirestoreDecorators;
}