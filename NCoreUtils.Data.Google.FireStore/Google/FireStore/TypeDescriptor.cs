using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using NCoreUtils.Data.Google.FireStore.Collections;

namespace NCoreUtils.Data.Google.FireStore
{
    public struct TypeDescriptor
    {
        public ConstructorInfo Ctor { get; }

        public Type Type => Ctor.DeclaringType;

        public bool IsPolymorphic => Dervied.Length > 0;

        public ImmutableArray<Type> Dervied { get; }

        public string Name { get; }

        public PropertyDescriptor? IdProperty { get; }

        public BindingArray<PropertyDescriptor> Properties { get; }

        public ImmutableDictionary<PropertyInfo, PropertyDescriptor> PropertyMap { get; }

        public TypeDescriptor(ConstructorInfo ctor, string name, IEnumerable<Type> derived, BindingArray<PropertyDescriptor> properties, PropertyDescriptor? idProperty)
        {
            Ctor = ctor;
            Name = name;
            Dervied = derived is null ? ImmutableArray<Type>.Empty : derived.ToImmutableArray();
            var propertyMapBuilder = ImmutableDictionary.CreateBuilder<PropertyInfo, PropertyDescriptor>();
            foreach (var p in properties)
            {
                propertyMapBuilder.Add(p.Property, p);
            }
            Properties = properties;
            PropertyMap = propertyMapBuilder.ToImmutable();
            IdProperty = idProperty;
        }

        public override string ToString()
            => $"[Name = {Name}, Type = {Type}]";
    }
}