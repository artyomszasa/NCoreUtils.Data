using System;

namespace NCoreUtils.Data
{
    public class SingletonDataRepositoryContextManager : IDataRepositoryContextManager
    {
        readonly object _lock = new object();
        readonly IServiceProvider _serviceProvider;
        readonly IDataRepositoryContextFactory _factory;

        IDataRepositoryContext _context;

        public SingletonDataRepositoryContextManager(IServiceProvider serviceProvider, IDataRepositoryContextFactory factory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public IDataRepositoryContext GetOrCreateContext(Guid guid)
        {
            if (Guid.Empty == guid)
            {
                var context = _context;
                if (null != context)
                {
                    return context;
                }
                lock (_lock)
                {
                    context = _context;
                    if (null != context)
                    {
                        return context;
                    }
                    context = _factory.Create(_serviceProvider);;
                    _context = context;
                    return context;
                }
            }
            throw new InvalidOperationException("Only default repository context supported by the singleton data repository manager.");
        }
    }
}