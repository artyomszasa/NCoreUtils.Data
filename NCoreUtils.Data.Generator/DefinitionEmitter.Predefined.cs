using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NCoreUtils.Data;

internal partial class DefinitionEmitter
{
    private static class Modifiers
    {
        public static SyntaxTokenList PrivateReadonly { get; } = TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));

        public static SyntaxTokenList PrivateStaticReadonly { get; } = TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));

        public static SyntaxTokenList PrivateStatic { get; } = TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword));

        public static SyntaxTokenList PrivateSealed { get; } = TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.SealedKeyword));

        public static SyntaxTokenList Public { get; } = TokenList(Token(SyntaxKind.PublicKeyword));

        public static SyntaxTokenList PublicStatic { get; } = TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));

        public static SyntaxTokenList PublicOverride { get; } = TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword));

        public static SyntaxTokenList Out { get; } = TokenList(Token(SyntaxKind.OutKeyword));
    }

    private static class IdentifierNames
    {
        public static IdentifierNameSyntax Add { get; } = IdentifierName("Add");

        public static IdentifierNameSyntax AddRange { get; } = IdentifierName("AddRange");

        public static IdentifierNameSyntax Body { get; } = IdentifierName("Body");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax builder { get; } = IdentifierName("builder");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax collection { get; } = IdentifierName("collection");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax collectionType { get; } = IdentifierName("collectionType");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax converter { get; } = IdentifierName("converter");

        public static IdentifierNameSyntax Constructor { get; } = IdentifierName("Constructor");

        public static IdentifierNameSyntax Count { get; } = IdentifierName("Count");

        public static IdentifierNameSyntax Create { get; } = IdentifierName("Create");

        public static IdentifierNameSyntax Current { get; } = IdentifierName("Current");

        public static IdentifierNameSyntax DocumentId { get; } = IdentifierName("DocumentId");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax entity { get; } = IdentifierName("entity");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax enumerator { get; } = IdentifierName("enumerator");

        public static IdentifierNameSyntax GetEnumerator { get; } = IdentifierName("GetEnumerator");

        public static IdentifierNameSyntax Key { get; } = IdentifierName("Key");

        public static IdentifierNameSyntax KeyEnumConversionHelpers { get; } = IdentifierName("KeyEnumConversionHelpers");

        public static IdentifierNameSyntax KeyFieldExpressionFactory { get; } = IdentifierName("KeyFieldExpressionFactory");

        public static IdentifierNameSyntax KeyCollectionFactoryFactory { get; } = IdentifierName("KeyCollectionFactoryFactory");

        public static IdentifierNameSyntax KeyCollectionWrapperFactory { get; } = IdentifierName("KeyCollectionWrapperFactory");

        public static IdentifierNameSyntax Method { get; } = IdentifierName("Method");

        public static IdentifierNameSyntax MoveNext { get; } = IdentifierName("MoveNext");

        public static IdentifierNameSyntax Name { get; } = IdentifierName("Name");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax property { get; } = IdentifierName("property");

        public static IdentifierNameSyntax Property { get; } = IdentifierName("Property");

        public static IdentifierNameSyntax SetMetadata { get; } = IdentifierName("SetMetadata");

        public static IdentifierNameSyntax Singleton { get; } = IdentifierName("Singleton");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax snapshot { get; } = IdentifierName("snapshot");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax source { get; } = IdentifierName("source");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static IdentifierNameSyntax wrapper { get; } = IdentifierName("wrapper");
    }

    private static class TypeNames
    {
        private static ConcurrentDictionary<string, TypeSyntax> Cache { get; } = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static TypeSyntax @bool { get; } = ParseTypeName("bool");

        public static TypeSyntax ConstructorInfo { get; } = ParseTypeName("System.Reflection.ConstructorInfo");

        public static TypeSyntax DataEntity { get; } = ParseTypeName("NCoreUtils.Data.Model.DataEntity");

        public static TypeSyntax DataProperty { get; } = ParseTypeName("NCoreUtils.Data.Model.DataProperty");

        public static TypeSyntax FieldPath { get; } = ParseTypeName("global::Google.Cloud.Firestore.FieldPath");

        public static TypeSyntax FirestoreConverter { get; } = ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.FirestoreConverter");

        public static TypeSyntax FirestoreFieldExpression { get; } = ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Expressions.FirestoreFieldExpression");

        public static TypeSyntax FirestoreFieldExpressionOfString { get; } = ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Expressions.FirestoreFieldExpression<string>");

        public static TypeSyntax FirestoreMetadataExtensions { get; } = ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Internal.FirestoreMetadataExtensions");

        public static TypeSyntax ICollectionBuilder { get; } = ParseTypeName("NCoreUtils.Data.ICollectionBuilder");

        public static TypeSyntax ICollectionFactory { get; } = ParseTypeName("NCoreUtils.Data.ICollectionFactory");

        public static TypeSyntax ICollectionFactoryFactory { get; } = ParseTypeName("NCoreUtils.Data.ICollectionFactoryFactory");

        public static TypeSyntax ICollectionWrapper { get; } = ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Internal.ICollectionWrapper");

        public static TypeSyntax ICollectionWrapperFactory { get; } = ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Internal.ICollectionWrapperFactory");

        public static TypeSyntax IFirestoreFieldExpressionFactory { get; } = ParseTypeName("NCoreUtils.Data.Google.Cloud.Firestore.Internal.IFirestoreFieldExpressionFactory");

        public static TypeSyntax ImmutableList { get; } = ParseTypeName("global::System.Collections.Immutable.ImmutableList");

        public static TypeSyntax LinqExpression { get; } = ParseTypeName("System.Linq.Expressions.Expression");

        public static TypeSyntax LinqMethodCallExpression { get; } = ParseTypeName("System.Linq.Expressions.MethodCallExpression");

        public static TypeSyntax LinqNewExpression { get; } = ParseTypeName("System.Linq.Expressions.NewExpression");

        public static TypeSyntax MappingHelpers { get; } = ParseTypeName("NCoreUtils.Data.Mapping.Helpers");

        public static TypeSyntax MethodInfo { get; } = ParseTypeName("System.Reflection.MethodInfo");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static TypeSyntax @object { get; } = ParseTypeName("object");

        public static TypeSyntax PropertyInfo { get; } = ParseTypeName("System.Reflection.PropertyInfo");

        public static TypeSyntax Type { get; } = ParseTypeName("System.Type");

        public static TypeSyntax GetOrParse(string source)
            => Cache.GetOrAdd(source, static source => ParseTypeName(source));
    }

    private static ParameterListSyntax EmptyParameters { get; } = ParameterList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken));

    private static ArgumentListSyntax EmptyArguments { get; } = ArgumentList(Token(SyntaxKind.OpenParenToken), default, Token(SyntaxKind.CloseParenToken));

}