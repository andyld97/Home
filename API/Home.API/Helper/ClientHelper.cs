using Home.Data.Events;
using System;
using System.Linq;

namespace Home.API.Helper
{
    public static class ClientHelper
    {
        /// <summary>
        /// Notifies all active client queues that there is a new event for the device
        /// </summary>
        /// <param name="eventKind"></param>
        /// <param name="device"></param>
        public static void NotifyClientQueues(EventQueueItem.EventKind eventKind, Home.Model.Device device)
        {
            var now = DateTime.Now;

            lock (Program.EventQueues)
            {
                foreach (var queue in Program.EventQueues)
                {
                    queue.LastEvent = now;
                    var item = new EventQueueItem() { DeviceID = device.ID, EventDescription = eventKind, EventOccured = now, EventData = new EventData(device) };
                    if (eventKind != EventQueueItem.EventKind.ACK)
                        queue.Events.Enqueue(item);
                    else
                    {
                        var ack = queue.LastAck.FirstOrDefault(p => p.DeviceID == device.ID);
                        if (ack == null)
                            queue.LastAck.Add(item);
                        else
                        {
                            queue.LastAck.Remove(ack);
                            queue.LastAck.Add(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Notifies all active client queues that there is a new event for the device
        /// </summary>
        /// <param name="eventKind"></param>
        /// <param name="device"></param>
        public static void NotifyClientQueues(EventQueueItem.EventKind eventKind, home.Models.Device device)
        {
            NotifyClientQueues(eventKind, ModelConverter.ConvertDevice(device));
        }

        /// <summary>
        /// Notifies all active client queues that there is a new event for the device
        /// </summary>
        /// <param name="eventKind"></param>
        /// <param name="deviceId"></param>
        public static void NotifyClientQueues(EventQueueItem.EventKind eventKind, string deviceId)
        {
            var now = DateTime.Now;

            lock (Program.EventQueues)
            {
                foreach (var queue in Program.EventQueues)
                {
                    queue.LastEvent = now;
                    queue.Events.Enqueue(new EventQueueItem() { DeviceID = deviceId, EventDescription = eventKind, EventOccured = now });
                }
            }
        }
    }
}
