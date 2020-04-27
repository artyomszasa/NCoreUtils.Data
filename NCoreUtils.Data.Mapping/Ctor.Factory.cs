using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NCoreUtils.Data
{
    public partial class Ctor
    {
        private static bool Eqi(string? a, string? b)
            => StringComparer.InvariantCultureIgnoreCase.Equals(a, b);

        public static IEnumerable<Ctor> GetSuitableCtors(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            foreach (var candidate in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
            {
                var parameters = candidate.GetParameters();
                var mappings = new List<PropertyMapping>(properties.Length);
                var skip = false;
                foreach (var property in properties)
                {
                    if (parameters.TryGetFirst(
                        parameter
                            => Eqi(parameter.Name, property.Name)
                            && property.PropertyType.IsAssignableFrom(parameter.ParameterType),
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
                    yield return (Ctor)Activator.CreateInstance(typeof(Ctor<>).MakeGenericType(type), new object[]
                    {
                        candidate,
                        mappings
                    });
                }
            }
        }

        public static Ctor GetCtor(Type type)
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