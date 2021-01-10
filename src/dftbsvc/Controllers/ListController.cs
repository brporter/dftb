using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dftbsvc.Models;
using dftbsvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace dftbsvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ListController
        : ControllerBase
    {
        readonly ILogger<ListController> _logger;
        readonly IItemRepository _repository;
        readonly IEventProcessor _eventProcessor;

        public ListController(ILogger<ListController> logger, IItemRepository itemRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        }

        [HttpGet("itemlist")]
        public async Task<ItemList> GetItemListAsync(Guid accountId)
        {
            var itemList = await _repository.GetItemListAsync(accountId);

            return itemList;
        }

        [HttpGet("systemItemTemplates")]
        public async Task<IEnumerable<ItemTemplate>> GetSystemItemTemplatesAsync(DateTime since)
        {
            return await _repository.GetItemTemplatesAsync(Guid.Empty, since);
        }

        [HttpGet("itemTemplates")]
        public async Task<IEnumerable<ItemTemplate>> GetItemTemplatesAsync(Guid accountId, DateTime since)
        {
            return await _repository.GetItemTemplatesAsync(accountId, since);
        }

        [HttpPost("addItem")]
        public async Task AddItemAsync(Guid accountId, Guid itemTemplateId, Guid listId, int demandQuantity, int acquiredQuantity)
        {
            var itemEvent = new ItemEvent() {
                JournalId = Guid.NewGuid(), // TODO: replace with sequential ID generator
                ItemId = Guid.NewGuid(), // TODO: replace with sequential ID generator
                ListId = listId,
                ItemTemplateId = itemTemplateId,
                DemandQuantity = demandQuantity,
                AcquiredQuantity = acquiredQuantity,
                Operation = Operation.Create
            };

            await _repository.RegisterItemJournalEntryAsync(accountId, itemEvent);
        }
    }
}