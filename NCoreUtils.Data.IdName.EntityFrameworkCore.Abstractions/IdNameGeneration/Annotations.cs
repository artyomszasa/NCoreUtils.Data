using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private class IdNameSourcePropertyDataConverter : JsonConverter<IdNameSourcePropertyData>
        {
            private static readonly byte[] _binType = Encoding.ASCII.GetBytes("type");

            private static readonly byte[] _binSource = Encoding.ASCII.GetBytes("source");

            private static readonly byte[] _binOther = Encoding.ASCII.GetBytes("other");

            private static readonly JsonEncodedText _jsonType = JsonEncodedText.Encode("type");

            private static readonly JsonEncodedText _jsonSource = JsonEncodedText.Encode("source");

            private static readonly JsonEncodedText _jsonOther = JsonEncodedText.Encode("other");

            public override IdNameSourcePropertyData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}.");
                }
                reader.Read();
                string? typeName = default;
                string? sourcePropertyName = default;
                var additionalPropertyNames = new List<string>();
                while (reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new InvalidOperationException($"Expected {JsonTokenType.PropertyName}, got {reader.TokenType}.");
                    }
                    if (reader.ValueSpan == _binType)
                    {
                        reader.Read();
                        typeName = reader.GetString();
                        reader.Read();
                    }
                    else if (reader.ValueSpan == _binSource)
                    {
                        reader.Read();
                        sourcePropertyName = reader.GetString();
                        reader.Read();
                    }
                    else if (reader.ValueSpan == _binOther)
                    {
                        reader.Read();
                        if (reader.TokenType != JsonTokenType.StartArray)
                        {
                            throw new InvalidOperationException($"Expected {JsonTokenType.StartArray}, got {reader.TokenType}.");
                        }
                        reader.Read();
                        while (reader.TokenType != JsonTokenType.EndArray)
                        {
                            additionalPropertyNames.Add(reader.GetString());
                            reader.Read();
                        }
                        reader.Read();
                    }
                }
                reader.Read();
                return new IdNameSourcePropertyData(typeName!, sourcePropertyName!, additionalPropertyNames.ToArray());
            }

            public override void Write(Utf8JsonWriter writer, IdNameSourcePropertyData value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString(_jsonType, value.TypeName);
                writer.WriteString(_jsonSource, value.SourcePropertyName);
                writer.WritePropertyName(_jsonOther);
                writer.WriteStartArray();
                foreach (var other in value.AdditionalPropertyNames)
                {
                    writer.WriteStringValue(other);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }

        [JsonConverter(typeof(IdNameSourcePropertyDataConverter))]
        public class IdNameSourcePropertyData
        {
            public string TypeName { get; }

            public string SourcePropertyName { get; }

            public string[] AdditionalPropertyNames { get; }

            public IdNameSourcePropertyData(string typeName, string sourcePropertyName, string[] additionalPropertyNames)
            {
                TypeName = typeName;
                SourcePropertyName = sourcePropertyName;
                AdditionalPropertyNames = additionalPropertyNames;
            }
        }

        internal static Assembly? _generatedAssembly = null;

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
            public static IdNameSourcePropertyAnnotation Unpack(string? raw)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<IdNameSourcePropertyData>(raw);
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
                return JsonSerializer.Serialize(new IdNameSourcePropertyData(
                    typeName: SourceNameProperty.DeclaringType.AssemblyQualifiedName,
                    sourcePropertyName: SourceNameProperty.Name,
                    additionalPropertyNames: AdditionalIndexProperties.Select(e => e.Name).ToArray()
                ));
            }
        }

        public sealed class GetIdNameFunctionAnnotation
        {
            static readonly Regex _regex = new Regex("^(.*)\\|(.*)\\|(.*)\\|(.*)$", RegexOptions.Compiled);

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