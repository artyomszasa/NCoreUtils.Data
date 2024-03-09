using System;

namespace NCoreUtils.Data;

/// <summary>
/// Dfines functionality to maintain creation and last modification time tracking for data entity.
/// </summary>
[Obsolete("Override DataRepoository implementation to handle time tracking.")]
public interface IHasTimeTracking
{
    /// <summary>
    /// Creation time represented by ticks passed since 0001.01.01 00:00:00 (UTC).
    /// </summary>
    long Created { get; set; }

    /// <summary>
    /// Last modification time represented by ticks passed since 0001.01.01 00:00:00 (UTC).
    /// </summary>
    long Updated { get; set; }
}