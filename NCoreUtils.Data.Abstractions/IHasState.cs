namespace NCoreUtils.Data;

/// <summary>
/// Defines functionality to access and manipulate inner state of the data entity.
/// </summary>
public interface IHasState
{
    /// <summary>
    /// Gets or sets inner state of the data entity.
    /// </summary>
    State State { get; set; }
}