using ResX.Transactions.Domain.Enums;

namespace ResX.Transactions.Application.DTOs;

public record CreateTransactionRequest(
    Guid ListingId,
    Guid DonorId,
    TransactionType Type,
    string? Notes);