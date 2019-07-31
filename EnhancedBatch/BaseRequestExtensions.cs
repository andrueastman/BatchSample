using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public static class BaseRequestExtensions
    {
        public static void GetAsync<T>(this IBaseRequest request , ResponseHandler responseHandler)
        {
            Task<T> requestTask =  responseHandler.Query.SendMessage<T>(request.GetHttpRequestMessage());
            requestTask.ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    responseHandler.InvokeSuccessAction(t.Result);
                }
            });
            requestTask.Wait();
        }
    }
}