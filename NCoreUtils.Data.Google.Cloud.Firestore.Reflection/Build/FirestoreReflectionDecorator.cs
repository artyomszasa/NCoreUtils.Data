using NCoreUtils.Data.Google.Cloud.Firestore.Internal;

namespace NCoreUtils.Data.Build;

public static class FirestoreReflectionDecorator
{
    public static DataModelBuilder AddReflectionBasedFirestoreDecorations(this DataModelBuilder model)
    {
        var enumTypes = new HashSet<Type>();
        var fieldExpressionFactory = new ReflectionFieldExpressionFactory();
        foreach (var entityBuilder in model.Entities)
        {
            foreach (var (property, propertyBuilder) in entityBuilder.Properties)
            {
                if (property.PropertyType.IsEnum)
                {
                    enumTypes.Add(property.PropertyType);
                }
                propertyBuilder.SetMetadata(FirestoreMetadataExtensions.KeyFieldExpressionFactory, fieldExpressionFactory);
            }
        }
        model.SetMetadata(FirestoreMetadataExtensions.KeyEnumConversionHelpers, new ReflectionEnumConversionHelpers(enumTypes));
        return model;
    }
}