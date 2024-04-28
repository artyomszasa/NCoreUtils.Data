using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal partial class DefinitionEmitter(GenerationContext context)
{
    private GenerationContext Context { get; } = context;

    private static MemberAccessExpressionSyntax SimpleMemberAccessExpression(ExpressionSyntax expression, SimpleNameSyntax name)
        => MemberAccessExpression(
            kind: SyntaxKind.SimpleMemberAccessExpression,
            expression: expression,
            name: name
        );

    private static MemberAccessExpressionSyntax SimpleMemberAccessExpression(ExpressionSyntax expression, string name)
        => SimpleMemberAccessExpression(expression, IdentifierName(name));

    private MethodDeclarationSyntax EmitPropertyDefinitionInitializer(
        ContextTarget contextTarget,
        IPropertySymbol property,
        ref TypeWrapper entityType,
        NameFactoryInfo? nameFactory,
        out string methodName)
    {
        const string PropertyInfoVariableName = "propertyInfo";
        var propertyTypeSyntax = ParseTypeName(property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        var dataPropertyTypeSyntax = ParseTypeName($"NCoreUtils.Data.Build.DataPropertyBuilder<{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>");
        var memberExpressionType = GenericName(
            identifier: Identifier("Expression"),
            typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
            {
                GenericName(
                    identifier: Identifier("Func"),
                    typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
                    {
                        entityType.Syntax,
                        propertyTypeSyntax
                    }))
                )
            }))
        );
        // (PropertyInfo)((MemberExpression)((Expression<Func<[ENTITY], [PROPERTY]>>)(e => e.[PROPERTYNAME]!)).Body).Member
        var propertyInfoInitSyntax = CastExpression(
            type: ParseTypeName(nameof(System.Reflection.PropertyInfo)),
            expression: SimpleMemberAccessExpression(
                expression: ParenthesizedExpression(CastExpression(
                    type: ParseTypeName(nameof(MemberExpression)),
                    expression: SimpleMemberAccessExpression(
                        expression: ParenthesizedExpression(CastExpression(
                            type: memberExpressionType,
                            expression: ParenthesizedExpression(SimpleLambdaExpression(
                                parameter: Parameter(Identifier("e")),
                                body: PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, SimpleMemberAccessExpression(
                                    expression: IdentifierName("e"),
                                    name: property.Name
                                ))
                            ))
                        )),
                        name: nameof(LambdaExpression.Body)
                    )
                )),
                name: nameof(MemberExpression.Member)
            )
        );
        // new DataPropertyBuilder( ... )
        ExpressionSyntax expr = ObjectCreationExpression(
            newKeyword: Token(SyntaxKind.NewKeyword).WithTrailingTrivia(Whitespace(" ")),
            type: dataPropertyTypeSyntax,
            argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
            {
                Argument(IdentifierName(PropertyInfoVariableName))
            })),
            initializer: default
        );
        // DECORATE NAME --> builder.SetName([NAME])
        expr = InvocationExpression(
            expression: MemberAccessExpression(
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: expr,
                operatorToken: Token(SyntaxKind.DotToken).WithLeadingTrivia(ElasticCarriageReturnLineFeed),
                name: IdentifierName("SetName")
            ),
            ArgumentList(SeparatedList(new ArgumentSyntax[]
            {
                Argument(nameFactory is null
                    ? LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(property.Name))
                    : InvocationExpression(
                        expression: nameFactory.GetPropertyNameAccessSyntax,
                        argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(PropertyInfoVariableName))
                        }))
                    )
                )
            }))
        );
        if (contextTarget.GenerateFirestoreDecorators)
        {
            // DECORATE NAME --> builder.SetMetadata(FirestoreMetadataExtensions.KeyFieldExpressionFactory, [Factory].Singleton)
            expr = InvocationExpression(
                expression: SimpleMemberAccessExpression(expr, IdentifierNames.SetMetadata),
                argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(SimpleMemberAccessExpression(TypeNames.FirestoreMetadataExtensions, IdentifierNames.KeyFieldExpressionFactory)),
                    Argument(SimpleMemberAccessExpression(ParseTypeName(GetFieldExpressionFactoryClassName(property.Type)), IdentifierNames.Singleton))
                }))
            );
        }

        // STATIC METHOD
        methodName = $"Initialize{property.Name}In{entityType.Name}";
        return MethodDeclaration(
            attributeLists: default!,
            modifiers: Modifiers.PrivateStatic,
            returnType: dataPropertyTypeSyntax,
            explicitInterfaceSpecifier: default!,
            identifier: Identifier(methodName),
            typeParameterList: default!,
            parameterList: ParameterList(Token(SyntaxKind.OpenParenToken), SeparatedList<ParameterSyntax>(), Token(SyntaxKind.CloseParenToken)),
            constraintClauses: default,
            body: Block(
                LocalDeclarationStatement(VariableDeclaration(
                    IdentifierName("var"),
                    SeparatedList(new VariableDeclaratorSyntax[]
                    {
                        VariableDeclarator(Identifier(PropertyInfoVariableName), argumentList: null, initializer: EqualsValueClause(propertyInfoInitSyntax))
                    })
                )),
                ReturnStatement(expr)
            ),
            semicolonToken: default
        );
    }

    private IEnumerable<IPropertySymbol> CollectProperties(ITypeSymbol type)
    {
        foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (!property.IsStatic && property.DeclaredAccessibility == Accessibility.Public && !property.IsOverride)
            {
                yield return property;
            }
        }
        if (type.BaseType is not null && !(type.BaseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum))
        {
            foreach (var property in CollectProperties(type.BaseType))
            {
                yield return property;
            }
        }
    }

    private static MethodDeclarationSyntax EmitCtorArgumentInitializer(IReadOnlyList<string> initializers)
    {
        const string varName = "properties";
        var returnTypeSyntax = ParseTypeName($"System.Collections.Generic.Dictionary<{nameof(System.Reflection.PropertyInfo)}, NCoreUtils.Data.Build.DataPropertyBuilder>");
        var statements = new List<StatementSyntax>(initializers.Count * 2 + 2)
        {
            LocalDeclarationStatement(VariableDeclaration(
                IdentifierName("var"),
                SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(Identifier(varName), argumentList: null, initializer: EqualsValueClause(
                        ObjectCreationExpression(
                            Token(SyntaxKind.NewKeyword),
                            returnTypeSyntax,
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(initializers.Count)))
                            })),
                            initializer: default
                        )
                    ))
                })
            ))
        };
        var i = 0;
        foreach (var initializer in initializers)
        {
            var propVarName = $"property{i++}";
            statements.Add(LocalDeclarationStatement(VariableDeclaration(
                IdentifierName("var"),
                SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(Identifier(propVarName), argumentList: null, initializer: EqualsValueClause(
                        InvocationExpression(
                            expression: IdentifierName(initializer)
                        )
                    ))
                })
            )));
            statements.Add(ExpressionStatement(InvocationExpression(
                SimpleMemberAccessExpression(IdentifierName(varName), "Add"),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(SimpleMemberAccessExpression(IdentifierName(propVarName), "Property")),
                    Argument(IdentifierName(propVarName))
                }))
            )));
        }
        statements.Add(ReturnStatement(IdentifierName(varName)));

        // STATIC METHOD
        return MethodDeclaration(
            attributeLists: default!,
            modifiers: Modifiers.PrivateStatic,
            returnType: returnTypeSyntax,
            explicitInterfaceSpecifier: default!,
            identifier: Identifier("InitializeProperties"),
            typeParameterList: default!,
            parameterList: ParameterList(Token(SyntaxKind.OpenParenToken), SeparatedList<ParameterSyntax>(), Token(SyntaxKind.CloseParenToken)),
            constraintClauses: default,
            body: Block(statements),
            semicolonToken: default
        );
    }

    private static SeparatedSyntaxList<ExpressionSyntax> SuppressedWarningCodes { get; }
        = SeparatedList(new ExpressionSyntax[]
        {
           IdentifierName("CS0618")
        });

    private TypeDeclarationSyntax EmitDataEntityBuilder(ContextTarget contextTarget, EntityTarget target)
    {
        var properties = CollectProperties(target.Type.Symbol).ToList();

        var members = new List<MemberDeclarationSyntax>();
        var initializers = new List<string>();
        foreach (var property in properties)
        {
            members.Add(EmitPropertyDefinitionInitializer(contextTarget, property, ref target.Type, target.NameFactory ?? contextTarget.DefaultNameFactory, out var methodName));
            initializers.Add(methodName);
        }
        var className = $"{target.Type.Name}Builder";

        members.Add(EmitCtorArgumentInitializer(initializers));

        var nameFactory = target.NameFactory ?? contextTarget.DefaultNameFactory;
        var ctor = ConstructorDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            identifier: Identifier(className),
            parameterList: EmptyParameters,
            initializer: ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ArgumentList(
                Token(SyntaxKind.OpenParenToken),
                SeparatedList(new ArgumentSyntax[]
                {
                    Argument(TypeOfExpression(target.Type.Syntax)),
                    Argument(InvocationExpression(IdentifierName("InitializeProperties"), ArgumentList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken))))
                }),
                Token(SyntaxKind.CloseParenToken)
            )),
            body: Block(Token(SyntaxKind.OpenBraceToken), List(new StatementSyntax[]
            {
                ExpressionStatement(InvocationExpression(
                    IdentifierName("SetName"),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(nameFactory is null
                            ? LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(target.Type.Name))
                            : InvocationExpression(
                                expression: nameFactory.GetTypeNameAccessSyntax,
                                argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                                {
                                    Argument(TypeOfExpression(target.Type.Syntax))
                                }))
                            )
                        )
                    }))
                ))
            }), Token(SyntaxKind.CloseBraceToken))
        )
            .WithLeadingTrivia(TriviaList(Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), SuppressedWarningCodes, true))))
            .WithTrailingTrivia(TriviaList(Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), SuppressedWarningCodes, false))));
        members.Add(ctor);

        return ClassDeclaration(
            attributeLists: List<AttributeListSyntax>(),
            modifiers: TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword)),
            identifier: Identifier(className),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(ParseTypeName($"NCoreUtils.Data.Build.DataEntityBuilder<{target.Type.FullName}>"))
            })),
            constraintClauses: default,
            members: List(members)
        );
    }

    private TypeDeclarationSyntax EmitEnumFactory(Dictionary<string, string> names, ref TypeWrapper enumType)
    {
        var enumFullName = enumType.FullName;
        var enumTypeSyntax = ParseTypeName(enumFullName);
        var enumFactoryClassName = GenerateName(names, ref enumType);
        var enumFactoryTypeSyntax = ParseTypeName(enumFactoryClassName);
        var factoryBaseTypeSyntax = ParseTypeName($"NCoreUtils.Data.Google.Cloud.Firestore.Internal.EnumConversionHelper<{enumFullName}>");
        var enumInfoTypeSyntax = ParseTypeName($"NCoreUtils.Data.Google.Cloud.Firestore.Internal.IEnumInfo<{enumFullName}>");
        var readonlyListTypeSyntax = ParseTypeName($"System.Collections.Generic.IReadOnlyList<{enumFullName}>");
        var arrayTypeSyntax = ArrayType(enumTypeSyntax, List(
        [
            ArrayRankSpecifier(SeparatedList(new ExpressionSyntax[] { OmittedArraySizeExpression() }))
        ]));

        var valuesField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: readonlyListTypeSyntax,
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("values"),
                        argumentList: default,
                        initializer: EqualsValueClause(
                            ArrayCreationExpression(
                                type: arrayTypeSyntax,
                                initializer: InitializerExpression(
                                    SyntaxKind.ArrayInitializerExpression,
                                    expressions: SeparatedList(enumType.Symbol.GetMembers()
                                        .OfType<IFieldSymbol>()
                                        .Select(f => (ExpressionSyntax)SimpleMemberAccessExpression(enumTypeSyntax, f.Name))
                                        .ToArray())
                                )
                            )
                        )
                    )
                })
            )
        );

        var singletonProperty = PropertyDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PublicStatic,
            type: enumFactoryTypeSyntax,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("Singleton"),
            accessorList: AccessorList(List(new AccessorDeclarationSyntax[]
            {
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            })),
            expressionBody: default,
            initializer: EqualsValueClause(ObjectCreationExpression(
                type: enumFactoryTypeSyntax,
                argumentList: ArgumentList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken)),
                initializer: default
            )),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var getValuesMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: readonlyListTypeSyntax,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("GetValues"),
            typeParameterList: default,
            parameterList: EmptyParameters,
            constraintClauses: default,
            body: default,
            expressionBody: ArrowExpressionClause(IdentifierName("values")),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var infoProperty = PropertyDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PublicOverride,
            type: enumInfoTypeSyntax,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("Info"),
            accessorList: default,
            expressionBody: ArrowExpressionClause(IdentifierName("this")),
            initializer: default,
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        return ClassDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateSealed,
            identifier: Identifier(enumFactoryClassName),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(factoryBaseTypeSyntax),
                SimpleBaseType(enumInfoTypeSyntax)
            })),
            constraintClauses: default,
            members: List(new MemberDeclarationSyntax[] { valuesField, singletonProperty, infoProperty, getValuesMethod })
        );

        static string GenerateName(Dictionary<string, string> names, ref TypeWrapper enumType)
        {
            var @base = enumType.Name;
            var i = 0;
            string? result = null;
            do
            {
                var candidate = i == 0 ? $"{@base}EnumFactory" : $"{@base}{i}EnumFactory";
                if (!names.ContainsKey(candidate))
                {
                    names.Add(candidate, enumType.FullName);
                    result = candidate;
                }
            }
            while (result is null);
            return result;
        }
    }

    private TypeDeclarationSyntax EmitEnumConversionHelpers(IReadOnlyCollection<KeyValuePair<string, string>> enumFactoryNames)
    {
        var tryGetHelpereMethodBody = enumFactoryNames.Select(kv =>
        {
            var factoryName = kv.Key;
            var enumTypeName = kv.Value;
            return (StatementSyntax)IfStatement(
                condition: BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName("enumType"), TypeOfExpression(ParseTypeName(enumTypeName))),
                statement: Block(
                    openBraceToken: Token(SyntaxKind.OpenBraceToken),
                    statements: List(new StatementSyntax[]
                    {
                        ExpressionStatement(AssignmentExpression(
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: IdentifierName("helper"),
                            right: SimpleMemberAccessExpression(ParseTypeName(factoryName), "Singleton")
                        )),
                        ReturnStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression))
                    }),
                    closeBraceToken: Token(SyntaxKind.CloseBraceToken)
                )
            );
        }).ToList();
        tryGetHelpereMethodBody.Add(ExpressionStatement(AssignmentExpression(
            kind: SyntaxKind.SimpleAssignmentExpression,
            left: IdentifierName("helper"),
            right: IdentifierName("default")
        )));
        tryGetHelpereMethodBody.Add(ReturnStatement(LiteralExpression(SyntaxKind.FalseLiteralExpression)));

        var tryGetHelperMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: ParseTypeName("bool"),
            explicitInterfaceSpecifier: default!,
            identifier: Identifier("TryGetHelper"),
            typeParameterList: default!,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(
                    attributeLists: default,
                    modifiers: default,
                    type: ParseTypeName("Type"),
                    identifier: Identifier("enumType"),
                    @default: default
                ),
                Parameter(
                    attributeLists: List(new AttributeListSyntax[]
                    {
                        AttributeList(SeparatedList(new AttributeSyntax[]
                        {
                            Attribute(ParseName("System.Diagnostics.CodeAnalysis.MaybeNullWhen"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                            {
                                AttributeArgument(LiteralExpression(SyntaxKind.FalseLiteralExpression))
                            })))
                        }))
                    }),
                    modifiers: Modifiers.Out,
                    type: ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Internal.IEnumConversionHelper"),
                    identifier: Identifier("helper"),
                    @default: default
                )
            })),
            constraintClauses: default,
            body: Block(
                openBraceToken: Token(SyntaxKind.OpenBraceToken),
                statements: List(tryGetHelpereMethodBody),
                closeBraceToken: Token(SyntaxKind.CloseBraceToken)
            ),
            semicolonToken: default
        );

        return ClassDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateSealed,
            identifier: Identifier("EnumConversionHelpers"),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[] { SimpleBaseType(ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Internal.IEnumConversionHelpers")) })),
            constraintClauses: default,
            members: List([(MemberDeclarationSyntax)tryGetHelperMethod])
        );
    }

    private static string GetFieldExpressionFactoryClassName(ref TypeWrapper type)
        => $"FirestoreFieldExpressionFactoryOf{type.SafeName}";

    private static string GetFieldExpressionFactoryClassName(ITypeSymbol type)
        => $"FirestoreFieldExpressionFactoryOf{TypeWrapper.GetSafeName(type)}";

    private TypeDeclarationSyntax EmitFieldExpressionFactory(ref TypeWrapper type)
    {
        var className = GetFieldExpressionFactoryClassName(ref type);
        var typeSyntax = ParseTypeName(className);
        var statements = new List<StatementSyntax>();
        if (type.Symbol.SpecialType == SpecialType.System_String)
        {
            // id/key handling
            statements.Add(IfStatement(
                condition: BinaryExpression(
                    kind: SyntaxKind.LogicalAndExpression,
                    left: BinaryExpression(SyntaxKind.NotEqualsExpression, SimpleMemberAccessExpression(IdentifierNames.entity, IdentifierNames.Key), LiteralExpression(SyntaxKind.NullLiteralExpression)),
                    right: BinaryExpression(
                        kind: SyntaxKind.LogicalAndExpression,
                        left: BinaryExpression(
                            kind: SyntaxKind.EqualsExpression,
                            left: SimpleMemberAccessExpression(SimpleMemberAccessExpression(IdentifierNames.entity, IdentifierNames.Key), IdentifierNames.Count),
                            right: LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1))
                        ),
                        right: BinaryExpression(
                            kind: SyntaxKind.EqualsExpression,
                            left: SimpleMemberAccessExpression(
                                ElementAccessExpression(
                                    SimpleMemberAccessExpression(IdentifierNames.entity, IdentifierNames.Key),
                                    BracketedArgumentList(SeparatedList(new ArgumentSyntax[] { Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))) }))
                                ),
                                IdentifierNames.Property
                            ),
                            right: SimpleMemberAccessExpression(IdentifierNames.property, IdentifierNames.Property)
                        )
                    )
                ),
                statement: Block(
                    openBraceToken: Token(SyntaxKind.OpenBraceToken),
                    statements: List(new StatementSyntax[]
                    {
                        ReturnStatement(ObjectCreationExpression(
                            type: TypeNames.FirestoreFieldExpressionOfString,
                            argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierNames.converter),
                                Argument(IdentifierNames.snapshot),
                                Argument(SimpleMemberAccessExpression(TypeNames.FieldPath, IdentifierNames.DocumentId))
                            })),
                            initializer: default
                        ))
                    }),
                    closeBraceToken: Token(SyntaxKind.CloseBraceToken)
                )
            ));
        }
        // generic expression
        statements.Add(ReturnStatement(ObjectCreationExpression(
            type: TypeNames.GetOrParse($"NCoreUtils.Data.Google.Cloud.Firestore.Expressions.FirestoreFieldExpression<{type.FullName}>"),
            argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
            {
                Argument(IdentifierNames.converter),
                Argument(IdentifierNames.snapshot),
                Argument(InvocationExpression(
                    expression: SimpleMemberAccessExpression(TypeNames.ImmutableList, IdentifierNames.Create),
                    argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(SimpleMemberAccessExpression(IdentifierNames.property, IdentifierNames.Name))
                    }))
                ))
            })),
            initializer: default
        )));

        // method
        var createMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.FirestoreFieldExpression,
            explicitInterfaceSpecifier: default!,
            identifier: Identifier("Create"),
            typeParameterList: default!,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(attributeLists: default, modifiers: default, type: TypeNames.DataEntity, Identifier("entity"), @default: default),
                Parameter(attributeLists: default, modifiers: default, type: TypeNames.DataProperty, Identifier("property"), @default: default),
                Parameter(attributeLists: default, modifiers: default, type: TypeNames.FirestoreConverter, Identifier("converter"), @default: default),
                Parameter(attributeLists: default, modifiers: default, type: TypeNames.LinqExpression, Identifier("snapshot"), @default: default)
            })),
            constraintClauses: default,
            body: Block(Token(SyntaxKind.OpenBraceToken), List(statements), Token(SyntaxKind.CloseBraceToken)),
            semicolonToken: default
        );

        // singleton
        var singletonProperty = PropertyDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PublicStatic,
            type: typeSyntax,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("Singleton"),
            accessorList: AccessorList(List(new AccessorDeclarationSyntax[]
            {
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            })),
            expressionBody: default,
            initializer: EqualsValueClause(ObjectCreationExpression(
                type: typeSyntax,
                argumentList: ArgumentList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken)),
                initializer: default
            )),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        return ClassDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateSealed,
            identifier: Identifier(className),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[] { SimpleBaseType(TypeNames.IFirestoreFieldExpressionFactory) })),
            constraintClauses: default,
            members: List(new MemberDeclarationSyntax[] { createMethod, singletonProperty })
        );
    }

    private static string GetMutableCollectionFactoryClassName(ref TypeWrapper mutableCollectionType)
        => $"MutableCollectionFactoryFor{mutableCollectionType.SafeName}";

    private static string GetImmutableCollectionFactoryClassName(ref TypeWrapper mutableCollectionType)
        => $"ImmutableCollectionFactoryFor{mutableCollectionType.SafeName}";

    private static bool HasAddRangeMethod(ITypeSymbol type)
    {
        return type.GetMembers("AddRange").OfType<IMethodSymbol>().Any(m => !m.IsStatic && m.Parameters.Length == 1);
    }

    private readonly struct GetEnumeratorMethodInfo(INamedTypeSymbol enumeratorType, bool isExpicit)
    {
        public INamedTypeSymbol EnumeratorType { get; } = enumeratorType;

        public bool IsExplicit { get; } = isExpicit;
    }

    private static bool TryGetGetEnumeratorMethod(ITypeSymbol type, out GetEnumeratorMethodInfo enumeratorInfo)
    {
        if (type.GetMembers("GetEnumerator").OfType<IMethodSymbol>().TryGetFirst(m => !m.IsStatic && m.Parameters.Length == 0, out var method))
        {
            if (method.ExplicitInterfaceImplementations.Any())
            {
                enumeratorInfo = new((INamedTypeSymbol)method.ReturnType, true);
            }
            enumeratorInfo = new((INamedTypeSymbol)method.ReturnType, false);
            return true;
        }
        if (type.BaseType is not null && SpecialType.None != type.BaseType.SpecialType)
        {
            return TryGetGetEnumeratorMethod(type.BaseType, out enumeratorInfo);
        }
        enumeratorInfo = default;
        return false;
    }

    private TypeDeclarationSyntax EmitMutableCollectionFactory(
        ref TypeWrapper elementType,
        ref TypeWrapper mutableCollectionType)
    {
        var className = GetMutableCollectionFactoryClassName(ref mutableCollectionType);
        var collectionTypeSyntax = ParseTypeName(mutableCollectionType.FullName);
        var enumeratorType = new TypeWrapper((INamedTypeSymbol)(mutableCollectionType.Symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => !m.IsStatic && m.Name == "GetEnumerator" && m.Parameters.Length == 0)
            ?? throw new InvalidOperationException($"No GetEnumerator method found for {mutableCollectionType.FullName}."))
            .ReturnType);
        var enumeratorTypeSyntax = TypeNames.GetOrParse(enumeratorType.FullName);

        // "Add" method
        // ((MethodCallExpression)((Expression<Action<[COLLECTION]>>)(set => set.Add(default))).Body).Method
        var addMethodExpressionType = GenericName(
            identifier: Identifier("Expression"),
            typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
            {
                GenericName(
                    identifier: Identifier("Action"),
                    typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
                    {
                        collectionTypeSyntax
                    }))
                )
            }))
        );
        var addMethodInitSyntax = SimpleMemberAccessExpression(
            expression: ParenthesizedExpression(
                expression: CastExpression(
                    type: TypeNames.LinqMethodCallExpression,
                    expression: ParenthesizedExpression(
                        expression: SimpleMemberAccessExpression(
                            expression: ParenthesizedExpression(
                                expression: CastExpression(
                                    type: addMethodExpressionType,
                                    expression: ParenthesizedExpression(SimpleLambdaExpression(
                                        parameter: Parameter(Identifier("collection")),
                                        body: InvocationExpression(
                                            expression: SimpleMemberAccessExpression(IdentifierNames.collection, IdentifierNames.Add),
                                            argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                                            {
                                                Argument(PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, LiteralExpression(SyntaxKind.DefaultLiteralExpression)))
                                            }))
                                        )
                                    ))
                                )
                            ),
                            name: IdentifierNames.Body
                        )
                    )
                )
            ),
            name: IdentifierNames.Method
        );

        // "AddRange" method --> optional
        // ((MethodCallExpression)((Expression<Action<[COLLECTION]>>)(set => set.AddRange(default!))).Body).Method
        var addRangeMethodInitSyntax = HasAddRangeMethod(mutableCollectionType.Symbol)
            ? SimpleMemberAccessExpression(
                expression: ParenthesizedExpression(
                    expression: CastExpression(
                        type: TypeNames.LinqMethodCallExpression,
                        expression: ParenthesizedExpression(
                            expression: SimpleMemberAccessExpression(
                                expression: ParenthesizedExpression(
                                    expression: CastExpression(
                                        type: addMethodExpressionType,
                                        expression: ParenthesizedExpression(SimpleLambdaExpression(
                                            parameter: Parameter(Identifier("collection")),
                                            body: InvocationExpression(
                                                expression: SimpleMemberAccessExpression(IdentifierNames.collection, IdentifierNames.AddRange),
                                                argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                                                {
                                                    Argument(PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, LiteralExpression(SyntaxKind.DefaultLiteralExpression)))
                                                }))
                                            )
                                        ))
                                    )
                                ),
                                name: IdentifierNames.Body
                            )
                        )
                    )
                ),
                name: IdentifierNames.Method
            )
            : default;

        if (!TryGetGetEnumeratorMethod(mutableCollectionType.Symbol, out var enumeratorInfo))
        {
            throw new GenerationException(DiagnosticDescriptors.CollectionGetEnumeratorMissing, default, [mutableCollectionType.FullName]);
        }

        // var x = ((MethodCallExpression)((Expression<Action<HashSet<int>>>)(set => set.GetEnumerator())).Body).Method;

        // "GetEnumerator" method
        // ((MethodCallExpression)((Expression<Action<[COLLECTION]>>)(set => set.GetEnumerator())).Body).Method
        var getEnumeratorMethodInitSyntax = SimpleMemberAccessExpression(
            expression: ParenthesizedExpression(
                expression: CastExpression(
                    type: TypeNames.LinqMethodCallExpression,
                    expression: ParenthesizedExpression(
                        expression: SimpleMemberAccessExpression(
                            expression: ParenthesizedExpression(
                                expression: CastExpression(
                                    type: addMethodExpressionType,
                                    expression: ParenthesizedExpression(SimpleLambdaExpression(
                                        parameter: Parameter(Identifier("collection")),
                                        body: InvocationExpression(
                                            expression: SimpleMemberAccessExpression(
                                                expression: enumeratorInfo.IsExplicit
                                                    ? CastExpression(
                                                        type: GenericName(
                                                            identifier: Identifier("IEnumerable"),
                                                            typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
                                                            {
                                                                elementType.Syntax
                                                            }))
                                                        ),
                                                        expression: IdentifierNames.collection
                                                    )
                                                    : IdentifierNames.collection,
                                                name: IdentifierNames.GetEnumerator
                                            ),
                                            argumentList: EmptyArguments
                                        )
                                    ))
                                )
                            ),
                            name: IdentifierNames.Body
                        )
                    )
                )
            ),
            name: IdentifierNames.Method
        );

        // var x = ((MethodCallExpression)((Expression<Func<HashSet<int>.Enumerator, bool>>)(enumerator => enumerator.MoveNext())).Body).Method;

        // enumerator."MoveNext" method
        // ((MethodCallExpression)((Expression<Func<[ENUMERATOR]>>)(enumerator => enumerator.MoveNext()).Body).Method
        var moveNextExpressionType = GenericName(
            identifier: Identifier("Expression"),
            typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
            {
                GenericName(
                    identifier: Identifier("Func"),
                    typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
                    {
                        ParseTypeName(enumeratorInfo.EnumeratorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                        TypeNames.@bool
                    }))
                )
            }))
        );
        var moveNextMethodInitSyntax = SimpleMemberAccessExpression(
            expression: ParenthesizedExpression(
                expression: CastExpression(
                    type: TypeNames.LinqMethodCallExpression,
                    expression: ParenthesizedExpression(
                        expression: SimpleMemberAccessExpression(
                            expression: ParenthesizedExpression(
                                expression: CastExpression(
                                    type: moveNextExpressionType,
                                    expression: ParenthesizedExpression(SimpleLambdaExpression(
                                        parameter: Parameter(Identifier("enumerator")),
                                        body: InvocationExpression(
                                            expression: SimpleMemberAccessExpression(IdentifierNames.enumerator, IdentifierNames.MoveNext),
                                            argumentList: EmptyArguments
                                        )
                                    ))
                                )
                            ),
                            name: IdentifierNames.Body
                        )
                    )
                )
            ),
            name: IdentifierNames.Method
        );

        // var x = (PropertyInfo)((MemberExpression)((Expression<Func<HashSet<int>.Enumerator, int>>)(e => e.Current)).Body).Member;

        // enumerator."Current" property
        // (PropertyInfo)((MemberExpression)((Expression<Func<[ENUMERATOR], [ELEMENT]>>)(e => e.Current)).Body).Member;
        var currentPropertyExpressionType = GenericName(
            identifier: Identifier("Expression"),
            typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
            {
                GenericName(
                    identifier: Identifier("Func"),
                    typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
                    {
                        ParseTypeName(enumeratorInfo.EnumeratorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                        elementType.Syntax
                    }))
                )
            }))
        );
        var currentProperty = CastExpression(
            type: ParseTypeName(nameof(PropertyInfo)),
            expression: SimpleMemberAccessExpression(
                expression: ParenthesizedExpression(CastExpression(
                    type: ParseTypeName(nameof(MemberExpression)),
                    expression: SimpleMemberAccessExpression(
                        expression: ParenthesizedExpression(CastExpression(
                            type: currentPropertyExpressionType,
                            expression: ParenthesizedExpression(SimpleLambdaExpression(
                                parameter: Parameter(Identifier("e")),
                                body: SimpleMemberAccessExpression(
                                    expression: IdentifierName("e"),
                                    name: IdentifierNames.Current
                                )
                            ))
                        )),
                        name: nameof(LambdaExpression.Body)
                    )
                )),
                name: nameof(MemberExpression.Member)
            )
        );

        // default ctor expression
        // ((NewExpression)((Expression<Func<[COLLECTION]>>)(() => new [COLLECTION]())).Body).Constructor
        var ctorExpressionType = GenericName(
            identifier: Identifier("Expression"),
            typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
            {
                GenericName(
                    identifier: Identifier("Func"),
                    typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[]
                    {
                        collectionTypeSyntax
                    }))
                )
            }))
        );
        var ctorInitSyntax = PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, SimpleMemberAccessExpression(
            expression: ParenthesizedExpression(
                expression: CastExpression(
                    type: TypeNames.LinqNewExpression,
                    expression: SimpleMemberAccessExpression(
                        expression: ParenthesizedExpression(
                            expression: CastExpression(
                                type: ctorExpressionType,
                                expression: ParenthesizedExpression(
                                    expression: ParenthesizedLambdaExpression(
                                        parameterList: EmptyParameters,
                                        body: ObjectCreationExpression(type: collectionTypeSyntax, argumentList: EmptyArguments, initializer: default)
                                    )
                                )
                            )
                        ),
                        name: IdentifierNames.Body
                    )
                )
            ),
            name: IdentifierNames.Constructor
        ));

        var addMethodField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: TypeNames.MethodInfo,
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("addMethod"),
                        argumentList: default,
                        initializer: EqualsValueClause(addMethodInitSyntax)
                    )
                })
            )
        );

        var addRangeMethodField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: NullableType(TypeNames.MethodInfo, Token(SyntaxKind.QuestionToken)),
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("addRangeMethod"),
                        argumentList: default,
                        initializer: EqualsValueClause((ExpressionSyntax?)addRangeMethodInitSyntax ?? LiteralExpression(SyntaxKind.DefaultLiteralExpression))
                    )
                })
            )
        );

        var getEnumeratorMethodField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: TypeNames.MethodInfo,
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("getEnumeratorMethod"),
                        argumentList: default,
                        initializer: EqualsValueClause(getEnumeratorMethodInitSyntax)
                    )
                })
            )
        );

        var moveNextMethodField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: TypeNames.MethodInfo,
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("moveNextMethod"),
                        argumentList: default,
                        initializer: EqualsValueClause(moveNextMethodInitSyntax)
                    )
                })
            )
        );

        var currentPropertyField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: TypeNames.PropertyInfo,
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("currentProperty"),
                        argumentList: default,
                        initializer: EqualsValueClause(currentProperty)
                    )
                })
            )
        );

        var ctorField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: TypeNames.ConstructorInfo,
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("ctor"),
                        argumentList: default,
                        initializer: EqualsValueClause(ctorInitSyntax)
                    )
                })
            )
        );

        var singletonProperty = PropertyDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PublicStatic,
            type: ParseTypeName(className),
            explicitInterfaceSpecifier: default,
            identifier: Identifier("Singleton"),
            accessorList: AccessorList(List(new AccessorDeclarationSyntax[]
            {
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            })),
            expressionBody: default,
            initializer: EqualsValueClause(ObjectCreationExpression(
                type: ParseTypeName(className),
                argumentList: EmptyArguments,
                initializer: default
            )),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var elementTypeProperty = PropertyDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            type: TypeNames.Type,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("ElementType"),
            accessorList: default,
            expressionBody: ArrowExpressionClause(TypeOfExpression(elementType.Syntax)),
            initializer: default,
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var collectionTypeProperty = PropertyDeclaration(
            attributeLists: List(new AttributeListSyntax[]
            {
                AttributeList(SeparatedList(new AttributeSyntax[]
                {
                    Attribute(ParseName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                    {
                        AttributeArgument(SimpleMemberAccessExpression(ParseTypeName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"), "All"))
                    })))
                }))
            }),
            modifiers: Modifiers.Public,
            type: TypeNames.Type,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CollectionType"),
            accessorList: default,
            expressionBody: ArrowExpressionClause(TypeOfExpression(mutableCollectionType.Syntax)),
            initializer: default,
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var createNewExpression1Method = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.LinqExpression,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CreateNewExpression"),
            typeParameterList: default,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(
                    attributeLists: default,
                    modifiers: default,
                    type: GenericName(
                        identifier: Identifier("IEnumerable"),
                        typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[] { TypeNames.LinqExpression }))
                    ),
                    identifier: Identifier("items"),
                    @default: default
                )
            })),
            constraintClauses: default,
            body: default,
            expressionBody: ArrowExpressionClause(
                InvocationExpression(
                    expression: SimpleMemberAccessExpression(TypeNames.LinqExpression, "ListInit"),
                    argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(InvocationExpression(
                            expression: SimpleMemberAccessExpression(TypeNames.LinqExpression, "New"),
                            argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName("ctor"))
                            }))
                        )),
                        Argument(InvocationExpression(
                            expression: SimpleMemberAccessExpression(IdentifierName("items"), "Select"),
                            argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(SimpleLambdaExpression(
                                    parameter: Parameter(Identifier("item")),
                                    body: InvocationExpression(
                                        expression: SimpleMemberAccessExpression(TypeNames.LinqExpression, "ElementInit"),
                                        argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                                        {
                                            Argument(IdentifierName("addMethod")),
                                            Argument(IdentifierName("item"))
                                        }))
                                    )
                                ))
                            }))
                        ))
                    }))
                )
            ),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var createNewExpression2Method = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.LinqExpression,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CreateNewExpression"),
            typeParameterList: default,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(
                    attributeLists: default,
                    modifiers: default,
                    type: TypeNames.LinqExpression,
                    identifier: Identifier("items"),
                    @default: default
                )
            })),
            constraintClauses: default,
            body: default,
            expressionBody: ArrowExpressionClause(
                InvocationExpression(
                    expression: SimpleMemberAccessExpression(TypeNames.MappingHelpers, "CreateNewCollectionExpressionWithEnumerableInitialization"),
                    argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(TypeOfExpression(mutableCollectionType.Syntax)),
                        Argument(TypeOfExpression(ParseTypeName(enumeratorInfo.EnumeratorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))),
                        Argument(IdentifierName("addMethod")),
                        Argument(IdentifierName("addRangeMethod")),
                        Argument(IdentifierName("getEnumeratorMethod")),
                        Argument(IdentifierName("moveNextMethod")),
                        Argument(IdentifierName("currentProperty")),
                        Argument(IdentifierName("items"))
                    }))
                )
            ),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var createBuilderMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.ICollectionBuilder,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CreateBuilder"),
            typeParameterList: default,
            parameterList: EmptyParameters,
            constraintClauses: default,
            body: default,
            expressionBody: ArrowExpressionClause(
                ObjectCreationExpression(ParseTypeName($"NCoreUtils.Data.Mapping.MutableCollectionBuilder<{mutableCollectionType.FullName}, {elementType.FullName}>"), EmptyArguments, default)
            ),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        return ClassDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateSealed,
            identifier: Identifier(className),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(TypeNames.ICollectionFactory)
            })),
            constraintClauses: default,
            members: List(new MemberDeclarationSyntax[]
            {
                addMethodField,
                addRangeMethodField,
                getEnumeratorMethodField,
                moveNextMethodField,
                currentPropertyField,
                ctorField,
                singletonProperty,
                elementTypeProperty,
                collectionTypeProperty,
                createNewExpression1Method,
                createNewExpression2Method,
                createBuilderMethod
            })
        );
    }

    private TypeDeclarationSyntax EmitImmutableCollectionFactory(
        ref TypeWrapper elementType,
        ref TypeWrapper mutableCollectionType,
        ref TypeWrapper immutableCollectionType)
    {
        var mutableFactoryClassName = GetMutableCollectionFactoryClassName(ref mutableCollectionType);
        var className = GetImmutableCollectionFactoryClassName(ref immutableCollectionType);
        var mutableFactoryTypeSyntax = ParseTypeName(mutableFactoryClassName);

        var factoryField = FieldDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateStaticReadonly,
            declaration: VariableDeclaration(
                type: mutableFactoryTypeSyntax,
                variables: SeparatedList(new VariableDeclaratorSyntax[]
                {
                    VariableDeclarator(
                        identifier: Identifier("factory"),
                        argumentList: default,
                        initializer: EqualsValueClause(SimpleMemberAccessExpression(ParseTypeName(mutableFactoryClassName), IdentifierNames.Singleton))
                    )
                })
            )
        );

        var singletonProperty = PropertyDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PublicStatic,
            type: ParseTypeName(className),
            explicitInterfaceSpecifier: default,
            identifier: Identifier("Singleton"),
            accessorList: AccessorList(List(new AccessorDeclarationSyntax[]
            {
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            })),
            expressionBody: default,
            initializer: EqualsValueClause(ObjectCreationExpression(
                type: ParseTypeName(className),
                argumentList: EmptyArguments,
                initializer: default
            )),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var elementTypeProperty = PropertyDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            type: TypeNames.Type,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("ElementType"),
            accessorList: default,
            expressionBody: ArrowExpressionClause(TypeOfExpression(elementType.Syntax)),
            initializer: default,
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var collectionTypeProperty = PropertyDeclaration(
            attributeLists: List(new AttributeListSyntax[]
            {
                AttributeList(SeparatedList(new AttributeSyntax[]
                {
                    Attribute(ParseName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                    {
                        AttributeArgument(SimpleMemberAccessExpression(ParseTypeName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"), "All"))
                    })))
                }))
            }),
            modifiers: Modifiers.Public,
            type: TypeNames.Type,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CollectionType"),
            accessorList: default,
            expressionBody: ArrowExpressionClause(TypeOfExpression(mutableCollectionType.Syntax)),
            initializer: default,
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var createNewExpression1Method = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.LinqExpression,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CreateNewExpression"),
            typeParameterList: default,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(
                    attributeLists: default,
                    modifiers: default,
                    type: GenericName(
                        identifier: Identifier("IEnumerable"),
                        typeArgumentList: TypeArgumentList(SeparatedList(new TypeSyntax[] { TypeNames.LinqExpression }))
                    ),
                    identifier: Identifier("items"),
                    @default: default
                )
            })),
            constraintClauses: default,
            body: default,
            expressionBody: ArrowExpressionClause(
                InvocationExpression(
                    expression: SimpleMemberAccessExpression(IdentifierName("factory"), "CreateNewExpression"),
                    argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName("items"))
                    }))
                )
            ),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var createNewExpression2Method = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.LinqExpression,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CreateNewExpression"),
            typeParameterList: default,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(
                    attributeLists: default,
                    modifiers: default,
                    type: TypeNames.LinqExpression,
                    identifier: Identifier("items"),
                    @default: default
                )
            })),
            constraintClauses: default,
            body: default,
            expressionBody: ArrowExpressionClause(
                InvocationExpression(
                    expression: SimpleMemberAccessExpression(IdentifierName("factory"), "CreateNewExpression"),
                    argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName("items"))
                    }))
                )
            ),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        var createBuilderMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.ICollectionBuilder,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("CreateBuilder"),
            typeParameterList: default,
            parameterList: EmptyParameters,
            constraintClauses: default,
            body: default,
            expressionBody: ArrowExpressionClause(
                InvocationExpression(
                    expression: SimpleMemberAccessExpression(IdentifierName("factory"), "CreateBuilder"),
                    argumentList: EmptyArguments
                )
            ),
            semicolonToken: Token(SyntaxKind.SemicolonToken)
        );

        return ClassDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateSealed,
            identifier: Identifier(className),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(TypeNames.ICollectionFactory)
            })),
            constraintClauses: default,
            members: List(new MemberDeclarationSyntax[]
            {
                factoryField,
                singletonProperty,
                elementTypeProperty,
                collectionTypeProperty,
                createNewExpression1Method,
                createNewExpression2Method,
                createBuilderMethod
            })
        );
    }

    private TypeDeclarationSyntax EmitCollectionFactoryFactory(
        RList<TypeWrapper> mutableCollectionTypes,
        RList<TypeWrapper> immutableCollectionTypes)
    {
        var statements = new List<StatementSyntax>(mutableCollectionTypes.Count + immutableCollectionTypes.Count + 2);
        for (var i = 0; i < mutableCollectionTypes.Count; ++i)
        {
            var typeSyntax = mutableCollectionTypes[i].Syntax;
            var factoryTypeSyntax = ParseTypeName(GetMutableCollectionFactoryClassName(ref mutableCollectionTypes[i]));

            var condition = BinaryExpression(kind: SyntaxKind.EqualsExpression, left: IdentifierNames.collectionType, right: TypeOfExpression(typeSyntax));
            var assign = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierNames.builder, SimpleMemberAccessExpression(factoryTypeSyntax, IdentifierNames.Singleton));
            statements.Add(IfStatement(
                condition: condition,
                statement: Block(List(new StatementSyntax[]
                {
                    ExpressionStatement(assign),
                    ReturnStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression))
                }))
            ));
        }
        for (var i = 0; i < immutableCollectionTypes.Count; ++i)
        {
            var typeSyntax = immutableCollectionTypes[i].Syntax;
            var factoryTypeSyntax = ParseTypeName(GetImmutableCollectionFactoryClassName(ref immutableCollectionTypes[i]));
            statements.Add(IfStatement(
                condition: BinaryExpression(SyntaxKind.EqualsExpression, IdentifierNames.collectionType, TypeOfExpression(typeSyntax)),
                statement: Block(List(new StatementSyntax[]
                {
                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierNames.builder, SimpleMemberAccessExpression(factoryTypeSyntax, IdentifierNames.Singleton))),
                    ReturnStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression))
                }))
            ));
        }
        statements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierNames.builder, LiteralExpression(SyntaxKind.DefaultLiteralExpression))));
        statements.Add(ReturnStatement(LiteralExpression(SyntaxKind.FalseLiteralExpression)));

        var tryCreateMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.@bool,
            explicitInterfaceSpecifier: default!,
            identifier: Identifier("TryCreate"),
            typeParameterList: default!,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(
                    attributeLists: List(new AttributeListSyntax[]
                    {
                        AttributeList(SeparatedList(new AttributeSyntax[]
                        {
                            Attribute(ParseName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                            {
                                AttributeArgument(SimpleMemberAccessExpression(ParseTypeName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"), "All"))
                            })))
                        }))
                    }),
                    modifiers: default,
                    type: TypeNames.Type,
                    identifier: Identifier("collectionType"),
                    @default: default
                ),
                Parameter(
                    attributeLists: List(new AttributeListSyntax[]
                    {
                        AttributeList(SeparatedList(new AttributeSyntax[]
                        {
                            Attribute(ParseName("System.Diagnostics.CodeAnalysis.MaybeNullWhen"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                            {
                                AttributeArgument(LiteralExpression(SyntaxKind.FalseLiteralExpression))
                            })))
                        }))
                    }),
                    modifiers: Modifiers.Out,
                    type: TypeNames.ICollectionFactory,
                    identifier: Identifier("builder"),
                    @default: default
                )
            })),
            constraintClauses: default,
            body: Block(statements),
            semicolonToken: default
        );

        return ClassDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateSealed,
            identifier: Identifier("ContextCollectionFactoryFactory"),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(TypeNames.ICollectionFactoryFactory)
            })),
            constraintClauses: default,
            members: List(new MemberDeclarationSyntax[]
            {
                tryCreateMethod
            })
        );
    }

    private bool IsCollectionType(ITypeSymbol type)
            => type.SpecialType == SpecialType.None
                && type.AllInterfaces.Any(static s => s.Name == "IEnumerable")
                && !SymbolEqualityComparer.Default.Equals(Context.ArrayOfByte, type);

    private TypeDeclarationSyntax EmitCollectionWrapperFactory(RDictionary allTypes)
    {
        var statements = new List<StatementSyntax>();
        {
            int i = 0;
            foreach (ref var type in allTypes)
            {
                if (!IsCollectionType(type.Symbol))
                {
                    var varName = $"enumerable{i++}";
                    var statement = IfStatement(
                        condition: IsPatternExpression(
                            expression: IdentifierNames.source,
                            pattern: DeclarationPattern(ParseTypeName($"IEnumerable<{type.FullName}>"), SingleVariableDesignation(Identifier(varName)))
                        ),
                        statement: Block(List(new StatementSyntax[]
                        {
                            ExpressionStatement(AssignmentExpression(
                                kind: SyntaxKind.SimpleAssignmentExpression,
                                left: IdentifierNames.wrapper,
                                right: ObjectCreationExpression(
                                    type: ParseTypeName($"NCoreUtils.Data.Google.Cloud.Firestore.Internal.AnyCollectionWrapper<{type.FullName}>"),
                                    argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[] { Argument(IdentifierName(varName)) })),
                                    initializer: default
                                )
                            )),
                            ReturnStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression))
                        }))
                    );
                    statements.Add(statement);
                }
            }
            statements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierNames.wrapper, LiteralExpression(SyntaxKind.DefaultLiteralExpression))));
            statements.Add(ReturnStatement(LiteralExpression(SyntaxKind.FalseLiteralExpression)));
        }


        var tryCreateMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: Modifiers.Public,
            returnType: TypeNames.@bool,
            explicitInterfaceSpecifier: default!,
            identifier: Identifier("TryCreate"),
            typeParameterList: default!,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(attributeLists: default, modifiers: default, type: TypeNames.@object, identifier: Identifier("source"), @default: default),
                Parameter(
                    attributeLists: List(new AttributeListSyntax[]
                    {
                        AttributeList(SeparatedList(new AttributeSyntax[]
                        {
                            Attribute(ParseName("System.Diagnostics.CodeAnalysis.MaybeNullWhen"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
                            {
                                AttributeArgument(LiteralExpression(SyntaxKind.FalseLiteralExpression))
                            })))
                        }))
                    }),
                    modifiers: Modifiers.Out,
                    type: TypeNames.ICollectionWrapper,
                    identifier: Identifier("wrapper"),
                    @default: default
                )
            })),
            constraintClauses: default,
            body: Block(statements),
            semicolonToken: default
        );

        return ClassDeclaration(
            attributeLists: default,
            modifiers: Modifiers.PrivateSealed,
            identifier: Identifier("ContextCollectionWrapperFactory"),
            typeParameterList: default,
            baseList: BaseList(SeparatedList(new BaseTypeSyntax[]
            {
                SimpleBaseType(TypeNames.ICollectionWrapperFactory)
            })),
            constraintClauses: default,
            members: List(new MemberDeclarationSyntax[]
            {
                tryCreateMethod
            })
        );
    }

    private INamedTypeSymbol GetMutableCollectionForImmutableCollection(
        ref TypeWrapper immutableCollectionType,
        ref TypeWrapper elementType)
    {
        if (immutableCollectionType.Symbol is INamedTypeSymbol named)
        {
            if (named.ConstructedFrom?.Name is "IReadOnlyList" or "IReadOnlyCollection" or "IEnumerable")
            {
                return Context.ListOfT.Construct(elementType.Symbol);
            }
            if (named.ConstructedFrom?.Name is "IReadOnlySet")
            {
                return Context.HashSetOfT.Construct(elementType.Symbol);
            }
        }
        throw new GenerationException(DiagnosticDescriptors.NoMutableCollectionFound, default, immutableCollectionType.FullName);
    }

    private TypeDeclarationSyntax EmitContext(ContextTarget contextTarget)
    {
        var modelBuilderTypeSyntax = ParseTypeName("NCoreUtils.Data.Build.DataModelBuilder");
        var addBuildersExpression = contextTarget.Entities.Aggregate(
            seed: (ExpressionSyntax)IdentifierName("model"),
            func: static (expr, target) => InvocationExpression(
                expression: SimpleMemberAccessExpression(
                    expression: expr.WithTrailingTrivia(EndOfLine("\r\n"), Whitespace("        ")),
                    name: "AddEntityBuilder"
                ),
                argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(
                        ObjectCreationExpression(
                            ParseTypeName($"{target.Type.Name}Builder"),
                            ArgumentList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken)),
                            initializer: default
                        )
                    )
                }))
            )
        );
        var applyMethodBody = new List<StatementSyntax>()
        {
            ExpressionStatement(addBuildersExpression)
        };
        if (contextTarget.GenerateFirestoreDecorators)
        {
            applyMethodBody.Add(ExpressionStatement(InvocationExpression(
                expression: SimpleMemberAccessExpression(
                    expression: IdentifierName("model"),
                    name: "SetMetadata"
                ),
                argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(SimpleMemberAccessExpression(TypeNames.FirestoreMetadataExtensions, IdentifierNames.KeyEnumConversionHelpers)),
                    Argument(ObjectCreationExpression(ParseTypeName("EnumConversionHelpers"), EmptyArguments, default))
                }))
            )));

            applyMethodBody.Add(ExpressionStatement(InvocationExpression(
                expression: SimpleMemberAccessExpression(
                    expression: IdentifierName("model"),
                    name: "SetMetadata"
                ),
                argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(SimpleMemberAccessExpression(TypeNames.FirestoreMetadataExtensions, IdentifierNames.KeyCollectionFactoryFactory)),
                    Argument(ObjectCreationExpression(ParseTypeName("ContextCollectionFactoryFactory"), EmptyArguments, default))
                }))
            )));

            applyMethodBody.Add(ExpressionStatement(InvocationExpression(
                expression: SimpleMemberAccessExpression(
                    expression: IdentifierName("model"),
                    name: "SetMetadata"
                ),
                argumentList: ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(SimpleMemberAccessExpression(TypeNames.FirestoreMetadataExtensions, IdentifierNames.KeyCollectionWrapperFactory)),
                    Argument(ObjectCreationExpression(ParseTypeName("ContextCollectionWrapperFactory"), EmptyArguments, default))
                }))
            )));
        }
        applyMethodBody.Add(ReturnStatement(IdentifierName("model")));
        var applyMethod = MethodDeclaration(
            attributeLists: default,
            modifiers: TokenList(Modifiers.Public),
            returnType: modelBuilderTypeSyntax,
            explicitInterfaceSpecifier: default,
            identifier: Identifier("Apply"),
            typeParameterList: default,
            parameterList: ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(attributeLists: default, modifiers: TokenList(/*Token(SyntaxKind.ThisKeyword)*/), type: modelBuilderTypeSyntax, identifier: Identifier("model"), @default: default)
            })),
            constraintClauses: default,
            body: Block(
                attributeLists: default,
                openBraceToken: Token(SyntaxKind.OpenBraceToken),
                statements: List(applyMethodBody),
                closeBraceToken: Token(SyntaxKind.CloseBraceToken)
            ),
            expressionBody: default,
            semicolonToken: default
        );
        var members = new List<MemberDeclarationSyntax>(contextTarget.Entities.Length + 1);
        foreach (var target in contextTarget.Entities)
        {
            members.Add(EmitDataEntityBuilder(contextTarget, target));
        }
        members.Add(applyMethod);
        if (contextTarget.GenerateFirestoreDecorators)
        {
            var typeCache = new RDictionary();
            var allPropertyTypes = new HashSet<ISymbol>(
                contextTarget.Entities
                    .SelectMany(target => target.Type.Symbol.GetMembers())
                    .OfType<IPropertySymbol>()
                    .Where(static p => !p.IsStatic)
                    .Select(static p => p.Type),
                SymbolEqualityComparer.Default
            );
            var enumTypes = new HashSet<ISymbol>(
                allPropertyTypes
                    .Cast<ITypeSymbol>()
                    .Where(static t => t.TypeKind == TypeKind.Enum),
                SymbolEqualityComparer.Default
            );
            var collectionTypes = new HashSet<ISymbol>(
                allPropertyTypes
                    .Cast<ITypeSymbol>()
                    .Where(IsCollectionType),
                SymbolEqualityComparer.Default
            );
            foreach (var type in allPropertyTypes.Cast<ITypeSymbol>())
            {
                ref var wrapper = ref typeCache.GetOrAdd(type);
                members.Add(EmitFieldExpressionFactory(ref wrapper));
            }
            var enumFactoryNames = new Dictionary<string, string>();
            foreach (var enumType in enumTypes.Cast<ITypeSymbol>())
            {
                ref var wrapper = ref typeCache.GetOrAdd(enumType);
                members.Add(EmitEnumFactory(enumFactoryNames, ref wrapper));
            }
            var mutableCollectionTypes = new RList<TypeWrapper>(collectionTypes.Count);
            var immutableCollectionTypes = new RList<TypeWrapper>(collectionTypes.Count);
            foreach (var collectionType in collectionTypes.CastAsNamedTypeSymbol())
            {
                ref var elementWrapper = ref typeCache.GetOrAdd(GetElementType(collectionType));
                if (IsImmutableCollectionType(collectionType))
                {
                    ref var collectionWrapper = ref immutableCollectionTypes.Add(new TypeWrapper(collectionType));
                    ref var mutableCollectionWrapper = ref mutableCollectionTypes.Add(new TypeWrapper(GetMutableCollectionForImmutableCollection(ref collectionWrapper, ref elementWrapper)));
                    members.Add(EmitMutableCollectionFactory(ref elementWrapper, ref mutableCollectionWrapper));
                    members.Add(EmitImmutableCollectionFactory(ref elementWrapper, ref mutableCollectionWrapper, ref collectionWrapper));
                }
                else
                {
                    ref var collectionWrapper = ref mutableCollectionTypes.Add(new TypeWrapper(collectionType));
                    members.Add(EmitMutableCollectionFactory(ref elementWrapper, ref collectionWrapper));
                }
            }
            members.Add(EmitEnumConversionHelpers(enumFactoryNames));
            members.Add(EmitCollectionFactoryFactory(mutableCollectionTypes, immutableCollectionTypes));
            members.Add(EmitCollectionWrapperFactory(typeCache));
        }
        return ClassDeclaration(
            attributeLists: List<AttributeListSyntax>(),
            modifiers: TokenList(Token(SyntaxKind.PartialKeyword)),
            identifier: Identifier(contextTarget.ContextType.Name),
            typeParameterList: default,
            baseList: default,
            constraintClauses: default,
            members: List(members)
        );

        static bool IsImmutableCollectionType(INamedTypeSymbol type)
            => !type.GetMembers("Add").OfType<IMethodSymbol>().Any(m => !m.IsStatic && m.Parameters.Length == 1)
                && (type.BaseType is null || type.BaseType.SpecialType == SpecialType.None || IsImmutableCollectionType(type.BaseType));

        static INamedTypeSymbol GetElementType(INamedTypeSymbol type)
        {
            if (type.AllInterfaces.TryGetFirst(i => i.IsGenericType && i.TypeArguments.Length == 1 && i.ConstructedFrom.Name == "IEnumerable", out var enumerableInterface))
            {
                return (INamedTypeSymbol)enumerableInterface.TypeArguments[0];
            }
            throw new InvalidOperationException($"Cannot get element type for {type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
        }
    }

    public CompilationUnitSyntax EmitCompilationUnit(ContextTarget contextTarget)
    {
        SyntaxTriviaList syntaxTriviaList = TriviaList(
            Comment("// <auto-generated/>"),
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true))
        );

        var @namespace = contextTarget.ContextType.Symbol.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
        return CompilationUnit()
            .AddUsings(
                UsingDirective(ParseTypeName("System")),
                UsingDirective(ParseTypeName("System.Linq.Expressions")),
                UsingDirective(ParseTypeName("System.Reflection"))
            )
            .AddMembers(
                NamespaceDeclaration(IdentifierName(@namespace))
                    .WithLeadingTrivia(syntaxTriviaList)
                    .AddMembers(EmitContext(contextTarget))
            )
            .NormalizeWhitespace();
    }
}