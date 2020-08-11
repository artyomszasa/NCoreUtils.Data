using NCoreUtils.Rest;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Data.Rest
{
    public interface IDataRestClientCache
    {
        IHttpRestClient GetOrAdd(IRestClientConfiguration configuration);
    }
}