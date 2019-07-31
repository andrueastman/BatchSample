using System;
using System.Collections.Generic;

namespace EnhancedBatch
{
    public class ResponseHandler
    {
        public HttpQuery Query { get; }
        private Action<Exception> _serverExceptionHandler;
        private Action<Exception> _clientExceptionHandler;
        private readonly Dictionary<Type, object> _delegateMap;

        public ResponseHandler(HttpQuery query)
        {
            Query = query;
            _delegateMap = new Dictionary<Type, object>();
        }
        
        internal void OnSuccess<T>(Action<T> successHandler)
        {
            if (!_delegateMap.ContainsKey(typeof(T)))
            {
                _delegateMap[typeof(T)] = successHandler;
            }
        }

        public void InvokeSuccessAction<T>(T item)
        {
            Action<T> handler = GetSuccessAction<T>(typeof(T));
            try
            {
                handler(item);
            }
            catch (Exception e)
            {
                _serverExceptionHandler(e);
                _clientExceptionHandler(e);
            }
        }

        public Action<T> GetSuccessAction<T>(Type type)
        {
            if (_delegateMap.TryGetValue(type, out var function))
            {
                Action<T> handler = (Action<T>)function;
                return handler;
            }

            return default;
        }

        internal void OnServerError(Action<Exception> serverExceptionHandler)
        {
            _serverExceptionHandler = serverExceptionHandler;
        }

        internal void OnClientError(Action<Exception> clientExceptionHandler)
        {
            _clientExceptionHandler = clientExceptionHandler;
        }
    }
}