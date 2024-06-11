namespace NCoreUtils.Data.Build;

public interface IConvention
{
    /// <summary>
    /// Applies convention to the property.
    /// </summary>
    /// <param name="propertyBuilder">Property to apply convention to.</param>
    void Apply(DataPropertyBuilder propertyBuilder);

    /// <summary>
    /// Applies convention to the entity and optionally to all its properties.
    /// </summary>
    /// <param name="entityBuilder">Entity to apply convention to.</param>
    /// <param name="applyToProperties">Whether to apply convention to the properties.</param>
    void Apply(DataEntityBuilder entityBuilder, bool applyToProperties = true);
}