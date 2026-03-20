namespace ResX.Disputes.Application.DTOs;

public record DisputeDto(
    Guid Id,
    Guid TransactionId,
    Guid InitiatorId,
    Guid RespondentId,
    string Reason,
    string Status,
    string? Resolution,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    IReadOnlyList<EvidenceDto> Evidences);

public record EvidenceDto(
    Guid Id,
    Guid SubmittedBy,
    string Description,
    IReadOnlyList<string> FileUrls,
    DateTime SubmittedAt);
