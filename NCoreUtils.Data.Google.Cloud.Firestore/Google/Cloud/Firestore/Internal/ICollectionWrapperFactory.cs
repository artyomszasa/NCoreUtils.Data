using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public interface ICollectionWrapperFactory
{
    bool TryCreate(object source, [MaybeNullWhen(false)] out ICollectionWrapper wrapper);

    ICollectionWrapper Create(object source)
        => TryCreate(source, out var wrapper)
            ? wrapper
            : throw new InvalidOperationException($"Unable to create collection wrapper for {source}. Consider overriding CollectionWrapperFactory in model metadata.");
}
