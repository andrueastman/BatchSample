using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public class HttpQuery
    {
        public GraphServiceClient GraphClient { get; }
        private List<Task> _taskCollection;

        public HttpQuery(GraphServiceClient graphClient)
        {
            GraphClient = graphClient;
            _taskCollection = new List<Task>();//empty at start
            TokenBarrier().Wait();
        }

        public void AddRequest<T>(IBaseRequest request, Action<T> handlerFunc)
        {
            HttpRequestMessage httpRequestMessage = request.GetHttpRequestMessage();

            Task task = SendMessage<T>(httpRequestMessage).ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    handlerFunc(t.Result);
                }
            });

            _taskCollection.Add(task);
        }

        private async Task<T> SendMessage<T>(HttpRequestMessage httpRequestMessage)
        {
            await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage).ConfigureAwait(false);

            HttpResponseMessage response =  await GraphClient.HttpProvider.SendAsync(httpRequestMessage).ConfigureAwait(false);

            if (response.Content == null)
                return default;

            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return GraphClient.HttpProvider.Serializer.DeserializeObject<T>(responseString);

        }

        public dynamic PopulateAsync(object model)
        {
            dynamic returnObject = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)returnObject;
            foreach (var typeArguments in model.GetType().GetProperties())
            {
                if (typeArguments.GetValue(model) is IBaseRequest request)
                {
                    AddRequest<dynamic>(request,u =>
                    {
                        dictionary.Add(typeArguments.Name, u);
                    });
                }
            }

            ExecuteAsync();

            return returnObject;
        }

        private async Task TokenBarrier()
        {
            var user2 = await GraphClient.Me.Request().GetAsync(); //HACK!!!! //TODO //FIXME
            Console.WriteLine("Barrier crossed: " + user2.DisplayName);
        }

        public void ExecuteAsync()
        {
            Task.WaitAll(_taskCollection.ToArray());
            _taskCollection = new List<Task>();
        }
    }
}