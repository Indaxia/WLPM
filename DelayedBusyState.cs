using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace wlpm
{
    public delegate void executionCallback();

    public class DelayedBusyState
    {
        ConcurrentDictionary<string, executionCallback> asapHandlers;

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
            asapHandlers = new ConcurrentDictionary<string, executionCallback>();
        }

        /**
         * Executes the specified function when the instance is free. If it's already free executes in moment
         */
        public void invokeASAP(string uniqueId, executionCallback handler)
        {
            executionCallback h = null;
            if(asapHandlers.ContainsKey(uniqueId)) {
                asapHandlers.Remove(uniqueId, out h);
            }
            asapHandlers.AddOrUpdate(uniqueId, handler, (k,v) => {
                return v;
            });

            if(! _isBusy) {
                invokeAll();
            }
        }

        public void UnsubscribeASAPEvent(string uniqueId)
        {
            executionCallback h = null;
            if(asapHandlers.ContainsKey(uniqueId)) {
                asapHandlers.Remove(uniqueId, out h);
            }
        }

        public bool IsSubscribedOnASAP(string uniqueId)
        {
            return asapHandlers.ContainsKey(uniqueId);
        }

        private void invokeAll()
        {
            executionCallback h = null;
            foreach(KeyValuePair<string, executionCallback> handler in asapHandlers) {
                asapHandlers.Remove(handler.Key, out h);
                handler.Value.Invoke();
            }
        }
    }
}