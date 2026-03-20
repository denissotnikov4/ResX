using MediatR;

namespace ResX.Transactions.Application.Commands.DisputeTransaction;

public record DisputeTransactionCommand(
    Guid TransactionId,
    Guid RequestingUserId) : IRequest<Unit>;
