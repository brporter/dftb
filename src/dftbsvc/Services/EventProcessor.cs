using System;
using System.Threading;
using System.Threading.Tasks;

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
        readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);

        public EventProcessor(ICommandGenerator commandGenerator)
        {
            _generator = commandGenerator ?? throw new ArgumentNullException(nameof(commandGenerator));
        }

        public Task ProcessEventsAsync<T>(IItemRepository repository, IQueueService<T> queueService)
        {
            if (_resetEvent.WaitOne(0))
            {
                // process events
                return Task.Run( async () => {
                    try 
                    {
                        var queueItems = await queueService.DequeueAsync();

                        foreach (var queueItem in queueItems)
                        {
                            var command = _generator.GenerateCommand<T>(repository, queueItem.Item);
                            
                            await command.ExecuteAsync(queueItem.Item, async (item, success) => {
                                if (success)
                                {
                                    await queueService.DeleteItemAsync(queueItem);
                                }
                                else
                                {
                                    await queueService.EnqueueAsync(queueItem.Item);
                                }
                            });
                        }
                    } 
                    finally 
                    {
                        _resetEvent.Set();
                    }
                });
            }

            return Task.CompletedTask;
        }
    }
}