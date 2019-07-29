using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    internal class HttpQuery
    {
        public GraphServiceClient GraphClient { get; }
        private readonly List<Task> _taskCollection;

        public HttpQuery(GraphServiceClient graphClient)
        {
            GraphClient = graphClient;
            _taskCollection = new List<Task>();//empty at start
            TokenBarrier().Wait();
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

            _taskCollection.Add(task);
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
                        return u;
                    });
                }
            }

            ExecuteAsync();

            return returnObject;
        }

        public async Task TokenBarrier()
        {
            var user2 = await GraphClient.Me.Request().GetAsync(); //HACK!!!! //TODO //FIXME
            Console.WriteLine("Barrier crossed: " + user2.DisplayName);
        }

        public void ExecuteAsync()
        {
            Task.WaitAll(_taskCollection.ToArray());
        }
    }
}