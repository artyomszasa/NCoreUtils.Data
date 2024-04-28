using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal sealed class NameFactoryInfo(INamedTypeSymbol type)
{
    private TypeWrapper _type = new(type ?? throw new ArgumentNullException(nameof(type)));

    private TypeSyntax? _typeSyntax;

    // private ExpressionSyntax? _accessSyntax;

    private ExpressionSyntax? _getTypeNameAccessSyntax;

    private ExpressionSyntax? _getPropertyNameAccessSyntax;

    public TypeSyntax TypeSyntax => _typeSyntax ??= ParseTypeName(_type.FullName);

    // public ExpressionSyntax AccessSyntax => _accessSyntax ??= EmitAccessSyntax();

    public ExpressionSyntax GetTypeNameAccessSyntax => _getTypeNameAccessSyntax ??= EmitGetTypeNameAccessSyntax();

    public ExpressionSyntax GetPropertyNameAccessSyntax => _getPropertyNameAccessSyntax ??= EmitGetPropertyNameAccessSyntax();

    private ExpressionSyntax EmitGetTypeNameAccessSyntax()
    {
        const string SingletonPropertyName = "Singleton";
        var getTypeMember = _type.Symbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Name == "GetName" && m.Parameters.Length == 1 && m.Parameters[0].Type.Name == "Type")
            ?? throw new GenerationException(DiagnosticDescriptors.NameFactoryGetTypeNameMissing, _type.Symbol.Locations.FirstOrDefault(), [_type.FullName]);
        if (getTypeMember.IsStatic)
        {
            return MemberAccessExpression(
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: TypeSyntax,
                name: IdentifierName("GetName")
            );
        }
        var singletonProperty = _type.Symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.IsStatic && p.Name == SingletonPropertyName);
        if (singletonProperty is not null)
        {
            return MemberAccessExpression(
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: MemberAccessExpression(
                    kind: SyntaxKind.SimpleMemberAccessExpression,
                    expression: TypeSyntax,
                    name: IdentifierName(SingletonPropertyName)
                ),
                name: IdentifierName("GetName")
            );
        }
        return MemberAccessExpression(
            kind: SyntaxKind.SimpleMemberAccessExpression,
            expression: ObjectCreationExpression(
                type: TypeSyntax,
                argumentList: ArgumentList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken)),
                initializer: default
            ),
            name: IdentifierName("GetName")
        );
    }

    private ExpressionSyntax EmitGetPropertyNameAccessSyntax()
    {
        const string SingletonPropertyName = "Singleton";
        var getTypeMember = _type.Symbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Name == "GetName" && m.Parameters.Length == 1 && m.Parameters[0].Type.Name == "PropertyInfo")
            ?? throw new GenerationException(DiagnosticDescriptors.NameFactoryGetPropertyNameMissing, _type.Symbol.Locations.FirstOrDefault(), [_type.FullName]);
        if (getTypeMember.IsStatic)
        {
            return MemberAccessExpression(
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: TypeSyntax,
                name: IdentifierName("GetName")
            );
        }
        var singletonProperty = _type.Symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.IsStatic && p.Name == SingletonPropertyName);
        if (singletonProperty is not null)
        {
            return MemberAccessExpression(
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: MemberAccessExpression(
                    kind: SyntaxKind.SimpleMemberAccessExpression,
                    expression: TypeSyntax,
                    name: IdentifierName(SingletonPropertyName)
                ),
                name: IdentifierName("GetName")
            );
        }
        return MemberAccessExpression(
            kind: SyntaxKind.SimpleMemberAccessExpression,
            expression: ObjectCreationExpression(
                type: TypeSyntax,
                argumentList: ArgumentList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken)),
                initializer: default
            ),
            name: IdentifierName("GetName")
        );
    }
}