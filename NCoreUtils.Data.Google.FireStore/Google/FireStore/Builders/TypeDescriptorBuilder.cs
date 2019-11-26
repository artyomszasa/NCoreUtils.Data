using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NCoreUtils.Data.Google.FireStore.Collections;

namespace NCoreUtils.Data.Google.FireStore.Builders
{
    public abstract class TypeDescriptorBuilder
    {
        protected static MethodInfo _gmInvokeProperty;

        static TypeDescriptorBuilder()
        {
            _gmInvokeProperty = typeof(TypeDescriptorBuilder)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .First(m => m.IsGenericMethodDefinition && m.Name == nameof(InvokeProperty));
        }

        static PropertyDescriptorBuilder<TProp> InvokeProperty<T, TProp>(TypeDescriptorBuilder<T> builder, PropertyInfo property)
        {
            var arg = Expression.Parameter(typeof(T));
            return builder.Property(Expression.Lambda<Func<T, TProp>>(
                Expression.Property(arg, property),
                arg
            ));
        }

        public ModelBuilder Model { get; }

        public ConstructorInfo Ctor { get; set; }

        public Type Type => Ctor.DeclaringType;

        public string Name { get; set; }

        public PropertyDescriptorBuilder IdProperty { get; set; }

        public List<PropertyDescriptorBuilder> Properties { get; } = new List<PropertyDescriptorBuilder>();

        public HashSet<Type> Derived { get; } = new HashSet<Type>();

        public bool IsRoot { get; set; }

        internal TypeDescriptorBuilder(ModelBuilder model, ConstructorInfo ctor)
        {
            Model = model;
            Ctor = ctor ?? throw new ArgumentNullException(nameof(ctor));
        }

        public abstract TypeDescriptor Build();

    }

    public sealed class TypeDescriptorBuilder<T> : TypeDescriptorBuilder
    {
        public TypeDescriptorBuilder(ModelBuilder modelBuilder, ConstructorInfo ctor)
            : base(modelBuilder, ctor)
        {
            if (!typeof(T).IsAssignableFrom(ctor.DeclaringType))
            {
                throw new InvalidOperationException($"Invalid ctor.");
            }
        }

        public TypeDescriptorBuilder<T> UseStorageName(string name)
        {
            Name = name;
            return this;
        }

        public PropertyDescriptorBuilder<TProp> Property<TProp>(Expression<Func<T, TProp>> selector)
        {
            if (selector.TryExtractProperty(out var property))
            {
                if (!Properties.TryGetFirst(e => e.Property.IsSameOrOverridden(property), out var builder))
                {
                    builder = new PropertyDescriptorBuilder<TProp>(this, property);
                    Properties.Add(builder);
                }
                return (PropertyDescriptorBuilder<TProp>)builder;
            }
            throw new InvalidOperationException($"Selector must be a property expression, {selector} given.");
        }

        public TypeDescriptorBuilder<T> HasDerivate<TDerived>()
            where TDerived : T
        {
            Derived.Add(typeof(TDerived));
            return this;
        }

        public TypeDescriptorBuilder<T> HasId<TProp>(Expression<Func<T, TProp>> selector)
        {
            IdProperty = Property(selector);
            return this;
        }

        public sealed override TypeDescriptor Build()
        {
            if (IsRoot)
            {
                if (!typeof(IHasId<string>).IsAssignableFrom(typeof(T)))
                {
                    throw new InvalidOperationException($"Root object {typeof(T)} does not implement {typeof(IHasId<string>)}.");
                }
                var map = typeof(T).GetInterfaceMap(typeof(IHasId<string>));
                var getId = map.TargetMethods[0];
                var prop = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(p => p.GetMethod.Equals(getId));
                if (prop is null)
                {
                    throw new InvalidOperationException($"Unable to find property on {typeof(T)} that implements {typeof(IHasId<string>)}.{nameof(IHasId<string>.Id)}.");
                }
                switch (prop.GetCustomAttribute<TargetPropertyAttribute>())
                {
                    case null:
                        break;
                    case TargetPropertyAttribute attr:
                        var targetProp = typeof(T).GetProperty(attr.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (targetProp is null)
                        {
                            throw new InvalidOperationException($"Unable to find target property on {typeof(T)} for {typeof(IHasId<string>)}.{nameof(IHasId<string>.Id)}.");
                        }
                        prop = targetProp;
                        break;
                }
                IdProperty = (PropertyDescriptorBuilder)_gmInvokeProperty.MakeGenericMethod(typeof(T), prop.PropertyType).Invoke(null, new object[] { this, prop });
            }
            // Update property binding information
            var ctorParameters = Ctor.GetParameters();
            var parameterBindings = new List<PropertyDescriptor>(Properties.Count);
            var propertyBindings = new List<PropertyDescriptor>(Properties.Count);
            foreach (var property in Properties)
            {
                var par = ctorParameters.FirstOrDefault(par => StringComparer.OrdinalIgnoreCase.Equals(property.Property.Name, par.Name));
                if (par is null)
                {
                    propertyBindings.Add(property.Build());
                }
                else
                {
                    property.ParameterIndex = par.Position;
                    parameterBindings.Add(property.Build());
                }
            }
            parameterBindings.Sort((a, b) => Comparer<int>.Default.Compare(a.ParameterIndex, b.ParameterIndex));
            var bindings = new BindingArray<PropertyDescriptor>(parameterBindings.Concat(propertyBindings).ToImmutableArray(), parameterBindings.Count);
            // Create descriptor
            return new TypeDescriptor(
                Ctor,
                Name ?? Model.NamingConvention.Consolidate(Type.Name),
                Derived,
                bindings,
                IdProperty?.Build()
            );
        }
    }
}