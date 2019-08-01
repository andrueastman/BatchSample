using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public static class BaseRequestExtensions
    {
        public static async Task GetAsync<T>(this IBaseRequest request , ResponseHandler responseHandler)
        {
            var message = request.GetHttpRequestMessage();
            await request.Client.AuthenticationProvider.AuthenticateRequestAsync(message);

            var httpResponse = await request.Client.HttpProvider.SendAsync(message);

            await responseHandler.HandleResponse<T>(httpResponse);
        }

        public static async Task GetAsync(this IUserRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<User>(request,responseHandler);
        }

        public static async Task GetAsync(this ICalendarRequest request, ResponseHandler responseHandler)
        {
            await GetAsync<Calendar>(request, responseHandler);
        }
    }
}