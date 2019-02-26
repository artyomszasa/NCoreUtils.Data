using System;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Provides functionality to instantiate repository contexts.
    /// </summary>
    public interface IDataRepositoryContextFactory
    {
        /// <summary>
        /// Creates and initializes new instance of the repository context within the provided scope.
        /// </summary>
        /// <param name="serviceProvider">Service provider that defines the scope.</param>
        /// <returns>Newly created instance of the repository context.</returns>
        IDataRepositoryContext Create(IServiceProvider serviceProvider);
    }
}