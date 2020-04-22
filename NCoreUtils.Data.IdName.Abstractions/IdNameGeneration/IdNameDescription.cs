using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class IdNameDescription
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ImmutableArray<PropertyInfo> ToArray(IEnumerable<PropertyInfo> source)
            => source == null ? ImmutableArray<PropertyInfo>.Empty : source.ToImmutableArray();

        public PropertyInfo IdNameProperty { get; }

        public PropertyInfo NameSourceProperty { get; }

        public IStringDecomposer Decomposer { get; }

        public ImmutableArray<PropertyInfo> AdditionalIndexProperties { get; }

        public IdNameDescription(
            PropertyInfo idNameProperty,
            PropertyInfo nameSourceProperty,
            IStringDecomposer decomposer,
            ImmutableArray<PropertyInfo> additionalIndexProperties)
        {
            IdNameProperty = idNameProperty ?? throw new ArgumentNullException(nameof(idNameProperty));
            NameSourceProperty = nameSourceProperty ?? throw new ArgumentNullException(nameof(nameSourceProperty));
            Decomposer = decomposer ?? throw new ArgumentNullException(nameof(decomposer));
            AdditionalIndexProperties = additionalIndexProperties.IsDefault ? ImmutableArray<PropertyInfo>.Empty : additionalIndexProperties;
        }

        public IdNameDescription(
            PropertyInfo idNameProperty,
            PropertyInfo nameSourceProperty,
            IStringDecomposer decomposer,
            IEnumerable<PropertyInfo> additionalIndexProperties)
            : this(idNameProperty, nameSourceProperty, decomposer, ToArray(additionalIndexProperties))
        { }

        public IdNameDescription(
            PropertyInfo idNameProperty,
            PropertyInfo nameSourceProperty,
            IStringDecomposer decomposer,
            PropertyInfo[] additionalIndexProperties)
            : this(idNameProperty, nameSourceProperty, decomposer, (IEnumerable<PropertyInfo>)additionalIndexProperties)
        { }
    }
}