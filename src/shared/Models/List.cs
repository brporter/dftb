using System;

namespace dftbsvc.Models
{
    public record ItemList
    {
        public Guid ListId { get; set; }
        public Item[] Items { get; set; }
        public ItemTemplate[] Templates { get; set; }
        public Guid AccountId { get; set; }
        public DateTime Retrieved { get; set; }
    }

    public record ItemTemplate
    {
        public Guid TemplateId { get; set; }
        public Guid AccountId { get; set; }
        public string Name { get; set; }
        public Uri ImageUrl { get; set; }
        public string Upc { get; set; }
    }

    public record ItemState
    {
        public Guid StateId { get; set; }
        public Guid ItemId { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime Created { get; set; }
        public DateTime Completed { get; set; }
    }

    public record Item
    {
        public Guid ItemId { get; set; }
        public Guid ListId { get; set; }
        public Guid TemplateId { get; set; }
        public int DemandQuantity { get; set; }
        public int AcquiredQuantity { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime Created { get; set; }
        public DateTime Deleted { get; set; }
    }

    public enum ItemOperation
    {
        None,
        Create,
        Update,
        Delete,
        Complete
    }

    public enum ItemOperationResult
    {
        Success,
        Conflict
    }
}