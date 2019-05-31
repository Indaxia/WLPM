using System.Collections.Concurrent;
using System.Collections.Generic;

namespace wlpm
{
    public delegate void executionCallback();

    public class DelayedBusyState
    {
        ConcurrentDictionary<string, executionCallback> handlers;

        public bool isBusy {
            get{ return _isBusy; }
            set{
                _isBusy = value;
                if(!value) {
                    invokeAll();
                }
            }
        }
        private bool _isBusy = false;  

        public DelayedBusyState()
        {
            handlers = new ConcurrentDictionary<string, executionCallback>();
        }

        /**
         * Executes the specified function when the instance is free. If it's already free executes in moment
         */
        public void invokeASAP(string uniqueId, executionCallback handler)
        {
            executionCallback h = null;
            if(handlers.ContainsKey(uniqueId)) {
                handlers.Remove(uniqueId, out h);
            }
            handlers.AddOrUpdate(uniqueId, handler, (k,v) => {
                return v;
            });

            if(! _isBusy) {
                invokeAll();
            }
        }

        private void invokeAll()
        {
            executionCallback h = null;
            foreach(KeyValuePair<string, executionCallback> handler in handlers) {
                handlers.Remove(handler.Key, out h);
                handler.Value.Invoke();
            }
        }
    }
}