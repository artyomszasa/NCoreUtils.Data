using System;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.Data.Model;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

public static class FirestoreMetadataExtensions
{
    public const string KeyFieldExpressionFactory = "Firestore:FieldExpressionFactory";

    public const string KeyEnumConversionHelpers = "Firestore:EnumConversionHelpers";

    public const string KeyCollectionFactoryFactory = "Firestore:CollectionFactoryFactory";

    public const string KeyCollectionWrapperFactory = "Firestore:CollectionWrapperFactory";

    public static bool TryGetFirestoreFieldExpressionFactory(
        this DataProperty property,
        [MaybeNullWhen(false)] out IFirestoreFieldExpressionFactory factory)
    {
        if (property.TryGetValue(KeyFieldExpressionFactory, out var boxed)
            && boxed is IFirestoreFieldExpressionFactory f)
        {
            factory = f;
            return true;
        }
        factory = default;
        return false;
    }

    public static IFirestoreFieldExpressionFactory GetFirestoreFieldExpressionFactory(this DataProperty property)
        => property.TryGetFirestoreFieldExpressionFactory(out var factory)
            ? factory
            : throw new InvalidOperationException($"No firestore field expression factory found for {property}. Either use generated model creation or reflection based decorator.");

    public static bool TryGetEnumConversionHelpers(
        this DataModel model,
        [MaybeNullWhen(false)] out IEnumConversionHelpers helpers)
    {
        if (model.TryGetValue(KeyEnumConversionHelpers, out var boxed)
            && boxed is IEnumConversionHelpers h)
        {
            helpers = h;
            return true;
        }
        helpers = default;
        return false;
    }

    public static IEnumConversionHelpers GetEnumConversionHelpers(this DataModel model)
        => model.TryGetEnumConversionHelpers(out var helper)
            ? helper
            : throw new InvalidOperationException($"No enum conversion helpers found. Either use generated model creation or reflection based decorator.");

    public static bool TryGetCollectionFactoryFactory(
        this DataModel model,
        [MaybeNullWhen(false)] out ICollectionFactoryFactory factory)
    {
        if (model.TryGetValue(KeyCollectionFactoryFactory, out var boxed)
            && boxed is ICollectionFactoryFactory f)
        {
            factory = f;
            return true;
        }
        factory = default;
        return false;
    }

    public static ICollectionFactoryFactory GetCollectionFactoryFactory(this DataModel model)
        => model.TryGetCollectionFactoryFactory(out var factory)
            ? factory
            : throw new InvalidOperationException($"No collection factory factory found. Either use generated model creation or reflection based decorator.");

    private sealed class CompositeCollectionFactoryFactory(ICollectionFactoryFactory factory1, ICollectionFactoryFactory factory2)
        : ICollectionFactoryFactory
    {
        public bool IsCollection([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType, [MaybeNullWhen(false)] out Type elementType)
            => factory1.IsCollection(collectionType, out elementType) || factory2.IsCollection(collectionType, out elementType);

        public bool TryCreate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type collectionType, [MaybeNullWhen(false)] out ICollectionFactory builder)
            => factory1.TryCreate(collectionType, out builder) || factory2.TryCreate(collectionType, out builder);
    }

    public static Build.DataModelBuilder OverrideCollectionFactoryFactory(this Build.DataModelBuilder builder, ICollectionFactoryFactory factory)
    {
        if (builder.GetMetadata(KeyCollectionFactoryFactory) is ICollectionFactoryFactory factory0)
        {
            builder.SetMetadata(KeyCollectionFactoryFactory, new CompositeCollectionFactoryFactory(factory0, factory));
        }
        else
        {
            builder.SetMetadata(KeyCollectionFactoryFactory, factory);
        }
        return builder;
    }

    public static bool TryGetCollectionWrapperFactory(
        this DataModel model,
        [MaybeNullWhen(false)] out ICollectionWrapperFactory factory)
    {
        if (model.TryGetValue(KeyCollectionFactoryFactory, out var boxed)
            && boxed is ICollectionWrapperFactory f)
        {
            factory = f;
            return true;
        }
        factory = default;
        return false;
    }

    public static ICollectionWrapperFactory GetCollectionWrapperFactory(this DataModel model)
        => model.TryGetCollectionWrapperFactory(out var factory)
            ? factory
            : throw new InvalidOperationException($"No collection wrapper factory found. Either use generated model creation or reflection based decorator.");

    private sealed class CompositeCollectionWrapperFactory(ICollectionWrapperFactory factory0, ICollectionWrapperFactory factory1)
        : ICollectionWrapperFactory
    {
        public bool TryCreate(object source, [MaybeNullWhen(false)] out ICollectionWrapper wrapper)
            => factory0.TryCreate(source, out wrapper) || factory1.TryCreate(source, out wrapper);
    }

    public static Build.DataModelBuilder OverrideCollectionWrapperFactory(this Build.DataModelBuilder builder, ICollectionWrapperFactory factory)
    {
        if (builder.GetMetadata(KeyCollectionWrapperFactory) is ICollectionWrapperFactory factory0)
        {
            builder.SetMetadata(KeyCollectionWrapperFactory, new CompositeCollectionWrapperFactory(factory0, factory));
        }
        else
        {
            builder.SetMetadata(KeyCollectionWrapperFactory, factory);
        }
        return builder;
    }
}