namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public interface IValueSource<T> : IValueSource
    {
        new T GetValue(object instance);
    }
}