using MediatR;
using ResX.Transactions.Application.DTOs;

namespace ResX.Transactions.Application.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>;