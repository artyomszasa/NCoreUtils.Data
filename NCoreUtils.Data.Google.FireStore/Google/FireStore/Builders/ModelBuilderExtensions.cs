using System;
using System.Linq.Expressions;

namespace NCoreUtils.Data.Google.FireStore.Builders
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder Root<T>(this ModelBuilder builder, Action<TypeDescriptorBuilder<T>> configure)
            where T : IHasId<string>
        {
            configure(builder.Root<T>());
            return builder;
        }

        public static ModelBuilder Root<T>(this ModelBuilder builder, Expression<Func<T>> expression, Action<TypeDescriptorBuilder<T>> configure)
            where T : IHasId<string>
        {
            configure(builder.Root(expression));
            return builder;
        }

        public static ModelBuilder Owned<T>(this ModelBuilder builder, Action<TypeDescriptorBuilder<T>> configure)
        {
            configure(builder.Owned<T>());
            return builder;
        }

        public static ModelBuilder Owned<T>(this ModelBuilder builder, Expression<Func<T>> expression, Action<TypeDescriptorBuilder<T>> configure)
        {
            configure(builder.Owned(expression));
            return builder;
        }
    }
}