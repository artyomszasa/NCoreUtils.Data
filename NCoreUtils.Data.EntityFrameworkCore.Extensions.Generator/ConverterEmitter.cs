using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal class ConverterEmitter
{
    private static readonly string SelfVersion = typeof(ConverterEmitter).Assembly.GetName()?.Version.ToString() ?? string.Empty;

    private static TypeSyntax StringTypeSyntax { get; } = ParseTypeName("string");

    private static NullableTypeSyntax NullableStringTypeSyntax { get; } = NullableType(StringTypeSyntax);

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

    private static LiteralExpressionSyntax TrueLiteralExpression { get; } = LiteralExpression(SyntaxKind.TrueLiteralExpression);

    private static LiteralExpressionSyntax FalseLiteralExpression { get; } = LiteralExpression(SyntaxKind.FalseLiteralExpression);

    private static LiteralExpressionSyntax BooleanLiteralExpression(bool value) => value
        ? TrueLiteralExpression
        : FalseLiteralExpression;

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

    [SuppressMessage("Style", "IDE0300:Simplify collection initialization", Justification = "For better compatiibility")]
    private static MethodDeclarationSyntax EmitToJsonMethod(TargetData target)
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
                    IdentifierName(target.Target.JsonSerializerContextPropertyName)
                ))
            }))
        );

        var bodyExpression = ConditionalExpression(
            IsPatternExpression(
                IdentifierName("source"),
                ConstantPattern(NullLiteralExpression)
            ),
            // nullable esetben NULL, egyébként alapértelmezett érték
            target.IsNullable
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(target.DefaultValue)),
            serializeExpression
        );

        var targetTypeSyntax = ParseTypeName(target.Target.QualifiedName);
        return MethodDeclaration(target.IsNullable ? NullableStringTypeSyntax : StringTypeSyntax , "ToJson")
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(Identifier("source")).WithType(target.IsNullable ? NullableType(targetTypeSyntax) : targetTypeSyntax)
            })))
            .WithExpressionBody(ArrowExpressionClause(bodyExpression))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    [SuppressMessage("Style", "IDE0300:Simplify collection initialization", Justification = "For better compatiibility")]
    private static MethodDeclarationSyntax EmitFromJsonMethod(TargetData target)
    {
        var targetTypeSyntax = ParseTypeName(target.Target.QualifiedName);
        var defaultValueExpression = target.IsArrayLike && !target.IsNullable
            ? (ExpressionSyntax)InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("System.Array"),
                    GenericName(Identifier("Empty"), TypeArgumentList(SeparatedList(new [] { ParseTypeName(target.Item.QualifiedName) })))
                )
            )
            : DefaultExpression(targetTypeSyntax);

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
                        IdentifierName(target.Target.JsonSerializerContextPropertyName)
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

        StatementSyntax checkedDeserializeStatement;
        if (target.IsNullable)
        {
            checkedDeserializeStatement = Block(
                openBraceToken: Token(SyntaxKind.OpenBraceToken),
                statements: List(new StatementSyntax[]
                {
                    IfStatement(
                        condition: IsPatternExpression(
                            IdentifierName("source"),
                            ConstantPattern(NullLiteralExpression)
                        ),
                        statement: ReturnStatement(
                            DefaultExpression(targetTypeSyntax)
                        )
                    ),
                    safeDeserializeStatement
                }),
                closeBraceToken: Token(SyntaxKind.CloseBraceToken)
            );
        }
        else
        {
            checkedDeserializeStatement = safeDeserializeStatement;
        }

        return MethodDeclaration(target.IsNullable ? NullableType(targetTypeSyntax) : targetTypeSyntax, "FromJson")
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(Identifier("source")).WithType(target.IsNullable ? NullableStringTypeSyntax : StringTypeSyntax)
            })))
            .WithBody(Block(checkedDeserializeStatement));
    }

    private static ConstructorDeclarationSyntax EmitCtor(TargetData target)
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

    private static ClassDeclarationSyntax EmitComparerClass(TargetData target)
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

    private static ClassDeclarationSyntax EmitClass(TargetData target)
    {
        var members = new System.Collections.Generic.List<MemberDeclarationSyntax>()
        {
            EmitSingletonProperty(ParseTypeName(target.Host.Name)),
            EmitToJsonMethod(target),
            EmitFromJsonMethod(target),
            EmitCtor(target)
        };
        if (target.Comparer is null)
        {
            members.Add(EmitComparerClass(target));
        }

        var accessibilityToken = target.Host.DeclaredAccessibility switch
        {
            Accessibility.Internal => SyntaxKind.InternalKeyword,
            _ => SyntaxKind.PublicKeyword
        };

        var baseType = target.IsNullable
            ? ParseTypeName($"Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<{target.Target.QualifiedName}?, string?>")
            : ParseTypeName($"Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<{target.Target.QualifiedName}, string>");

        return ClassDeclaration(target.Host.Name)
            .AddAttributeLists(
                AttributeList(SeparatedList(new AttributeSyntax[]
                {
                    Attribute(ParseName("System.CodeDom.Compiler.GeneratedCodeAttribute"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                    {
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("NCoreUtils.Data.Builders"))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(SelfVersion)))
                    })))
                }))
            )
            .AddModifiers(
                Token(TriviaList(Comment("/// <inheritdoc/>")), accessibilityToken, TriviaList()),
                Token(SyntaxKind.PartialKeyword)
            )
            .AddBaseListTypes(SimpleBaseType(baseType))
            .AddMembers(members.ToArray());
    }

    private static ClassDeclarationSyntax EmitExtensions(TargetData target)
    {
        var targetQualifiedName = target.IsNullable
            ? $"{target.Target.QualifiedName}?"
            : target.Target.QualifiedName;

        var extensionTypeName = $"PropertyBuilder{target.Host.Name}Extensions";
        var propertyBuilderType = ParseTypeName($"Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<{targetQualifiedName}>");
        var comparerSingletonExpression = SimpleMemberAccessExpression(
            target.Comparer is SymbolData { QualifiedName: var comparerQualifiedName }
                ? IdentifierName(comparerQualifiedName)
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
            "IsRequired",
            Argument(BooleanLiteralExpression(!target.IsNullable))
        );
        expression = target.IsNullable
            ? SimpleInvocationExpression(
                expression,
                "HasDefaultValue",
                Argument(NullLiteralExpression)
            )
            : SimpleInvocationExpression(
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
            .AddAttributeLists(
                AttributeList(SeparatedList(new AttributeSyntax[]
                {
                    Attribute(ParseName("System.CodeDom.Compiler.GeneratedCodeAttribute"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                    {
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("NCoreUtils.Data.Builders"))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(SelfVersion)))
                    })))
                }))
            )
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

    public static CompilationUnitSyntax EmitCompilationUnit(TargetData target)
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