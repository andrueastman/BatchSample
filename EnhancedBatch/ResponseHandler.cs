using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace EnhancedBatch
{
    public class ResponseHandler
    {
        private Action<Exception> _serverExceptionHandler;
        private Action<Exception> _clientExceptionHandler;
        private readonly Dictionary<Type, object> _delegateMap;
        public ResponseHandler()
        {
            _delegateMap = new Dictionary<Type, object>();
        }
        
        public void OnSuccess<T>(Action<T> successHandler)
        {
            if (!_delegateMap.ContainsKey(typeof(T)))
            {
                _delegateMap[typeof(T)] = successHandler;
            }
        }
        public void OnServerError(Action<Exception> serverExceptionHandler)
        {
            _serverExceptionHandler = serverExceptionHandler;
        }

        public void OnClientError(Action<Exception> clientExceptionHandler)
        {
            _clientExceptionHandler = clientExceptionHandler;
        }

        public async Task HandleResponse<T>(HttpResponseMessage responseMessage)
        {
            if (responseMessage.StatusCode.CompareTo(HttpStatusCode.BadRequest) > 0 )
            {
                //check if in the 400s
                _serverExceptionHandler(new ServiceException(new Error
                {
                    Message = $"HTTP Error {responseMessage.StatusCode}"
                }));
                return;
            }

            try
            {
                if (responseMessage.Content == null)
                    return;

                var responseString = await responseMessage.Content.ReadAsStringAsync();
                var returnObject = JsonConvert.DeserializeObject<T>(responseString);
                InvokeSuccessAction(returnObject);
            }
            catch (Exception e)
            {
                _clientExceptionHandler(e);
            }
            
        }

        private void InvokeSuccessAction<T>(T item)
        {
            Action<T> handler = GetSuccessAction<T>(typeof(T));
            handler(item);
        }

        private Action<T> GetSuccessAction<T>(Type type)
        {
            if (!_delegateMap.TryGetValue(type, out var function))
                return default;

            Action<T> handler = (Action<T>)function;
            return handler;
        }

    }
}