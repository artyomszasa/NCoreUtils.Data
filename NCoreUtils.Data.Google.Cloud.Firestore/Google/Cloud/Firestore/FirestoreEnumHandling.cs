namespace NCoreUtils.Data.Google.Cloud.Firestore
{
    public enum FirestoreEnumHandling
    {
        /// Store enum value as single number.
        AsSingleNumber = 0,
        /// Store non-flags enum value as number and flags as number array (allows HasFlag condition).
        AsNumberOrNumberArray = 1,
        /// Store enum value as single string, use <c>|</c> to concatenate multiple flags.
        AlwaysAsString = 2,
        /// Store non-flags enum value as string and flags as string array (allows HasFlag condition).
        AsStringOrStringArray = 3
    }
}