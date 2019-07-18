using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;

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
            Task task = GraphClient.HttpProvider.SendAsync(httpRequestMessage).ContinueWith(
                async t =>
                {
                    if (t.IsCompleted)
                    {
                        HttpResponseMessage response = t.Result;
                        if (response.Content != null)
                        {
                            var responseString = await response.Content.ReadAsStringAsync();
                            T variaDeserializeObject = GraphClient.HttpProvider.Serializer.DeserializeObject<T>(responseString);
                            handlerFunc(variaDeserializeObject);
                        }
                    }
                }
            );

            _requestCollection.Add(task);
        }

        public void ConfigureBatchSize(int size)
        {
            BatchSize = size;
        }

        public void ExecuteAsync()
        {
            Task.WaitAll(_requestCollection.ToArray());
        }
    }
}