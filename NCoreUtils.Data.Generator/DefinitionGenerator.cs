using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Data;

[Generator]
public class DefinitionGenerator : IIncrementalGenerator
{
    private readonly struct TargetOrError
    {
        public static TargetOrError FromTarget(ContextTarget target)
            => new(target ?? throw new ArgumentNullException(nameof(target)), default);

        public static TargetOrError FromError(DiagnosticData error)
            => new(default, error ?? throw new ArgumentNullException(nameof(error)));

        public static TargetOrError FromError(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
            => FromError(new DiagnosticData(descriptor, location, messageArgs));

        public ContextTarget? Target { get; }

        public DiagnosticData? Error { get; }

        [MemberNotNullWhen(true, nameof(Error))]
        [MemberNotNullWhen(false, nameof(Target))]
        public bool IsError => Error is not null;

        private TargetOrError(ContextTarget? target, DiagnosticData? error)
        {
            Target = target;
            Error = error;
        }
    }

    private const string Attributes = @"#nullable enable
namespace NCoreUtils.Data.Build
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DataEntityAttribute : System.Attribute
    {
        public System.Type EntityType { get; }

        public System.Type? NameFactory { get; set; }

        public bool HasNoKey { get; set; }

        public DataEntityAttribute(System.Type entityType)
        {
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DataDefinitionGenerationOptionsAttribute : System.Attribute
    {
        public System.Type? DefaultNameFactory { get; set; }

        public bool GenerateFirestoreDecorators { get; set; }
    }
}
";

    private static UTF8Encoding Utf8 { get; } = new(false);

    private TargetOrError FetchContextTarget(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ctx.SemanticModel.Compilation.HasLanguageVersionAtLeastEqualTo(LanguageVersion.CSharp10, out var currentVersion))
        {
            return TargetOrError.FromError(DiagnosticDescriptors.IncompatibleLanguageVersion, ctx.TargetNode.GetLocation(), currentVersion);
        }
        if (ctx.TargetSymbol is not INamedTypeSymbol namedContextTypeSymbol)
        {
            return TargetOrError.FromError(DiagnosticDescriptors.InvalidContextSymbol, ctx.TargetNode.GetLocation());
        }
        if (ctx.TargetNode is not TypeDeclarationSyntax typeDeclarationSyntax)
        {
            return TargetOrError.FromError(DiagnosticDescriptors.InvalidContextNode, ctx.TargetNode.GetLocation());
        }
        try
        {
            var entities = ctx.Attributes
                .Where(a => a.AttributeClass?.Name == "DataEntityAttribute")
                .Select(a =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (a.ConstructorArguments[0].Value is not INamedTypeSymbol namedEntityTypeSymbol)
                    {
                        throw new GenerationException(new DiagnosticData(DiagnosticDescriptors.InvalidEntitySymbol, ctx.TargetNode.GetLocation()));
                    }
                    return new EntityTarget(
                        namedEntityTypeSymbol,
                        a.NamedArguments.TryGetFirst("NameFactory", out var nameFactory)
                            ? nameFactory.Value as INamedTypeSymbol
                            : default,
                        a.NamedArguments.TryGetFirst("HasNoKey", out var hasNoKey) && (bool)hasNoKey.Value!
                    );
                });
            var optionsAttribute = ctx.TargetSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "DataDefinitionGenerationOptionsAttribute");
            INamedTypeSymbol? defaultNameFactory = default;
            bool generateFirestoreDecorators = false;
            if (optionsAttribute is not null)
            {
                defaultNameFactory = optionsAttribute.NamedArguments.TryGetFirst("DefaultNameFactory", out var dnameFactory)
                    ? dnameFactory.Value as INamedTypeSymbol
                    : default;
                generateFirestoreDecorators = optionsAttribute.NamedArguments.TryGetFirst("GenerateFirestoreDecorators", out var gfd) && (bool)gfd.Value!;
            }
            cancellationToken.ThrowIfCancellationRequested();
            return TargetOrError.FromTarget(new ContextTarget(
                semanticModel: ctx.SemanticModel,
                syntax: typeDeclarationSyntax,
                contextType: namedContextTypeSymbol,
                entities: entities.ToImmutableArray(),
                defaultNameFactory: defaultNameFactory,
                generateFirestoreDecorators: generateFirestoreDecorators
            ));
        }
        catch (GenerationException exn)
        {
            return TargetOrError.FromError(exn.Error);
        }
        catch (Exception exn) when (exn is not OperationCanceledException)
        {
            return TargetOrError.FromError(DiagnosticDescriptors.UnexpectedError, ctx.TargetNode.GetLocation(), exn.GetType(), exn.Message, exn.StackTrace);
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context => context.AddSource("DataBuilderAttributes.g.cs", SourceText.From(Attributes, Utf8)));

        IncrementalValuesProvider<TargetOrError> targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NCoreUtils.Data.Build.DataEntityAttribute",
            (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
            FetchContextTarget
        );

        context.RegisterSourceOutput(targets, (ctx, targetOrError) =>
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            if (targetOrError.IsError)
            {
                var (descriptor, location, messageArgs) = targetOrError.Error;
                ctx.ReportDiagnostic(Diagnostic.Create(
                    descriptor: descriptor,
                    location: location,
                    messageArgs: messageArgs
                ));
                return;
            }
            var target = targetOrError.Target;
            try
            {
                if (target.GenerateFirestoreDecorators)
                {
                    if (!target.SemanticModel.Compilation.ReferencedAssemblyNames.Any(e => e.Name == "NCoreUtils.Data.Google.Cloud.Firestore"))
                    {
                        throw new GenerationException(DiagnosticDescriptors.FirestoreReferenceMissing, target.Syntax.GetLocation());
                    }
                }
                var emitter = new DefinitionEmitter(new GenerationContext(target.SemanticModel));
                var unitSyntax = emitter.EmitCompilationUnit(target);
                ctx.AddSource($"{target.ContextType.Name}.g.cs", unitSyntax.GetText(Utf8));
            }
            catch (GenerationException exn)
            {
                var err = exn.Error;
                ctx.ReportDiagnostic(Diagnostic.Create(
                    descriptor: err.Descriptor,
                    location: err.Location ?? target.Syntax.GetLocation(),
                    messageArgs: err.MessageArgs
                ));
            }
            catch (Exception exn)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DiagnosticDescriptors.UnexpectedError,
                    location: default,
                    messageArgs: [exn.GetType().FullName, exn.Message, exn.StackTrace.Replace("\n", " ")]
                ));
            }
        });
    }
}