using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class IdNameDescriptionBuilder<T>
    {
        public PropertyInfo IdNameProperty { get; set; }

        public PropertyInfo NameSourceProperty { get; set; }

        public IStringDecomposer Decompose { get; set; }

        public List<PropertyInfo> AdditionalIndexProperties { get; private set; } = new List<PropertyInfo>();

        public IdNameDescriptionBuilder<T> SetIdNameProperty(PropertyInfo propertyInfo)
        {
            IdNameProperty = propertyInfo;
            return this;
        }

        public IdNameDescriptionBuilder<T> SetNameSourceProperty(PropertyInfo propertyInfo)
        {
            NameSourceProperty = propertyInfo;
            return this;
        }

        public IdNameDescriptionBuilder<T> SetDecompose(IStringDecomposer decompose)
        {
            Decompose = decompose;
            return this;
        }

        public IdNameDescriptionBuilder<T> AddAdditionalIndexProperties(IEnumerable<PropertyInfo> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            AdditionalIndexProperties.AddRange(properties);
            return this;
        }

        public IdNameDescriptionBuilder<T> SetIdNameProperty(Expression<Func<T, string>> selector)
            => SetIdNameProperty(selector.ExtractProperty());

        public IdNameDescriptionBuilder<T> SetNameSourceProperty(Expression<Func<T, string>> selector)
            => SetNameSourceProperty(selector.ExtractProperty());

        public IdNameDescriptionBuilder<T> SetAdditionalIndexProperties(Expression<Func<T, object>> selector)
        {
            if (selector.TryExtractProperty(out var property))
            {
                AdditionalIndexProperties = new List<PropertyInfo> { property };
                return this;
            }
            if (selector.Body is NewExpression newExpression)
            {
                if (null == newExpression.Members || (newExpression.Members.Count != newExpression.Arguments.Count))
                {
                    throw new InvalidOperationException("Invalid expression.");
                }
                AdditionalIndexProperties = newExpression.Arguments
                    .Select(expr => expr is MemberExpression mexpr && mexpr.Member is PropertyInfo prop
                                    ? prop
                                    : throw new InvalidOperationException($"Unable to extract property from {expr}."))
                    .ToList();
                return this;
            }
            throw new InvalidOperationException($"Unable to extract properties from {selector}.");
        }

        public IdNameDescription Build() => new IdNameDescription(IdNameProperty, NameSourceProperty, Decompose, AdditionalIndexProperties);
    }
}