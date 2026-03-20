using MediatR;

namespace ResX.Transactions.Application.Commands.CancelTransaction;

public record CancelTransactionCommand(
    Guid TransactionId,
    Guid RequestingUserId) : IRequest<Unit>;