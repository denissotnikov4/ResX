using ResX.Common.Domain;

namespace ResX.Transactions.Domain.Events;

public record TransactionCancelledDomainEvent(Guid TransactionId) : DomainEvent;