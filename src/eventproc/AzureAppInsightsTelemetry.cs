using System;
using dftbsvc.Services;
using Microsoft.ApplicationInsights;

namespace dftb.EventProc
{
    public class AzureAppInsightsTelemetry
        : dftbsvc.Services.IApplicationTelemetry
    {
        TelemetryClient _client;

        public AzureAppInsightsTelemetry(TelemetryClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public void TrackQueueItems(QueueItemOperation operation, string queueName, long count)
        {
            var m = _client.GetMetric(
                new Microsoft.ApplicationInsights.Metrics.MetricIdentifier(
                    "Queues",
                    queueName,
                    Enum.GetName(operation)
                ));

            m.TrackValue(count, queueName);
        }

        public void TrackQueueItems(QueueItemOperation operation, string queueName)
            => TrackQueueItems(operation, queueName, 1);
    }
}
