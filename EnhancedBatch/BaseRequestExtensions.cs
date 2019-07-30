using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public static class BaseRequestExtensions
    {
        public static void GetAsync<T>(this IBaseRequest request , ResponseHandler responseHandler)
        {
            responseHandler.Query.AddRequest<T>(request,responseHandler.GetSuccessAction<T>(typeof(T)));
        }
    }
}