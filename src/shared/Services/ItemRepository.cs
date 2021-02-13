using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using dftbsvc.Models;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Data.Common;

namespace dftbsvc.Services
{
    public interface IItemRepository
    {
        Task<ItemList> GetItemListAsync(Guid accountId);
        Task<bool> RegisterItemJournalEntryAsync(Guid accountId, ItemEvent itemEvent);
        Task<bool> RecordItemJournalEntryAsync(Guid accountId, ItemEvent itemEvent);
        Task<bool> RegisterItemTemplateJournalEntryAsync(Guid accountId, ItemTemplateEvent templateEvent);
        Task<IEnumerable<ItemTemplate>> GetItemTemplatesAsync(Guid accountId, DateTime since);

        Task<bool> CreateItemAsync(ItemEvent itemEvent);
        Task<bool> DeleteItemAsync(ItemEvent itemEvent);
        Task<bool> UpdateItemAsync(ItemEvent itemEvent);

        Task<bool> CreateItemTemplateAsync(ItemTemplateEvent templateEvent);
        Task<bool> DeleteItemTemplateAsync(ItemTemplateEvent templateEvent);
        Task<bool> UpdateItemTemplateAsync(ItemTemplateEvent templateEvent);
    }

    public class DbContext
    {
        public DbProviderFactory Factory { get; set; }
        public string ConnectionString { get; set; }

        public DbConnection GetConnection()
        {
            var connection = Factory.CreateConnection();
            connection.ConnectionString = ConnectionString;

            return connection;
        }
    }

    public class DbItemRepository
        : IItemRepository
    {
        readonly ILogger<DbItemRepository> _logger;
        readonly DbContext _context;
        readonly IQueueService<ItemEvent> _itemEventQueue;
        readonly IQueueService<ItemTemplateEvent> _itemTemplateEventQueue;

        private class UriTypeHandler
            : SqlMapper.TypeHandler<Uri>
        {
            public override Uri Parse(object value)
            {
                var s = value.ToString();

                if (!string.IsNullOrWhiteSpace(s))
                    return new Uri(s);

                return null;
            }

            public override void SetValue(IDbDataParameter parameter, Uri value)
            {
                parameter.Value = value.ToString();
            }
        }

        static DbItemRepository()
        {
            SqlMapper.AddTypeHandler<Uri>(new UriTypeHandler());
        }

        public DbItemRepository(
            ILogger<DbItemRepository> logger, 
            DbContext context, 
            IQueueService<ItemEvent> itemQueue,
            IQueueService<ItemTemplateEvent> templateQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            _itemEventQueue = itemQueue ?? throw new ArgumentNullException(nameof(itemQueue));
            _itemTemplateEventQueue = templateQueue ?? throw new ArgumentNullException(nameof(templateQueue));
        }

        public async Task<bool> CreateItemAsync(ItemEvent itemEvent)
        {
            // TODO: These methods all need to assume that itemEvent is possible poorly ordered; if the event
            // is older than the latest edit to the record, it needs to take the contextually correct action.
            // Ignoring an event is *success*, and we should return true in that case, even though affectedRecords will likely be zero.

            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            bool eventRecorded = false;

            try 
            {
                eventRecorded = await this.RecordItemJournalEntryAsync(Guid.NewGuid(), itemEvent);
            }
            catch (System.Data.Common.DbException de)
            {
                _logger.LogError($"A failure was encountered while recording item event with identifier {itemEvent.ItemId}: {de}");
                return false;
            }

            if (!eventRecorded)
                return true; // our event is out of order, a later write has already occurred

            try 
            {
                var affectedRecords = await connection.ExecuteAsync("[UserData].[CreateItem]", 
                    new {
                        ItemId = itemEvent.ItemId,
                        ListId = itemEvent.ListId,
                        ItemTemplateId = itemEvent.ItemTemplateId,
                        DemandQuantity = itemEvent.DemandQuantity,
                        AcquiredQuantity = itemEvent.AcquiredQuantity
                    }, 
                    null, 
                    null, 
                    CommandType.StoredProcedure);

                return affectedRecords == 1;
            } 
            catch (System.Data.Common.DbException de)
            {
                _logger.LogError($"A failure was encoutnered while processing item event with identifier {itemEvent.ItemId}: {de}");
                return false;
            }
        }

        public async Task<bool> DeleteItemAsync(ItemEvent itemEvent)
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            try 
            {
                var affectedRecords = await connection.ExecuteAsync("[UserData].[DeleteItem]", 
                    new { 
                        ItemId = itemEvent.ItemId 
                    }, 
                    null, 
                    null, 
                    CommandType.StoredProcedure);

                return affectedRecords == 1;
            } 
            catch (System.Data.Common.DbException de)
            {
                _logger.LogError($"A failure was encoutnered while processing item event with identifier {itemEvent.ItemId}: {de}");
                return false;
            }
        }

        public async Task<bool> UpdateItemAsync(ItemEvent itemEvent)
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            try 
            {
                var affectedRecords = await connection.ExecuteAsync("[UserData].[UpdateItem]", 
                    new {
                        ItemId = itemEvent.ItemId,
                        ListId = itemEvent.ListId,
                        ItemTemplateId = itemEvent.ItemTemplateId,
                        DemandQuantity = itemEvent.DemandQuantity,
                        AcquiredQuantity = itemEvent.AcquiredQuantity
                    }, 
                    null, 
                    null, 
                    CommandType.StoredProcedure);

                return affectedRecords == 1;
            } 
            catch (System.Data.Common.DbException de)
            {
                _logger.LogError($"A failure was encoutnered while processing item event with identifier {itemEvent.ItemId}: {de}");
                return false;
            }
        }

        public Task<bool> CreateItemTemplateAsync(ItemTemplateEvent templateEvent)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteItemTemplateAsync(ItemTemplateEvent templateEvent)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateItemTemplateAsync(ItemTemplateEvent templateEvent)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RecordItemJournalEntryAsync(Guid accountId, ItemEvent itemEvent)
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            var eventParameters = new DynamicParameters();
            eventParameters.AddDynamicParams(itemEvent);

            var affectedRecords = await connection.ExecuteAsync(
                "[Journal].[RegisterItemEvent]", 
                param:eventParameters, 
                commandType:CommandType.StoredProcedure)
                .ConfigureAwait(false);

            return affectedRecords == 1;
        }

        public async Task<bool> RegisterItemJournalEntryAsync(Guid accountId, ItemEvent itemEvent)
        {
            await _itemEventQueue.EnqueueAsync(itemEvent).ConfigureAwait(false);

            return true;
        }

        public async Task<bool> RegisterItemTemplateJournalEntryAsync(Guid accountId, ItemTemplateEvent templateEvent)
        {
            using var connection = _context.GetConnection();

            var recordCount = await connection.ExecuteAsync(@"
                INSERT INTO [Journal].[ItemTemplate]
                    (JournalId, OperationId, ItemTemplateId, Name, ImageUrl, UPC, Created)
                VALUES
                    (@JournalId, @Operation, @ItemTemplateId, @Name, @ImageUrl, @Upc, @Created)
            ",
            templateEvent);

            return recordCount == 1;
        }

        public async Task<IEnumerable<ItemTemplate>> GetItemTemplatesAsync(Guid accountId, DateTime since)
        {
            using var connection = _context.GetConnection();

            var itemTemplates = await connection.QueryAsync<ItemTemplate>(@"
                SELECT 
                    ItemTemplateId AS TemplateId, AccountId, Name, ImageUrl, UPC, Created
                FROM
                    [UserData].[ItemTemplate]
                WHERE
                    (AccountId = @AccountId OR AccountId = @SystemAccountId)
                    AND
                    Created >= @Since
            ",
            new {
                AccountId = accountId,
                SystemAccountId = Guid.Empty,
                Since = since
            });

            return itemTemplates;
        }

        public async Task<ItemList> GetItemListAsync(Guid accountId)
        {
            using var connection = _context.GetConnection();

            var reader = await connection.QueryMultipleAsync(@"
                SELECT List.*
                FROM
                    [UserData].[List] List
                WHERE
                    List.AccountId = @AccountId

                SELECT ItemTemplate.*
                FROM
                    [UserData].[ItemTemplate] ItemTemplate
                    INNER JOIN [UserData].[Item] Item on Item.TemplateId = ItemTemplate.ItemTemplateId
                    INNER JOIN [UserData].[List] List on List.ListId = Item.ListId AND List.AccountId = @AccountId

                SELECT Item.*
                FROM
                    [UserData].[Item] Item
                    INNER JOIN [UserData].[List] List ON List.ListId = Item.ListId AND List.AccountId = @AccountId
            ", new { AccountId = accountId });

            var list = (await reader.ReadAsync<ItemList>()).Single();
            var itemTemplates = await reader.ReadAsync<ItemTemplate>();
            var items = await reader.ReadAsync<Item>();

            list.Items = items.ToArray();
            list.Templates = itemTemplates.ToArray();
            list.Retrieved = DateTime.UtcNow;

            return list;
        }
    }
}