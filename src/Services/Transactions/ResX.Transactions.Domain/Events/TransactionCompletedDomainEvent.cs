using ResX.Common.Domain;

namespace ResX.Transactions.Domain.Events;

public record TransactionCompletedDomainEvent(
    Guid TransactionId,
    Guid DonorId,
    Guid RecipientId) : DomainEvent;