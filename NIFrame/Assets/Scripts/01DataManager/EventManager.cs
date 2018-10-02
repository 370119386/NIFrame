using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NI
{
    public delegate void EventHandler(object argv);

    class EventManager : Singleton<EventManager>
    {
        protected Dictionary<Event, EventHandler> mEventDispatcher = new Dictionary<Event, EventHandler>();

        public void RegisterEvent(Event eEvent,EventHandler handler)
        {
            if(!mEventDispatcher.ContainsKey(eEvent))
            {
                mEventDispatcher.Add(eEvent, handler);
            }
            else
            {
                mEventDispatcher[eEvent] = System.Delegate.Combine(mEventDispatcher[eEvent], handler) as EventHandler;
            }
        }

        public void UnRegisterEvent(Event eEvent, EventHandler handler)
        {
            if(mEventDispatcher.ContainsKey(eEvent))
            {
                mEventDispatcher[eEvent] = System.Delegate.Remove(mEventDispatcher[eEvent], handler) as EventHandler;
            }
        }

        public void SendEvent(Event eEvent,object argv = null)
        {
            if(mEventDispatcher.ContainsKey(eEvent))
            {
                var handler = mEventDispatcher[eEvent];
                if(null != handler)
                {
                    handler.Invoke(argv);
                }
            }
        }
    }
}