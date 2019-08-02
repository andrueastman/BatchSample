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
            _taskCollection = new List<Task>();
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
            HttpResponseMessage response =  await GraphClient.HttpProvider.SendAsync(httpRequestMessage).ConfigureAwait(false);

            if (response.Content == null)
                return default;

            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return GraphClient.HttpProvider.Serializer.DeserializeObject<T>(responseString);

        }

        public async Task<dynamic> PopulateAsync(object model)
        {
            dynamic returnObject = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)returnObject;

            foreach (var typeArguments in model.GetType().GetProperties())
            {
                if (typeArguments.GetValue(model) is IBaseRequest request)//make sure the type is a base request
                {
                    AddRequest<dynamic>(request,u =>
                    {
                        dictionary.Add(typeArguments.Name, u);//map the name with the object that comes back
                    });
                }
            }

            await ExecuteAsync();

            return returnObject;
        }

        private async Task TokenBarrier()
        {
            //Just authenticate a dummy message but no need to send it out coz we just need a valid token in the cache
            var dummyRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/");
            await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(dummyRequestMessage);
            Console.WriteLine("Token barrier crossed");
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await Task.WhenAll(_taskCollection.ToArray());
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e);
                //throw;
            }
            finally
            {
                _taskCollection.Clear();
            }
        }
    }
}