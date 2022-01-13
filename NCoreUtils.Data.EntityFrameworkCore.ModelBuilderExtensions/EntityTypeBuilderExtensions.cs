using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NCoreUtils.Data
{
    public static class EntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> HasId<TEntity, TId>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : class, IHasId<TId>
        {
            var idSelector = LinqExtensions.ReplaceExplicitProperties<Func<TEntity, object?>>(e => e.Id!);
            builder.HasKey(idSelector!);
            return builder;
        }

        public static EntityTypeBuilder<TEntity> HasTimeTracking<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : class, IHasTimeTracking
        {
            var createdSelector = LinqExtensions.ReplaceExplicitProperties<Func<TEntity, object?>>(e => e.Created);
            var updatedSelector = LinqExtensions.ReplaceExplicitProperties<Func<TEntity, object?>>(e => e.Updated);
            builder.HasIndex(createdSelector!);
            builder.HasIndex(updatedSelector!);
            return builder;
        }

        public static PropertyBuilder<string> StringProperty<TEntity>(
            this EntityTypeBuilder<TEntity> builder,
            Expression<Func<TEntity, string>> selector,
            int? maxLength = null,
            bool? required = null,
            bool isUnicode = true)
            where TEntity : class
        {
            var b = builder.Property(selector);
            if (maxLength.HasValue)
            {
                b.HasMaxLength(maxLength.Value);
            }
            if (required.HasValue)
            {
                b.IsRequired(required.Value);
            }
            return b.IsUnicode(isUnicode);
        }

        public static PropertyBuilder<string> HasTitle<TEntity>(
            this EntityTypeBuilder<TEntity> builder,
            Expression<Func<TEntity, string>> selector,
            int maxLength = 320,
            bool required = true,
            bool isUnicode = true)
            where TEntity : class
            => builder.StringProperty(selector, maxLength, required, isUnicode);
    }
}