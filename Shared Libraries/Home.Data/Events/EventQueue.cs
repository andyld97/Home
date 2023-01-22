using System;
using System.Collections.Generic;

namespace Home.Data.Events
{
    public class EventQueue
    {
        public string ClientID { get; set; }

        public DateTime LastEvent { get; set; } = DateTime.MinValue;

        public DateTime LastClientRequest { get; set; } = DateTime.MinValue;

        public Queue<EventQueueItem> Events { get; set; } = new Queue<EventQueueItem>();

        public List<EventQueueItem> LastAck { get; set; } = new List<EventQueueItem>();

        public override string ToString()
        {
            return $"e{ClientID}";
        }
    }
}
