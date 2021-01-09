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
        Task RegisterItemJournalEntryAsync(Guid accountId, ItemEvent itemEvent);
        Task<IEnumerable<ItemTemplate>> GetItemTemplatesAsync(Guid accountId, DateTime since);
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

        public DbItemRepository(ILogger<DbItemRepository> logger, DbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task RegisterItemJournalEntryAsync(Guid accountId, ItemEvent itemEvent)
        {
            using var connection = _context.GetConnection();

            var recordCount = await connection.ExecuteAsync(@"
                INSERT INTO [UserData].[ItemJournal]
                    (JournalId, OperationId, ItemId, ItemTemplateId, DemandQuantity, AcquiredQuantity, JournalEntryCreated)
                VALUES
                    (@JournalId, @Operation, @ItemId, @ItemTemplateId, @DemandQuantity, @AcquiredQuantity, )
            ",
            itemEvent);
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

/*        public async Task RegisterItemTemplateJournalEntryAsync(ItemTemplateEvent templateEvent)
        {

        }*/

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