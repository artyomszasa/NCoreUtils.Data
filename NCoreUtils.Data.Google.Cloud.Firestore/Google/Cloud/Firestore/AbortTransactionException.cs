using System.Runtime.Serialization;

namespace NCoreUtils.Data.Google.Cloud.Firestore;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Used internally")]
public class AbortTransactionException : Exception
{
    public AbortTransactionException() : base() { }

    public AbortTransactionException(Exception innerException) : base("Transaction has been aborted due to an error.", innerException) { }

#if !NET8_0_OR_GREATER
    protected AbortTransactionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
#endif
}