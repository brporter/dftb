using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace dftbsvc.Services
{
    public interface IEventProcessor
    {
        Task ProcessEventsAsync<T>(IItemRepository repository, IQueueService<T> queueService);
    }

    public class EventProcessor
        : IEventProcessor
    {
        readonly ICommandGenerator _generator;
        readonly ILogger _log;
        readonly IApplicationTelemetry _telemetry;

        public EventProcessor(ILogger<EventProcessor> log, ICommandGenerator commandGenerator, IApplicationTelemetry telemetry)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _generator = commandGenerator ?? throw new ArgumentNullException(nameof(commandGenerator));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async Task ProcessEventsAsync<T>(IItemRepository repository, IQueueService<T> queueService)
        {
            using (_log.BeginScope("Processing [{0}] Queue Items", queueService.Name))
            {
                var queueItems = await queueService.DequeueAsync();

                _telemetry.TrackQueueItems(QueueItemOperation.Retrieved, queueService.Name, queueItems.Count());

                foreach (var queueItem in queueItems)
                {
                    try
                    {
                        var command = _generator.GenerateCommand(repository, queueItem.Item);

                        if (await command.ExecuteAsync(queueItem.Item))
                        {
                            await queueService.DeleteItemAsync(queueItem);

                            _telemetry.TrackQueueItems(QueueItemOperation.Processed, queueService.Name);
                        }
                    }
                    catch (System.Data.Common.DbException dbe)
                    {
                        _log.LogError(dbe, "Queue Item Command Execution Failed");
                        _telemetry.TrackQueueItems(QueueItemOperation.Failed, queueService.Name);
                    }
                }
            }
        }
    }
}