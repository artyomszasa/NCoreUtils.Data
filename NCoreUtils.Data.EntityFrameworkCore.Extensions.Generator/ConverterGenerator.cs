using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NCoreUtils.Data;

[Generator(LanguageNames.CSharp)]
public class ConverterGenerator : IIncrementalGenerator
{
    private readonly struct TargetOrError
    {
        public ConverterTarget? Target { get; }

        public DiagnosticData? Error { get; }

        private TargetOrError(ConverterTarget? target, DiagnosticData? error)
        {
            if (target is null && error is null)
            {
                throw new InvalidOperationException("Either target or error must be not null.");
            }
            Target = target;
            Error = error;
        }

        public TargetOrError(ConverterTarget target) : this(target, default) { }

        public TargetOrError(DiagnosticData error) : this(default, error) { }
    }

    private const string AttributesSource = @"
namespace NCoreUtils.Data
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class AsJsonStringConverterAttribute : System.Attribute
    {
        public System.Type Target { get; }

        public System.Type SerializerContext { get; }

        public string DefaultValue { get; set; }

        public System.Type Comparer { get; set; }

        public AsJsonStringConverterAttribute(System.Type target, System.Type serializerContext)
        {
            Target = target;
            SerializerContext = serializerContext;
        }
    }
}";

    private static UTF8Encoding Utf8 { get; } = new(false);

    private static Regex RegexNewLine { get; } = new("\r*\n\r*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string? AsOneLine(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }
        return RegexNewLine.Replace(source, " ");
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context => context.AddSource("JsonConversionAttributes.g.cs", SourceText.From(AttributesSource, Utf8)));

        var targets = context.SyntaxProvider.ForAttributeWithMetadataName<TargetOrError>(
            "NCoreUtils.Data.AsJsonStringConverterAttribute",
            (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
            (ctx, cancellationToken) =>
            {
                if (!ctx.SemanticModel.Compilation.HasLanguageVersionAtLeastEqualTo(LanguageVersion.CSharp10))
                {
                    return new(new DiagnosticData(
                        DiagnosticDescriptors.IncompatibleLanguageVersion,
                        ctx.TargetNode.GetLocation(),
                        new object[] { ((CSharpCompilation)ctx.SemanticModel.Compilation).LanguageVersion }
                    ));
                }
                if (!ctx.SemanticModel.Compilation.ReferencedAssemblyNames.Any(a => a.Name == "Microsoft.EntityFrameworkCore.Relational"))
                {
                    return new(new DiagnosticData(
                        DiagnosticDescriptors.EFRelationalReferenceRequired,
                        ctx.TargetNode.GetLocation(),
                        Array.Empty<object>()
                    ));
                }
                try
                {
                    if (ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol)
                    {
                        return default;
                    }
                    if (ctx.Attributes.Length == 0)
                    {
                        return default;
                    }
                    var attr = ctx.Attributes[0];
                    var target = attr.ConstructorArguments[0].Value as ITypeSymbol
                        ?? throw new BuilderGenerationException(new DiagnosticData(
                            DiagnosticDescriptors.InvalidTargetType,
                            ctx.TargetNode.GetLocation(),
                            Array.Empty<object>()
                        ));
                    var serializerContext = attr.ConstructorArguments[1].Value as ITypeSymbol
                        ?? throw new BuilderGenerationException(new DiagnosticData(
                            DiagnosticDescriptors.InvalidSerializerContextType,
                            ctx.TargetNode.GetLocation(),
                            Array.Empty<object>()
                        ));
                    string? defaultValue = default;
                    if (attr.NamedArguments.TryGetFirst(a => a.Key == "DefaultValue", out var arg))
                    {
                        if (arg.Value.Kind != TypedConstantKind.Primitive)
                        {
                            throw new InvalidOperationException("Default value is " + arg.Value.Kind.ToString());
                        }
                        if (arg.Value.Value is not string s)
                        {
                            throw new InvalidOperationException("Default value is " + (arg.Value.Value?.GetType().FullName ?? "null"));
                        }
                        defaultValue = s;
                    }
                    SymbolName comparer = default;
                    if (attr.NamedArguments.TryGetFirst(a => a.Key == "Comparer", out arg))
                    {
                        if (arg.Value.Value is not ITypeSymbol comparerType)
                        {
                            throw new InvalidOperationException("Comparer is " + (arg.Value.Value?.GetType().FullName ?? "null"));
                        }
                        comparer = new(comparerType);
                    }
                    return new TargetOrError(new ConverterTarget(
                        (TypeDeclarationSyntax)ctx.TargetNode,
                        new(namedTypeSymbol),
                        new(target),
                        new(serializerContext),
                        defaultValue,
                        comparer
                    ));
                }
                catch (Exception exn)
                {
                    if (exn is BuilderGenerationException bexn)
                    {
                        return new TargetOrError(bexn.DiagnosticData);
                    }
                    return new TargetOrError(new DiagnosticData(
                        DiagnosticDescriptors.UnexpectedError,
                        ctx.TargetNode.GetLocation(),
                        new object[] { exn.GetType().FullName, exn.Message, AsOneLine(exn.StackTrace) ?? string.Empty }
                    ));
                }
            }
        );

        var needsExtensions = targets.Collect().Select((targets, _) => targets.Any(e => e.Target is ConverterTarget target && !target.Comparer.HasValue && target.IsArrayLike));

        context.RegisterSourceOutput(needsExtensions, (ctx, shouldEmit) =>
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            if (shouldEmit)
            {
                try
                {
                    var unit = HelpersEmitter.EmitCompilationUnit();
                    ctx.AddSource("ValueComparisonHelpers.g.cs", unit.GetText(Utf8));
                }
                catch (BuilderGenerationException exn)
                {
                    var err = exn.DiagnosticData;
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        descriptor: err.Descriptor,
                        location: err.Location,
                        messageArgs: err.MessageArgs
                    ));
                }
                catch (Exception exn)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DiagnosticDescriptors.UnexpectedError,
                        location: default,
                        messageArgs: new object[] { exn.GetType().FullName, exn.Message, AsOneLine(exn.StackTrace) ?? string.Empty }
                    ));
                }
            }
        });

        context.RegisterSourceOutput(targets, (ctx, targetOrError) =>
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            if (targetOrError.Target is null)
            {
                var error = targetOrError.Error;
                ctx.ReportDiagnostic(Diagnostic.Create(
                    error?.Descriptor ?? DiagnosticDescriptors.UnexpectedError,
                    error?.Location,
                    error?.MessageArgs ?? new object[] { string.Empty, string.Empty, string.Empty }
                ));
                return;
            }
            var target = targetOrError.Target;
            try
            {
                var syntax = ConverterEmitter.EmitCompilationUnit(target);
                ctx.AddSource($"{target.Host.Name}.g.cs", syntax.GetText(Utf8));
            }
            catch (BuilderGenerationException exn)
            {
                var err = exn.DiagnosticData;
                ctx.ReportDiagnostic(Diagnostic.Create(
                    descriptor: err.Descriptor,
                    location: err.Location ?? target.Node.GetLocation(),
                    messageArgs: err.MessageArgs
                ));
            }
            catch (Exception exn)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DiagnosticDescriptors.UnexpectedError,
                    location: default,
                    messageArgs: new object[] { exn.GetType().FullName, exn.Message, AsOneLine(exn.StackTrace) ?? string.Empty }
                ));
            }
        });
    }
}