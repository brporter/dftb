using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using dftbsvc.Models;

namespace dftbsvc.Services
{
    public interface IQueueService<T>
    {
        string Name { get; }
        Task EnqueueAsync(T item);
        Task<IEnumerable<IQueueItem<T>>> DequeueAsync();

        Task<bool> DeleteItemAsync(IQueueItem<T> item);
    }

    public abstract class AzureQueueService<T>
        : IQueueService<T>
    {
        const int MaxMessages = 32;

        static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        readonly QueueServiceClient _serviceClient;
        readonly QueueClient _client;

        public AzureQueueService(string connectionString, string queueName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException(nameof(connectionString));

            _serviceClient = new QueueServiceClient(connectionString);

            Name = queueName;

            _client = _serviceClient.GetQueueClient(queueName);
            _client.CreateIfNotExists();
        }

        private QueueClient Client => _client;

        public string Name { get; }

        public async Task<bool> DeleteItemAsync(IQueueItem<T> item)
        {
            var aqi = item as AzureQueueItem;
            var resp = await Client.DeleteMessageAsync(aqi.MessageId, aqi.PopReceipt);

            return resp.Status == 200;
        }

        public async Task<IEnumerable<IQueueItem<T>>> DequeueAsync()
        {
            var messages = await Client.ReceiveMessagesAsync(MaxMessages);
            var items = new List<IQueueItem<T>>();

            return messages.Value.AsParallel().Select(
                m => new AzureQueueItem(
                    m.Body.ToObjectFromJson<T>(s_serializerOptions), 
                    m.MessageId, 
                    m.PopReceipt))
                .ToArray();
        }

        public async Task EnqueueAsync(T item)
        {
            var messageReceipt = await Client.SendMessageAsync(
                BinaryData.FromObjectAsJson(item, s_serializerOptions)
            );
        }

        private class AzureQueueItem
            : IQueueItem<T>
        {
            public AzureQueueItem(T item, string messageId, string popReceipt)
            {
                Item = item;
                MessageId = messageId;
                PopReceipt = popReceipt;

            }

            public string MessageId { get; }
            public string PopReceipt { get; }
            public T Item { get; }
        }
    }

    public interface IQueueItem<T>
    {
        T Item { get; }
    }

    public sealed class AzureItemEventQueueService
        : AzureQueueService<ItemEvent>
    {
        const string QueueName = "itemeventqueue";

        public AzureItemEventQueueService(string connectionString)
            : base(connectionString, QueueName)
        { }
    }

    public sealed class AzureItemTemplateEventQueueService
        : AzureQueueService<ItemTemplateEvent>
    {
        const string QueueName = "itemtemplateeventqueue";

        public AzureItemTemplateEventQueueService(string connectionString)
            : base(connectionString, QueueName)
        { }
    }
}