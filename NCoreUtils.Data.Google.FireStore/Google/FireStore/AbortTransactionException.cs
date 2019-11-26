using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Data.Google.FireStore
{
    [Serializable]
    public class AbortTransactionException : Exception
    {
        public AbortTransactionException() : base() { }

        protected AbortTransactionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}