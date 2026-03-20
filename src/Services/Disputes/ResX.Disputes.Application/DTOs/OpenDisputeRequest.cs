namespace ResX.Disputes.Application.DTOs;

public record OpenDisputeRequest(
    Guid TransactionId,
    Guid RespondentId,
    string Reason);