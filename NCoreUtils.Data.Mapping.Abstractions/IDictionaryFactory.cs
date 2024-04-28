using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Data;

public interface IDictionaryFactory : ICollectionFactory
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type KeyValueType { get; }
}