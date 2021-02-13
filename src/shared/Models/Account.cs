using System;

namespace dftbsvc.Models {
    public record Account {
        public Guid AccountId { get; set; }
        public string EmailAddress { get; set; }
    }
}