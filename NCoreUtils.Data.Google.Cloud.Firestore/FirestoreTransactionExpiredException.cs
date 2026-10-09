namespace NCoreUtils.Data;

public class FirestoreTransactionExpiredException : Exception
{
    private const string DefaultMessage = "Firestore transaction has expired and should be rerun manually.";

    public FirestoreTransactionExpiredException() : base(DefaultMessage) { }

    public FirestoreTransactionExpiredException(string message) : base(message ?? DefaultMessage) { }

    public FirestoreTransactionExpiredException(Exception innerException) : base(DefaultMessage, innerException) { }

    public FirestoreTransactionExpiredException(string message, Exception innerException)
        : base(message ?? DefaultMessage, innerException)
    { }
}