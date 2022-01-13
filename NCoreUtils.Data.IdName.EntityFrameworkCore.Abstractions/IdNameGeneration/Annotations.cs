using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NCoreUtils.Data.IdNameGeneration
{
    public static class Annotations
    {
        internal static Assembly? _generatedAssembly = null;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only invoked internally.")]
        private static Type ResolveType(string typeName)
        {
            return Type.GetType(typeName) ?? Type.GetType(
                typeName,
                name => _generatedAssembly != null && _generatedAssembly.GetName() == name ? _generatedAssembly : null,
                null)
                ?? throw new InvalidOperationException($"Unable to get type for \"{typeName}\".");
        }

        public const string IdNameSourceProperty = nameof(IdNameSourceProperty);

        public const string GetIdNameFunction = nameof(GetIdNameFunction);

        public sealed class IdNameSourcePropertyAnnotation
        {
            public static IdNameSourcePropertyAnnotation Unpack(string? raw)
            {
                try
                {
                    if (raw is null)
                    {
                        throw new InvalidOperationException("Raw annotation is null.");
                    }
                    var data = JsonSerializer.Deserialize(raw, IdNameGenerationAnnotationSerializationContext.Default.IdNameSourcePropertyData);
                    if (data is null)
                    {
                        throw new InvalidOperationException("Unable to deserialize raw annotation.");
                    }
                    var ty = ResolveType(data.TypeName);
                    var sourceNameProperty = ty.GetProperty(data.SourcePropertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (null == sourceNameProperty)
                    {
                        throw new IdNameGenerationAnnotationException($"Unresolvable property name {data.SourcePropertyName} for type {ty.FullName} in annotation.");
                    }
                    var additionalProperties = data.AdditionalPropertyNames
                        .Select(name => {
                            var property = ty.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (null == property)
                            {
                                throw new IdNameGenerationAnnotationException($"Unresolvable property name {name} for type {ty.FullName} in annotation.");
                            }
                            return property;
                        });


                    return new IdNameSourcePropertyAnnotation(sourceNameProperty, additionalProperties.ToImmutableArray());
                }
                catch (IdNameGenerationAnnotationException)
                {
                    throw;
                }
                catch (Exception exn)
                {
                    throw new IdNameGenerationAnnotationException("Invalid annotation.", exn);
                }
            }

            public PropertyInfo SourceNameProperty { get; }

            public ImmutableArray<PropertyInfo> AdditionalIndexProperties { get; }

            public IdNameSourcePropertyAnnotation(PropertyInfo sourceNameProperty, ImmutableArray<PropertyInfo> additionalIndexProperties)
            {
                SourceNameProperty = sourceNameProperty ?? throw new ArgumentNullException(nameof(sourceNameProperty));
                AdditionalIndexProperties = additionalIndexProperties;
            }

            public string Pack()
            {
                var annotation = new IdNameSourcePropertyData(
                    typeName: SourceNameProperty.DeclaringType!.AssemblyQualifiedName!,
                    sourcePropertyName: SourceNameProperty.Name,
                    additionalPropertyNames: AdditionalIndexProperties.Select(e => e.Name).ToArray()
                );
                return JsonSerializer.Serialize(annotation, IdNameGenerationAnnotationSerializationContext.Default.IdNameSourcePropertyData);
            }
        }

        public sealed class GetIdNameFunctionAnnotation
        {
            static readonly Regex _regex = new("^(.*)\\|(.*)\\|(.*)\\|(.*)$", RegexOptions.Compiled);

            [RequiresUnreferencedCode("TypeName of the annotation must be explicitly preserved.")]
            public static GetIdNameFunctionAnnotation Unpack(string? raw)
            {
                if (string.IsNullOrEmpty(raw))
                {
                    throw new IdNameGenerationAnnotationException("Annotation is empty.");
                }
                var m = _regex.Match(raw);
                if (!m.Success)
                {
                    throw new IdNameGenerationAnnotationException("Annotation has invalid format.");
                }
                var functionSchema = m.Groups[1].Value;
                var functionName = m.Groups[2].Value;

                var typeName = m.Groups[3].Value;
                var ty = Type.GetType(typeName) ?? Type.GetType(
                    typeName,
                    name => _generatedAssembly != null && _generatedAssembly.GetName().FullName == name.FullName ? _generatedAssembly : null,
                    null)
                    ?? throw new InvalidOperationException($"Unable to get type for \"{typeName}\"");
                if (null == ty)
                {
                    throw new IdNameGenerationAnnotationException($"Unresolvable type {m.Groups[3].Value} in annotation.");
                }
                var method = ty.GetMethod(m.Groups[4].Value, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (null == method)
                {
                    throw new IdNameGenerationAnnotationException($"Unresolvable property name {m.Groups[4].Value} for type {m.Groups[3].Value} in annotation.");
                }
                return new GetIdNameFunctionAnnotation(method, functionSchema, functionName);
            }

            public MethodInfo Method { get; }

            public string FunctionSchema { get; }

            public string FunctionName { get; }

            public GetIdNameFunctionAnnotation(MethodInfo method, string functionSchema, string functionName)
            {
                Method = method ?? throw new ArgumentNullException(nameof(method));
                FunctionSchema = functionSchema;
                FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
            }

            public string Pack()
            {
                return $"{FunctionSchema}|{FunctionName}|{Method.DeclaringType!.AssemblyQualifiedName}|{Method.Name}";
            }
        }
    }
}