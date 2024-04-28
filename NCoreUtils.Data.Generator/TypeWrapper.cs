using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Data;

internal struct TypeWrapper(ITypeSymbol? symbol)
{
    public static string GetSafeName(ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol named && named.IsGenericType)
        {
            return $"{symbol.Name}Of{string.Join(string.Empty, named.TypeArguments.Select(GetSafeName))}";
        }
        if (symbol is IArrayTypeSymbol array)
        {
            return $"ArrayOf{GetSafeName(array.ElementType)}";
        }
        return symbol.Name;
    }

    private readonly ITypeSymbol? _symbol = symbol;

    private string? _fullName;

    private string? _safeName;

    private TypeSyntax? _syntax;

    public ITypeSymbol Symbol { get; } = symbol ?? throw new InvalidOperationException("Trying to access Symbol of an empty TypeWrapper.");

    public readonly string Name => Symbol.Name;

    public readonly bool Empty => _symbol is null;

    public string FullName => _fullName ??= Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string SafeName => _safeName ??= GetSafeName(Symbol);

    public TypeSyntax Syntax => _syntax ??= SyntaxFactory.ParseTypeName(FullName);
}