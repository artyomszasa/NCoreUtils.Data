using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data.Google.FireStore.Transformations
{
    public class DocumentPropertySource<T> : DocumentPropertySource, IValueSource<T>
    {
        public override Type Type => typeof(T);

        public DocumentPropertySource(string path)
            : base(path)
        {
        }

        object IValueSource.GetValue(object instance) => GetValue(instance);

        protected override object GetBoxedValue(object instance) => GetValue(instance);

        public T GetValue(object instance)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (instance is DocumentSnapshot document)
            {
                return document.GetValue<T>(Path);
            }
            if (instance is IReadOnlyDictionary<string, object> dictionary)
            {
                var val = dictionary.GetOrDefault(Path);
                return val is null ? default : (T)val;
            }
            throw new InvalidOperationException($"Invalid instance of type {instance.GetType()}, expected {typeof(DocumentSnapshot)}.");
        }
    }
}