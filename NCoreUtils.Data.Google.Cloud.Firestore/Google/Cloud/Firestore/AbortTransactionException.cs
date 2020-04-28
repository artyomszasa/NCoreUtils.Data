using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    [Serializable]
    public class AbortTransactionException : Exception
    {
        public AbortTransactionException() : base() { }

        public AbortTransactionException(Exception innerException) : base("Transaction has been aborted due to error.", innerException) { }

        protected AbortTransactionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}