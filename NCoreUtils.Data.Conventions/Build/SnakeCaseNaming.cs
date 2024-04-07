namespace NCoreUtils.Data.Build;

public class SnakeCaseNaming : IConvention
{
    /// <inheritdoc />
    public void Apply(DataPropertyBuilder propertyBuilder)
        => propertyBuilder.SetName(NamingConvention.SnakeCase.Apply(propertyBuilder.Property.Name));

    /// <inheritdoc />
    public void Apply(DataEntityBuilder entityBuilder, bool applyToProperties = true)
    {
        entityBuilder.SetName(NamingConvention.SnakeCase.Apply(entityBuilder.EntityType.Name));
        if (applyToProperties)
        {
            foreach (var property in entityBuilder.Properties.Values)
            {
                Apply(property);
            }
        }
    }
}