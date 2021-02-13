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
                            try 
                            {
                                var command = _generator.GenerateCommand<T>(repository, queueItem.Item);
                                
                                if (await command.ExecuteAsync(queueItem.Item))
                                {
                                    await queueService.DeleteItemAsync(queueItem);
                                }
                            }
                            catch (System.Data.Common.DbException)
                            { }
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