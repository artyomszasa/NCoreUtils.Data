namespace NCoreUtils.Data
{
    /// <summary>
    /// Represents required flag as part of some composite data.
    /// </summary>
    public sealed class RequiredFlag : DataFlag
    {
        /// <summary>
        /// Key of the required flag within dictionary representation of composite data.
        /// </summary>
        public static CaseInsensitive KeyRequired { get; } = "Required";
        /// <summary>
        /// Initializes new instance of <see cref="T:NCoreUtils.Data.RequiredFlag" /> with the specified boolean value.
        /// </summary>
        /// <param name="value">The value of the flag.</param>
        public RequiredFlag(bool value) : base(value, KeyRequired) { }
    }
}