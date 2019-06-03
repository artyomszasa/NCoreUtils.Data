using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;
using NCoreUtils.Data.Events;

namespace NCoreUtils.AspNetCore.Data
{
    public class RequestScopedEventHandler<TEventHandler> : IDataEventHandler
        where TEventHandler : IDataEventHandler
    {
        readonly IHttpContextAccessor _httpContextAccessor;

        public RequestScopedEventHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (null != httpContext)
            {
                var handler = ActivatorUtilities.CreateInstance<TEventHandler>(httpContext.RequestServices);
                try
                {
                    await handler.HandleAsync(@event, cancellationToken);
                }
                finally
                {
                    (handler as IDisposable)?.Dispose();
                }
            }
        }
    }
}