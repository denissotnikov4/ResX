using MediatR;

namespace ResX.Transactions.Application.Commands.AgreeTransaction;

public record AgreeTransactionCommand(
    Guid TransactionId,
    Guid DonorId) : IRequest<Unit>;