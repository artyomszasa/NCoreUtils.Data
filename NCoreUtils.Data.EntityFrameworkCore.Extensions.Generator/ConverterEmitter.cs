using System;
using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal class ConverterEmitter
{
    private static TypeSyntax StringTypeSyntax { get; } = ParseTypeName("string");

    private static ExpressionSyntax STJSerializerExpression { get; } = IdentifierName("System.Text.Json.JsonSerializer");

    private static LiteralExpressionSyntax NullLiteralExpression { get; } = LiteralExpression(SyntaxKind.NullLiteralExpression);

    private static MemberAccessExpressionSyntax SimpleMemberAccessExpression(ExpressionSyntax expression, SimpleNameSyntax member)
        => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, member);

    private static MemberAccessExpressionSyntax SimpleMemberAccessExpression(ExpressionSyntax expression, string member)
        => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName(member));

    private static InvocationExpressionSyntax SimpleInvocationExpression(string staticMethodName, params ArgumentSyntax[] arguments)
        => InvocationExpression(IdentifierName(staticMethodName), ArgumentList(SeparatedList(arguments)));

    private static InvocationExpressionSyntax SimpleInvocationExpression(ExpressionSyntax instance, string methodName, params ArgumentSyntax[] arguments)
        => InvocationExpression(SimpleMemberAccessExpression(instance, IdentifierName(methodName)), ArgumentList(SeparatedList(arguments)));

    private static LiteralExpressionSyntax StringLiteralExpression(string value) => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));

    private static string GetJsonSerializerContextPropertyName(INamedTypeSymbol symbol)
    {
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

    private static PropertyDeclarationSyntax EmitSingletonProperty(TypeSyntax type)
    {
        return PropertyDeclaration(type, "Singleton")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithAccessorList(AccessorList(List(new AccessorDeclarationSyntax[]
            {
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            })))
            .WithInitializer(EqualsValueClause(
                ObjectCreationExpression(type).WithArgumentList(ArgumentList(SeparatedList(Array.Empty<ArgumentSyntax>())))
            ))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static MethodDeclarationSyntax EmitToJsonMethod(ConverterTarget target)
    {
        var serializeExpression = InvocationExpression(
            SimpleMemberAccessExpression(STJSerializerExpression, IdentifierName("Serialize")),
            ArgumentList(SeparatedList(new ArgumentSyntax[]
            {
                Argument(IdentifierName("source")),
                Argument(SimpleMemberAccessExpression(
                    SimpleMemberAccessExpression(
                        ParseTypeName(target.SerializerContext.QualifiedName),
                        IdentifierName("Default")
                    ),
                    IdentifierName(GetJsonSerializerContextPropertyName((INamedTypeSymbol)target.Target.Symbol!))
                ))
            }))
        );

        var bodyExpression = ConditionalExpression(
            IsPatternExpression(
                IdentifierName("source"),
                ConstantPattern(NullLiteralExpression)
            ),
            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(target.DefaultValue)),
            serializeExpression
        );

        return MethodDeclaration(StringTypeSyntax , "ToJson")
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(Identifier("source")).WithType(ParseTypeName(target.Target.QualifiedName))
            })))
            .WithExpressionBody(ArrowExpressionClause(bodyExpression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static MethodDeclarationSyntax EmitFromJsonMethod(ConverterTarget target)
    {
        var defaultValueExpression = target.IsArrayLike
            ? (ExpressionSyntax)InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("System.Array"),
                    GenericName(Identifier("Empty"), TypeArgumentList(SeparatedList(new [] { ParseTypeName(target.Item.QualifiedName) })))
                )
            )
            : DefaultExpression(ParseTypeName(target.Target.QualifiedName));

        var deserializeExpression = BinaryExpression(
            SyntaxKind.CoalesceExpression,
            InvocationExpression(
                SimpleMemberAccessExpression(STJSerializerExpression, IdentifierName("Deserialize")),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(IdentifierName("source")),
                    Argument(SimpleMemberAccessExpression(
                        SimpleMemberAccessExpression(
                            ParseTypeName(target.SerializerContext.QualifiedName),
                            IdentifierName("Default")
                        ),
                        IdentifierName(GetJsonSerializerContextPropertyName((INamedTypeSymbol)target.Target.Symbol!))
                    ))
                }))
            ),
            defaultValueExpression
        );

        var safeDeserializeStatement = TryStatement(
            Block(ReturnStatement(deserializeExpression)),
            List(new CatchClauseSyntax[]
            {
                CatchClause(null, null, Block(ReturnStatement(defaultValueExpression)))
            }),
            null
        );

        return MethodDeclaration(ParseTypeName(target.Target.QualifiedName), "FromJson")
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(Identifier("source")).WithType(StringTypeSyntax)
            })))
            .WithBody(Block(safeDeserializeStatement));
    }

    private static ConstructorDeclarationSyntax EmitCtor(ConverterTarget target)
    {
        return ConstructorDeclaration(target.Host.Name)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SeparatedList(Array.Empty<ParameterSyntax>())))
            .WithInitializer(ConstructorInitializer(
                SyntaxKind.BaseConstructorInitializer,
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(SimpleLambdaExpression(Parameter(Identifier("data")), SimpleInvocationExpression("ToJson", Argument(IdentifierName("data"))))),
                    Argument(SimpleLambdaExpression(Parameter(Identifier("json")), SimpleInvocationExpression("FromJson", Argument(IdentifierName("json"))))),
                }))
            ))
            .WithBody(Block());
    }

    private static ClassDeclarationSyntax EmitComparerClass(ConverterTarget target)
    {
        var baseType = ParseTypeName($"Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<{target.Target.QualifiedName}>");

        var eq0 = target.IsArrayLike
            ? SimpleInvocationExpression(IdentifierName("System.Linq.Enumerable"), "SequenceEqual", Argument(IdentifierName("a")), Argument(IdentifierName("b")))
            : SimpleInvocationExpression(IdentifierName("a"), "Equals", Argument(IdentifierName("b")));

        var eq = ParenthesizedLambdaExpression(
            ParameterList(Token(SyntaxKind.OpenParenToken), SeparatedList(new ParameterSyntax[]
            {
                Parameter(Identifier("a")),
                Parameter(Identifier("b"))
            }), Token(SyntaxKind.CloseParenToken)),
            BinaryExpression(
                SyntaxKind.LogicalOrExpression,
                SimpleInvocationExpression("ReferenceEquals", Argument(IdentifierName("a")), Argument(IdentifierName("b"))),
                BinaryExpression(
                    SyntaxKind.LogicalAndExpression,
                    BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SimpleInvocationExpression("ReferenceEquals", Argument(IdentifierName("a")), Argument(NullLiteralExpression))),
                        PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SimpleInvocationExpression("ReferenceEquals", Argument(IdentifierName("b")), Argument(NullLiteralExpression)))
                    ),
                    eq0
                )
            )
        );

        var hash0 = target.IsArrayLike
            ? SimpleInvocationExpression(IdentifierName("NCoreUtils.Data.EntityFrameworkCore.Extensions.ValueComparisonHelpers"), "AggregateHash", Argument(IdentifierName("data")))
            : SimpleInvocationExpression(IdentifierName("data"), "GetHashCode");

        var hash = SimpleLambdaExpression(Parameter(Identifier("data")), hash0);

        var ctor = ConstructorDeclaration("Comparer")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SeparatedList(Array.Empty<ParameterSyntax>())))
            .WithInitializer(ConstructorInitializer(
                SyntaxKind.BaseConstructorInitializer,
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(eq),
                    Argument(hash)
                }))
            ))
            .WithBody(Block());


        return ClassDeclaration("Comparer")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .AddBaseListTypes(SimpleBaseType(baseType))
            .AddMembers(ctor, EmitSingletonProperty(ParseTypeName("Comparer")));

    }

    private static ClassDeclarationSyntax EmitClass(ConverterTarget target)
    {
        var members = new System.Collections.Generic.List<MemberDeclarationSyntax>()
        {
            EmitSingletonProperty(ParseTypeName(target.Host.Name)),
            EmitToJsonMethod(target),
            EmitFromJsonMethod(target),
            EmitCtor(target)
        };
        if (!target.Comparer.HasValue)
        {
            members.Add(EmitComparerClass(target));
        }

        var accessibilityToken = target.Host.Symbol!.DeclaredAccessibility switch
        {
            Accessibility.Internal => SyntaxKind.InternalKeyword,
            _ => SyntaxKind.PublicKeyword
        };

        var baseType = ParseTypeName($"Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<{target.Target.QualifiedName}, string>");

        return ClassDeclaration(target.Host.Name)
            .AddModifiers(
                Token(TriviaList(Comment("/// <inheritdoc/>")), accessibilityToken, TriviaList()),
                Token(SyntaxKind.PartialKeyword)
            )
            .AddBaseListTypes(SimpleBaseType(baseType))
            .AddMembers(members.ToArray());
    }

    private static ClassDeclarationSyntax EmitExtensions(ConverterTarget target)
    {
        var extensionTypeName = $"PropertyBuilder{target.Host.Name}Extensions";
        var propertyBuilderType = ParseTypeName($"Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<{target.Target.QualifiedName}>");
        var comparerSingletonExpression = SimpleMemberAccessExpression(
            target.Comparer.HasValue
                ? IdentifierName(target.Comparer.QualifiedName)
                : IdentifierName(target.Host.Name + ".Comparer"),
            "Singleton"
        );

        ExpressionSyntax expression = IdentifierName("builder");
        expression = SimpleInvocationExpression(
            expression,
            "HasConversion",
            Argument(SimpleMemberAccessExpression(IdentifierName(target.Host.Name), "Singleton"))
        );
        expression = SimpleInvocationExpression(
            expression,
            "HasColumnType",
            Argument(StringLiteralExpression("jsonb"))
        );
        expression = SimpleInvocationExpression(
            expression,
            "HasDefaultValueSql",
            Argument(StringLiteralExpression($"'{target.DefaultValue}'::jsonb"))
        );
        expression = SimpleMemberAccessExpression(expression, "Metadata");
        expression = SimpleInvocationExpression(
            expression,
            "SetValueComparer",
            Argument(comparerSingletonExpression)
        );

        return ClassDeclaration(extensionTypeName)
            .AddModifiers(
                Token(TriviaList(Comment("/// <inheritdoc/>")), SyntaxKind.PublicKeyword, TriviaList()),
                Token(SyntaxKind.StaticKeyword)
            )
            .AddMembers(
                MethodDeclaration(propertyBuilderType, "HasJsonConversion")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(new []
                    {
                        Parameter(Identifier("builder")).WithType(propertyBuilderType).WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword))),
                    })))
                    .WithBody(Block(List(new StatementSyntax[]
                    {
                        ExpressionStatement(expression),
                        ReturnStatement(IdentifierName("builder"))
                    })))
            );
    }

    public static CompilationUnitSyntax EmitCompilationUnit(ConverterTarget target)
    {
        SyntaxTriviaList syntaxTriviaList = TriviaList(
            Comment("// <auto-generated/>"),
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true))
        );

        return CompilationUnit()
            .AddUsings(UsingDirective(IdentifierName("Microsoft.EntityFrameworkCore")))
            .AddMembers(
                NamespaceDeclaration(IdentifierName(target.Host.Namespace))
                    .WithLeadingTrivia(syntaxTriviaList)
                    .AddMembers(
                        EmitClass(target),
                        EmitExtensions(target)
                    )
            )
            .NormalizeWhitespace();
    }
}