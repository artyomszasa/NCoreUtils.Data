using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Data.IdNameGeneration
{
    [Serializable]
    public class IdNameGenerationAnnotationException : FormatException
    {
        public IdNameGenerationAnnotationException(string message)
            : base(message)
        { }

        public IdNameGenerationAnnotationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected IdNameGenerationAnnotationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}