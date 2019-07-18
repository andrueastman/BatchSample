using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    internal class HttpQuery
    {
        public GraphServiceClient GraphClient { get; }
        private readonly List<Task> _requestCollection;
        public int BatchSize { get; private set; } = 20;

        public HttpQuery(GraphServiceClient graphClient)
        {
            GraphClient = graphClient;
            _requestCollection = new List<Task>();//empty at start
        }

        public void AddRequest<T>(IBaseRequest request, Func<T, T> handlerFunc)
        {
            HttpRequestMessage httpRequestMessage = request.GetHttpRequestMessage();
            Task task = SendMessage<T>(httpRequestMessage).ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    handlerFunc(t.Result);
                }
            });

            _requestCollection.Add(task);
        }

        public async Task<T> SendMessage<T>(HttpRequestMessage httpRequestMessage)
        {
            await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage);

            HttpResponseMessage response =  await GraphClient.HttpProvider.SendAsync(httpRequestMessage);

            if (response.Content != null)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                return  GraphClient.HttpProvider.Serializer.DeserializeObject<T>(responseString);
                
            }

            return default;
        }

        public void ExecuteAsync()
        {

            Task.WaitAll(_requestCollection.ToArray());
        }
    }
}