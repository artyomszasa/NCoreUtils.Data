using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal sealed class GenerationContext
{
    public SemanticModel SemanticModel { get; }

    public Compilation Compilation { get; }

    public ITypeSymbol ArrayOfByte { get; }

    public INamedTypeSymbol ListOfT { get; }

    public INamedTypeSymbol HashSetOfT { get; }

    public GenerationContext(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
        Compilation = semanticModel.Compilation;
        ArrayOfByte = Compilation.CreateArrayTypeSymbol(Compilation.GetSpecialType(SpecialType.System_Byte));
        ListOfT = Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")
            ?? throw new InvalidOperationException("System.Collections.Generic.List<T> cannot be resolved.");
        HashSetOfT = Compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1")
            ?? throw new InvalidOperationException("System.Collections.Generic.HashSet<T> cannot be resolved.");
    }
}