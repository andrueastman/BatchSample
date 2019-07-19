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
        private readonly List<HttpRequestMessage> _requestCollection;
        private readonly List<Task> _taskCollection;
        public int BatchSize { get; private set; } = 20;

        public HttpQuery(GraphServiceClient graphClient)
        {
            GraphClient = graphClient;
            _requestCollection = new List<HttpRequestMessage>();//empty at start
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
            _requestCollection.Add(httpRequestMessage);
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

        public async Task TokenBarrier()
        {
            var user2 = await GraphClient.Me.Request().GetAsync(); //HACK!!!! //TODO //FIXME
            Console.WriteLine("Barrier crossed: " + user2.DisplayName);
        }

        public void ExecuteAsync()
        {
            Task.WaitAll(_taskCollection.ToArray());
        }

        public void ExecuteBatch()
        {
            _taskCollection.Clear();//flush it

            int batches = _requestCollection.Count / BatchSize + (_requestCollection.Count % BatchSize > 0 ? 1 : 0);

            for (var batchIndex = 0; batchIndex < batches ; batchIndex++ )
            {
                BatchRequestContent batchRequestContent = new BatchRequestContent();
                
                for (var i = batchIndex *BatchSize ; i < _requestCollection.Count && i < batchIndex * (BatchSize) + BatchSize; i++)
                {
                    BatchRequestStep requestStep1 = new BatchRequestStep($"{i}", _requestCollection[i]);
                    batchRequestContent.AddBatchRequestStep(requestStep1);
                }

                HttpRequestMessage batchRequestMessage = new HttpRequestMessage
                {
                    Content = batchRequestContent,
                    RequestUri = new Uri("https://graph.microsoft.com/v1.0/$batch"),
                    Method = HttpMethod.Post
                };
                
                CreateBatchMessage(batchRequestMessage);

            }

            Task.WaitAll(_taskCollection.ToArray());
        }

        public void CreateBatchMessage(HttpRequestMessage batchRequestMessage)
        {
            GraphClient.AuthenticationProvider.AuthenticateRequestAsync(batchRequestMessage).Wait();
            Task task = GraphClient.HttpProvider.SendAsync(batchRequestMessage).ContinueWith(
                async t =>
                {
                    if (t.IsCompleted)
                    {
                        Console.WriteLine("Batch completed");
                        BatchResponseContent batchResponseContent = new BatchResponseContent(t.Result);
                        Dictionary<string, HttpResponseMessage> responses = await batchResponseContent.GetResponsesAsync();
                        Console.WriteLine(responses.Count);

                    }
                });

            _taskCollection.Add(task);
        }
    }
}