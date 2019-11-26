using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.Google.FireStore.Builders
{
    public sealed class ModelBuilder
    {
        public NamingConvention NamingConvention { get; set; } = NamingConvention.CamelCase;

        public List<TypeDescriptorBuilder> Types { get; set; } = new List<TypeDescriptorBuilder>();

        public ModelBuilder UseNamingConvention(NamingConvention namingConvention)
        {
            NamingConvention = namingConvention ?? throw new ArgumentNullException(nameof(namingConvention));
            return this;
        }

        TypeDescriptorBuilder<T> Object<T>(ConstructorInfo ctor, bool isRoot)
        {
            if (!Types.TryGetFirst(e => e.Type.Equals(typeof(T)), out var builder))
            {
                builder = new TypeDescriptorBuilder<T>(this, ctor);
                Types.Add(builder);
            }
            builder.IsRoot = isRoot;
            return (TypeDescriptorBuilder<T>)builder;
        }

        TypeDescriptorBuilder<T> Object<T>(Expression<Func<T>> expression, bool isRoot)
        {
            if (expression is NewExpression newExpression)
            {
                var ctor = newExpression.Constructor;
                return Object<T>(ctor, isRoot);
            }
            throw new InvalidOperationException($"Expression must be a constructor expression, {expression} given.");
        }

        TypeDescriptorBuilder<T> Object<T>(bool isRoot)
        {
            var ctors = typeof(T).GetConstructors(typeof(T).IsAbstract
                ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                : BindingFlags.Instance | BindingFlags.Public
            );
            switch (ctors.Length)
            {
                case 0:
                    throw new InvalidOperationException($"No suitable constructor defined for {typeof(T)}.");
                case 1:
                    return Object<T>(ctors[0], isRoot);
                case 2:
                    if (ctors[0].GetParameters().Length == 0)
                    {
                        return Object<T>(ctors[1], isRoot);
                    }
                    if (ctors[1].GetParameters().Length == 0)
                    {
                        return Object<T>(ctors[0], isRoot);
                    }
                    throw new InvalidOperationException($"Ambigous constructor for {typeof(T)}, use expression based definition instead.");
                default:
                    throw new InvalidOperationException($"Ambigous constructor for {typeof(T)}, use expression based definition instead.");
            }
        }

        public TypeDescriptorBuilder<T> Root<T>(ConstructorInfo ctor)
            where T : IHasId<string>
            => Object<T>(ctor, true);

        public TypeDescriptorBuilder<T> Root<T>(Expression<Func<T>> expression)
            where T : IHasId<string>
            => Object(expression, true);

        public TypeDescriptorBuilder<T> Root<T>()
            where T : IHasId<string>
            => Object<T>(true);

        public TypeDescriptorBuilder<T> Owned<T>(ConstructorInfo ctor)
            => Object<T>(ctor, false);

        public TypeDescriptorBuilder<T> Owned<T>(Expression<Func<T>> expression)
            => Object(expression, false);

        public TypeDescriptorBuilder<T> Owned<T>()
            => Object<T>(false);

        public Model Build()
            => new Model(Types.Select(b => b.Build()));
    }
}