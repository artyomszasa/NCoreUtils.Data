using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Google.Cloud.Firestore.V1;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class FirestoreConversionException : InvalidOperationException
{
    private static string FormatMessage(Type requestedClrType, Value.ValueTypeOneofCase firestoreType)
        => $"Unable to convert firestore value of type {firestoreType} to {requestedClrType}.";

    public Type RequestedClrType { get; }

    public Value.ValueTypeOneofCase FirestoreType { get; }

#if !NET8_0_OR_GREATER
    [RequiresUnreferencedCode("The type might be removed")]
    protected FirestoreConversionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        RequestedClrType = Type.GetType(info.GetString(nameof(RequestedClrType)) ?? string.Empty, true)!;
        FirestoreType = (Value.ValueTypeOneofCase)info.GetInt32(nameof(FirestoreType));
    }
#endif

    public FirestoreConversionException(Type requestedClrType, Value.ValueTypeOneofCase firestoreType)
        : base(FormatMessage(requestedClrType, firestoreType))
    {
        RequestedClrType = requestedClrType;
        FirestoreType = firestoreType;
    }

    public FirestoreConversionException(Type requestedClrType, Value.ValueTypeOneofCase firestoreType, Exception innerException)
        : base(FormatMessage(requestedClrType, firestoreType), innerException)
    {
        RequestedClrType = requestedClrType;
        FirestoreType = firestoreType;
    }

#if !NET8_0_OR_GREATER
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(RequestedClrType), RequestedClrType.AssemblyQualifiedName);
        info.AddValue(nameof(FirestoreType), (int)FirestoreType);
    }
#endif
}