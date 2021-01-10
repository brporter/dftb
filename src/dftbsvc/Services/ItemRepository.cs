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
        Task<bool> RegisterItemTemplateJournalEntryAsync(Guid accountId, ItemTemplateEvent templateEvent);
        Task<IEnumerable<ItemTemplate>> GetItemTemplatesAsync(Guid accountId, DateTime since);

        Task CreateItemAsync(ItemEvent itemEvent, Action<ItemEvent, bool> postAction);
        Task DeleteItemAsync(ItemEvent itemEvent, Action<ItemEvent, bool> postAction);
        Task UpdateItemAsync(ItemEvent itemEvent, Action<ItemEvent, bool> postAction);

        Task CreateItemTemplateAsync(ItemTemplateEvent templateEvent, Action<ItemTemplateEvent, bool> postAction);
        Task DeleteItemTemplateAsync(ItemTemplateEvent templateEvent, Action<ItemTemplateEvent, bool> postAction);
        Task UpdateItemTemplateAsync(ItemTemplateEvent templateEvent, Action<ItemTemplateEvent, bool> postAction);
        
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
        readonly IEventProcessor _eventProcessor;
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
            IEventProcessor eventProcessor,
            IQueueService<ItemEvent> itemQueue,
            IQueueService<ItemTemplateEvent> templateQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
            _itemEventQueue = itemQueue ?? throw new ArgumentNullException(nameof(itemQueue));
            _itemTemplateEventQueue = templateQueue ?? throw new ArgumentNullException(nameof(templateQueue));
        }

        public async Task CreateItemAsync(ItemEvent itemEvent, Action<ItemEvent, bool> postAction)
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            DbTransaction transaction = null;
            var postActionRan = false;

            try 
            {
                transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
                var affectedRecords = await connection.ExecuteAsync("[UserData].[CreateItem]", itemEvent, transaction, null, CommandType.StoredProcedure);

                if (affectedRecords == 1)
                {
                    postAction(itemEvent, true);
                    postActionRan = true;
                }

                await transaction.CommitAsync();
            } 
            catch 
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                if (postActionRan)
                    postAction(itemEvent, false);
                
                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task DeleteItemAsync(ItemEvent itemEvent, Action<ItemEvent, bool> postAction)
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            DbTransaction transaction = null;
            var postActionRan = false;

            try 
            {
                transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
                var affectedRecords = await connection.ExecuteAsync("[UserData].[DeleteItem]", new { ItemId = itemEvent.ItemId }, transaction, null, CommandType.StoredProcedure);

                if (affectedRecords == 1)
                {
                    postAction(itemEvent, true);
                    postActionRan = true;
                }

                await transaction.CommitAsync();
            } 
            catch 
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                if (postActionRan)
                    postAction(itemEvent, false);
                
                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task UpdateItemAsync(ItemEvent itemEvent, Action<ItemEvent, bool> postAction)
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            DbTransaction transaction = null;
            var postActionRan = false;

            try 
            {
                transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
                var affectedRecords = await connection.ExecuteAsync("[UserData].[UpdateItem]", itemEvent, transaction, null, CommandType.StoredProcedure);

                if (affectedRecords == 1)
                {
                    postAction(itemEvent, true);
                    postActionRan = true;
                }

                await transaction.CommitAsync();
            } 
            catch 
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                if (postActionRan)
                    postAction(itemEvent, false);
                
                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task CreateItemTemplateAsync(ItemTemplateEvent templateEvent, Action<ItemTemplateEvent, bool> postAction)
        {
            throw new NotImplementedException();
        }

        public Task DeleteItemTemplateAsync(ItemTemplateEvent templateEvent, Action<ItemTemplateEvent, bool> postAction)
        {
            throw new NotImplementedException();
        }

        public Task UpdateItemTemplateAsync(ItemTemplateEvent templateEvent, Action<ItemTemplateEvent, bool> postAction)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RegisterItemJournalEntryAsync(Guid accountId, ItemEvent itemEvent)
        {
            using var connection = _context.GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);

            var returnValue = false;
            var eventParameters = new DynamicParameters();
            eventParameters.AddDynamicParams(itemEvent);

            DbTransaction transaction = null;

            try 
            {
                transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);
                var affectedRecords = await connection.ExecuteAsync("[Journal].[RegisterItemEvent]", transaction:transaction, param:eventParameters, commandType:CommandType.StoredProcedure).ConfigureAwait(false);

                if (affectedRecords == 1)
                {
                    await _itemEventQueue.EnqueueAsync(itemEvent).ConfigureAwait(false);
                }

                await transaction.CommitAsync().ConfigureAwait(false);
                _ = _eventProcessor.ProcessEventsAsync(this, _itemEventQueue);
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync().ConfigureAwait(false);
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync().ConfigureAwait(false);
            }



            return returnValue;
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