namespace NCoreUtils.Data.Build
{
    public class CamelCaseNaming
    {
        public void Apply(DataPropertyBuilder propertyBuilder)
        {
            propertyBuilder.SetName(NamingConvention.CamelCase.Apply(propertyBuilder.Property.Name));
        }

        public void Apply(DataEntityBuilder entityBuilder, bool applyToProperties = true)
        {
            entityBuilder.SetName(NamingConvention.CamelCase.Apply(entityBuilder.EntityType.Name));
            if (applyToProperties)
            {
                foreach (var property in entityBuilder.Properties.Values)
                {
                    Apply(property);
                }
            }
        }
    }
}