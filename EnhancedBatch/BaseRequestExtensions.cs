using System.Threading.Tasks;
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
        public static async Task GetAsync<T>(this IBaseRequest request , ResponseHandler responseHandler)
        {
            var httpResponse = await request.Client.HttpProvider.SendAsync(request.GetHttpRequestMessage()).ConfigureAwait(false);
            await responseHandler.HandleResponse<T>(httpResponse).ConfigureAwait(false);
        }

        /// <summary>
        /// IUserRequest extension method that takes in a handler for the response.
        /// </summary>
        /// <param name="request">Request to send out</param>
        /// <param name="responseHandler">Handler for the response</param>
        /// <returns></returns>
        public static async Task GetAsync(this IUserRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<User>(request,responseHandler).ConfigureAwait(false);
        }

        /// <summary>
        /// ICalendarRequest extension method that takes in a handler for the response.
        /// </summary>
        /// <param name="request">Request to send out</param>
        /// <param name="responseHandler">Handler for the response</param>
        /// <returns></returns>
        public static async Task GetAsync(this ICalendarRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<Calendar>(request, responseHandler).ConfigureAwait(false);
        }

        /// <summary>
        /// IDriveRequest extension method that takes in a handler for the response.
        /// </summary>
        /// <param name="request">Request to send out</param>
        /// <param name="responseHandler">Handler for the response</param>
        /// <returns></returns>
        public static async Task GetAsync(this IDriveRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<Drive>(request, responseHandler).ConfigureAwait(false);
        }
    }
}