using System;
using System.Threading;
using System.Threading.Tasks;
using dftbsvc.Models;
using dftbsvc.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;

namespace dftb.EventProc
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        static IConfiguration Configuration { get; set; }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                    Configuration = builder.Build()
                )
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<EventWorker>();

                    services.AddSingleton<IQueueService<ItemEvent>, AzureItemEventQueueService>(
                        (_) => {
                            return new AzureItemEventQueueService(Configuration["ConnectionStrings:storage"]);
                        }
                    );

                    services.AddSingleton<IQueueService<ItemTemplateEvent>, AzureItemTemplateEventQueueService>(
                        (_) => new AzureItemTemplateEventQueueService(Configuration["ConnectionStrings:storage"])
                    );

                    services.AddSingleton<ICommandGenerator, CommandGenerator>();

                    services.AddSingleton<DbContext>((serviceProvider) => new DbContext()
                    {
                        Factory = System.Data.SqlClient.SqlClientFactory.Instance,
                        ConnectionString = Configuration["ConnectionStrings:dftb"]
                    });

                    services.AddSingleton<IItemRepository, DbItemRepository>();

                    services.AddSingleton<IEventProcessor, EventProcessor>();

                    services.AddSingleton<IApplicationTelemetry, AzureAppInsightsTelemetry>();
                    services.AddApplicationInsightsTelemetryWorkerService();
                });
    }

    class EventWorker
        : BackgroundService
    {
        readonly IEventProcessor _processor;
        readonly IQueueService<ItemEvent> _itemEventQueueService;
        readonly IQueueService<ItemTemplateEvent> _itemTemplateEventQueueService;
        readonly IItemRepository _itemRepository;
        readonly IHostApplicationLifetime _hal;

        readonly ILogger _log;

        public EventWorker(
            ILogger<EventWorker> log,
            IEventProcessor processor,
            IItemRepository itemRepository,
            IQueueService<ItemEvent> itemEventQueueService,
            IQueueService<ItemTemplateEvent> itemTemplateEventQueueService,
            IHostApplicationLifetime hal)
        {
            _log = log;

            _itemEventQueueService = itemEventQueueService;
            _itemTemplateEventQueueService = itemTemplateEventQueueService;

            _processor = processor;
            _itemRepository = itemRepository;

            _hal = hal;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (_log.BeginScope("Starting EventWorker"))
            {
                return Task.Run(async () =>
                {
                    // TODO: need to support inter-processing cancellation

                    using (_log.BeginScope("Processing Queue Events"))
                    {
                        _log.LogInformation("Processing item events.");

                        if (!stoppingToken.IsCancellationRequested)
                            await _processor.ProcessEventsAsync(_itemRepository, _itemEventQueueService);
                        else
                        {
                            _log.LogWarning("Cancellation requested prior to processing the item event queue.");
                            return;
                        }

                        _log.LogInformation("Processing item template events.");

                        if (!stoppingToken.IsCancellationRequested)
                            await _processor.ProcessEventsAsync(_itemRepository, _itemTemplateEventQueueService);
                        else
                        {
                            _log.LogWarning("Cancellation requested prior to processing the item template event queue.");
                            return;
                        }
                    }

                    _hal.StopApplication();
                });
            }
        }
    }
}
