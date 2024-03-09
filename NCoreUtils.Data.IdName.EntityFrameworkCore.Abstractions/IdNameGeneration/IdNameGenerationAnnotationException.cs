using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Data.IdNameGeneration
{
#if !NET8_0_OR_GREATER
    [Serializable]
#endif
    public class IdNameGenerationAnnotationException : FormatException
    {
        public IdNameGenerationAnnotationException(string message)
            : base(message)
        { }

        public IdNameGenerationAnnotationException(string message, Exception innerException)
            : base(message, innerException)
        { }

#if !NET8_0_OR_GREATER
        protected IdNameGenerationAnnotationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }
}