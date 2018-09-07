using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NCoreUtils.Data.IdNameGeneration
{
    public static class Annotations
    {
        public class IdNameSourcePropertyData
        {
            public string TypeName { get; set; }

            public string SourcePropertyName { get; set; }

            public string[] AdditionalPropertyNames { get; set; }
        }

        internal static Assembly _generatedAssembly = null;

        static Type ResolveType(string typeName)
        {
            return Type.GetType(typeName) ?? Type.GetType(
                typeName,
                name => _generatedAssembly != null && _generatedAssembly.GetName() == name ? _generatedAssembly : null,
                null);
        }

        public const string IdNameSourceProperty = nameof(IdNameSourceProperty);

        public const string GetIdNameFunction = nameof(GetIdNameFunction);

        public sealed class IdNameSourcePropertyAnnotation
        {
            public static IdNameSourcePropertyAnnotation Unpack(string raw)
            {
                try
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<IdNameSourcePropertyData>(raw);
                    var ty = ResolveType(data.TypeName);
                    var sourceNameProperty = ty.GetProperty(data.SourcePropertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (null == sourceNameProperty)
                    {
                        throw new IdNameGenerationAnnotationException($"Unresolvable property name {sourceNameProperty.Name} for type {ty.FullName} in annotation.");
                    }
                    var additionalProperties = data.AdditionalPropertyNames
                        .Select(name => {
                            var property = ty.GetProperty(data.SourcePropertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (null == property)
                            {
                                throw new IdNameGenerationAnnotationException($"Unresolvable property name {property.Name} for type {ty.FullName} in annotation.");
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
                return Newtonsoft.Json.JsonConvert.SerializeObject(new IdNameSourcePropertyData
                {
                    TypeName = SourceNameProperty.DeclaringType.AssemblyQualifiedName,
                    SourcePropertyName = SourceNameProperty.Name,
                    AdditionalPropertyNames = AdditionalIndexProperties.Select(e => e.Name).ToArray()
                });
            }
        }

        public sealed class GetIdNameFunctionAnnotation
        {
            static readonly Regex _regex = new Regex("^(.*)\\|(.*)\\|(.*)\\|(.*)$", RegexOptions.Compiled);

            public static GetIdNameFunctionAnnotation Unpack(string raw)
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
                    null);
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
                return $"{FunctionSchema}|{FunctionName}|{Method.DeclaringType.AssemblyQualifiedName}|{Method.Name}";
            }
        }
    }
}