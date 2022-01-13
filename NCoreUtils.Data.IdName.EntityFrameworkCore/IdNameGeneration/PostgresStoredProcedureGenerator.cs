using System.IO;
using System.Text;

namespace NCoreUtils.Data.IdNameGeneration
{
    public class PostgresStoredProcedureGenerator : IStoredProcedureGenerator
    {
        static readonly string _template;

        static PostgresStoredProcedureGenerator()
        {
            var type = typeof(PostgresStoredProcedureGenerator);
            var resourceName = type.Namespace + ".postgres_template.psql";
            using var reader = new StreamReader(type.Assembly.GetManifestResourceStream(resourceName)!, Encoding.UTF8);
            _template = reader.ReadToEnd();
        }

        public string Generate(string functionNamespace, string functionName)
        {
            var fname =
                string.IsNullOrEmpty(functionNamespace)
                    ? functionName
                    : $"\"{functionNamespace}\".\"{functionName}\"";
            return _template.Replace("%FUNCTION_NAME%", fname);
        }
    }
}