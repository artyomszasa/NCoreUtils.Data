using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NCoreUtils.Data.Google.FireStore
{
    public class Model
    {
        readonly TypeDescriptor[] _typeDescriptors;

        readonly ImmutableDictionary<Type, int> _indices;

        public Model(IEnumerable<TypeDescriptor> typeDescriptors)
        {
            _typeDescriptors = typeDescriptors.ToArray();
            var builder = ImmutableDictionary.CreateBuilder<Type, int>();
            for (var i = 0; i < _typeDescriptors.Length; ++i)
            {
                builder.Add(_typeDescriptors[i].Type, i);
            }
            _indices = builder.ToImmutable();
        }

        public ref readonly TypeDescriptor GetTypeDescriptor(Type type)
        {
            if (_indices.TryGetValue(type, out var index))
            {
                return ref _typeDescriptors[index];
            }
            throw new InvalidOperationException($"Type {type} is not a registered object type.");
        }
    }
}