using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data;

public partial class Ctor
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Eqi(string? a, string? b)
        => StringComparer.InvariantCultureIgnoreCase.Equals(a, b);

    private static bool TryGetFirst(ParameterInfo[] parameters, PropertyInfo property, [MaybeNullWhen(false)] out ParameterInfo matchedParameter)
    {
        foreach (var parameter in parameters)
        {
            if (Eqi(parameter.Name, property.Name)
                && property.PropertyType.IsAssignableFrom(parameter.ParameterType))
            {
                matchedParameter = parameter;
                return true;
            }
        }
        matchedParameter = default;
        return false;
    }

    public static List<Ctor> GetSuitableCtors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var results = new List<Ctor>(4);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        foreach (var candidate in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            var parameters = candidate.GetParameters();
            var mappings = new List<PropertyMapping>(properties.Length);
            var skip = false;
            foreach (var property in properties)
            {
                if (TryGetFirst(parameters, property, out var matchedParameter))
                {
                    mappings.Add(PropertyMapping.ByCtorParameter(property, matchedParameter));
                }
                else if (property.CanWrite)
                {
                    mappings.Add(PropertyMapping.BySetter(property));
                }
                else
                {
                    skip = true;
                    break;
                }
            }
            mappings.Sort();
            if (!skip)
            {
                results.Add(new Ctor(candidate, mappings));
            }
        }
        return results;
    }

    public static Ctor GetCtor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var enumerator = GetSuitableCtors(type).GetEnumerator();
        if (!enumerator.MoveNext())
        {
            throw new InvalidOperationException($"No suitable ctor can be created for {type}.");
        }
        Ctor selected = enumerator.Current;
        int selectedKey = selected.Properties.Count(IsByCtorParameter);
        while (enumerator.MoveNext())
        {
            var next = enumerator.Current;
            var nextKey = next.Properties.Count(IsByCtorParameter);
            if (nextKey > selectedKey)
            {
                selected = next;
                selectedKey = nextKey;
            }
        }
        return selected;

        static bool IsByCtorParameter(PropertyMapping mapping) => mapping.IsByCtorParameter;
    }
}