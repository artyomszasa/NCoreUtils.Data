using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Data;

internal struct SymbolName
{
    private string? _qualifiedName;

    private string? _namespace;

    private readonly ITypeSymbol SymbolOrErr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Symbol ?? throw new InvalidOperationException("SymbolName is empty.");
    }

    public ITypeSymbol? Symbol { get; }

    public readonly string Name => SymbolOrErr.Name;

    public readonly bool HasValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Symbol is not null;
    }

    public string Namespace
        => _namespace ??= SymbolOrErr.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

    public string QualifiedName
        => _qualifiedName ??= SymbolOrErr.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public SymbolName(ITypeSymbol symbol)
    {
        _qualifiedName = default;
        _namespace = default;
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
    }
}

internal static class SymbolNameExtensions
{
    public static void ThrowIfEmpty(this in SymbolName symbolName, string paramName)
    {
        if (!symbolName.HasValue)
        {
            throw new ArgumentException($"\'{paramName}\' expected to contain a valid symbol.", paramName);
        }
    }
}

internal class ConverterTarget
{
    private const string DefaultArrayValue = "[]";

    private const string DefaultObjectValue = "{}";

    private static string GetDefaultValueFor(ITypeSymbol symbol)
    {
        if (symbol.TypeKind == TypeKind.Array)
        {
            return DefaultArrayValue;
        }
        foreach (var itype in symbol.Interfaces)
        {
            if (itype is INamedTypeSymbol named && named.IsGenericType && named.ConstructedFrom is not null && named.ConstructedFrom.Name == "IEnumerable")
            {
                return DefaultArrayValue;
            }
        }
        return DefaultObjectValue;
    }

    private static bool TryGetItemFor(ITypeSymbol symbol, [MaybeNullWhen(false)] out ITypeSymbol item)
    {
        if (symbol is IArrayTypeSymbol arraySymbol)
        {
            item = arraySymbol.ElementType;
            return true;
        }
        foreach (var itype in symbol.Interfaces)
        {
            if (itype is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 1 && named.ConstructedFrom is not null && named.ConstructedFrom.Name == "IEnumerable")
            {
                item = named.TypeArguments[0];
                return true;
            }
        }
        item = default;
        return false;
    }

    public TypeDeclarationSyntax Node { get; }

    public readonly SymbolName Host;

    public readonly SymbolName Target;

    public readonly SymbolName SerializerContext;

    public string DefaultValue { get; }

    public bool IsArrayLike { get; }

    public readonly SymbolName Item;

    public readonly SymbolName Comparer;

    public ConverterTarget(
        TypeDeclarationSyntax node,
        SymbolName host,
        SymbolName target,
        SymbolName serializerContext,
        string? defaultValue,
        SymbolName comparer)
    {
        host.ThrowIfEmpty(nameof(host));
        target.ThrowIfEmpty(nameof(target));
        serializerContext.ThrowIfEmpty(nameof(serializerContext));
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Host = host;
        Target = target;
        SerializerContext = serializerContext;
        DefaultValue = defaultValue ?? GetDefaultValueFor(target.Symbol!);
        if (TryGetItemFor(target.Symbol!, out var itemSymbol))
        {
            IsArrayLike = true;
            Item = new(itemSymbol);
        }
        else
        {
            IsArrayLike = false;
            Item = default;
        }
        Comparer = comparer;
    }
}