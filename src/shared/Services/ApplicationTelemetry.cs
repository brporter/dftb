using System;

namespace dftbsvc.Services
{
    public enum QueueItemOperation
    {
        Retrieved,
        Processed,
        Failed
    }

    public interface IApplicationTelemetry
    {
        void TrackQueueItems(QueueItemOperation operation, string queueName, long count);
        void TrackQueueItems(QueueItemOperation operation, string queueName);
    }
}
