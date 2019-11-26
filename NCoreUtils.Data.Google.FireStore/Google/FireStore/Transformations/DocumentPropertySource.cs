using System;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public abstract class DocumentPropertySource : IValueSource
    {
        public string Path { get; }

        public abstract Type Type { get; }

        public DocumentPropertySource(string path)
            => Path = path ?? throw new ArgumentNullException(nameof(path));

        object IValueSource.GetValue(object instance) => GetBoxedValue(instance);

        protected abstract object GetBoxedValue(object instance);
    }
}