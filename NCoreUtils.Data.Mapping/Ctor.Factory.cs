using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace NCoreUtils.Data
{
    public partial class Ctor
    {
        private sealed class ParameterPredicate
        {
            public PropertyInfo Property { get; }

            public ParameterPredicate(PropertyInfo property)
            {
                Property = property;
            }

            public bool Invoke(ParameterInfo parameter)
            {
                return Eqi(parameter.Name, Property.Name)
                    && Property.PropertyType.IsAssignableFrom(parameter.ParameterType);
            }
        }

        private static bool Eqi(string? a, string? b)
            => StringComparer.InvariantCultureIgnoreCase.Equals(a, b);

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Ctor<>))]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic dependency preserves types.")]
        [UnconditionalSuppressMessage("Trimming", "IL2111")]
        public static IEnumerable<Ctor> GetSuitableCtors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
        {
            var results = new List<Ctor>(4);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            var ctorCtor = typeof(Ctor<>).MakeGenericType(type).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
            foreach (var candidate in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
            {
                var parameters = candidate.GetParameters();
                var mappings = new List<PropertyMapping>(properties.Length);
                var skip = false;
                foreach (var property in properties)
                {
                    if (parameters.TryGetFirst(
                        new ParameterPredicate(property).Invoke,
                        out var matchedParameter))
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
                    results.Add((Ctor)ctorCtor.Invoke(new object[]
                    {
                        candidate,
                        mappings
                    }));
                }
            }
            return results;
        }

        public static Ctor GetCtor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
        {
            var candidates = GetSuitableCtors(type).ToList();
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException($"No suitable ctor can be created for {type}.");
            }
            return candidates.MaxBy(ctor => ctor.Properties.Count(m => m.IsByCtorParameter));
        }
    }
}