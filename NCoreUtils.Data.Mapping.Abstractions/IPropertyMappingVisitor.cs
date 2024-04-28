namespace NCoreUtils.Data
{
    public interface IPropertyMappingVisitor
    {
        void Visit(PropertyMapping.ByCtorParameterMapping byCtorParameter);

        void Visit(PropertyMapping.BySetterMapping bySetter);
    }

    public interface IPropertyMappingVisitor<T>
    {
        T Visit(PropertyMapping.ByCtorParameterMapping byCtorParameter);

        T Visit(PropertyMapping.BySetterMapping bySetter);
    }
}