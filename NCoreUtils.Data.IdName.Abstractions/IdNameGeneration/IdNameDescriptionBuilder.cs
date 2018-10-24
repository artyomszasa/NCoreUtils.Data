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
            AdditionalIndexProperties = new List<PropertyInfo>(selector.ExtractProperties(true));
            return this;
        }

        public IdNameDescription Build() => new IdNameDescription(IdNameProperty, NameSourceProperty, Decompose, AdditionalIndexProperties.ToArray());
    }
}