using MediatR;
using ResX.Transactions.Domain.Enums;

namespace ResX.Transactions.Application.Commands.CreateTransaction;

public record CreateTransactionCommand(
    Guid ListingId,
    Guid DonorId,
    Guid RecipientId,
    TransactionType Type,
    string? Notes) : IRequest<Guid>;