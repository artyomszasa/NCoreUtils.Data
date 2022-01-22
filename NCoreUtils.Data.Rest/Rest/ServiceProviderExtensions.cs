using System;

namespace NCoreUtils.Data.Rest
{
    internal static class ServiceProviderExtensions
    {
        private class OverriddenServiceProvider<T> : IServiceProvider
        {
            readonly IServiceProvider _base;
            readonly T _override;

            public OverriddenServiceProvider(IServiceProvider @base, T @override)
            {
                _base = @base ?? throw new ArgumentNullException(nameof(@base));
                _override = @override;
            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(T))
                {
                    return _override!;
                }
                return _base.GetService(serviceType);
            }
        }

        internal static IServiceProvider Override<T>(this IServiceProvider @base, T implementation)
            => new OverriddenServiceProvider<T>(@base, implementation);
    }
}