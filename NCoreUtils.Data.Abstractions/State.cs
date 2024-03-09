namespace NCoreUtils.Data;

/// <summary>
/// Represents inner state of the data entity.
/// </summary>
public enum State
{
    /// Data entity is only accessible through admin interfaces.
    NotPublic = 0,
    /// Data entity is accessible publicly, i.e. through both admin and public interfaces.
    Public = 1,
    /// Data entity is not accessible and is only kept in database in case it should be restored.
    Deleted = 2
}