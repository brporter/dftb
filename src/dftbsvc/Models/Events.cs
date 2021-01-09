using System;

namespace dftbsvc.Models
{
    public enum Operation
    {
        None,
        Create,
        Update,
        Delete
    }

    public abstract record Event
    {
        public Event()
        {
            Created = DateTime.UtcNow;
        }

        public Guid JournalId { get; init; }
        public Operation Operation { get; init; }
        public DateTime Created { get; }
    }

    public record ItemTemplateEvent
        : Event
    {
        public Guid ItemTemplateId { get; init; }
        public string Name { get; init; }
        public Uri ImageUrl { get; init; }
        public string Upc { get; init; }
    }

    public record ItemEvent
        : Event
    {
        public Guid ItemId { get; init; }
        public Guid ItemTemplateId { get; init; }
        public int DemandQuantity { get; init; }
        public int AcquiredQuantity { get; init; }
    }
}