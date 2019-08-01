using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public static class BaseRequestExtensions
    {
        public static async Task GetAsync<T>(this IBaseRequest request , ResponseHandler responseHandler)
        {
            var message = request.GetHttpRequestMessage();
            await request.Client.AuthenticationProvider.AuthenticateRequestAsync(message).ConfigureAwait(false);

            var httpResponse = await request.Client.HttpProvider.SendAsync(message).ConfigureAwait(false);

            await responseHandler.HandleResponse<T>(httpResponse).ConfigureAwait(false);
        }

        public static async Task GetAsync(this IUserRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<User>(request,responseHandler).ConfigureAwait(false);
        }

        public static async Task GetAsync(this ICalendarRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<Calendar>(request, responseHandler).ConfigureAwait(false);
        }
        
        public static async Task GetAsync(this IDriveRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<Drive>(request, responseHandler).ConfigureAwait(false);
        }
    }
}