using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public class HttpQuery
    {
        private readonly GraphServiceClient _graphClient;
        private readonly List<Task> _taskCollection;

        /// <summary>
        /// Constructor for the HttpQuery
        /// </summary>
        /// <param name="graphClient">Client to provide necessary request build mechanisms</param>
        public HttpQuery(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
            _taskCollection = new List<Task>();
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

            Task task = SendMessageAsyncTask<T>(httpRequestMessage).
                ContinueWith(t =>
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
            HttpResponseMessage response =  await _graphClient.HttpProvider.SendAsync(httpRequestMessage).ConfigureAwait(false);

            if (response.Content == null)
                return default;

            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return _graphClient.HttpProvider.Serializer.DeserializeObject<T>(responseString);

        }

        /// <summary>
        /// Populates a number of requests present in a dynamic object to send out.
        /// </summary>
        /// <param name="model">dynamic object containing list of requests to send out.</param>
        /// <returns></returns>
        public async Task<dynamic> PopulateAsync(object model)
        {
            dynamic returnObject = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)returnObject;//cast the expando to a dictionary

            //loop through each of the nested IBaseRequest objects present in the model object
            foreach (PropertyInfo propertyInfo in model.GetType().GetProperties())
            {
                //make sure the type is a base request
                if (propertyInfo.GetValue(model) is IBaseRequest request)
                {
                    AddRequest<dynamic>(request,u =>
                    {
                        //map the name with the object that comes back
                        dictionary.Add(propertyInfo.Name, u);
                    });
                }
            }
            //fire away
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
    }
}