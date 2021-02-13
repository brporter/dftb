using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dftbsvc.Models;
using dftbsvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        public ListController(ILogger<ListController> logger, IItemRepository itemRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        }

        [HttpGet("itemlist")]
        [Authorize]
        public async Task<ItemList> GetItemListAsync(Guid accountId)
        {
            var itemList = await _repository.GetItemListAsync(accountId);

            return itemList;
        }

        [HttpGet("systemItemTemplates")]
        [Authorize]
        public async Task<IEnumerable<ItemTemplate>> GetSystemItemTemplatesAsync(DateTime since)
        {
            return await _repository.GetItemTemplatesAsync(Guid.Empty, since);
        }

        [HttpGet("itemTemplates")]
        [Authorize]
        public async Task<IEnumerable<ItemTemplate>> GetItemTemplatesAsync(Guid accountId, DateTime since)
        {
            return await _repository.GetItemTemplatesAsync(accountId, since);
        }

        [HttpPost("addItem")]
        [Authorize]
        public async Task AddItemAsync(Guid accountId, Guid itemTemplateId, Guid listId, int demandQuantity, int acquiredQuantity)
        {
            var itemEvent = new ItemEvent() 
            {
                JournalId = IdGenerator.NewId(),
                ItemId = IdGenerator.NewId(), 
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