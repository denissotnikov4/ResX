using ResX.Common.Domain;

namespace ResX.Transactions.Domain.Events;

public record TransactionCreatedDomainEvent(
    Guid TransactionId,
    Guid ListingId,
    Guid DonorId,
    Guid RecipientId) : DomainEvent;