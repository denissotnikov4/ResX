using ResX.Transactions.Domain.Enums;

namespace ResX.Transactions.Application.DTOs;

public record TransactionDto(
    Guid Id,
    Guid ListingId,
    Guid DonorId,
    Guid RecipientId,
    string Type,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt);
