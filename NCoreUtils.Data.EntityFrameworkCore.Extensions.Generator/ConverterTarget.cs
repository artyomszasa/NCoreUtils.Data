using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NCoreUtils.Data;

internal sealed class SymbolData(
    string qualifiedName,
    string @namespace,
    string name,
    string jsonSerializerContextPropertyName,
    Accessibility declaredAccessibility)
    : IEquatable<SymbolData>
{
    public static bool operator ==(SymbolData? a, SymbolData? b)
        => a is null ? b is null : a.Equals(b);

    public static bool operator !=(SymbolData? a, SymbolData? b)
        => a is null ? b is not null : !a.Equals(b);

    private static string GetJsonSerializerContextPropertyName(ITypeSymbol symbol0)
    {
        if (symbol0 is not INamedTypeSymbol symbol)
        {
            if (symbol0 is IArrayTypeSymbol arraySymbol)
            {
                return $"{GetJsonSerializerContextPropertyName(arraySymbol.ElementType)}Array";
            }
            return symbol0.Name;
        }
        if (!symbol.IsGenericType)
        {
            return symbol.Name;
        }
        var buffer = ArrayPool<char>.Shared.Rent(8 * 1024);
        try
        {
            symbol.ConstructedFrom.Name.AsSpan().CopyTo(buffer.AsSpan());
            var offset = symbol.ConstructedFrom.Name.Length;
            foreach (var argument in symbol.TypeArguments)
            {
                var name = GetJsonSerializerContextPropertyName((INamedTypeSymbol)argument);
                var span = buffer.AsSpan(offset);
                name.AsSpan().CopyTo(span);
                offset += name.Length;
            }
            return new string(buffer, 0, offset);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    [return: NotNullIfNotNull(nameof(symbol))]
    public static SymbolData? FromSymbol(ITypeSymbol? symbol)
    {
        if (symbol is null)
        {
            return default;
        }
        return new(
            qualifiedName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            @namespace: symbol.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)),
            name: symbol.Name,
            jsonSerializerContextPropertyName: GetJsonSerializerContextPropertyName(symbol),
            declaredAccessibility: symbol.DeclaredAccessibility
        );
    }

    public string QualifiedName { get; } = qualifiedName;

    public string Namespace { get; } = @namespace;

    public string Name { get; } = name;

    public string JsonSerializerContextPropertyName { get; }  = jsonSerializerContextPropertyName;

    public Accessibility DeclaredAccessibility { get; } = declaredAccessibility;

    public bool Equals([NotNullWhen(true)] SymbolData? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return other is not null && QualifiedName == other.QualifiedName && DeclaredAccessibility == other.DeclaredAccessibility;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => Equals(obj as SymbolData);

    public override int GetHashCode()
        => StringComparer.InvariantCulture.GetHashCode(QualifiedName);
}

internal sealed class TargetData(
    SymbolData host,
    SymbolData target,
    SymbolData serializerContext,
    string defaultValue,
    bool isArrayLike,
    bool isNullable,
    SymbolData? item,
    SymbolData? comparer)
    : IEquatable<TargetData>
{
    public static bool operator ==(TargetData? a, TargetData? b)
        => a is null ? b is null : a.Equals(b);

    public static bool operator !=(TargetData? a, TargetData? b)
        => a is null ? b is not null : !a.Equals(b);

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

    public static TargetData Create(
        INamedTypeSymbol host,
        ITypeSymbol target,
        INamedTypeSymbol serializerContext,
        string? defaultValue,
        bool? nullable,
        INamedTypeSymbol? comparer)
    {
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (serializerContext is null)
        {
            throw new ArgumentNullException(nameof(serializerContext));
        }
        bool isArrayLike;
        SymbolData? item;
        if (TryGetItemFor(target, out var itemSymbol))
        {
            isArrayLike = true;
            item = SymbolData.FromSymbol(itemSymbol);
        }
        else
        {
            isArrayLike = false;
            item = default;
        }
        return new(
            host: SymbolData.FromSymbol(host),
            target: SymbolData.FromSymbol(target),
            serializerContext: SymbolData.FromSymbol(serializerContext),
            defaultValue: defaultValue ?? (isArrayLike ? "[]" : "{}"),
            isArrayLike: isArrayLike,
            isNullable: nullable ?? target.NullableAnnotation.HasFlag(NullableAnnotation.Annotated),
            item: item,
            comparer: SymbolData.FromSymbol(comparer)
        );
    }

    public bool Equals([NotNullWhen(true)] TargetData? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return other is not null
            && Host == other.Host
            && Target == other.Target
            && SerializerContext == other.SerializerContext
            && DefaultValue == other.DefaultValue
            && IsArrayLike == other.IsArrayLike
            && IsNullable == other.IsNullable
            && Item == other.Item
            && Comparer == other.Comparer;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => Equals(obj as TargetData);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Host.GetHashCode();
            hash = hash * 7 + Target.GetHashCode();
            hash = hash * 7 + SerializerContext.GetHashCode();
            return hash;
        }
    }

    public SymbolData Host { get; } = host ?? throw new ArgumentNullException(nameof(host));

    public SymbolData Target { get; } = target ?? throw new ArgumentNullException(nameof(target));

    public SymbolData SerializerContext { get; } = serializerContext ?? throw new ArgumentNullException(nameof(serializerContext));

    public string DefaultValue { get; } = defaultValue;

    [MemberNotNullWhen(true, nameof(Item))]
    public bool IsArrayLike { get; } = isArrayLike;

    public bool IsNullable { get; } = isNullable;

    public SymbolData? Item { get; } = isArrayLike
        ? item ?? throw new ArgumentNullException(nameof(item), "'item' must not be null for array-like targets.")
        : item;

    public SymbolData? Comparer { get; } = comparer;
}

[Obsolete]
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

[Obsolete]
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

[Obsolete]
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

    public bool IsNullable => Target.Symbol?.NullableAnnotation.HasFlag(NullableAnnotation.Annotated) ?? false;

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