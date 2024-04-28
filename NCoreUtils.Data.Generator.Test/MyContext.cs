using System.Reflection;
using NCoreUtils.Data.Build;

namespace NCoreUtils.Data;

[DataDefinitionGenerationOptions(GenerateFirestoreDecorators = true)]
[DataEntity(typeof(MyData), NameFactory = typeof(NameFactory))]
public partial class MyContext
{
    private static class NameFactory
    {
        public static string GetName(PropertyInfo property) => NamingConvention.SnakeCase.Apply(property.Name);

        public static string GetName(Type type) => NamingConvention.SnakeCase.Apply(type.Name);
    }
}