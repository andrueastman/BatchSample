using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public class ResponseHandler
    {
        public ViewModel Model { get; }
        public HttpQuery Query { get; }
        private Action<Exception> _serverExceptionHandler;
        private Action<Exception> _clientExceptionHandler;
        private readonly Dictionary<Type, object> delegateMap;

        public ResponseHandler(ViewModel viewModel, HttpQuery query)
        {
            this.Model = viewModel;
            this.Query = query;
        }
        
        internal void OnClientError(Action<Exception> exceptionProcessor)
        {
            _clientExceptionHandler = exceptionProcessor;
        }

        internal void OnSuccess<T>(Action<T> successHandler)
        {
            if (!delegateMap.ContainsKey(typeof(T)))
            {
                delegateMap[typeof(T)] = successHandler;
            }
        }

        public void InvokeSuccessAction<T>(T item)
        {
            object tmp;
            if (delegateMap.TryGetValue(typeof(T), out tmp))
            {
                Action<T> handler = (Action<T>) tmp;
                handler(item);
            }
        }

        public Action<T> GetSuccessAction<T>(Type type)
        {
            object tmp;
            if (delegateMap.TryGetValue(type, out tmp))
            {
                Action<T> handler = (Action<T>)tmp;
                return handler;
            }

            return default;
        }

        internal void OnServerError(Action<Exception> exceptionProcessor)
        {
            _serverExceptionHandler = exceptionProcessor;
        }
    }
}