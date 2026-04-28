using System;
using System.Collections.Generic;

namespace HTags.EventBus
{
    public static class HTagEventBus
    {
        private static readonly Dictionary<Type, Action[]> TagEventsBusses = new ();
        private static readonly Dictionary<Type, Action<EventArgs>[]> TagEventsBussesWithArgs = new ();
            
        public static void AddListener(IHTag tag, Action action)
        {
            if (TagEventsBusses.TryGetValue(tag.GetType(), out var tagEvents))
            {
                tagEvents[tag.TagIDs[0]] += action;
            }

            InitBus(tag.GetType(), tag.AllTagsCount)[tag.TagIDs[0]] += action;
        }
        
        public static void Raise(IHTag tag)
        {
            if (TagEventsBusses.TryGetValue(tag.GetType(), out var tagEvents))
            {
                for (int i = tag.TagIDs.Length - 1; i >= 0; --i)
                {
                    tagEvents[tag.TagIDs[i]].Invoke();
                }
            }
        }
        
        public static void RemoveListener(IHTag tag, Action action)
        {
            if (TagEventsBusses.TryGetValue(tag.GetType(), out var tagEvents))
            {
                tagEvents[tag.TagIDs[0]] -= action;
            }
        }
        
        public static void AddListener(IHTag tag, Action<EventArgs> action)
        {
            if (TagEventsBussesWithArgs.TryGetValue(tag.GetType(), out var tagEvents))
            {
                tagEvents[tag.TagIDs[0]] += action;
            }

            InitBusWithArgs(tag.GetType(), tag.AllTagsCount)[tag.TagIDs[0]] += action;
        }
        
        public static void Raise(IHTag tag, EventArgs e)
        {
            if (TagEventsBussesWithArgs.TryGetValue(tag.GetType(), out var tagEvents))
            {
                for (int i = tag.TagIDs.Length - 1; i >= 0; --i)
                {
                    tagEvents[tag.TagIDs[i]].Invoke(e);
                }
            }
        }
        
        public static void RemoveListener(IHTag tag, Action<EventArgs> action)
        {
            if (TagEventsBussesWithArgs.TryGetValue(tag.GetType(), out var tagEvents))
            {
                tagEvents[tag.TagIDs[0]] -= action;
            }
        }
        
        private static Action[] InitBus(Type tagType, int size)
        {
            var tagEvents = new Action[size];
            for (int i = size -1; i >= 0; --i)
            {
                tagEvents[i] = IdleAction;
            }

            return TagEventsBusses[tagType] = tagEvents;
        }
        
        private static void IdleAction() { }
        
        private static Action<EventArgs>[] InitBusWithArgs(Type tagType, int size)
        {
            var tagEvents = new Action<EventArgs>[size];
            for (int i = size -1; i >= 0; --i)
            {
                tagEvents[i] = IdleActionWitArgs;
            }

            return TagEventsBussesWithArgs[tagType] = tagEvents;
        }
        
        private static void IdleActionWitArgs(EventArgs e) { }
    }
}