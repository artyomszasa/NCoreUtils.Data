namespace NCoreUtils.Data.IdNameGeneration
{
    public interface IStringDecomposition
    {
        string MainPart { get; }

        string Rebuild(string mainPart, string? suffix);
    }
}