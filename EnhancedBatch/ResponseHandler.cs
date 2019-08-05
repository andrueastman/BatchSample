using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace EnhancedBatch
{
    /// <summary>
    /// Class for holding the handlers for the responses.
    /// </summary>
    public class ResponseHandler
    {
        private Action<Exception> _serverExceptionHandler;
        private Action<Exception> _clientExceptionHandler;
        private readonly Dictionary<Type, object> _delegateMap;
        public ResponseHandler()
        {
            _delegateMap = new Dictionary<Type, object>();
        }
        
        /// <summary>
        /// This function sets the handler of the action when a successful response is received.
        /// </summary>
        /// <typeparam name="T">Type of object to be handled.</typeparam>
        /// <param name="successHandler">Action to perform on object type T when a successful response
        /// is received.</param>
        public void OnSuccess<T>(Action<T> successHandler)
        {
            if (!_delegateMap.ContainsKey(typeof(T)))
            {
                _delegateMap[typeof(T)] = successHandler;
            }
        }

        /// <summary>
        /// This function sets the handler of the action when a server error occurs on performing the request.
        /// </summary>
        /// <param name="serverExceptionHandler">Action to perform on the exception that occurs.</param>
        public void OnServerError(Action<Exception> serverExceptionHandler)
        {
            _serverExceptionHandler = serverExceptionHandler;
        }

        /// <summary>
        /// This function sets the handler of the action when a client error occurs on performing the request.
        /// </summary>
        /// <param name="clientExceptionHandler">Action to perform on the exception that occurs.</param>
        public void OnClientError(Action<Exception> clientExceptionHandler)
        {
            _clientExceptionHandler = clientExceptionHandler;
        }

        /// <summary>
        /// This function sets the handler of the action when a client error occurs on performing the request.
        /// </summary>
        /// <param name="responseMessage">The <see cref="HttpResponseMessage">message</see> that is received as
        /// a response for the request</param>
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
                 
                var responseString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                var returnObject = JsonConvert.DeserializeObject<T>(responseString);
                InvokeSuccessAction(returnObject);

            }
            catch (Exception e)
            {
                _clientExceptionHandler(e);
            }
            
        }

        /// <summary>
        /// Invoke the set action for the object.
        /// </summary>
        /// <typeparam name="T">Object type being received</typeparam>
        /// <param name="item">Object to be performed action on.</param>
        private void InvokeSuccessAction<T>(T item)
        {
            Action<T> handler = GetSuccessAction<T>(typeof(T));
            handler(item);
        }

        /// <summary>
        /// Retrieve the action/handler for the object type.
        /// </summary>
        /// <typeparam name="T">Object type being handled</typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        private Action<T> GetSuccessAction<T>(Type type)
        {
            if (!_delegateMap.TryGetValue(type, out var function))
                return default;

            Action<T> handler = (Action<T>)function;
            return handler;
        }

    }
}