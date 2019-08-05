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
        private readonly List<Task> _taskCollection;

        /// <summary>
        /// Constructor for the HttpQuery
        /// </summary>
        /// <param name="graphClient">Client to provide necessary request build mechanisms</param>
        public HttpQuery(GraphServiceClient graphClient)
        {
            GraphClient = graphClient;
            _taskCollection = new List<Task>();
            TokenBarrier().Wait();
        }

        /// <summary>
        /// Add a request to be queued in parallel.
        /// </summary>
        /// <typeparam name="T">Object return type</typeparam>
        /// <param name="request">Request to be sent out</param>
        /// <param name="handlerFunc">Handler for the response.</param>
        public void AddRequest<T>(IBaseRequest request, Action<T> handlerFunc)
        {
            HttpRequestMessage httpRequestMessage = request.GetHttpRequestMessage();

            Task task = SendMessageAsyncTask<T>(httpRequestMessage).ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    handlerFunc(t.Result);
                }
            });

            _taskCollection.Add(task);
        }

        /// <summary>
        /// Creates a task of sending out a message
        /// </summary>
        /// <typeparam name="T">Object type to deserialize the response.</typeparam>
        /// <param name="httpRequestMessage">HttpRequest message to send out.</param>
        /// <returns></returns>
        private async Task<T> SendMessageAsyncTask<T>(HttpRequestMessage httpRequestMessage)
        {
            HttpResponseMessage response =  await GraphClient.HttpProvider.SendAsync(httpRequestMessage).ConfigureAwait(false);

            if (response.Content == null)
                return default;

            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return GraphClient.HttpProvider.Serializer.DeserializeObject<T>(responseString);

        }

        /// <summary>
        /// Populates a number of requests present in a dynamic object to send out.
        /// </summary>
        /// <param name="model">dynamic object containing list of requests to send out.</param>
        /// <returns></returns>
        public async Task<dynamic> PopulateAsync(object model)
        {
            dynamic returnObject = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)returnObject;

            foreach (var typeArguments in model.GetType().GetProperties())
            {
                //make sure the type is a base request
                if (typeArguments.GetValue(model) is IBaseRequest request)
                {
                    AddRequest<dynamic>(request,u =>
                    {
                        //map the name with the object that comes back
                        dictionary.Add(typeArguments.Name, u);
                    });
                }
            }

            await ExecuteAsync();

            return returnObject;
        }

        /// <summary>
        /// Execute all tasks present in the task list then empty the task list.
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteAsync()
        {
            try
            {
                await Task.WhenAll(_taskCollection.ToArray());
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                _taskCollection.Clear();
            }
        }

        /// <summary>
        /// This is a barrier synchronization mechanism/hack to acquire the token to have in cache
        /// so that requests being sent out in parallel by this instance do not necessarily have to spend time fetching tokens
        /// and use the local copy instead.
        /// </summary>
        /// <returns></returns>
        private async Task TokenBarrier()
        {
            //Just authenticate a dummy message but no need to send it out coz we just need a valid token in the cache
            var dummyRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/");
            await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(dummyRequestMessage);
//            Console.WriteLine("Token barrier crossed");
        }
    }
}