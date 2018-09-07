namespace NCoreUtils.Data.IdNameGeneration
{
    public interface ISupportsIdNameGeneration
    {
        bool GenerateIdNameOnInsert { get; }

        IdNameDescription IdNameDescription { get; }
    }
}