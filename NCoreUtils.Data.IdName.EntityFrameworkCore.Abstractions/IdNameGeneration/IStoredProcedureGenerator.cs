namespace NCoreUtils.Data.IdNameGeneration
{
    public interface IStoredProcedureGenerator
    {
        string Generate(string functionNamespace, string functionName);
    }
}