using System.Net.Http;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public static class BaseRequestExtensions
    {
        /// <summary>
        /// Type agnostic extension method that takes in a handler for the response.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request">Request to send out</param>
        /// <param name="responseHandler">Handler for the response</param>
        /// <returns></returns>
        public static void SendGet<T>(this IBaseRequest request , ResponseHandler responseHandler)
        {
            request.Client.HttpProvider.SendAsync(request.GetHttpRequestMessage()).
                ContinueWith(t =>
                {
                    if (t.IsCompleted)
                    {
                        HttpResponseMessage httpResponse = t.Result;
                        responseHandler.HandleResponse<T>(httpResponse).ConfigureAwait(false);
                    }
                });
        }

        /// <summary>
        /// IUserRequest extension method that takes in a handler for the response.
        /// </summary>
        /// <param name="request">Request to send out</param>
        /// <param name="responseHandler">Handler for the response</param>
        /// <returns></returns>
        public static void SendGet(this IUserRequest request, ResponseHandler responseHandler)
        {
            SendGet<User>(request,responseHandler);
        }

        /// <summary>
        /// ICalendarRequest extension method that takes in a handler for the response.
        /// </summary>
        /// <param name="request">Request to send out</param>
        /// <param name="responseHandler">Handler for the response</param>
        /// <returns></returns>
        public static void SendGet(this ICalendarRequest request, ResponseHandler responseHandler)
        {
            SendGet<Calendar>(request, responseHandler);
        }

        /// <summary>
        /// IDriveRequest extension method that takes in a handler for the response.
        /// </summary>
        /// <param name="request">Request to send out</param>
        /// <param name="responseHandler">Handler for the response</param>
        /// <returns></returns>
        public static void SendGet(this IDriveRequest request, ResponseHandler responseHandler)
        {
            SendGet<Drive>(request, responseHandler);
        }
    }
}