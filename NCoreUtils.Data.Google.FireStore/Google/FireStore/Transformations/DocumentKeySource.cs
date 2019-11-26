using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public class DocumentKeySource : IValueSource<string>
    {
        public Type Type => typeof(string);

        public string Path { get; }

        public DocumentKeySource(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string GetValue(object instance)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (instance is DocumentSnapshot document)
            {
                return document.Id;
            }
            if (instance is IReadOnlyDictionary<string, object> dictionary)
            {
                var val = dictionary.GetOrDefault(Path);
                return val as string;
            }
            throw new InvalidOperationException($"Invalid instance of type {instance.GetType()}, expected {typeof(DocumentSnapshot)}.");
        }

        object IValueSource.GetValue(object instance) => GetValue(instance);
    }
}